# Резервное копирование StudioB2B

Скрипт `studiob2b-backup.sh` запускается на сервере приложения (`192.168.1.130`). Он не публикует MariaDB в сеть: создаёт согласованные логические дампы внутри контейнера `deploy-db-1`, сжимает их и передаёт по SSH на RAID1-хранилище `192.168.1.111`.

В копию входят `StudioB2B_Master` и все базы, имена которых начинаются с `StudioB2B_Tenant_`. Каждый архив содержит одну базу в формате `sql.gz` и имеет имя вида `StudioB2B_Tenant_interparts_2026-08-12T03-30-00Z.sql.gz`.

На сервере резервных копий файлы лежат в `/mnt/raid1/backups/studiob2b`. Скрипт удаляет только собственные архивы старше 30 дней; существующие копии сервиса `dbbackup` не затрагиваются. Расписание systemd timer — ежедневно около 03:30, с разбросом до 10 минут после старта таймера.

Проверка:

```bash
sudo systemctl status studiob2b-backup.timer
sudo systemctl start studiob2b-backup.service
sudo journalctl -u studiob2b-backup.service -n 100 --no-pager
```
