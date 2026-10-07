#!/bin/bash
# psql-leader.sh — finds the Patroni leader and runs SQL

# Secrets never touch disk in plaintext: read them straight out of patroni1's own
# /run/secrets/ mount (Docker secrets are exposed there to any service that references them —
# patroni1 already references PATRONI_SUPERUSER_PASSWORD, and JUBE_APP_PASSWORD /
# JUBE_REPORTING_PASSWORD / JUBE_MIGRATION_PASSWORD are mounted there too, for exactly this).
read_secret() {
    docker exec "$(docker ps -q -f name=patroni1)" cat "/run/secrets/$1"
}

PATRONI_SUPERUSER_PASSWORD="$(read_secret PATRONI_SUPERUSER_PASSWORD)"
JUBE_APP_PASSWORD="$(read_secret JUBE_APP_PASSWORD)"
JUBE_REPORTING_PASSWORD="$(read_secret JUBE_REPORTING_PASSWORD)"
JUBE_MIGRATION_PASSWORD="$(read_secret JUBE_MIGRATION_PASSWORD)"
JUBE_PURGE_PASSWORD="$(read_secret JUBE_PURGE_PASSWORD)"

# Find the leader node name from patronictl

PATRONI_LIST=$(docker exec "$(docker ps -q -f name=patroni1)" \
    patronictl -c /etc/patroni.yml list 2>&1)

LEADER=$(printf '%s\n' "$PATRONI_LIST" | awk '/Leader/ {print $2}')

if [ -z "$LEADER" ]; then
    echo "ERROR: Could not determine Patroni leader — aborting. patronictl said:" >&2
    printf '%s\n' "$PATRONI_LIST" >&2
    exit 1
fi

echo "Leader is: $LEADER"
echo "Running SQL..."

docker exec -i \
    -e PGPASSWORD="$PATRONI_SUPERUSER_PASSWORD" \
    $(docker ps -q -f name=$LEADER) \
    psql -h haproxy -p 5432 -U postgres \
    -v app_user="$APP_USER" \
    -v app_db="$APP_DB" \
    -v app_password="$JUBE_APP_PASSWORD" \
    -v reporting_password="$JUBE_REPORTING_PASSWORD" \
    -v migration_password="$JUBE_MIGRATION_PASSWORD" \
    -v purge_password="$JUBE_PURGE_PASSWORD" \
    << EOF

CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS vector;
CREATE USER jube_app WITH PASSWORD '$JUBE_APP_PASSWORD';
CREATE USER jube_reporting WITH PASSWORD '$JUBE_REPORTING_PASSWORD';
CREATE USER jube_migration WITH PASSWORD '$JUBE_MIGRATION_PASSWORD';
CREATE USER jube_purge WITH PASSWORD '$JUBE_PURGE_PASSWORD';
ALTER USER jube_app SET search_path = public;
ALTER USER jube_reporting SET search_path = public;
ALTER USER jube_migration SET search_path = public;
ALTER USER jube_purge SET search_path = public;
GRANT USAGE, CREATE ON SCHEMA public TO jube_migration;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO jube_migration;
GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO jube_migration;
GRANT CREATE ON DATABASE :app_db TO jube_migration;
ALTER USER jube_migration NOCREATEDB NOCREATEROLE NOSUPERUSER;
GRANT USAGE ON SCHEMA public TO jube_app;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO jube_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO jube_app;
ALTER DEFAULT PRIVILEGES FOR ROLE jube_migration IN SCHEMA public GRANT SELECT, INSERT, UPDATE ON TABLES TO jube_app;
ALTER DEFAULT PRIVILEGES FOR ROLE jube_migration IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO jube_app;
GRANT USAGE ON SCHEMA public TO jube_reporting;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO jube_reporting;
ALTER DEFAULT PRIVILEGES FOR ROLE jube_migration IN SCHEMA public GRANT SELECT ON TABLES TO jube_reporting;

-- jube_purge gets SELECT/DELETE on exactly the tables InfrastructureHealthMetricsPurgeStarter chunk-deletes
-- from (Jube.Engine/BackgroundTasks/TaskStarters/InfrastructureHealthMetricsPurgeStarter.cs) and nothing else -
-- unlike jube_app/jube_reporting there is deliberately no ALTER DEFAULT PRIVILEGES here, so a table added to
-- that purge list later needs an explicit GRANT added to this script, rather than silently inheriting delete
-- rights it was never reviewed for.
GRANT USAGE ON SCHEMA public TO jube_purge;
GRANT SELECT, DELETE ON "ApplicationLogEntry" TO jube_purge;
GRANT SELECT, DELETE ON "ContainerLogEntry" TO jube_purge;
GRANT SELECT, DELETE ON "DockerContainerMetric" TO jube_purge;
GRANT SELECT, DELETE ON "DockerEvent" TO jube_purge;
GRANT SELECT, DELETE ON "DockerHostMetric" TO jube_purge;
GRANT SELECT, DELETE ON "DotNetRuntimeMetric" TO jube_purge;
GRANT SELECT, DELETE ON "EntityAnalysisModelResponseTimePipelineCounter" TO jube_purge;
GRANT SELECT, DELETE ON "EntityAnalysisModelStagePerformanceCounter" TO jube_purge;
GRANT SELECT, DELETE ON "EntityAnalysisModelTaskPerformanceCounter" TO jube_purge;
GRANT SELECT, DELETE ON "EtcdClusterEvent" TO jube_purge;
GRANT SELECT, DELETE ON "EtcdMemberStatus" TO jube_purge;
GRANT SELECT, DELETE ON "OpenTelemetryMetric" TO jube_purge;
GRANT SELECT, DELETE ON "OtlpDispatchCounter" TO jube_purge;
GRANT SELECT, DELETE ON "PatroniClusterEvent" TO jube_purge;
GRANT SELECT, DELETE ON "PatroniMemberStatus" TO jube_purge;
GRANT SELECT, DELETE ON "PostgresMetric" TO jube_purge;
GRANT SELECT, DELETE ON "PostgresReplicationStatus" TO jube_purge;
GRANT SELECT, DELETE ON "PostgresLogEntry" TO jube_purge;
GRANT SELECT, DELETE ON "RedisConnectionEvent" TO jube_purge;
GRANT SELECT, DELETE ON "RedisConnectionMultiplexerMetric" TO jube_purge;
GRANT SELECT, DELETE ON "RedisCallCounter" TO jube_purge;
GRANT SELECT, DELETE ON "RedisMetric" TO jube_purge;
GRANT SELECT, DELETE ON "RedisSentinelEvent" TO jube_purge;
GRANT SELECT, DELETE ON "ModelInvokeWarning" TO jube_purge;
GRANT SELECT, DELETE ON "ArchiverStagePerformanceCounter" TO jube_purge;
GRANT SELECT, DELETE ON "CaseCreationStagePerformanceCounter" TO jube_purge;
GRANT SELECT, DELETE ON "CaseCreationWarning" TO jube_purge;
GRANT SELECT, DELETE ON "ArchiverWarning" TO jube_purge;
GRANT SELECT, DELETE ON "CaptureQueueHealth" TO jube_purge;
GRANT SELECT, DELETE ON "RedisSentinelStatus" TO jube_purge;
GRANT SELECT, DELETE ON "RedisSlowOperation" TO jube_purge;
GRANT SELECT, DELETE ON "HttpProcessingCounter" TO jube_purge;
GRANT SELECT, DELETE ON "EntityAnalysisAsynchronousQueueBalance" TO jube_purge;
GRANT SELECT, DELETE ON "EntityAnalysisModelAsynchronousQueueBalance" TO jube_purge;
GRANT SELECT, DELETE ON "EntityAnalysisModelProcessingCounter" TO jube_purge;
GRANT SELECT, DELETE ON "HAProxyServerStatus" TO jube_purge;
GRANT SELECT, DELETE ON "HAProxyReachabilityProbe" TO jube_purge;
GRANT SELECT, DELETE ON "OverlayNetworkTaskDrift" TO jube_purge;
EOF

echo "Done."