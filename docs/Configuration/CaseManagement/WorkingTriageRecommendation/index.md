---
layout: default
title: Working the Triage Recommendation
nav_order: 34
parent: Case Management
grand_parent: Configuration
---

# Working the Triage Recommendation

The triage agent reads an open case in the manner of a first line analyst and recommends an outcome, being confirmed, not confirmed or inconclusive, with a short rationale and the facts it relied upon. It is off by default and it only recommends. No code path allows it to close, suspend, reassign or change a case, which is to say that a person decides.

The question it helps with is the one asked of every case on first opening it: is this likely to be worth pursuing, and why. The agent offers a first reading so that the analyst begins from a stated view and its evidence in place of a blank page, and the analyst is then asked to say whether that view is accepted. As each acceptance and each disagreement is recorded, the feature also yields a running measure of how far the agent can be trusted.

## The terms used

| Term             | Meaning                                                                                                                                                   |
|------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|
| Recommendation   | One reading of one case by the agent: an outcome, a rationale and the cited facts.                                                                        |
| Outcome          | Confirmed, meaning the activity is suspicious and should be escalated; not confirmed, meaning there is a reasonable explanation; or inconclusive, meaning the facts support neither. |
| Rationale        | The reasoning of the agent in plain prose, which it is asked to keep under 150 words.                                                                     |
| Cited facts      | Up to ten short citations of the facts the agent relied upon, each at most 300 characters.                                                                |
| Pending          | A recommendation that no person has yet accepted or overridden.                                                                                           |
| Superseded       | A pending recommendation replaced by a newer one because the facts of the case changed. It is no longer shown.                                            |
| Agent definition | The prompt given to the agent. It is identified by its SHA-256, so that two versions of the prompt can be told apart.                                     |
| Replay           | Running a prompt over cases that people have already decided, without showing it the decision, to measure how often it would have agreed.                 |
| Cut-off          | In a replay, the moment after which nothing about the case is shown to the model.                                                                         |
| Missed risk      | In a replay, a case that people confirmed and the model did not. It is the costly kind of error.                                                          |
| Over-escalated   | In a replay, a case that the model confirmed and people did not. It is the wasteful kind of error.                                                        |

## Where it lives

The triage agent is switched on by Environment Variables, described below, and runs in the background through [Ask Jooby](../../../AskJooby/index.html). Its work is then met in three places:

* On the case page, navigated to from the Cases page, where a case having a recommendation presents the card titled Triage recommendation in the Decide pane, under On this case.
* On the case page of a closed case picked for quality review, where the card titled Quality review is presented in the same place.
* Through the menu at Administration, then Agent replay, where a prompt and a model are measured against cases already decided.

{% comment %}SHOT id=case-triage-recommendation-card
     route=/cases
     steps=open-demo-case, scroll-to:Triage recommendation
     frame=section:Triage recommendation
     state=the Decide pane of an open demonstration case with the Triage recommendation card reading Recommends: confirmed, the rationale, three cited facts, the model name, and the Accept and Override controls awaiting a decision
     conveys=the agent states an outcome, why, and on what facts, and leaves the decision to the analyst
     alt=The Triage recommendation card in the Decide pane recommending that the case is confirmed, with its rationale and cited facts, awaiting acceptance or override by the analyst{% endcomment %}

## Turning it on

The Environment Variables are as follows:

| Value                  | Description                                                                                                         | Example |
|------------------------|---------------------------------------------------------------------------------------------------------------------|---------|
| EnableCaseTriage       | The master switch. False by default.                                                                                | True    |
| CaseTriageUsers        | The users the agent works as, comma separated. It sees only what each of them can read, in read only mode. No default. | TriageAgent |
| CaseTriageWorkflows    | The Guids of the Case Workflows that are triaged, comma separated. Where it is not set, nothing is triaged.         |         |
| CaseTriageMaximumCases | The most cases triaged for each user in a cycle. 25 by default.                                                     | 25      |
| WaitCaseTriage         | Milliseconds between cycles. 600000 by default, being ten minutes.                                                  | 600000  |

Ask Jooby must be configured on the node. The job is safe to run on a cluster: a case is taken under an advisory lock, and a case whose facts have not changed since its last recommendation is skipped without the model being called.

The Case Workflows to triage are named by Guid in the Environment Variable, and there is no switch for the purpose in the Case Workflow itself.

In each cycle, and for each user named, the agent takes the cases of those Case Workflows that are open, or suspended while open, and that have a case key, beginning with the cases never triaged and then those triaged longest ago. A closed case is never triaged. A recommendation counts towards CaseTriageMaximumCases only where one was written, so cases skipped as unchanged cost nothing. Where the model returns something that cannot be read as an outcome with a reason, nothing is written for that case, a warning is logged, and the case is tried again in a later cycle.

## What an analyst sees

Where a case has no recommendation the card is absent altogether, there being no empty state, which is how every case appears while the feature is off. Where a case has a recommendation, the Triage recommendation card presents the outcome, as Recommends: confirmed, Recommends: not confirmed or Recommends: inconclusive, followed by the rationale, the cited facts as a short list, and the name of the model in small type at the foot. The help mark beside the title reads "An AI assistant read this case and recommends an outcome. It only recommends: nothing is closed or changed until a person decides, and you can accept it or override it with a reason."

The outcome is to be read as an opinion with its evidence attached and never as a score. The useful habit is to read the cited facts first and to check each against the Journal of the case, for a recommendation whose citations hold up is worth a great deal more than its headline.

The analyst decides the recommendation as follows:

| Value    | Description                                                                                                        |
|----------|--------------------------------------------------------------------------------------------------------------------|
| Accept   | Records that the analyst accepts the recommendation.                                                               |
| Override | Asks Why are you overriding it?, the reason being required and limited to 500 characters, then Record override.    |

Upon Override being selected, the two buttons give way to a single line for the reason and a Record override button, which remains disabled until something other than spaces has been typed. Either decision is confirmed by the message Decision recorded, and the buttons are then replaced by a line of text.

Either is recorded once, with the analyst and the time, and is thereafter shown in the card as "Accepted by Administrator." or as "Overridden by Administrator:" followed by the reason. When the facts of the case change, a new recommendation supersedes one that is pending, whereas one already decided is kept as history and continues to be shown until a newer recommendation is made. Accept and Override are available on a pending recommendation whether or not the case is held by the analyst.

Keep in mind that accepting does not close the case. For a confirmed or not confirmed recommendation on an open case, accepting opens the normal confirmation to close, with the note filled in from the rationale. The case is then closed in the usual way, where the checklist, lock and eligibility rules all apply, or the confirmation is cancelled and the case left open. Accepting an inconclusive recommendation opens nothing.

The note is filled in as "Triage recommendation accepted (confirmed):" followed by the rationale, cut to the 1,000 characters a closing note allows, and only where the note was empty, so anything already typed there is left alone. The reason for closing is not chosen on the analyst's behalf.

By way of illustration, suppose the demonstration case on AccountId ACC-441, in First Line Review, has been triaged. The card might read Recommends: confirmed, with a rationale observing that a purchase was made in London forty minutes after one in Cairo, and cited facts naming those two transactions. An analyst who agrees selects Accept, sees Decision recorded, and is offered the confirmation Close C-1?, the number being that of the case, with the note already written, which may be confirmed, or cancelled should the case need to be escalated instead of closed. An analyst who knows the customer to be travelling selects Override, types the reason, selects Record override, and the card thereafter reads Overridden by, with the reason, for the next reader.

The following refusals may be returned, each as a message on the page:

| Circumstance                                                         | Message                                                  | Status |
|----------------------------------------------------------------------|----------------------------------------------------------|--------|
| Override with no reason                                              | A reason is required to override a recommendation.       | 400    |
| A reason over 500 characters                                         | The reason must be at most 500 characters.               | 400    |
| The recommendation was decided, or replaced, by the time of asking   | The recommendation has already been decided or replaced. | 409    |
| The recommendation does not exist, or the case is not visible        |                                                          | 404    |
| The user lacks the permission                                        |                                                          | 403    |

The third arises where two analysts have the same case open, or where the agent has written a newer recommendation since the page was drawn, in which circumstance reopening the case presents the current one.

Each step is written to the Events of the case, as Triage recommendation made, Triage recommendation accepted and Triage recommendation overridden, with the recommended outcome and, for an override, the reason.

## Overrides and quality review

Cases are picked for quality review at random, at the quality review sample rate of the Case Workflow, as described in [Working Quality Review Samples](../WorkingQualityReviewSamples/index.html). Upon an analyst overriding the agent, the case is drawn again at that rate multiplied by the CaseTriageQaOverrideFactor Environment Variable, which is 3 by default, to a rate of at most 1 overall. It follows that the cases where person and agent disagree are sampled more often.

The draw is deterministic, so a case already picked at the base rate stays picked. A Case Workflow whose rate is zero is never boosted. By way of example, at the default sample rate of 0.05, being one case in twenty, an overridden case is drawn at 0.15, being roughly one in seven. Where the draw picks the case, the event Picked for quality review is written with the rate that applied.

A case picked for review and then closed presents a Quality review card to anyone holding the case permission, other than the person having closed it. The reviewer judges the Handling of the case, as Pass, Fail or Not applicable, and, where the agent made a recommendation, the Triage recommendation in the same terms, with an optional comment. Reviews are append only, in the `CaseQaReview` table.

## Governance evidence

Each recommendation is a row of the `CaseAgentRun` table, recording the following for the purposes of governance:

* The model id as reported by the provider.
* A SHA-256 of the triage prompt, so that runs can be compared by agent definition.
* The Ask Jooby conversation holding the full working.
* The hash of the facts of the case.
* Who decided the recommendation, and when.

## Replaying the prompt over closed cases

To find out whether a prompt or a model is any good before it is trusted, it is replayed over cases that people have already decided. Set the EnableCaseTriageReplay Environment Variable to True, which is False by default, upon which the jobs server looks for replays to run at the interval set by WaitCaseTriageReplay, being 30000 milliseconds by default.

For each closed case the model is shown only what existed at the cut-off:

* The transactions of the case key up to that moment, being the ten most recent, clipped.
* The analyst notes written by then, and never the notes the assistant wrote.
* How many earlier closed cases of the same key were confirmed and not confirmed, counting only those closed before the cut-off.

The model is given no tools, so it cannot look anything else up, and it is never told the outcome. Its recommendation is stored beside the outcome people reached.

This measures a prompt and a model on a fixed bundle of facts. It does not measure the live, tool using agent, which reads the case through the tools of Ask Jooby, and the tools of the live agent have no means of being confined to a moment in the past. That is deliberate: a bundle can be proven free of later information, whereas tools reading live data cannot.

## The Agent replay page

Navigate through the menu to Administration, then Agent replay. A replay is started with the following, then Start replay:

| Value                       | Description                                                                                             | Example    |
|-----------------------------|---------------------------------------------------------------------------------------------------------|------------|
| From                        | The first day of the window in which the cases were closed. Whole days are taken, in UTC.               | 2026-10-01 |
| To                          | The last day of the window. It must be after From, and within 366 days of it.                           | 2026-10-31 |
| Most cases                  | The most cases to replay, newest closed first, from 1 to 200.                                           | 50         |
| Hide events after (minutes) | The number of minutes after the creation of each case at which the facts are cut off, from 0 to 10080. | 60         |

Beneath, every replay is listed with the following columns:

| Column         | Description                                                                          |
|----------------|--------------------------------------------------------------------------------------|
| Status         | Pending, Running, Completed or Failed.                                               |
| Agent          | The first characters of the SHA-256 of the prompt, identifying the agent definition. |
| Cases          | The number of cases selected.                                                        |
| Scored         | The number of cases for which a usable recommendation was returned.                  |
| Agreed         | The number on which the agent agreed with the analyst.                               |
| Missed risk    | The number that people confirmed and the model did not.                              |
| Over-escalated | The number that the model confirmed and people did not.                              |
| Agreement      | The rate of agreement.                                                               |

Most cases is 50 and Hide events after (minutes) is 60 upon the page being opened, and the dates are empty. Start replay remains disabled until both dates are given, To is not before From, and Most cases is above zero. Upon it being selected the message Replay started is shown and the replay joins the list as Pending. The list refreshes itself every ten seconds, so a replay can be watched moving through Running to Completed without the page being reloaded. Before any replay exists the page states No replays yet, and where the list cannot be read, Could not load the replays.

The Agent column shows the first ten characters of the SHA-256, and Agreement is a percentage to one decimal place, or a dash where nothing was scored.

The numbers are read as follows. Scored falling short of Cases means that the model gave answers that could not be used, which is itself a mark against a prompt. Agreement is the share of the scored cases on which the model and the analyst reached the same outcome. Of the disagreements, Missed risk is the one to watch most closely, for a model that fails to confirm what people confirmed would, were it trusted, let suspicious activity pass, whereas Over-escalated costs only the time of an analyst. A prompt with high agreement and a missed risk of zero or near it is the one to prefer.

The tick box of a replay is available only once it is Completed. Ticking two completed replays and selecting Compare selected, which is disabled unless exactly two are ticked, states how many cases the two have in common, how many were given the same recommendation, and how often each agreed with the analyst, in a sentence such as "48 cases in common; the same recommendation on 41; the first agreed with the analyst on 36 and the second on 40."

The following are refused, with the message shown on the page:

| Circumstance                                    | Message                                          |
|-------------------------------------------------|--------------------------------------------------|
| To is before From                               | The end of the window must be after its start.   |
| The window is longer than 366 days              | The window must be at most 366 days.             |
| Most cases is outside 1 to 200                  | The maximum cases must be between 1 and 200.     |
| Hide events after is outside 0 to 10080         | The cut-off must be between 0 and 10080 minutes. |
| A prompt over 4,000 characters, through the API | The prompt must be at most 4000 characters.      |

{% comment %}SHOT id=case-triage-agent-replay-page
     route=/administration/replay
     steps=toggle:Select for comparison, toggle:Select for comparison, click:Compare selected
     frame=page
     state=the Agent replay page listing three completed replays of the same window with differing agent definitions, two of them ticked, and the comparison sentence beneath stating the cases in common and how often each agreed with the analyst
     conveys=a prompt is measured against decided cases, and two versions are compared on the same cases before either is trusted
     alt=The Agent replay page with a list of replays showing cases scored, agreement, missed risk and over-escalation, two replays selected and a sentence comparing them{% endcomment %}

The page replays every Case Workflow in the tenant that the user can access, with the prompt that is built in. Narrowing a replay to a single Case Workflow, and testing a prompt in place of the built in one, are available through the API only.

## Example

A replay is requested through the API as follows, the Case Workflow and the prompt being optional, and the prompt being limited to 4,000 characters:

```http
POST /api/CaseAgentReplay/Start
```

```json
{
  "fromDate": "2026-10-01T00:00:00Z",
  "toDate": "2026-10-31T23:59:59Z",
  "maximumCases": 50,
  "asOfMinutes": 60,
  "caseWorkflowGuid": "5f0b6f0e-2f0c-4a57-9c1e-0d7f3a1b2c3d",
  "prompt": "A prompt to test in place of the built in prompt."
}
```

The results are read as follows:

```http
GET /api/CaseAgentReplay
GET /api/CaseAgentReplay/{guid}
GET /api/CaseAgentReplay/Compare/{first}/{second}
```

The first two report, for each replay, how many cases were scored, how many answers were unusable, the agreement rate, the missed risk, the over-escalation and how many the model called inconclusive. Each replay carries the SHA-256 of its prompt. Replaying the same window with two prompts and calling Compare gives the cases both scored, how often they gave the same answer, and how often each agreed with people.

## Permissions and security

Reading, accepting and overriding a recommendation, and recording a quality review, requires permission 1, Read Write Case, and the case must be visible to the user. A case cannot be reviewed by the user having closed it.

The Agent replay page and the replay API require permission 18, Read Write Case Workflow. A replay with no Case Workflow chosen covers every Case Workflow in the tenant that the person having requested it can access, and no other, and choosing a Case Workflow narrows it further. Access is checked again when the replay runs, so a replay never reads a Case Workflow that the requester has since lost. The notes shown to the model are limited to the tenant of the caller.

At most two replays can be active at once, and a third request is refused until one finishes.

The triage agent itself works as the users named in CaseTriageUsers, in read only mode, and so sees only what each of those users can read.
