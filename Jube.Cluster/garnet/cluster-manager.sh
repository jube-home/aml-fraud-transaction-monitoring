#!/bin/sh
PASS=$(cat "${RESP_PASSWORD_FILE:-/run/secrets/RESP_PASSWORD}")
NODES="${GARNET_NODES:-garnet1 garnet2 garnet3 garnet4}"
PORT="${GARNET_PORT:-6379}"
INTERVAL="${INTERVAL:-5}"
CONFIRM="${CONFIRM:-3}"
TOTAL=$(echo $NODES | wc -w)
STRIKES=0

cli() {
  h=$1
  shift
  timeout 4 redis-cli -h "$h" -p "$PORT" -a "$PASS" --no-auth-warning "$@" 2>/dev/null
}

ip_of() { getent hosts "$1" | awk '{print $1; exit}'; }

alive() { [ "$(cli "$1" PING)" = "PONG" ]; }

myself_field() { cli "$1" CLUSTER NODES | awk -v f="$2" '$3 ~ /myself/ {print $f; exit}'; }

while true; do
  LIVE=""
  for n in $NODES; do alive "$n" && LIVE="$LIVE $n"; done
  set -- $LIVE
  if [ "$#" -eq 0 ]; then sleep "$INTERVAL"; continue; fi

  # Primary = the live node that sees itself as master holding slots, highest config epoch winning.
  PRIMARY=""
  BEST=-1
  for n in $LIVE; do
    line=$(cli "$n" CLUSTER NODES | awk '$3 ~ /myself/ && $3 ~ /master/ && NF >= 9 {print $1, $7; exit}')
    [ -z "$line" ] && continue
    epoch=${line#* }
    if [ "$epoch" -gt "$BEST" ]; then BEST=$epoch; PRIMARY=$n; PRIMARY_ID=${line% *}; fi
  done

  # No live primary but gossip still remembers one holding slots: it has failed. Garnet's gossip detects the
  # failure but never promotes a replica itself (no election), so promote the most caught-up live replica once
  # the outage has persisted for CONFIRM consecutive checks.
  if [ -z "$PRIMARY" ]; then
    DEAD_ID=""
    for n in $LIVE; do
      DEAD_ID=$(cli "$n" CLUSTER NODES | awk '$3 !~ /myself/ && $3 ~ /master/ && NF >= 9 {print $1; exit}')
      [ -n "$DEAD_ID" ] && break
    done
    if [ -n "$DEAD_ID" ]; then
      STRIKES=$((STRIKES + 1))
      if [ "$STRIKES" -ge "$CONFIRM" ]; then
        BEST=-1
        CANDIDATE=""
        for n in $LIVE; do
          [ "$(myself_field "$n" 4)" = "$DEAD_ID" ] || continue
          off=$(cli "$n" INFO replication | tr -d '\r' | awk -F: '$1 == "master_repl_offset" {print $2}')
          off=${off:-0}
          if [ "$off" -gt "$BEST" ]; then BEST=$off; CANDIDATE=$n; fi
        done
        if [ -n "$CANDIDATE" ]; then
          echo "failover: primary $DEAD_ID down for $STRIKES checks, promoting $CANDIDATE (offset $BEST)"
          cli "$CANDIDATE" CLUSTER FAILOVER FORCE
          STRIKES=0
        fi
      fi
      sleep "$INTERVAL"
      continue
    fi
  fi
  [ -n "$PRIMARY" ] && STRIKES=0

  # Fresh cluster: no live node holds slots, and every node is up -> garnet1 takes all slots, rest replicate it.
  if [ -z "$PRIMARY" ]; then
    if [ "$#" -eq "$TOTAL" ]; then
      first=${NODES%% *}
      echo "bootstrap: assigning slots to $first"
      cli "$first" CLUSTER ADDSLOTSRANGE 0 16383
      for n in $NODES; do [ "$n" != "$first" ] && cli "$first" CLUSTER MEET "$(ip_of "$n")" "$PORT"; done
      sleep 5
      id=$(cli "$first" CLUSTER MYID)
      for n in $NODES; do [ "$n" != "$first" ] && cli "$n" CLUSTER REPLICATE "$id"; done
    fi
    sleep "$INTERVAL"
    continue
  fi

  # Gossip membership: make sure the primary knows every live node at its current address.
  for n in $LIVE; do
    [ "$n" = "$PRIMARY" ] && continue
    ip=$(ip_of "$n")
    [ -z "$ip" ] && continue
    nid=$(cli "$n" CLUSTER MYID)
    cli "$PRIMARY" CLUSTER NODES | grep -q "^$nid .* $ip:$PORT@" || cli "$PRIMARY" CLUSTER MEET "$ip" "$PORT"
  done

  # Reconcile: any live node that is not a replica of the primary (stale old primary rejoining, wiped replica)
  # is demoted and resynchronised from it.
  for n in $LIVE; do
    [ "$n" = "$PRIMARY" ] && continue
    if [ "$(myself_field "$n" 4)" != "$PRIMARY_ID" ]; then
      echo "reconcile: $n -> replica of $PRIMARY ($PRIMARY_ID)"
      cli "$n" CLUSTER REPLICATE "$PRIMARY_ID"
    fi
  done

  sleep "$INTERVAL"
done
