# Rolling `pg_stat_statements` onto an already-running cluster

This is a checklist, not a tutorial. Full explanation is in the Deployment Runbook under
"Changing a Postgres Parameter on an Already-Bootstrapped Cluster" — read this first if something below doesn't
make sense.

## The one thing to remember

**Editing `patroni/patroni{1,2,3,4}.yml` and redeploying the image does nothing on your existing cluster.**
Those files only get read the first time a cluster is ever bootstrapped. Your cluster already exists, so Patroni
never looks at them again — it reads config from etcd instead. The only way to change a live cluster is
`patronictl edit-config`.

## Order of operations (do not reorder these)

1. **Push the config change into etcd** — `patronictl edit-config` on any patroni container. This does **not**
   restart anything by itself.
2. **Check `patronictl list`** — every member should now show pending restart.
3. **Rolling-restart replicas first, one at a time.** Never restart more than one node at once — you'd have no
   standby left if something went wrong mid-restart.
4. **Restart the leader last, on purpose.** This is the one that actually causes a failover (a replica gets
   promoted, briefly no writes). Pick a quiet moment for this specific step, not the whole rollout.
5. **Only now** let `jube-jobs` run (or re-run) the migration that does
   `CREATE EXTENSION IF NOT EXISTS pg_stat_statements;`.

If you do step 5 before step 4, the migration fails loudly and cleanly — but only because it has an explicit guard
for exactly this (its first statement checks `current_setting('shared_preload_libraries')` and raises an error if
`pg_stat_statements` isn't in it). Without that guard, `CREATE EXTENSION IF NOT EXISTS pg_stat_statements;` would
actually **succeed** even without the restart — it only registers catalog objects, and the missing shared-memory
hook only bites later, the first time something queries the view. Don't assume "the migration didn't complain"
means the restart happened; trust the guard, not the extension creation line.

## Gotchas that will actually bite you

- **`shared_preload_libraries` needs a real restart, not a reload.** `pg_ctl reload` / `SIGHUP` will not pick it
  up. This is why steps 3–4 exist at all.
- **It must be identical across every node.** Don't hand-patch one patroni container's config and call it done —
  Patroni pushes the DCS-stored value to all members for exactly this reason. Trust `edit-config`, don't improvise
  per-node.
- **Restarting the leader = a failover.** Applications reconnect through HAProxy and should ride it out, but it is
  a real, brief write interruption. Don't do it mid-business-day if you can avoid it.
- **The dev `docker-compose.yml` is not affected by any of this.** That's a single container with `-c` command
  flags — a plain `docker compose up -d --force-recreate postgres` (or stack redeploy) picks up the new flags
  immediately. All of the ceremony above is specifically a Patroni/DCS problem, not a Postgres problem.
- **`pg_stat_statements` has no history before the extension exists.** The moment `CREATE EXTENSION` succeeds,
  it starts counting from zero — nothing from before that instant is recoverable. Not a bug, just don't expect
  historical data on day one.
- **Rolling it back is the same dance in reverse** — `edit-config` back to the old value, then another full
  rolling restart. There is no fast undo.

## Quick sanity checks

```bash
# Did the config actually land, and who still needs a restart?
docker exec -it $(docker ps -q -f name=patroni1) patronictl -c /etc/patroni.yml list postgres-cluster

# Is it actually loaded, post-restart?
docker exec -it $(docker ps -q -f name=patroni1) su-exec postgres psql -U postgres -c "SHOW shared_preload_libraries;"

# Did the extension get created, and is it collecting anything yet?
docker exec -it $(docker ps -q -f name=patroni1) su-exec postgres psql -U postgres \
    -c "SELECT extname FROM pg_extension WHERE extname = 'pg_stat_statements';"
docker exec -it $(docker ps -q -f name=patroni1) su-exec postgres psql -U postgres \
    -c "SELECT count(*) FROM pg_stat_statements;"
```

If the last query errors with something like `pg_stat_statements must be loaded via shared_preload_libraries`,
step 3/4 (the restart) did not actually happen on the node you're connected to — go back and check
`patronictl list` again.
