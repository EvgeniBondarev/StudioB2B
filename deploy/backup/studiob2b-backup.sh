#!/usr/bin/env bash
# Creates logical backups of the StudioB2B master and tenant MariaDB databases
# and sends them to the RAID1 backup host over SSH.
set -Eeuo pipefail

readonly APP_DIR="${APP_DIR:-/home/volna/studiob2b}"
readonly COMPOSE_FILE="${COMPOSE_FILE:-$APP_DIR/deploy/docker-compose.production.yml}"
readonly ENV_FILE="${ENV_FILE:-$APP_DIR/deploy/.env.production}"
readonly BACKUP_HOST="${BACKUP_HOST:-volna@192.168.1.111}"
readonly BACKUP_DIR="${BACKUP_DIR:-/mnt/raid1/backups/studiob2b}"
readonly SSH_KEY="${SSH_KEY:-/home/volna/.ssh/id_ed25519_studiob2b_backup}"
readonly RETENTION_DAYS="${RETENTION_DAYS:-30}"
readonly LOCK_FILE="${LOCK_FILE:-/tmp/studiob2b-backup.lock}"

exec 9>"$LOCK_FILE"
flock -n 9 || { echo "StudioB2B backup is already running" >&2; exit 0; }

for required in docker gzip ssh scp; do
  command -v "$required" >/dev/null || { echo "Missing command: $required" >&2; exit 1; }
done
[[ -r "$ENV_FILE" ]] || { echo "Missing environment file: $ENV_FILE" >&2; exit 1; }
[[ -r "$SSH_KEY" ]] || { echo "Missing SSH key: $SSH_KEY" >&2; exit 1; }

compose=(docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE")
ssh_options=(-i "$SSH_KEY" -o BatchMode=yes -o StrictHostKeyChecking=yes -o ConnectTimeout=15)
work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

"${compose[@]}" exec -T db sh -c \
  'mariadb -uroot -p"$MARIADB_ROOT_PASSWORD" -NBe "SHOW DATABASES;"' < /dev/null \
  | awk '/^StudioB2B_Master$|^StudioB2B_Tenant_[A-Za-z0-9_]+$/' \
  > "$work_dir/databases"

[[ -s "$work_dir/databases" ]] || { echo "No StudioB2B databases found" >&2; exit 1; }

ssh "${ssh_options[@]}" "$BACKUP_HOST" "mkdir -p '$BACKUP_DIR'"
timestamp="$(date -u +%Y-%m-%dT%H-%M-%SZ)"

while IFS= read -r database || [[ -n "$database" ]]; do
  archive="$work_dir/${database}_${timestamp}.sql.gz"
  echo "Backing up $database"
  "${compose[@]}" exec -T db sh -c \
    'exec mariadb-dump -uroot -p"$MARIADB_ROOT_PASSWORD" --single-transaction --routines --events --triggers --add-drop-database --databases "$1"' \
    < /dev/null \
    sh "$database" | gzip -9 > "$archive"
  gzip -t "$archive"
  scp "${ssh_options[@]}" "$archive" "$BACKUP_HOST:$BACKUP_DIR/"
done < "$work_dir/databases"

ssh "${ssh_options[@]}" "$BACKUP_HOST" \
  "find '$BACKUP_DIR' -maxdepth 1 -type f -name 'StudioB2B_*.sql.gz' -mtime +$RETENTION_DAYS -delete"

echo "StudioB2B backup completed: $(wc -l < "$work_dir/databases") database(s)"
