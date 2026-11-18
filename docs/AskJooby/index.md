---
layout: default
title: Ask Jooby
nav_order: 10
has_children: true
---

# Ask Jooby

Ask Jooby is the conversational agent layer over the Jube service surface. The pages in this section describe the tools
an agent calls, how a turn is orchestrated across agents, how the Pasture (the searchable documentation and code corpus)
is populated, how large results are cached behind a `data_pointer`, and how traffic is screened in both directions. The
catalogue of agents and every operation they own is in the [Agent Hierarchy](../AgentHierarchy/index.md) section, nested
beneath this one.

## Who can see a conversation

A conversation, its messages and the files attached to it belong to the user who created it. Another user in the same
tenant cannot list, open, send into, rate, upload to, download from or delete it, and the transcript and audit downloads
are refused to other users likewise. Tenant isolation applies first.

## What the chat window renders

Replies are rendered as Markdown with raw HTML switched off. Markdown images are replaced by the text
`[image removed: alt]` so a reply cannot make the browser fetch a remote address carrying data in the query string.
Links are safe schemes only and open in a new tab. The content security policy (`img-src 'self' data:` plus the map tile
hosts) is the second line of defence.

## Changes need your approval

A tool that writes or deletes (a Write or Delete service operation) never runs the first time the agent asks. The server
holds the call, the chat shows a card with the tool name and the exact arguments, and nothing has changed. Approve or
Decline posts the decision to `POST /api/AskJoobyConversation/{id}/Approvals/{approvalId}`; the chat then tells Jooby,
which repeats the identical call. An approval covers exactly that tool with exactly those arguments, once, for that user
in that conversation, and expires after 30 minutes; the agent cannot approve its own request because only an
authenticated call from the conversation's owner can. A restore point is still taken immediately before the approved
write. Writing to the tenant-wide (System) memory needs the same approval. `AskJoobyRequireWriteApproval=False` switches
the card off; `AskJoobyReadOnly=True` removes write tools altogether.

## Limits

Chat sends are limited per user and per tenant in a rolling window, and the number of replies streaming at once is
capped per user and per tenant. A refusal is HTTP 429 with a localised message and `Retry-After`. Settings:
`AskJoobyRateWindowSeconds` 60, `AskJoobyMaxMessagesPerUserPerWindow` 20, `AskJoobyMaxMessagesPerTenantPerWindow` 120,
`AskJoobyMaxConcurrentTurnsPerUser` 3, `AskJoobyMaxConcurrentTurnsPerTenant` 20. Counted per process, so a multi-node
deployment multiplies the figure. Tool calls and tokens per turn are bounded by the
`AskJoobyMaxSearchesPerTurn`, `AskJoobyMaxConcurrentToolCalls` and token budget settings.

## Sandboxed R

`RunScript` is a separate permission and runs on the `jube-r` container: read-only filesystem, all capabilities dropped,
256 processes, 2 GB, 2 CPUs, a 300 second ceiling, and a network with no route to the database, cache or
application. The script is checked against a forbidden-function list before it runs, including file, connection,
directory and process-forking functions. Data reaches a script only as pointers resolved for the caller's own
conversation.

## Outbound calls

Web search and company-record lookups go to the single endpoint the operator configures (never a host the agent or a
user chooses), without following redirects, with a 60 second timeout, an 8 MB response cap, and every result and the
provider's synthesised answer screened for prompt injection and truncated before the agent sees them. Queries are
screened for secrets. They can contain whatever the agent chooses to ask, including names from the case in hand. There
is no per-tenant switch: `EnableWebSearch` and `EnableOpenCorporates` are deployment-wide.
