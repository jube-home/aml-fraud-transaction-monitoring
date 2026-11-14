---
layout: default
title: Working the Triage Recommendation
nav_order: 34
parent: Case Management
grand_parent: Configuration
---

# Working the Triage Recommendation

The triage agent reads an open case the way a first-line analyst would and recommends an outcome: confirmed, not
confirmed or inconclusive, with a short rationale and the facts it relied on. It is off by default and it only
recommends. No code path lets it close, suspend, reassign or change a case; a person decides.

## Turning it on

| Setting                  | Meaning                                                                                   |
|--------------------------|-------------------------------------------------------------------------------------------|
| `EnableCaseTriage`       | Master switch, `False` by default.                                                        |
| `CaseTriageUsers`        | The users the agent works as. It sees only what each of them can read, in read-only mode. |
| `CaseTriageWorkflows`    | The case workflow Guids that are triaged. Null triages nothing.                           |
| `CaseTriageMaximumCases` | The most cases triaged for each user in a cycle.                                          |
| `WaitCaseTriage`         | Milliseconds between cycles.                                                              |

AskJooby must be configured on the node. The job is cluster safe: a case is taken under an advisory lock, and a case
whose facts have not changed since its last recommendation is skipped without calling the model.

## What an analyst sees

When a case has a recommendation, the case page shows a **Triage recommendation** card with the outcome, the rationale
and the cited facts. **Accept** records the decision; **Override** needs a reason (at most 500 characters) and records
it. Either is recorded once, with the analyst and the time. When the facts of the case change, a new recommendation
supersedes a pending one, and one already decided is kept as history.

Accepting does not close the case. For a confirmed or not confirmed recommendation on an open case it opens the normal
close confirmation with the note pre-filled from the rationale; close it in the usual way, where the checklist, lock and
eligibility rules all apply, or cancel and leave it open. An inconclusive recommendation opens nothing.

Each step is written to the case timeline: **Triage recommendation made**, **accepted** and **overridden**, with the
recommended outcome and, for an override, the reason.

<!-- TODO(screenshot): The case page showing the Triage recommendation card with Accept and Override. -->

## Overrides and quality review

Cases are picked for quality review at random at the workflow's QA sample rate. When an analyst overrides the agent, the
case is drawn again at that rate multiplied by `CaseTriageQaOverrideFactor` (3 by default, at most 1 overall), so cases
where person and agent disagree are sampled more. The draw is deterministic, so a case already picked at the base rate
stays picked. A workflow whose QA rate is zero is never boosted.

A case picked for review and then closed shows a **Quality review** card to anyone with case permission except the
person who closed it. The reviewer judges how the analyst handled the case (pass, fail or not applicable) and, if the
agent made a recommendation, how good it was, with an optional comment. Reviews are append only (`CaseQaReview`).

<!-- TODO(screenshot): The Quality review card on a closed, sampled case. -->

## Governance evidence

Each recommendation (the `CaseAgentRun` table) records the model id the provider reported, a SHA-256 of the triage
prompt (so runs can be compared by agent definition), the AskJooby conversation that holds the full working, the hash of
the facts, and who decided and when.

## Replaying the prompt over closed cases

To find out whether a prompt or model is any good before it is trusted, replay it over cases people have already
decided. Set `EnableCaseTriageReplay` to `True`, then request a replay (`POST /api/CaseAgentReplay/Start`, permission
Read Write Case Workflow) with a closed-date window, a maximum of up to 200 cases, how many minutes after creation the
facts are cut off (60 by default), an optional workflow, and optionally a prompt to test in place of the built-in one.

For each closed case the model is shown only what existed at the cut-off: the transactions of the case key up to that
moment (the ten most recent, clipped), the analyst notes written by then (never the notes the assistant wrote), and how
many earlier closed cases of the same key were confirmed and not confirmed, counting only those closed before the
cut-off. It is given no tools, so it cannot look anything else up, and it is never told the outcome. Its recommendation
is stored beside the outcome people reached.

`GET /api/CaseAgentReplay` and `GET /api/CaseAgentReplay/{guid}` report, per replay, how many were scored, how many
answers were unusable, the agreement rate, **missed risk** (people confirmed, the model did not), **over-escalation**
(the model confirmed, people did not) and how many it called inconclusive. Replaying the same window with two prompts
and calling `GET /api/CaseAgentReplay/Compare/{first}/{second}` gives the cases both scored, how often they gave the
same answer, and how often each agreed with people. Each replay carries the SHA-256 of its prompt.

This measures a prompt and a model on a fixed bundle of facts. It does not measure the live, tool-using agent, which
reads the case through AskJooby's tools. That is deliberate: a bundle can be proven free of later information, whereas
tools reading live data cannot.

<!-- TODO(docs): not built: a per-workflow switch on the workflow form (the setting lists workflow Guids instead), and an as-of scope for the live agent's own tools. -->

A replay with no workflow chosen covers every workflow in the tenant that the person who requested it can access, and no
other; choosing a workflow narrows it further. Access is re-checked when the replay runs, so a replay never reads a
workflow the requester has since lost. A replay only reads cases in workflows the caller can access, notes shown to the
model are limited to the caller's tenant, and at most two replays can be active at once; a third request is refused
until one finishes.

## The Agent replay page

Administration > Agent replay (Read Write Case Workflow permission) starts a replay for a date window, with the most
cases to take and how many minutes of events to hide after each case's decision, and lists every replay with its state,
the agent definition it ran, the cases selected and scored, how many the agent agreed with the analyst, the missed-risk
and over-escalation counts and the agreement rate. Tick two completed replays and choose Compare selected to see how
many cases they share, how many got the same recommendation and how often each agreed with the analyst.

<!-- TODO(screenshot): The Agent replay page with two replays selected for comparison. -->
