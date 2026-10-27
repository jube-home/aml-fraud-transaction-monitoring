# UI states, validation, breakpoints, accessibility

Blueprint §3 verbatim. Every component handles each applicable state and attribute.


### 3.1 UI states (one pattern each, from D3)

| State | Rule | Skeleton or indicator |
|---|---|---|
| Loading | Shown after 150 ms (no flash). After 3 s the label becomes "Still loading {section}…". | `skeleton` blocks at the final size: grid is 10 rows × 28/36 px by density; case header 2 lines; ActivationCard 1 strip + 180 px chart; JubeChart at its `height`. |
| Empty | Say what to do next, with one action. | Lucide `inbox` + sentence + link. Cases filter: "No cases in this filter." Journal: "No transactions in this window. Widen to 90 days." Prior cases: "No earlier cases for this case key." |
| Failed | Say how to fix it + Retry + `ref` (the correlation id from ProblemDetails `traceId`). | `alert alert-error` inside the section only; the page never goes blank. |
| Stale | Last good values + "as at hh:mm" + Refresh. | `alert alert-warning` compact line. |
| Restricted | "Restricted link": no id, name or status. | `badge badge-ghost` + tooltip. Never show a 403 page for a nested item. |
| Offline | ConnectionBanner; operations disabled; drafts kept. | — |
| Case changed | LiveChangeBanner; changed section outlined; open lightbox shows the refreshed preview and "I've seen the change". | — |
| Graph over cap | "Over 150 nodes: hide hubs or use 1 hop" with both buttons. | — |
| Model not explainable | "No explanation available for this model" (never a fabricated chart). | — |
| Payment outcome unknown | "Outcome unknown", with "Retry safely (same key)". Never "Failed". | — |

### 3.2 Validation rules

- **Typed reasons** (BreakLock, Reject, RefusePayment, ReleasePayment, ChangePriority, StopClock, emergency config change): trimmed length ≥ 10. The error shows on blur, then live.
- **Close:** outcome required; typology required when outcome ∈ {TP, Suspicious}; rationale ≥ 20 characters or a template; submit disabled while `preview.unmet.length > 0`. "Do it now" resolves one unmet item through its own operation.
- **File:** approval state `Approved` is required. Scorecard red items block; amber needs an acknowledgement each.
- **Payment decision:** `decision ∈ {1,2,3}`; reason ≥ 10; evidence note selected or "No customer contact" chosen explicitly. If `decision = Release` and `amount > user.releaseLimit`, the verb becomes "Request release" and the operation becomes `RequestRelease`. Never bulk (G10).
- **Extend hold:** the new expiry must not pass the ceiling from the resolved `PaymentHoldPolicy` row (`MaxHold` in `HoldUnit` from `HoldClockStart`); a next step is required. If the row is EscalateOnly with no ceiling, only the reason and next step are required.
- **Step-up code:** exactly 6 digits (TOTP). Three failures end in a 60 s lockout, shown as a countdown.
- **DTO forms:** client rules mirror the attributes, and the server is authoritative. Save is disabled while not `dirty` or while any visible required field is empty.
- **Mentions:** `@` opens a user picker limited to users with access to the case. An unknown mention is plain text, never an Inbox item.
- **Double-submit:** every primary button is disabled while `phase ∈ {previewing, executing}`; the idempotency key protects anything that slips through.

### 3.3 Responsive breakpoints

| Width | Layout |
|---|---|
| ≥ 1920 | Case page with three columns (key panel 280, main fluid, right 380); Journal and Network side by side are allowed. |
| 1280–1919 | Three columns (key panel 240, right 340). This is the design baseline, 1366. |
| 1024–1279 (read-and-act) | Two columns: key panel collapses into a header popover; right column becomes a bottom sheet. Journal, Network, Compose and configuration show "Open on desktop" with a copy-link button. |
| < 1024 (read-and-act, phone) | One column: Summary, HeldPaymentCard, Activations (read-only), Inbox, approvals. Lightboxes become full-screen `modal-bottom`, and targets are ≥ 44 px. The PaymentDecisionForm and ApproverView work fully. |
| Team wall | Fixed 1920×1080 (kiosk), scales with CSS `zoom` to fit; no interaction. |

### 3.4 Accessibility

- **Landmarks:** `header` (TopNav), `nav` (SectionNav), `main` (CaseMain), `aside` (CaseKeyPanel, RightColumn), each with `aria-label`.
- **Grid:** Tabulator gets `role="grid"`, `aria-rowcount`, `aria-selected`. Row Enter = `row-activate`. Arrow keys move the row. In-cell buttons are reachable with Tab and have `aria-label="Tag ARC-771203"`.
- **Lightbox:** `role="dialog" aria-modal="true" aria-labelledby` = title id. Focus moves to the first invalid input, or the first control. Focus is trapped and returns to the invoker on close. Esc closes (in the result phase, only after the result is shown). The StepUpPanel takes focus when it appears, and "Back to the form" returns focus to Submit.
- **Drawers:** `role="dialog"` (non-modal Peek: `aria-modal="false"`), with Esc to close and focus returned to the row.
- **Live regions:**
  - Toasts: `aria-live="polite"`.
  - ConnectionBanner and "Outcome unknown": `role="alert"`.
  - Hold countdowns announce only at 60, 15 and 5 minutes (G11), never every second.
  - The step-up expiry warns 60 s before, with "Keep verified".
- **Colour never carries meaning alone:** risk chips carry band text; Merge lanes carry letters; diary states carry words; tag pills carry text.
- **Contrast:** 4.5:1 for text in both themes and both densities (compact 12 px minimum). Focus ring `outline-2 outline-offset-2` in the `primary` token.
- **Charts:** `JubeChart` wraps the SVG in `role="img"` with `aria-label={title}` and a visually hidden `<table>` fallback when the endpoint provides `?format=table`.
- **Keyboard map (single source):** Ctrl/Cmd+K palette · `g i` inbox · `g t` team · `?` sheet · Space peek · W skim · x select · H decide payment · 1–6 sections · R risk · D close · F file · c compose · Alt+J AskJooby · Alt+← back · Ctrl+Enter submit · Esc close. Unavailable keys open a popover explaining why.

---