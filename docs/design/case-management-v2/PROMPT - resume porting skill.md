# Prompt: resume the `jube-vue-port` skill

Run after unzipping `design_handoff_case_management_v2/` at the repository root.

---

The `design_handoff_case_management_v2/` folder is now present at the repository root. Confirm with `ls design_handoff_case_management_v2/skill-vue-blueprint/SKILL.md design_handoff_case_management_v2/design design_handoff_case_management_v2/specs design_handoff_case_management_v2/brand` before doing anything else.

Then finish `.claude/skills/jube-vue-port/`:

1. Read `skill-vue-blueprint/SKILL.md` in full. Replace every placeholder, inferred rule or "not available" note in `SKILL.md`, `styling.md`, `reactivity.md`, `components.md` and `migration-map.md` with the blueprint's actual text: D-V1 to D-V29 verbatim in §0; the complete component tree with every prop and emit from §1; composables, stores, QueryClient defaults and the ServiceChange invalidation map from §2; UI states, breakpoints and accessibility from §3; phases V0–V15 with gates from §4; the keep/drop table from §5; the six-step generation contract from §6; the security posture from §7.
2. Read `design/Decision log.md` and cite the decision id or endpoint ref beside every rule that lacks one.
3. Read `brand/Jube Brand and Style Guide.docx` (unzip and parse `word/document.xml`) for the exact hex values, type scale and voice rules; replace any guessed colours in the daisyUI theme.
4. Read both files under `specs/` for the endpoint reference ids and the Razor → Vue file map; reconcile against `migration-map.md`.
5. Write the five blocked recipes under `recipes/`, each complete, compilable and with its Vitest test.
6. Rewrite `open-questions.md`: remove every item that the folder has now answered; keep only genuine disagreements between the handoff and the code (file and line), plus the three carried items (default AI model, Similar cases screen, login route).

Rules unchanged: do not extend or redesign a decision; prototypes (`*.dc.html`) are design references only; Jube voice, British spelling, D-Vn or endpoint ref beside every rule. When finished, list changed files and the remaining open questions, and stop.
