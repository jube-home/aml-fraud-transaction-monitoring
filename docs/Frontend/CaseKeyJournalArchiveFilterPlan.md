# Case Key Journal and Replay: one filter over the Archive

The journal and Replay filter the Archive itself, not the journal's lens columns. The builder is the case-search builder without the Select pane and without any case fields: only the model's own fields (payload, abstractions, counters, dictionaries, sanctions, adaptations and the rest of the completions list), with the case-search operators (equal, not equal, less, less or equal, greater, greater or equal, like, not like), nested groups and AND/OR.

## Decisions (owner, 2026-10-25)

- Full run of the Archive, the same field list as case search minus the Case table fields.
- The case key and value are fixed and are not part of the user's filter. With lanes or a drill, each lane's own key and value apply and the filter sits on top of every lane.
- If the user has been explicit (a filter is set), the result is exactly what the filter selects. A later option shows everything with the filtered rows picked out.
- Saved replays keep the filter as JSON; the Save replay design is a separate step once the function is proven.
- Journal and Replay move together.

## Shape

- The client sends the rule tree as compact JSON: groups of `condition` and `rules`, rules of `id`, `operator` and `value`. Nothing else is trusted.
- The server resolves every `id` against the model's completions, takes the SQL path and data type from there, checks the operator against the type, converts the value, and parameterises it. The client never supplies SQL, a field path or a type.
- The compiled predicate is appended to the key/window query that already runs on `Archive a`, so every consumer (rows, count, track, tag job, saved replay) filters in the database and the 500-row limit applies after the filter.
- Limits: 100 rules, depth 6, 20,000 characters.

## Stages

1. Server compiler (`ArchiveFilter`) and its tests.
2. Match and rows queries take the compiled predicate; matcher, rows, track, tag job and replay use it; the old in-memory evaluator and its shared cases go.
3. Client: one builder over the completions list for both pages; URL `jf` carries the compact JSON; the client evaluator goes.
4. Tests, docs, agent tool descriptions.

## Built (stage 1)

- `ArchiveFilter` (server) parses the compact rule JSON, resolves each id against the model's completions (`ArchiveFilterFields`), checks the operator against the field type, converts the value and returns a parameterised predicate. Every query that reads the Archive for a journal view takes it: the rows query, the count, the match query behind Track and Tag all matching, and the pinned rows.
- `GET /api/CaseKeyJournal` takes `filter` and reports `meta.total`, the key's transactions in the window before the filter (up to 100,000), beside `meta.rowCount`. The statistics endpoint is not given the filter.
- Track, tag job preview and submit, and saved replays store and run the same JSON. A stored value that cannot be read as the field's type (a text in a number field) is refused as `FilterUnreadable`.
- The old in-memory evaluator, its shared parity cases, and the lens and case columns of the tag job (`FilterLens`, `FilterCaseId`, dropped by `DropCaseKeyJournalTagJobFilterLensAndCase`) are gone. A filter saved in the old format no longer loads and is dropped on restore.
- Client: one `JournalFilterBuilder` over the model's completions for both pages, with the case-search operators. `jf` and `rf` carry the compact JSON.
- Known in case search: its parser maps `greater` to `>=`. The Archive filter uses `>` and `>=` correctly; case search is unchanged.

## Built (stage 3)

- Replay has a switch, shown only while a filter is set: show everything and pick out the matches (`ra=1` in the address). Track takes `IncludeUnmatched` and then returns every transaction of the view with a `Matched` flag. The summary, the legs, the impossible journeys and the counts still cover only the matching ones.
- The unmatched are drawn grey and faint, are never joined into a trail, and never feed a leg. With improbable legs only on, the impossible legs of the matches are kept along with every dim transaction. The legend says what the grey means.
- The setting is in the address only: a saved replay does not store it yet and opens with it off.

## Built (journal and map in one card)

- The Replay section is gone. The Case Journal is one story, top to bottom: the window and activations-only, the statistics, peers and charts (which the keys and the filter do not touch), then one card holding the scope (keys and filter), the grid with its own controls, and the map with its own controls.
- The scope has two rows in a label column and a content column: the keys (lane chips, the case key locked, extra keys chosen from the workflow's drill fields) and the filter conditions (removable chips), with the match count, Clear, Save view and Saved views in a footer.
- Grid controls (lens, pinned only, columns, tagging, export) sit above the grid. Map controls (travel limits, trails, heat, improbable legs only, show everything, fit) sit above the map, and the map is left out when the model has no location. Nothing is greyed out: what cannot be used is not shown.
- Tagging controls are inline in the grid toolbar. The selection count, Tag and Clear appear only while rows are selected.
- Saved views store the window, the keys, the filter, the lens and the map options (limits, improbable only, show everything). Trails and heat are kept in the address only. The backend entity is still named replay.
