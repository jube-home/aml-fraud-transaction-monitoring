---
layout: default
title: Curated Dynamic Expressions
nav_order: 22
parent: Models
grand_parent: Configuration
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# Curated Dynamic Expressions

Every extension method described in [Rule Compilation Tokens and Extensions](../RuleCompilationAlgorithm/index.html)
is a real, compiled C# method shipped in a Jube release — adding a new one means writing code, testing it and waiting
for the next release. Curated Dynamic Expressions close that gap for short, self-contained pieces of logic (a
validation, a lookup, a classification) that an administrator wants to make available to rules immediately, without a
release, while keeping the same security guarantees the token-based rule compiler already relies on.

An expression is a named row an administrator writes directly into the `DictionaryEvalExpression` table — there is no
administrative UI for this yet, consistent with how [`RuleScriptToken`](../RuleCompilationAlgorithm/index.html) rows are
managed today. Once curated, it is used from a rule exactly like any other extension method:

``` vb
If Payload("Email").IsCorporateEmail Then
    Return Matched
End If
```

`IsCorporateEmail` is not a real method here — it is a row in `DictionaryEvalExpression`. Nothing else about the rule
changes: the rule author writes plain VB.NET fluent syntax, with no visible reference to how the value is actually
produced.

## The DictionaryEvalExpression Table

| Column         | Description                                                                                                                                                                                                                                                               |
|----------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Id`           | Primary key.                                                                                                                                                                                                                                                              |
| `Name`         | The bare identifier rules use, e.g. `IsCorporateEmail`. Must not collide with a real extension method or another curated name.                                                                                                                                            |
| `Expression`   | The expression body, evaluated against a single bound parameter named `value` — e.g. `!value.EndsWith("gmail.com")`.                                                                                                                                                      |
| `ResultTypeId` | `1`=String, `2`=Integer, `3`=Double, `4`=DateTime, `5`=Boolean — the same `ReturnDataTypeId` convention already used by `EntityAnalysisModelInlineFunction` and `EntityAnalysisInlineScript`, reused here rather than introducing a second, differently-shaped type code. |
| `Compiled`     | Written back on every background model synchronisation cycle: `1` if the row compiled successfully, `0` if it did not.                                                                                                                                                    |
| `CompileError` | The compiler error text when `Compiled` is `0`; `null` otherwise. The same `Compiled`/`CompileError` convention used by Activation, Gateway and Abstraction Rules.                                                                                                        |

The migration that creates this table also seeds one worked example per `ResultTypeId` (`Shout`, `Length`,
`HalfLength`, `ParseAsDate`, `IsCorporateEmail`) so there is always a concrete, testable row of each supported type
present — including while `EnableDynamicEval` is off, since being off only stops rows being registered as rule tokens,
not their existence in the table.

## Enabling the Feature

The whole feature is off by default, gated by `EnableDynamicEval` (default `False` — see
[Environment Variables](../../../Concepts/EnvironmentVariables/index.html)). This is a single, blunt switch: with it
off, no curated names exist as far as the rule parser and the runtime are concerned, no matter what is sitting in
`DictionaryEvalExpression`.

## How It Stays Safe

Rule text in Jube is soft-parsed against a token allowlist before being reflection-compiled to native code (see
[Rule Compilation Tokens and Extensions](../RuleCompilationAlgorithm/index.html) for the full algorithm). Curated
Dynamic Expressions are layered on top of that mechanism rather than around it, along two independent lines of defence:

1. **The expression itself runs in a sandboxed interpreter, not the rule compiler.** Each `Expression` is compiled once,
   at background model synchronisation time, by an embedded expression interpreter. Only members of `value` (the bound
   parameter) plus `System.Math`, `System.Convert` and LINQ `Enumerable` are nameable in an expression - a
   fully-qualified name such as `System.IO.File` or `System.Diagnostics.Process` is simply an unknown identifier, since
   no other namespace or type is referenced. Ordinary metadata calls such as `value.GetType()` work (every object has
   one), but nothing reachable from a `Type` can be used to discover or invoke members dynamically - `.Assembly`,
   `.GetMethods()` and similar are rejected outright by the interpreter with a reflection-not-allowed error. Taken
   together this is a materially smaller surface than the VB.NET rule compiler itself, which compiles to a real assembly
   with the full breadth of referenced .NET libraries available to it.
2. **The underlying mechanism is never a reachable rule token, regardless of `EnableDynamicEval`.** A curated name like
   `IsCorporateEmail` is only ever expanded into its real, generic-typed call *after* the soft parser has already
   validated the rule author's original, unexpanded text against the token allowlist — so the expanded syntax never
   itself has to pass that check. The mechanism name that performs this expansion is permanently excluded from the token
   allowlist, in every configuration, so there is no way to reach it directly by typing it in a rule; only a name
   already curated in `DictionaryEvalExpression` can ever be rewritten into a call to it. Enabling `EnableDynamicEval`
   only ever adds more *named* tokens (one per curated row) to the allowlist, the same way adding a
   [`RuleScriptToken`](../RuleCompilationAlgorithm/index.html) row does — it never weakens the token check itself.

## Compilation and Synchronisation

Curated expressions follow the same "compile once, cache forever" principle as reflection-compiled rules: every row is
compiled exactly once per background model synchronisation cycle (see
[Environment Variables](../../../Concepts/EnvironmentVariables/index.html) for `ModelSynchronisationWait`) and the
compiled result is held in memory, keyed by `Name`, for real-time recall — there is no compilation cost on the
transaction path. A row that fails to compile (an unrecognised `ResultTypeId`, or an expression the interpreter cannot
parse)
has its `Compiled`/`CompileError` columns updated to record why, exactly as a failed Activation or Gateway Rule does,
and is simply left unavailable to rules until corrected — it does not stop other rows, or any other part of model
synchronisation, from proceeding.

A new or edited row only becomes usable in a rule once the token allowlist is rebuilt from the current set of curated
names, which happens as part of the same background model synchronisation cycle - a rule saved in the same cycle a row
is added may not yet see it. This mirrors how a new `RuleScriptToken` row also only takes effect from the next
synchronisation cycle onward.

This is deliberately minimal in the hope it is helpful but being no substitute for promoting to proper extension methods
in Jube:

- No administrative UI — rows are written directly to `DictionaryEvalExpression`, the same as `RuleScriptToken` today.
- Only the five `ResultTypeId` values already used elsewhere in the application (`1`=String, `2`=Integer, `3`=Double,
  `4`=DateTime, `5`=Boolean) are supported.
- An expression is a single statement-free body over one bound value — there is no equivalent of Inline Functions'
  multi-statement scripts here, by design: this feature is for small, easily reviewed pieces of logic, not general
  scripting.
