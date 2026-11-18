---
layout: default
title: Database and Reporting
nav_order: 7
parent: FAQ
---

# Database and Reporting

## Storage

**Q: Where is data stored?**

A: All configuration maps one-to-one to PostgreSQL tables, each with a version table alongside holding a copy of every version that came before. The big tables are Archive (JSONB), Archive Key, the case table (with a snapshot of the triggering transaction) and Archive Tag. Superseded archive records produced by reprocessing go to ArchiveVersion.

**Q: How do I query values inside the archive JSON?**

A: Use PostgreSQL JSONB operators. For efficiency, promote the key via Archive Key (the Archive Key switch of the [Request XPath](../Configuration/Models/RequestXPath/index.html)), which is what the Case Key Journal and its drill rely upon, or for a query of your own create an expression index, for example on `("Json" -> 'payload' ->> 'FieldName')`. Check explain plans and use `pg_stat_activity` to spot missing indexes, and remember that more indexes slow ingestion.

## Visualisation

**Q: What is the visualisation system?**

A: A self-service dashboard tool with a canvas, parameters and SQL data sources rendered as grids, charts, HTML or maps. Dashboards are viewed under Visualisation in the main menu and built under Administration, Visualisation setup. A dashboard can be embedded in the case page, as its Dashboard section, and role permissions can hide data sources or parameters. See [Visualisations](../Configuration/Visualisations/index.html).

**Q: Is SQL in visualisations safe?**

A: Reporting queries use bind variables, a SELECT-only parser (Assert Select Only) and nesting-depth guards, and run read only; there is no switch with which the filter can be disabled. Set `ReportConnectionString` to a role that can only read, as otherwise the SQL runs on the login of the application itself, behind only a read-only transaction and the parser, and Jube warns of this at startup. Reporting queries remain an exfiltration risk for whoever can author them, so treat dashboard authorship as a privileged permission. The AI assistant's SQL access is gated separately and more tightly still, see [AI and Agent Features](AiAndAgentFeatures.html) and the [Visualisation Security Policy](../Configuration/Visualisations/SecurityPolicy/index.html).
