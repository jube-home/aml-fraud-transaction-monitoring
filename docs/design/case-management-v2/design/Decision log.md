# Jube v2: decision log

For the build team. The product document (`Jube v2 - What is being built.md`) describes only what is being built; this file keeps how each choice was reached, what the code audit answered, and what is still open. 4 October 2026.

**Still open:** Egypt values in the payment hold policy (compliance; until then EscalateOnly with no payer notice); verify the 12 existing route names against the code before stage 1.

## 1. What changed in passes 2 and 3

Pass 2 applies decisions D18–D27 and proposals P11–P17 from spec 0.11. Pass 3 adds held payments, step-up and transaction tags from review.

| Area | Change in the prototype | Spec |
|---|---|---|
| Ordering | The single priority number is gone. The Cases page shows a **Lane** column and a **risk chip** (`Likely 56% · High · 62`) and sorts by lane, then Priority. C-9044 (Live money, Possible 48%, Moderate, 38) now sorts **first**. | 4.5, P11–P16 |
| Risk popover | Hovering the chip on the Cases page shows lane, likelihood, impact, urgency and Priority. The case header chip (`R`) opens the full case risk popover: the likelihood waterfall with the dependence discount ("shares inputs, counted at 30%"), impact components, urgency and lane move, the Priority formula, and the analyst assessment. | 4.5.12 |
| Risk matrix | Optional header on the Cases page: likelihood × impact grid of the loaded cases | 4.5.6 |
| "Ranked" line | New line in Case detail views: "Standard lane · Likely (56%), mainly from StructNet Very high and 2 hops from confirmed TP C-8812 · impact High · Priority 62" | S1-09 |
| Risk history | New table in section 6: every case risk recompute with trigger, method and result | 4.5.8, S6-05 |
| StructNet | Now gradient-boosted trees, tier Exact. The waterfall is in log-odds and sums exactly: base −3.18, +2.20, +1.55, +1.10, 4 others +0.80, −0.16 = +2.31 → 0.91, with the threshold (0.80 = +1.39) drawn as a dashed line. The Bayesian causal path is a prototype variant (Tweaks → Decomposition). | D20, 7.3, 7.11 |
| Anchors | Structuring 9d and the sanctions match anchor on the 11th cash deposit, ARC-771190 (09:40). StructNet anchors on the P2P out to a new beneficiary, ARC-771203 (10:14:55). Case detail views resolve against the first anchor, ARC-771190. | D21 |
| Baselines | "Typical" (median) for amounts and ages; "normal rate" (mean) for counts; the daily normal line is the mean of daily totals over 90 d, excluding the examined window; the chart states that days are in tenant time (Cairo) | D22, D23 |
| Colour | Case-creating rules (◆) are shown by weight, not red. Red now means risk only: likely or very likely, confirmed TP, the Very high band. | D27 |
| Fix | The model facts drawer did not open in pass 1. It does now, in both prototypes. | — |
| Held payments | A held payment card in the peek and case summary; one decision lightbox (Release, Refuse, Extend hold); escalation to a supervisor instead of an automatic decision; a release limit on each user; the result shown step by step, including "Outcome unknown" | 14, H1–H5 |
| Step-up | Re-authentication inside the lightbox that asked for it, with nothing entered lost; authenticator code first, passkey second, no SMS; used by File, Approve, Break lock, Release and Refuse | 14.3, H2, H6 |
| Transaction tags | Tag pills on every Journal row and in the transaction card, from the tags set up in configuration; undo instead of a lightbox | 15 |

---


---

## 13. Decisions (answered 24 Sep 2026; extended 4 Oct 2026)

Q1–Q10 were answered in review. Q11–Q24 are taken now so that nothing in this document is waiting on a choice; each is a recommendation that stands unless you change it before build.

| # | Question | Decision | Effect on the design |
|---|---|---|---|
| Q1 | AskJooby docked panel (20.8 q1) | **Replaces the Decide cockpit**; Alt+J toggles back | Right column swaps between Decide and AskJooby; the suggested step stays reachable in the panel's header |
| Q2 | Merge lanes vs "colour for risk and state only" | **A neutral categorical palette, for Merge lanes only** | New token set (low-chroma, equal lightness), never red or amber; every lane still carries its key label |
| Q3 | Team view wall-screen mode (q6) | **Yes, as well as the desk view** | Large type, auto-refresh, no interaction; the risk matrix and next breaches dominate |
| Q4 | Icon set (q3) | **Lucide (ISC)** | Replaces the prototype's text glyphs (⚡ ◷ ⚓ ◆ ★ ⚑) |
| Q5 | Step-up methods (8.2) | Superseded by **H6** (14.5): Jube has no step-up today, so build TOTP first, then passkeys; no SMS | See H6 |
| Q6 | Referral form branding | **Tenant logo and name only, no Jube chrome** | A single page and a receipt; nothing of Jube visible |
| Q7 | Report status format | **Generic, not tied to any format** | Lifecycle and FIU rejection drawn against generic field names; the adapter maps them |
| Q8 | Arabic and English (q7) | **Latin transliteration beside the Arabic name only; interface in one language** | Names show "مرقس ستيرلينغ · Marcus Sterling"-style pairs; no doubled labels |
| Q9 | Compact density floor on the Journal (q5) | **28 px rows** | Compact Tabulator rows at 28 px, 12 px text, 4.5:1 contrast kept |
| Q10 | What next | **Done.** The proposed order was followed: Team view and risk matrix, Inbox and live case state, AskJooby in the cockpit slot, then the remainder (pass 4, section 18) | — |
| Q11 | Graph library at the 150-node cap (open question 2) | **Closed:** `GraphDiagram` for Flow and Hierarchy, a dependency-free force layout for Force, capped at 150 nodes (section 16) | No change to the canvas |
| Q12 | Inbound decisions | **Credit, Credit and hold outbound, Return, Extend.** Return needs step-up; Credit does not | 12.1 |
| Q13 | Tell the sending PSP when we return an inbound payment | **Yes, a minimised notice** (identifiers, reason category, our case reference), never the case narrative | Op: NotifyCounterpartyPsp |
| Q14 | Payee notice on inbound holds | **Off by default**; turning it on runs the tipping-off check | 12.1 |
| Q15 | Step-up on recall | **Request recall: no** (no money leaves). **Return funds on an inbound recall: yes** | 12.2 |
| Q16 | Partial returns on inbound recalls | **Allowed, up to the available balance**, with the shortfall and where it went shown | 12.2 |
| Q17 | Payee check as a model input | **Yes, as standard features** offered to every model; the raw scheme code is kept | 12.3 |
| Q18 | Who can choose a claim exception | **Any analyst may propose; gross negligence needs a supervisor; AskJooby and agents never choose an exception** | 12.4 |
| Q19 | Claim excess | **Tenant choice per scheme row, 0 by default**, and always 0 with an active vulnerability record | `ReimbursementScheme.MaxExcess` |
| Q20 | Step-up and limits on Reimburse | **Step-up yes, and the per-user release limit (H3) applies.** Decline needs maker-checker | 12.4 |
| Q21 | Vulnerability in risk models | **Excluded** from case risk and fraud models | 12.5 |
| Q22 | Who sees the vulnerability driver | **Needs are visible to anyone working the case; the driver and characteristic need an audited reveal** | 12.5 |
| Q23 | Read audit on Sensitive accounts | **Ask for a reason outside assignment, never block**; views kept 24 months by default | 12.7 |
| Q24 | Further ideas (12.8) | **Not built yet.** Share fraud signal is first in line once the EU sharing rules are final; the others follow on request | 12.8 |

**Already decided elsewhere and confirmed here:**
- **Egypt hold row:** EscalateOnly with no payer notice until compliance supplies values (section 16).
- **AskJooby personas:** build the **Analyst Agent** first, then the **Rule Writer**.
- **Suppressions:** expiry mandatory (maximum 90 days), maker-checker above 30 days, no step-up, Lists menu removed (22.6).

**Still a task, not a decision:** verify the 12 existing route names against the code before V1.

---


---

### 14.5 Hold decisions (settled 24 Sep 2026)

Where you asked for industry practice, the main reference is the UK regime, currently the most explicit one:
- Since the Payment Services (Amendment) Regulations 2024 came into force on 30 October 2024, UK PSPs can delay an outbound payment by up to four business days, but only where they have reasonable grounds to suspect fraud by someone other than the payer.
- The FCA reads this as requiring the PSP to execute as soon as it has contacted the payer or others and established that the payment should go ahead, not to wait for the end of the four days.
- The payer must be told the reason for the delay and what they need to do, no later than the end of the next business day.

For step-up, NIST SP 800-63B-4 requires systems at AAL2 to offer at least one phishing-resistant method, and says OTP and push codes don't count as phishing-resistant.

| # | Question | Decision | What to build |
|---|---|---|---|
| H1 | Hold expires undecided | **Escalate, don't auto-decide.** One hour before expiry (tenant-set), Status becomes **Hold expiring** and the case goes to the supervisor. It re-escalates every 30 min until someone acts. If the jurisdiction pack's hard maximum is reached, the pack's default applies (UK: release unless there's evidence to refuse, since the payment can't be held past the legal limit). | Status "Hold expiring" in the workflow; diary escalation rule; **email** to the supervisor with a deep link into the case (the Inbox gets the same item); a pack field `MaxHold` and `OnMaxHold` |
| H2 | Step-up on Release / Refuse | **Yes, both.** They are irreversible and leave Jube. **Extend hold does not**, because no money moves. | Add ReleasePayment and RefusePayment to the 8.2 list |
| H3 | Second person | **Per-user release limit.** It is set on the user (for example EGP 25,000). At or below the limit, Release goes ahead. Above it, Release becomes **Request release**, and a second user whose limit covers the amount approves it with step-up. Refuse has no limit, because it's the safe direction. | `User.PaymentReleaseLimit` (with currency); Op: RequestRelease; reuse the approver lightbox |
| H4 | Extension limit | **No count limit; a time ceiling from the jurisdiction pack.** UK: 4 business days from receipt of the payment order. Each extension records a reason and a next step (for example "awaiting call-back"). Tenants can set a shorter ceiling. | Pack fields `MaxHold` (business days) and `HoldClock` (from receipt); a reason and next step on Op: ExtendHold |
| H5 | Confirmation | **Synchronous HTTP.** The answer to the call is the confirmation. On timeout the outcome is unknown, and "Retry safely" re-sends with the same idempotency key. So the listener must treat a repeated key as the same request. | Idempotency-key contract in the HTTP Endpoint spec; timeout (default 30 s); execution log |
| H6 | Step-up methods | **Build TOTP first, passkey second.** Phase 1: an authenticator-app code (RFC 6238), with enrolment and recovery codes. Phase 2: **passkeys (WebAuthn)**, as the phishing-resistant option NIST requires. **Password re-entry only** where the tenant signs in through an external identity provider that does its own MFA; then step-up is a fresh IdP login (`prompt=login`, `max_age`). **No SMS**: it's phishable and SIM-swappable. | Enrolment screen per user; `POST /api/Authentication/StepUp/Begin` and `/Complete`; a step-up token valid 5 min (15 min for a run of payment decisions); an audit record on each CaseEvent |

**Payer notification** (UK): when a hold starts, the pack's notification template goes out automatically through the dispatcher, subject to the tipping-off check. The hold card shows when it was sent.

---


---

## 16. Open before build

The code audit (`ui-specification-golden-audit.md`, 27 Sep 2026) answered the verification items:
- **Tag routes:** `PUT /api/Invoke/Archive/Tag` writes `ArchiveTag`. Tag configuration is `EntityAnalysisModelTag`, per model.
- **Archived scores:** every transaction already carries its scores in `Archive.Json`. `ArchiveScore` (D24) is a queryable projection over existing data, not new capture.
- **Charting:** `Jube.Charting` provides `Chart.Column()`, `Chart.Sparkline()` and `Chart.Waterfall()` on the `accord-analytics` branch. The earlier "port to Radzen" line is withdrawn: all charts go through `Jube.Charting`.
- **Graph library (open question 2):** closed. `GraphDiagram` draws the Flow and Hierarchy layouts and snapshots; a small dependency-free force layout draws the Force view, capped at 150 nodes (the same cap as `ModelGraphBuilder`).
- **Who looked at the case (G12):** closed. A new `CaseEvent` type, `CaseViewed`, is written on Fetch, Skim and Peek.

**Answered against the code (27 Sep 2026):**
- **Session refresh:** `TokenRefreshMiddleware` extends the session automatically on every request, so there is nothing to call.
- **Current user:** `GET /api/Me` is new, as is the user's release limit.
- **Rule validation:** uses the existing `POST /api/Parser`.
- **Excel export:** moves client-side, which removes today's external `proxy.jube.io` download proxy.
- **SameSite:** `Strict` comes from the `SecureHttpCookie` setting.
- **Agent triage (M1):** off by default, opt-in per filter.
- **Web research (M4):** opt-in per tenant, with every query logged.
- **Order:** M2 and M3 are built first.

**Decided:** hold ceilings are controlled by a table with one row per market, and the payer notice is optional per market (see "Payment hold policy" below). Compliance fills in the Egypt row. Until they do, it defaults to EscalateOnly with no notice.


**Illustrative, not from the spec:**
- the case risk figures and history rows;
- the hold rule, ARC-771240, the CBS references, S. Okoro and the EGP 25,000 limit;
- all pass 5 data (M1–M6).

---


---

## 17. Critical review: gaps found against current practice

A last pass against current regulation and industry practice. Each item says what's missing and what to build. None of them changes a screen already designed; they add to it.

| # | Gap | Why it matters | What to build |
|---|---|---|---|
| G1 | **Inbound holds** | Everything designed holds an **outbound** payment. The mule side (money arriving at an account like ACC-02 Alpha LLC) is where networks are caught, and FCA guidance (FG24/6) also covers delaying inbound payments where fraud is suspected. | A direction on the held payment card ("Inbound to ACC-02"). For inbound, the decisions are **Credit to account** / **Return to sender** / **Extend hold**. Same lightbox, same step-up. |
| G2 | **Payment already gone: recall** | On instant rails (UK Faster Payments, SEPA Instant) the payment often completes before anyone looks, so there is nothing to hold. The action then is a recall or return request to the receiving bank. | Op: RequestRecall on an executed transaction (transaction card and cockpit), with its own diary for the receiving bank's answer. |
| G3 | **Verification of Payee result** | Since 9 October 2025, euro-area PSPs must offer Verification of Payee (non-euro EU: 9 July 2027). The UK equivalent is Confirmation of Payee. A payer who goes ahead after "no match" or "close match" is one of the strongest APP-scam signals. | Show the VoP/CoP result (match, close match, no match, other) on the held payment card and the transaction card. Feed it to the models as an input. |
| G4 | **APP reimbursement claims (UK)** | J1 ends with "flagged for reimbursement workflows [verify]". The UK rule has two clocks: **5 business days**, which can be paused ("stop the clock"), and an absolute **35 business days**. The receiving PSP must be notified within 2 hours. | A claim case type with a **pausable diary** and an **absolute diary** (the diary "paused" state is already on the not-designed list; this is where it's needed). Ops: StopClock (reason required), NotifyReceivingPSP, Reimburse (with the excess and the vulnerability exemption). |
| G5 | **Vulnerable customer** | UK Consumer Duty and the reimbursement rules treat vulnerable customers differently: no excess, and more care in contact. | A vulnerability flag with its source in the case key panel, shown in the payment lightbox and on the claim. |
| G6 | **Payer told about the hold** | The UK rules require the payer to be told the reason and what to do by the end of the next business day. The design sends the notice (14.5) but doesn't show it. | A line on the held payment card: "Payer notified 08:55 by SMS template 'Payment paused'", or "Not yet sent: due by end of 25 Sep". |
| G7 | **Payment system unavailable** | If core banking can't be reached, Release and Refuse can't complete. Queuing an irreversible decision silently is unsafe. | A banner in the lightbox, "Payment system unavailable since 08:41". Submit is disabled (never queued). The hold diary keeps running, and the supervisor escalation (H1) starts early. |
| G8 | **Step-up bound to what was approved** | Under PSD2 strong customer authentication, the authentication is linked to the exact amount and payee ("dynamic linking"). A 15-minute window alone would let a stolen session approve a different payment. | For payment decisions, the step-up token is bound to the **preview id** (amount, beneficiary, decision). A changed input means a new step-up. Other operations keep the time window. |
| G9 | **Bulk tagging in the Journal** | Tagging 11 structuring deposits one by one is slow. | Select several Journal rows, then **Tag…** (the same menu), as one operation with one undo. |
| G10 | **No bulk Release** | Releasing money in bulk defeats the per-user limit and the review. | Release is never bulk. Refuse is bulk only for payments to the same beneficiary (a mule), with one reason. |
| G11 | **Timers and accessibility** | WCAG 2.2 asks that time limits can be adjusted or warned about, and that frequent updates don't flood screen readers. | Step-up expiry warns 60 s before, with "Keep verified". Hold countdowns announce politely at thresholds only (1 h, 15 min), not every second. Tag pills never rely on colour alone. |
| G12 | **Who looked at the case** | Field reveals are audited (8), but ordinary case views are not. Staff browsing VIP or employee accounts is a standard insider-risk control. | A read audit for case and payload views, and a supervisor view of "viewed by". Check first whether conflict-of-interest (8.1) already covers this. |

**Build order suggestion:** G6, G7, G8, G10 and G11 are small and belong in the same phase as held payments. G1, G2, G3, G4, G5, G9 and G12 are designed in section 12 (pass 7), with their decisions in section 13.

---


---

## Ideas not yet built

These go beyond the gaps list. Each is small, reuses what is designed, and is not built unless you confirm (Q24).
- **Fraud data sharing:** the EU Payment Services Regulation will require PSPs to share fraud data, such as the IBANs of suspicious payees, with a data protection impact assessment. A **Share fraud signal** operation would send a minimised record (identifier, typology, date, case reference) to a configured sharing endpoint, with its own approval.
- **Mule account exit:** after a mule is confirmed, a checklist case covers closing the account, answering open recalls and claims from the remaining balance, and reporting to the national fraud database.
- **Scam type on claims:** a fixed scam-type list on the claim (purchase, investment, romance, impersonation of police or bank, invoice redirection, and so on) feeds regulatory returns and the outcome tile.
- **Hold-to-claim continuity:** when a released payment later becomes a claim, the claim shows the hold decision, who made it and the evidence, so the review can see whether the release was reasonable.


---

## Suppressions: Decisions taken (change any before build)

1. **Expiry is mandatory:** default 14 days, maximum 90 days per tenant, and no indefinite suppressions. Anything longer-lived is a rule-tuning or list problem.
2. **Maker-checker** for model scope or more than 30 days. MLRO approval for keys with a pending SAR or a confirmed TP in the last 12 months. Floor rules can never be suppressed.
3. **No step-up** for suppression. It is reversible and time-bound, and approval covers the risky cases.
4. **Tuning signal:** a rule with more than 20 active suppressions is flagged in rule tuning (M6) and on the Team view, because suppressing one key at a time hides a badly tuned rule.
5. **The Lists menu moves** from the main menu to model configuration. Customer-service roles keep **Suppressions**, and lose direct list editing.

---

## Corrections adopted from the code audit

- charts use `Jube.Charting`, not Radzen;
- AskJooby is described by capability, because the model is a deployment setting (the default is `deepseek-v4.1-flash`), not Phi-4 Mini;
- only the Researcher persona exists today, and the other five are new work;
- CodeQL and Playwright are new CI work;
- `Jube.Tests` has 7,526 tests;
- the widget spelling is `colour`;
- saving a session-modified filter is new (CP-19), but editing one already exists.
