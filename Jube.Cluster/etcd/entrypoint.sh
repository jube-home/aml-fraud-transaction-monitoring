#!/bin/bash
set -e

PATRONI_DCS_PREFIX="/service/postgres-cluster"
ETCD_ROOT_PASSWORD_FILE="/run/secrets/ETCD_ROOT_PASSWORD"
PATRONI_ETCD3_PASSWORD_FILE="/run/secrets/PATRONI_ETCD3_PASSWORD"

"$@" &
etcd_pid=$!

forward_signal() {
    kill -TERM "$etcd_pid" 2>/dev/null || true
}
trap forward_signal TERM INT

bootstrap_auth() {
    if [ ! -f "$ETCD_ROOT_PASSWORD_FILE" ] || [ ! -f "$PATRONI_ETCD3_PASSWORD_FILE" ]; then
        return 0
    fi

    until etcdctl endpoint health >/dev/null 2>&1; do
        sleep 3
    done

    if etcdctl auth status 2>/dev/null | grep -q "Authentication Status: true"; then
        return 0
    fi

    etcdctl user get root >/dev/null 2>&1 \
        || etcdctl user add root --interactive=false < "$ETCD_ROOT_PASSWORD_FILE" >/dev/null 2>&1
    etcdctl user grant-role root root >/dev/null 2>&1 || true

    etcdctl role get patroni-role >/dev/null 2>&1 \
        || etcdctl role add patroni-role >/dev/null 2>&1
    etcdctl role grant-permission patroni-role readwrite "$PATRONI_DCS_PREFIX" --prefix=true >/dev/null 2>&1 || true

    etcdctl user get patroni >/dev/null 2>&1 \
        || etcdctl user add patroni --interactive=false < "$PATRONI_ETCD3_PASSWORD_FILE" >/dev/null 2>&1
    etcdctl user grant-role patroni patroni-role >/dev/null 2>&1 || true

    etcdctl auth enable >/dev/null 2>&1 || true
}

bootstrap_auth &

wait "$etcd_pid"
