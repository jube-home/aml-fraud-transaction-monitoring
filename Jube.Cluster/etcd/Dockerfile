FROM quay.io/coreos/etcd:v3.5.34 AS etcd-binaries

FROM alpine:3.21

RUN apk add --no-cache bash

COPY --from=etcd-binaries /usr/local/bin/etcd /usr/local/bin/etcd
COPY --from=etcd-binaries /usr/local/bin/etcdctl /usr/local/bin/etcdctl

COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENV ETCDCTL_API=3

ENTRYPOINT ["/entrypoint.sh"]
