---
layout: default
title: Database and Reporting
nav_order: 7
parent: FAQ
---

# Database and Reporting

## Storage

**Q: Where is data stored?**

A: All configuration maps one-to-one to PostgreSQL tables. The big tables are Archive (JSONB), Archive Key, the case
table (with a snapshot of the triggering transaction) and Archive Tag. Superseded archive records produced by
reprocessing go to ArchiveVersion.

**Q: How do I query values inside the archive JSON?**

A: Use PostgreSQL JSONB operators. For efficiency, promote the key via Archive Key (the Report Table switch) or create
an expression index. Every search key used in cache or drill needs an expression index, for example on
`("Json" -> 'payload' ->> 'FieldName')`. Check explain plans and use `pg_stat_activity` to spot missing indexes, and
remember that more indexes slow ingestion.

## Visualisation

**Q: What is the visualisation system?**

A: A self-service dashboard tool with a canvas, parameters and SQL data sources rendered as grids or charts. Dashboards
can be embedded in the case view, and role permissions can hide cards or parameters.

**Q: Is SQL in visualisations safe?**

A: Reporting queries use a read-only connection, bind variables, a SELECT-only parser (Assert Select Only) and
nesting-depth guards; the switch that could disable the filter was removed. They remain an exfiltration risk for whoever
can author them, so treat dashboard authorship as a privileged permission. The AI assistant's SQL access is gated
separately and more tightly still — see [AI and Agent Features](AiAndAgentFeatures.html).
