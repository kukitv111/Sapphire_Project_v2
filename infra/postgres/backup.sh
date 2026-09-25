#!/bin/sh
set -eu

: "${PGHOST:?}"
: "${PGDATABASE:?}"
: "${PGUSER:?}"
: "${PGPASSWORD:?}"
mkdir -p /backups

while :; do
    stamp=$(date -u +%Y%m%dT%H%M%SZ)
    target="/backups/${PGDATABASE}-${stamp}.dump"
    if pg_dump --format=custom --no-owner --no-acl --file="${target}.partial" "$PGDATABASE"; then
        mv "${target}.partial" "$target"
        find /backups -type f -name "${PGDATABASE}-*.dump" -mtime +7 -delete
        echo "backup_complete database=${PGDATABASE} file=${target}"
    else
        rm -f "${target}.partial"
        echo "backup_failed database=${PGDATABASE}" >&2
    fi
    now=$(date -u +%s)
    sleep "$((3600 - now % 3600))"
done
