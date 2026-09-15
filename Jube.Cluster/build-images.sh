#!/bin/bash
# Builds the four jube-cluster images (Patroni, App, Monitoring, OpenTelemetry Listener) from
# the current git HEAD and tags each with its short commit SHA, e.g. jube.patroni:801ac72.
#
# jube-otel-listener is included even though it's only a throwaway local OTLP receiver for
# manual testing, because docker-compose.yml deploys it unconditionally (replicas: 1, not
# gated behind EnableOpenTelemetry) - without its image, that service has nothing to run and
# sits failing in `docker service ls`.
#
# Run this from anywhere - it locates the repo root relative to its own location,
# since Jube.App/Dockerfile, Jube.Monitoring/Dockerfile and Jube.OpenTelemetryListener/Dockerfile
# COPY project files by repo-root-relative paths and must be built with the repo root as
# context, not Jube.Cluster/. (Jube.Cluster/patroni/Dockerfile is self-contained, but is built
# the same way here for consistency.)
#
# Usage:
#   ./build-images.sh              # tag with HEAD's short SHA
#   ./build-images.sh abc1234      # tag with an explicit SHA/ref instead of HEAD
#   ./build-images.sh --no-env     # build only, skip writing .env
#   ./build-images.sh --save       # also docker save each image to Jube.Cluster/Images/
#
# --save tarballs are too big to push to git - Jube.Cluster/Images/ is gitignored.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

if [ ! -d "$REPO_ROOT/.git" ]; then
    echo "ERROR: $REPO_ROOT does not look like the repo root (no .git) - aborting." >&2
    exit 1
fi

UPDATE_ENV=1
SAVE_IMAGES=0
REF="HEAD"
for arg in "$@"; do
    case "$arg" in
        --no-env) UPDATE_ENV=0 ;;
        --save) SAVE_IMAGES=1 ;;
        *) REF="$arg" ;;
    esac
done

cd "$REPO_ROOT"

if [ -n "$(git status --porcelain)" ]; then
    echo "WARNING: working tree has uncommitted changes - the image will not exactly match the SHA it's tagged with." >&2
fi

SHA="$(git rev-parse --short "$REF")"
echo "Building images from $REF ($SHA)..."

PATRONI_IMAGE="jube.patroni:${SHA}"
JUBE_IMAGE="jube.app:${SHA}"
JUBE_MONITORING_IMAGE="jube.monitoring:${SHA}"
JUBE_OTEL_LISTENER_IMAGE="jube.opentelemetrylistener:${SHA}"

echo
echo "==> Building ${PATRONI_IMAGE} (context: Jube.Cluster/patroni)"
docker build --no-cache \
    -f "$REPO_ROOT/Jube.Cluster/patroni/Dockerfile" \
    -t "$PATRONI_IMAGE" \
    "$REPO_ROOT/Jube.Cluster/patroni"

echo
echo "==> Building ${JUBE_IMAGE} (context: repo root)"
docker build --no-cache \
    -f "$REPO_ROOT/Jube.App/Dockerfile" \
    -t "$JUBE_IMAGE" \
    "$REPO_ROOT"

echo
echo "==> Building ${JUBE_MONITORING_IMAGE} (context: repo root)"
docker build --no-cache \
    -f "$REPO_ROOT/Jube.Monitoring/Dockerfile" \
    -t "$JUBE_MONITORING_IMAGE" \
    "$REPO_ROOT"

echo
echo "==> Building ${JUBE_OTEL_LISTENER_IMAGE} (context: repo root)"
docker build --no-cache \
    -f "$REPO_ROOT/Jube.OpenTelemetryListener/Dockerfile" \
    -t "$JUBE_OTEL_LISTENER_IMAGE" \
    "$REPO_ROOT"

echo
echo "Built:"
echo "  PATRONI_IMAGE=${PATRONI_IMAGE}"
echo "  JUBE_IMAGE=${JUBE_IMAGE}"
echo "  JUBE_MONITORING_IMAGE=${JUBE_MONITORING_IMAGE}"
echo "  JUBE_OTEL_LISTENER_IMAGE=${JUBE_OTEL_LISTENER_IMAGE}"

if [ "$SAVE_IMAGES" -eq 1 ]; then
    IMAGES_DIR="$SCRIPT_DIR/Images"
    mkdir -p "$IMAGES_DIR"
    echo
    for IMAGE in "$PATRONI_IMAGE" "$JUBE_IMAGE" "$JUBE_MONITORING_IMAGE" "$JUBE_OTEL_LISTENER_IMAGE"; do
        TAR_FILE="$IMAGES_DIR/${IMAGE}.tar"
        echo "==> Saving ${IMAGE} -> ${TAR_FILE}"
        docker save -o "$TAR_FILE" "$IMAGE"
    done
    echo
    echo "Saved images to $IMAGES_DIR"
fi

if [ "$UPDATE_ENV" -eq 1 ]; then
    ENV_FILE="$SCRIPT_DIR/.env"
    if [ -f "$ENV_FILE" ]; then
        for VAR in PATRONI_IMAGE JUBE_IMAGE JUBE_MONITORING_IMAGE JUBE_OTEL_LISTENER_IMAGE; do
            VALUE="${!VAR}"
            if grep -q "^${VAR}=" "$ENV_FILE"; then
                sed -i "s#^${VAR}=.*#${VAR}=${VALUE}#" "$ENV_FILE"
            else
                echo "${VAR}=${VALUE}" >> "$ENV_FILE"
            fi
        done
        echo
        echo "Updated image tags in $ENV_FILE"
    else
        echo
        echo "NOTE: $ENV_FILE not found - not writing it (it also needs CRITICAL_HOST_* zone" \
             "assignments this script has no way to know). Set these manually or via the" \
             "Deployment Runbook's .env step:"
        echo "  PATRONI_IMAGE=${PATRONI_IMAGE}"
        echo "  JUBE_IMAGE=${JUBE_IMAGE}"
        echo "  JUBE_MONITORING_IMAGE=${JUBE_MONITORING_IMAGE}"
        echo "  JUBE_OTEL_LISTENER_IMAGE=${JUBE_OTEL_LISTENER_IMAGE}"
    fi
fi

echo
echo "Next: from Jube.Cluster/, run ./deploy.sh (or 'docker stack deploy -c docker-compose.yml jube-cluster')."
