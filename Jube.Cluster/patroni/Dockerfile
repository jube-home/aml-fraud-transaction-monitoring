FROM alpine:3.21 AS builder

RUN apk add --no-cache \
    postgresql17-dev \
    python3 \
    python3-dev \
    py3-virtualenv \
    gcc \
    musl-dev \
    linux-headers \
    libffi-dev \
    openssl-dev \
    pkgconfig \
    make \
    libc-dev

RUN python3 -m venv /opt/patroni \
 && /opt/patroni/bin/pip install --no-cache-dir --upgrade pip setuptools wheel \
 && /opt/patroni/bin/pip install --no-cache-dir "patroni[etcd3]==3.3.2" psycopg2==2.9.9


FROM alpine:3.21

RUN apk add --no-cache \
    postgresql17 \
    postgresql17-client \
    postgresql17-contrib \
    postgresql-pgvector \
    python3 \
    curl \
    jq \
    bash \
    su-exec \
    pgbackrest

COPY --from=builder /opt/patroni /opt/patroni

RUN mkdir -p /var/lib/postgresql/data \
             /var/lib/postgresql/tmp \
             /var/lib/postgresql/tmp/pgbackrest-lock \
             /var/run/postgresql \
             /etc/patroni \
             /home/postgres/.config \
             /etc/pgbackrest \
             /var/log/pgbackrest \
             /var/spool/pgbackrest \
 && chown -R postgres:postgres \
        /var/lib/postgresql \
        /var/run/postgresql \
        /etc/patroni \
        /home/postgres \
        /etc/pgbackrest \
        /var/log/pgbackrest \
        /var/spool/pgbackrest \
 && chmod 750 /var/lib/postgresql/data \
 && chmod 700 /var/lib/postgresql/tmp \
 && chmod 755 /var/run/postgresql /etc/patroni /home/postgres/.config

ENV TMPDIR=/var/lib/postgresql/tmp

RUN mkdir -p /docker-entrypoint-initdb.d && chown postgres:postgres /docker-entrypoint-initdb.d

# Runs once, on the first node only, immediately after Patroni's initdb (bootstrap.post_bootstrap). Mirrors the
# official postgres image: POSTGRES_USER / POSTGRES_PASSWORD / POSTGRES_DB (each also as *_FILE or a Docker
# secret of the same name) and /docker-entrypoint-initdb.d/*.{sh,sql}. The Patroni superuser stays "postgres"
# with PATRONI_SUPERUSER_PASSWORD, so POSTGRES_USER other than "postgres" adds an extra superuser rather than
# renaming it, and POSTGRES_PASSWORD is not applied to "postgres" itself.
RUN cat > /post_bootstrap.sh << 'EOF'
#!/bin/bash
set -e

file_env() {
    local var="$1" file_var="${1}_FILE" default="${2:-}"
    if [ -n "${!var:-}" ]; then
        return
    elif [ -n "${!file_var:-}" ]; then
        export "$var"="$(cat "${!file_var}")"
    elif [ -f "/run/secrets/$var" ]; then
        export "$var"="$(cat "/run/secrets/$var")"
    else
        export "$var"="$default"
    fi
}

file_env POSTGRES_USER postgres
file_env POSTGRES_PASSWORD
file_env POSTGRES_DB "$POSTGRES_USER"
file_env PATRONI_ADMIN_PASSWORD

psql_admin() { psql -v ON_ERROR_STOP=1 -U postgres -d postgres "$@"; }

if [ "$POSTGRES_USER" != "postgres" ]; then
    if [ -z "$POSTGRES_PASSWORD" ]; then
        echo "POSTGRES_PASSWORD is required when POSTGRES_USER is not postgres" >&2
        exit 1
    fi
    psql_admin -v u="$POSTGRES_USER" -v p="$POSTGRES_PASSWORD" <<'SQL'
SELECT format('CREATE ROLE %I LOGIN SUPERUSER', :'u')
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'u') \gexec
SELECT format('ALTER ROLE %I WITH PASSWORD %L', :'u', :'p') \gexec
SQL
fi

if [ -n "$PATRONI_ADMIN_PASSWORD" ]; then
    psql_admin -v p="$PATRONI_ADMIN_PASSWORD" <<'SQL'
SELECT 'CREATE ROLE admin LOGIN CREATEROLE CREATEDB'
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'admin') \gexec
SELECT format('ALTER ROLE admin WITH PASSWORD %L', :'p') \gexec
SQL
fi

if [ "$POSTGRES_DB" != "postgres" ]; then
    psql_admin -v d="$POSTGRES_DB" -v u="$POSTGRES_USER" <<'SQL'
SELECT format('CREATE DATABASE %I OWNER %I', :'d', :'u')
 WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = :'d') \gexec
SQL
fi

for f in /docker-entrypoint-initdb.d/*; do
    case "$f" in
        *.sh)  echo "post_bootstrap: running $f"; bash "$f" ;;
        *.sql) echo "post_bootstrap: running $f"; psql -v ON_ERROR_STOP=1 -U postgres -d "$POSTGRES_DB" -f "$f" ;;
        *.sql.gz) echo "post_bootstrap: running $f"; gunzip -c "$f" | psql -v ON_ERROR_STOP=1 -U postgres -d "$POSTGRES_DB" ;;
    esac
done
EOF
RUN chmod +x /post_bootstrap.sh

RUN cat > /entrypoint.sh << 'EOF'
#!/bin/bash
set -e

load_secret() {
    local var_name=$1
    local file_path="/run/secrets/$var_name"
    if [ -f "$file_path" ]; then
        export "$var_name"=$(cat "$file_path")
    fi
}

if [ ! -f /var/lib/pgbackrest/repo/.initialized ]; then
    echo "Initialising pgBackRest repo directory ownership..."
    mkdir -p /var/lib/pgbackrest/repo
    chown -R postgres:postgres /var/lib/pgbackrest/repo
    chmod 750 /var/lib/pgbackrest/repo
    touch /var/lib/pgbackrest/repo/.initialized
fi

load_secret "PATRONI_SUPERUSER_PASSWORD"
load_secret "PATRONI_REPLICATION_PASSWORD"
load_secret "PATRONI_ADMIN_PASSWORD"

exec su-exec postgres patroni "$@"
EOF

RUN chmod +x /entrypoint.sh

ENV PATH="/opt/patroni/bin:$PATH"

HEALTHCHECK --interval=10s --timeout=5s --retries=3 \
    CMD curl -sf http://localhost:8008/health || exit 1

EXPOSE 8008 5432 8432

ENTRYPOINT ["/entrypoint.sh"]
CMD ["/etc/patroni.yml"]
