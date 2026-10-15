---
layout: default
title: Testing
nav_order: 9
has_children: true
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# Testing

A catalogue of the automated test suite, grouped by platform area. Each page covers one area, with a table per test
class listing every test and what it verifies. The catalogue is a statement of behaviour as much as of coverage: a test
named here is a guarantee the platform makes, so the tables double as a fine-grained functional specification of the
areas they cover.

Each class entry states the fully qualified test class, the number of tests, and where applicable the test category and
the xunit collection. A Kind of Fact is a single scenario; Theory is data-driven and runs once per supplied case, so the
real execution count is higher than the number of rows.

The categories partition the suite by the infrastructure a test needs. Unit tests run against no external services.
Service tests run against a real Postgres database and, where relevant, a real Redis cache. PenTest tests exercise the
running HTTP surface adversarially. AskJooby tests cover the agent layer, and Volume tests exercise bulk data paths.

The catalogue is kept exact by the suite itself: `TestDocumentationTests` fails the build when a test exists without a
row in these tables, when a documented test no longer exists, or when a class is documented on more than one page.
