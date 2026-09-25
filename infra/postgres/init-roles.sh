#!/bin/sh
set -eu

: "${POSTGRES_DB:?}"
: "${POSTGRES_USER:?}"
: "${SAPPHIRE_RUNTIME_ROLE:?}"
: "${SAPPHIRE_RUNTIME_PASSWORD:?}"
: "${SAPPHIRE_MIGRATION_ROLE:?}"
: "${SAPPHIRE_MIGRATION_PASSWORD:?}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
    -v db="$POSTGRES_DB" \
    -v runtime_role="$SAPPHIRE_RUNTIME_ROLE" \
    -v runtime_password="$SAPPHIRE_RUNTIME_PASSWORD" \
    -v migration_role="$SAPPHIRE_MIGRATION_ROLE" \
    -v migration_password="$SAPPHIRE_MIGRATION_PASSWORD" <<'SQL'
CREATE ROLE :"migration_role" LOGIN PASSWORD :'migration_password';
CREATE ROLE :"runtime_role" LOGIN PASSWORD :'runtime_password';
ALTER DATABASE :"db" OWNER TO :"migration_role";
ALTER SCHEMA public OWNER TO :"migration_role";
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT CONNECT ON DATABASE :"db" TO :"runtime_role";
GRANT USAGE ON SCHEMA public TO :"runtime_role";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO :"runtime_role";
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO :"runtime_role";
ALTER DEFAULT PRIVILEGES FOR ROLE :"migration_role" IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO :"runtime_role";
ALTER DEFAULT PRIVILEGES FOR ROLE :"migration_role" IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO :"runtime_role";
SQL
