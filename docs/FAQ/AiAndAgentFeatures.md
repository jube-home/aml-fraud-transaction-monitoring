---
layout: default
title: AI and Agent Features
nav_order: 13
parent: FAQ
---

# AI and Agent Features

**Q: What is Ask Jooby?**

A: An embedded AI assistant built on an agent framework over Jube's own service layer, with semantic tools rather than
raw database access. It helps with system monitoring and diagnostics, rule construction (grounded in the documentation
corpus and the Rule Taxonomy), model search and analytics. Every change it proposes goes through the same Maker Checker
approval as a human change, snapshots are taken for AI changes, and a read-only switch (`AskJoobyReadOnly`) confines it
to observation. A guard layer checks tool content so the assistant stays on topic and inside its mandate.

**Q: Which AI models does it need?**

A: Any OpenAI-compatible inference endpoint: an agentic model, an optional smaller validation model, and an embedding
model. Hosted serverless inference or a local endpoint (for example Ollama on a GPU box) both work. Data-sovereignty
requirements may rule out hosted providers outside your region, which is exactly why the endpoint is pluggable.

**Q: How does the agent query the database safely?**

A: Through heavily gated dynamic SQL, used only as a last resort behind the purpose-built tools. The SQL is parsed into
a syntax tree; only a single SELECT is allowed, with SELECT INTO, row locking, banned functions, system catalogues and
protected tables all refused. Each table must be on a closed allow-list, and the parser rewrites every table reference
into a subquery that enforces the caller's tenant and model-role scope. The connection is forced read-only with a
statement timeout, and every query is logged. Case tables are excluded from the allow-list entirely.

**Q: How does the agent know about the documentation?**

A: A retrieval corpus built from this documentation is chunked and loaded into the database, then searched with hybrid
retrieval: keyword and embedding search fused by reciprocal rank, with a reranker on top. The corpus is generated as a
build artefact, so editing a docs page takes effect at the next corpus refresh rather than immediately.

**Q: Does the agent remember things between conversations?**

A: Yes. It keeps a memory store of facts and reflections, scoped either globally per user or to a conversation, which it
can recall alongside document retrieval. Large tool results are parked in a data cache and handled by reference — tools
can render a chart or export a CSV from the full result without the model ever re-reading every row.

**Q: Is an MCP server provided?**

A: Not by default. It is considered optional and quick to add, and mostly useful for centralising prompts rather than
for orchestration.
