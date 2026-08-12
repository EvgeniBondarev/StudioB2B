import os, json, glob, subprocess, datetime, secrets
from flask import (Flask, request, send_file, redirect, session,
                   render_template_string)
from apscheduler.schedulers.background import BackgroundScheduler
from apscheduler.triggers.cron import CronTrigger

BACKUP_DIR = os.environ.get("BACKUP_DIR", "/backups")
STUDIOB2B_DIR = os.path.join(BACKUP_DIR, "studiob2b")
DATA_DIR   = os.environ.get("DATA_DIR", "/data")
SETTINGS   = os.path.join(DATA_DIR, "settings.json")
DB_HOST = os.environ.get("MYSQL_HOST", "mysql")
DB_PORT = os.environ.get("MYSQL_PORT", "3306")
DB_USER = os.environ.get("MYSQL_USER", "admin")
DB_PASS = os.environ.get("MYSQL_PASSWORD", "")
AUTH_USER = os.environ.get("BACKUP_AUTH_USER", "admin")
AUTH_PASS = os.environ.get("BACKUP_AUTH_PASS", "")
TZ = os.environ.get("TZ", "UTC")

DEFAULTS = {"enabled": True, "schedule": "0 8,20 * * *", "retention": 7}
last_status = {"time": None, "ok": None, "msg": "ещё не запускался"}

os.makedirs(BACKUP_DIR, exist_ok=True)
os.makedirs(DATA_DIR, exist_ok=True)

def load():
    try: s = json.load(open(SETTINGS))
    except Exception: s = {}
    for k, v in DEFAULTS.items(): s.setdefault(k, v)
    return s

def store(s): json.dump(s, open(SETTINGS, "w"))

def human(n):
    n = float(n)
    for u in ["Б", "КБ", "МБ", "ГБ"]:
        if n < 1024: return (f"{n:.0f} {u}" if u == "Б" else f"{n:.1f} {u}")
        n /= 1024
    return f"{n:.1f} ТБ"

def apply_retention():
    keep = int(load().get("retention", 7))
    files = sorted(glob.glob(os.path.join(BACKUP_DIR, "*.sql.gz")), key=os.path.getmtime, reverse=True)
    for f in files[keep:]:
        try: os.remove(f)
        except Exception: pass

def do_backup():
    ts = datetime.datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    name = f"all-databases_{ts}.sql.gz"
    path = os.path.join(BACKUP_DIR, name); tmp = path + ".tmp"
    env = dict(os.environ, MYSQL_PWD=DB_PASS)
    cmd = ["mariadb-dump", "-h", DB_HOST, "-P", str(DB_PORT), "-u", DB_USER,
           "--single-transaction", "--routines", "--events", "--all-databases"]
    try:
        with open(tmp, "wb") as out:
            p1 = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=env)
            p2 = subprocess.Popen(["gzip"], stdin=p1.stdout, stdout=out)
            p1.stdout.close()
            err = p1.stderr.read().decode(errors="replace")
            p2.communicate(); rc = p1.wait()
        if rc != 0: raise RuntimeError(err.strip() or f"mariadb-dump rc={rc}")
        os.rename(tmp, path); apply_retention()
        last_status.update(time=ts, ok=True, msg=f"OK: {name} ({human(os.path.getsize(path))})")
    except Exception as e:
        if os.path.exists(tmp):
            try: os.remove(tmp)
            except Exception: pass
        last_status.update(time=ts, ok=False, msg=f"Ошибка: {e}")
    return last_status

sched = BackgroundScheduler(timezone=TZ)
def reschedule():
    try: sched.remove_job("backup")
    except Exception: pass
    s = load()
    if s.get("enabled", True):
        sched.add_job(do_backup, CronTrigger.from_crontab(s["schedule"], timezone=TZ), id="backup")
sched.start(); reschedule()

app = Flask(__name__)
_skf = os.path.join(DATA_DIR, "secret.key")
if os.path.exists(_skf):
    app.secret_key = open(_skf, "rb").read()
else:
    app.secret_key = secrets.token_bytes(32)
    try: open(_skf, "wb").write(app.secret_key)
    except Exception: pass

@app.before_request
def guard():
    if not AUTH_PASS: return
    if request.endpoint in ("login", "static"): return
    if not session.get("auth"): return redirect("/login")

LOGIN = """
<!doctype html><html lang=ru><head><meta charset=utf-8>
<meta name=viewport content="width=device-width, initial-scale=1"><title>Вход — Бэкапы БД</title>
<style>
body{margin:0;background:#0d1117;color:#c9d1d9;font-family:-apple-system,Segoe UI,Roboto,sans-serif;
display:flex;align-items:center;justify-content:center;min-height:100vh}
.box{background:#161b22;border:1px solid #30363d;border-radius:12px;padding:28px 30px;width:320px}
h1{font-size:18px;color:#fff;margin:0 0 4px}.sub{color:#8b949e;font-size:13px;margin-bottom:18px}
label{display:block;color:#8b949e;font-size:13px;margin:10px 0 4px}
input{width:100%;box-sizing:border-box;background:#0d1117;border:1px solid #30363d;color:#c9d1d9;border-radius:6px;padding:10px}
button{width:100%;margin-top:18px;background:#238636;color:#fff;border:none;border-radius:6px;padding:11px;font-size:15px;cursor:pointer}
.err{background:#3a1416;color:#f85149;border-radius:6px;padding:9px 12px;margin-top:14px;font-size:13px}
</style></head><body>
<form class=box method=post>
 <h1>💾 Бэкапы базы данных</h1><div class=sub>Сервер volna · вход</div>
 <label>Логин</label><input name=username autofocus autocomplete=username>
 <label>Пароль</label><input name=password type=password autocomplete=current-password>
 <button type=submit>Войти</button>
 {% if err %}<div class=err>{{err}}</div>{% endif %}
</form></body></html>
"""

@app.route("/login", methods=["GET", "POST"])
def login():
    err = None
    if request.method == "POST":
        if request.form.get("username") == AUTH_USER and request.form.get("password") == AUTH_PASS:
            session["auth"] = True
            return redirect("/")
        err = "Неверный логин или пароль"
    return render_template_string(LOGIN, err=err)

@app.route("/logout")
def logout():
    session.clear(); return redirect("/login")

PAGE = """
<!doctype html><html lang=ru><head><meta charset=utf-8>
<meta name=viewport content="width=device-width, initial-scale=1">
<title>Бэкапы БД — volna</title>
<style>
*{box-sizing:border-box}
body{margin:0;background:#0d1117;color:#c9d1d9;font-family:-apple-system,Segoe UI,Roboto,sans-serif}
.wrap{max-width:900px;margin:0 auto;padding:32px}
.top{display:flex;justify-content:space-between;align-items:center}
h1{color:#fff;font-size:22px} h2{color:#fff;font-size:16px;margin-top:28px}
.card{background:#161b22;border:1px solid #30363d;border-radius:10px;padding:18px 20px;margin:14px 0}
label{display:block;margin:10px 0 4px;color:#8b949e;font-size:13px}
input[type=text],input[type=number]{background:#0d1117;border:1px solid #30363d;color:#c9d1d9;border-radius:6px;padding:8px 10px;width:100%}
.row{display:flex;gap:16px;flex-wrap:wrap} .row>div{flex:1;min-width:180px}
button,.btn{background:#238636;color:#fff;border:none;border-radius:6px;padding:9px 16px;cursor:pointer;font-size:14px;text-decoration:none;display:inline-block}
a.dl{color:#58a6ff} a.del{color:#f85149} a.logout{color:#8b949e;font-size:13px;text-decoration:none}
table{width:100%;border-collapse:collapse;margin-top:8px}
th,td{text-align:left;padding:8px 10px;border-bottom:1px solid #21262d;font-size:14px}
th{color:#8b949e}.ok{color:#3fb950}.err{color:#f85149}
.msg{padding:10px 14px;border-radius:6px;margin:10px 0}.msg.ok{background:#11331b}.msg.err{background:#3a1416}
small{color:#8b949e}code{background:#0d1117;border:1px solid #30363d;border-radius:4px;padding:1px 5px}
</style></head><body><div class=wrap>
<div class=top><h1>💾 Бэкапы базы данных</h1><a class=logout href=logout>выйти</a></div>
<small>Сервер <b>volna</b> · цель: <b>все базы MariaDB</b> · хранилище: <code>/mnt/raid1/backups</code> (RAID1) · TZ: {{tz}}</small>
{% if msg %}<div class="msg {{'err' if 'Ошибк' in msg or 'Неверн' in msg else 'ok'}}">{{msg}}</div>{% endif %}
<div class=card>
  <b>Последний запуск:</b>
  {% if status.ok is none %}<span>{{status.msg}}</span>
  {% elif status.ok %}<span class=ok>✔ {{status.msg}}</span> <small>({{status.time}})</small>
  {% else %}<span class=err>✖ {{status.msg}}</span> <small>({{status.time}})</small>{% endif %}
  <form method=post action=run style=margin-top:12px><button type=submit>Создать бэкап сейчас</button></form>
</div>
<h2>Настройки</h2>
<div class=card>
  <form method=post action=settings>
    <label><input type=checkbox name=enabled {{'checked' if s.enabled}}> Автоматические бэкапы по расписанию</label>
    <div class=row>
      <div><label>Расписание (cron, 5 полей)</label>
        <input type=text name=schedule value="{{s.schedule}}">
        <small>Рекомендуем: <code>0 8,20 * * *</code> — утром и вечером. Примеры: <code>0 3 * * *</code> — ежедневно в 03:00; <code>0 */6 * * *</code> — каждые 6 ч; <code>0 4 * * 0</code> — по воскресеньям 04:00</small></div>
      <div><label>Хранить копий</label><input type=number name=retention min=1 max=365 value="{{s.retention}}"></div>
    </div>
    <div style=margin-top:14px><button type=submit>Сохранить</button></div>
  </form>
  {% if next_run %}<small>Следующий запуск: {{next_run}}</small>{% endif %}
</div>
<h2>Резервные копии ({{files|length}})</h2>
<div class=card>
  {% if files %}<table><tr><th>Файл</th><th>Размер</th><th>Дата</th><th></th></tr>
  {% for f in files %}<tr><td>{{f.name}}</td><td>{{f.size}}</td><td>{{f.date}}</td>
    <td><a class=dl href="download/{{f.name}}">скачать</a> &nbsp;
        <a class=del href="#" onclick="if(confirm('Удалить {{f.name}}?')){document.getElementById('d{{loop.index}}').submit()}">удалить</a>
        <form id=d{{loop.index}} method=post action="delete/{{f.name}}" style=display:none></form></td></tr>
  {% endfor %}</table>{% else %}<small>Пока нет ни одной копии.</small>{% endif %}
</div>
<h2>StudioB2B: master и tenants ({{studiob2b_files|length}})</h2>
<div class=card>
  <small>Копии создаются на сервере приложения и передаются сюда по SSH. Хранилище: <code>/mnt/raid1/backups/studiob2b</code>.</small>
  {% if studiob2b_files %}<table><tr><th>Файл</th><th>Размер</th><th>Дата</th><th></th></tr>
  {% for f in studiob2b_files %}<tr><td>{{f.name}}</td><td>{{f.size}}</td><td>{{f.date}}</td>
    <td><a class=dl href="download/studiob2b/{{f.name}}">скачать</a> &nbsp;
        <a class=del href="#" onclick="if(confirm('Удалить {{f.name}}?')){document.getElementById('s{{loop.index}}').submit()}">удалить</a>
        <form id=s{{loop.index}} method=post action="delete/studiob2b/{{f.name}}" style=display:none></form></td></tr>
  {% endfor %}</table>{% else %}<small>Пока нет ни одной копии StudioB2B.</small>{% endif %}
</div></div></body></html>
"""

def list_files(directory=BACKUP_DIR):
    out = []
    for p in sorted(glob.glob(os.path.join(directory, "*.sql.gz")), key=os.path.getmtime, reverse=True):
        st = os.stat(p)
        out.append({"name": os.path.basename(p), "size": human(st.st_size),
                    "date": datetime.datetime.fromtimestamp(st.st_mtime).strftime("%Y-%m-%d %H:%M")})
    return out

@app.route("/")
def index():
    s = load(); job = sched.get_job("backup")
    nxt = job.next_run_time.strftime("%Y-%m-%d %H:%M %Z") if job and job.next_run_time else None
    return render_template_string(PAGE, s=s, status=last_status, files=list_files(),
                                  studiob2b_files=list_files(STUDIOB2B_DIR),
                                  msg=request.args.get("msg"), next_run=nxt, tz=TZ)

@app.route("/run", methods=["POST"])
def run():
    st = do_backup()
    return redirect("/?msg=" + ("Бэкап создан. " + st["msg"] if st["ok"] else st["msg"]))

@app.route("/settings", methods=["POST"])
def settings():
    sc = request.form.get("schedule", "").strip()
    try: CronTrigger.from_crontab(sc, timezone=TZ)
    except Exception: return redirect("/?msg=Неверное cron-выражение")
    s = load(); s["enabled"] = bool(request.form.get("enabled")); s["schedule"] = sc
    try: s["retention"] = max(1, int(request.form.get("retention", 7)))
    except Exception: s["retention"] = 7
    store(s); reschedule()
    return redirect("/?msg=Настройки сохранены")

@app.route("/download/<path:name>")
def download(name):
    safe = os.path.basename(name); path = os.path.join(BACKUP_DIR, safe)
    if not safe.endswith(".sql.gz") or not os.path.isfile(path): return "Не найдено", 404
    return send_file(path, as_attachment=True)

@app.route("/download/studiob2b/<path:name>")
def download_studiob2b(name):
    safe = os.path.basename(name); path = os.path.join(STUDIOB2B_DIR, safe)
    if not safe.endswith(".sql.gz") or not os.path.isfile(path): return "Не найдено", 404
    return send_file(path, as_attachment=True)

@app.route("/delete/<path:name>", methods=["POST"])
def delete(name):
    safe = os.path.basename(name); path = os.path.join(BACKUP_DIR, safe)
    if safe.endswith(".sql.gz") and os.path.isfile(path):
        os.remove(path); return redirect("/?msg=Удалено: " + safe)
    return redirect("/?msg=Файл не найден")

@app.route("/delete/studiob2b/<path:name>", methods=["POST"])
def delete_studiob2b(name):
    safe = os.path.basename(name); path = os.path.join(STUDIOB2B_DIR, safe)
    if safe.endswith(".sql.gz") and os.path.isfile(path):
        os.remove(path); return redirect("/?msg=Удалено: " + safe)
    return redirect("/?msg=Файл не найден")

if __name__ == "__main__":
    from waitress import serve
    serve(app, host="0.0.0.0", port=8080, threads=8)
