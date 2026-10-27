# Case management analytics: what exists, how the market compares, and the ideas backlog

Written 2026-10-23 alongside the Cases page work. Part 1 and 2 describe what is built and is checked against the code and its tests. Part 3 is a competitive read from general product knowledge of the AML and fraud case management market; **it has not been checked against current vendor documentation, so verify any line before it is used outside the team**. Part 4 is the ideas backlog.

## 1. What exists now

### 1.1 Data captured (new tables and fields)

| Item | What it records | Why it matters |
|---|---|---|
| `CaseSearchFilter` | A user's own saved search (builder JSON only, never SQL), private or shared with a role | Analysts keep their own queues and reports without an administrator |
| `CaseView` | When each user last looked at each case, the newest event and the risk assessment they saw | "Returned to me" and "what changed since you looked" |
| `CaseRiskAssessment` | Immutable likelihood, impact, lane and Priority per recompute, with the evidence behind each | Ordering that can be explained and audited, and a risk history |
| `CaseStatusTransition` | Every stay in every status: entered, why, by whom, left, why, seconds spent | Time in status, bottlenecks, cumulative flow, service levels. Backfilled from `CaseEvent` for existing cases |
| `CaseWorkflowStatus.OutcomeId` | Whether closing in this status means confirmed, not confirmed, or neither | Turns closed cases into labelled outcomes, which precision and calibration need |
| `Case.EntityAnalysisModelActivationRuleGuid` | The activation rule that created the case | Per-rule precision and false positive cost |
| Service level targets | First look, ownership and resolution minutes on the workflow; maximum minutes in a status | Service levels become measurable |
| Audit event 16, Peek | A summary was read without taking the lock | Time to first look counts a peek |

### 1.2 Measures defined

| Measure | Definition |
|---|---|
| Time to first look | Case creation to the first event of type 2 (Skim), 4 (Full Case Fetch) or 16 (Peek) |
| Time to ownership | Case creation to the first lock event: 2, 3, 6 or 14 |
| Time to resolution | Creation to closure of a case that closed (closed status 3) |
| Time in status | Seconds between entering and leaving a status, from `CaseStatusTransition` |
| Backlog | Cases created before the end of a day and not closed by it |
| Handoff | A manual lock (6) or an allocate lock (14) event on the case |
| Reopen | An entry into a status with reason Reopened |
| Outcome | The closing status's `OutcomeId`; suspend-bypass and suspend closures never count |
| Precision of a rule | Confirmed divided by confirmed plus not confirmed, over cases the rule created |
| False positive cost | Creation to closure time of the rule's not confirmed cases |
| Service level state | Met, on track, at risk (80 percent of the target elapsed) or breached |
| Ranking quality | Whether ordering closed cases by the Priority or likelihood in force when they were first looked at finds the confirmed ones sooner than working oldest first or at random |

### 1.3 Reads and charts

`GET /api/CaseSearch` (triage list), `/Counts/{workflow}` (rail), `/RiskMatrix`, `/RiskMatrixChart`, `POST /Skim`, `GET /api/CaseSummary/{id}` (peek), `GET /api/CaseAnalytics` (everything below as data) and `/Chart/{kind}` (SVG): flow, time to first look, time to resolution, ageing, time in each status, rule effort, rule precision, gains, calibration, arrivals, first look by hour, service level by week, handoffs. Each is also an Ask Jooby tool where it is a read.

## 2. What the numbers cannot yet say

Honest limits, so nobody over-reads a chart:

- **Likelihood is cold start** until closed cases carry an outcome. The status Outcome field must be set on at least one confirmed and one not confirmed status; the ranking and calibration charts say so when there are fewer than 30 decided cases or fewer than 5 of either kind.
- **Selection bias.** Analysts work the top of the list first, so outcomes skew towards what the ordering already favoured. The ranking chart therefore flatters the Priority ordering. The spec's remedy (a random QA slice of low priority cases) is not built.
- **Lanes 1 and 2 are empty** until payment holds and confirmed sanctions flags exist as data. Lane 3 comes from the case diary only.
- **Times are UTC**, including the arrival heatmap. A tenant time zone setting is not modelled.
- **Handoffs are lock events**, not a true owner history; an unlock followed by someone else's lock is not distinguished from an allocation.
- **Per person figures** are the caller's own plus a team median shown only for three or more people. There is deliberately no per analyst league table, because there is no supervisor permission yet to gate it.

## 3. Competitive read (unverified, general product knowledge)

Vendors considered: NICE Actimize (case manager and alert prioritisation), Oracle Financial Crime and Compliance Management, SAS Anti-Money Laundering, Featurespace ARIC Risk Hub, ComplyAdvantage, Hawk AI, Quantexa, Unit21, Feedzai, Sardine, Napier, Tookitaki. The ratings say how common a capability is in this market, not how any one vendor performs.

| Capability | Typical in the market | Jube now | Gap or lead |
|---|---|---|---|
| Queues with counts, saved filters | Universal | Filters card (built-in and workflow filters) and a My Filters card of own filters | Parity; own filters shared by role is above the usual |
| Priority score on each alert | Common, usually a black box | Lane plus Priority shown with likelihood, impact and the evidence, summing exactly | Lead on explainability; behind on evidence breadth (no model, network or sanctions inputs yet) |
| Explainability of the ranking | Uncommon beyond "top reasons" | Contribution waterfall data per case | Lead once the page draws it |
| Peek or preview without opening | Common | Summary with what changed since the last look | Parity, with the "since you looked" delta a differentiator |
| Skim or next case | Common | Returned cases first, atomic lock | Parity |
| "Returned to me" or reopen queue | Common but simple | Attention feed with reasons | Parity to lead |
| Case ageing report | Universal | Open cases by age and lane | Parity |
| SLA tracking and breach alerts | Common in the larger products | Four targets, states, watch list, overdue view | Parity; no notifications on breach yet |
| Throughput and backlog trend | Common | Daily flow with per status cumulative counts | Parity |
| Cycle time percentiles | Common in BI add-ons | p50 to p95 with histograms | Parity |
| Bottleneck by status | Uncommon | Time in each status, breach counts | Lead |
| Rule or scenario effectiveness | Common in the larger products ("alert quality") | Precision and analyst hours lost per rule | Parity, with effort cost a differentiator |
| Model or ordering validation against outcomes | Uncommon outside vendors selling AI | Gains against oldest first and random, AUC, lift, calibration, Brier | Lead if shown prominently, but needs outcomes |
| Workload and capacity | Common for supervisors | Own throughput against team median; arrival heatmap | Behind on per analyst balancing, by design for now |
| Quality assurance sampling | Common | None | Gap |
| Regulatory clocks (SAR and STR deadlines) | Common in the largest products | Single diary only | Gap, blocked on typed diaries |
| Network and entity context | Strong at Quantexa and Featurespace | Not in the case page yet | Gap, planned |
| Natural language over the data | Emerging | Ask Jooby tools over every read here | Lead |

## 4. Ideas backlog

Ranked within each group by value for effort. "Needs" names what is missing.

### 4.1 Quick wins on data that now exists

1. **Effort per true positive**: analyst hours spent divided by confirmed cases, per workflow and per rule. The most useful single KPI for tuning. Needs nothing new.
2. **Predicted breach**: from the historical resolution time distribution by lane and rule, flag open cases likely to breach before they do.
3. **Stuck locks**: cases locked for more than a set time with no event since, with a one click break lock.
4. **Week on week deltas and sparklines** on every headline tile.
5. **Flow efficiency**: touch time (lock held) against lead time, to separate waiting from working.
6. **Inflow spike alert**: control chart limits on daily arrivals, notifying a supervisor through the existing notification path.
7. **Export every table and chart** to Excel and CSV through the existing grid export.
8. **Scheduled digest** of the headline and the watch list to supervisors.

### 4.2 Needs a small addition

9. **Time zone per tenant** for the rhythm and the day boundaries.
10. **A supervisor permission**, then per analyst workload, availability and reassignment.
11. **QA sample slice**: a random share of low priority cases forced into review, which also repairs the selection bias in the ranking chart.
12. **Typed diaries** (regulatory deadline, customer response, hold), giving lanes 2 and 3 real meaning and a deadline countdown column.
13. **Reason codes on close**, so outcome carries typology and not only confirmed or not.
14. **Reopen reasons**, so a reopen is explained and not only counted.

### 4.3 Larger pieces

15. **Learned combiner** for likelihood once enough outcomes exist, with backtest, shadow mode and promotion as the spec describes.
16. **More evidence families**: model scores, network proximity to confirmed cases, sanctions similarity, customer tier, analyst findings.
17. **Rule tuning loop**: from the noisy rule list straight into the existing backtest and the override surface.
18. **Calibration and drift monitoring** on a schedule, notifying the model owner when calibration error or AUC moves.
19. **Capacity planning**: arrivals, handling time and target response give the analysts needed per hour, shown against who is on shift.
20. **Case clustering and duplicate detection** from shared keys, devices or counterparties, offered as one batch review.
21. **Customer level view**: repeat alerts on a key after false positive closure, suggesting a suppression.
22. **Simulation**: replay the last month under a different ordering or threshold and show the effect on time to first look and on confirmed cases found early.

### 4.4 Where the page should draw attention first

The Cases page rail already has the counts. The analytics belong in three places, each a long canvas section in the existing pattern, not tabs: a pulse strip on the Results tab of the Cases page (open now and closed today with fourteen day trends, overdue, and a My day card with a progress ring against the workflow daily goal, the streak of days the goal was met, and the day against a typical day); a Queue health tab for supervisors (ageing, flow, bottlenecks, service levels, rhythm); and a Quality tab for model and rule owners (rule effort and precision, gains, calibration). The tabs share one workflow selector and the Take next button. The peek drawer carries the per case view: the contribution waterfall, the stays timeline and the since you looked list.
