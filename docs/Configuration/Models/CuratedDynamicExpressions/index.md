---
layout: default
title: Curated Dynamic Expressions
nav_order: 22
parent: Models
grand_parent: Configuration
---

# Curated Dynamic Expressions

Every extension method described in [Rule Compilation Tokens and Extensions](../RuleCompilationAlgorithm/index.html) is a real, compiled C# method shipped in a Jube release, which is to say that adding a new one means writing code, testing it and waiting for the next release. Curated Dynamic Expressions close that gap for short, self-contained pieces of logic (a validation, a lookup, a classification) that an administrator wants to make available to rules immediately, without a release, while keeping the same security guarantees the token-based rule compiler already relies on.

Once curated, an expression is used from a rule exactly in the manner of any other extension method:

``` vb
If Payload.Email.IsGmailEmail Then
    Return True
End If
```

`IsGmailEmail` is not a real method here, rather it is a row in the `DictionaryEvalExpression` table. Nothing else about the rule changes: the rule author writes plain VB.NET fluent syntax, with no visible reference to how the value is actually produced.

This example, and every curated name on this page, is illustrative of the mechanism only, and is not a promise that any particular name exists on a given instance. `IsGmailEmail` happens to be one of the rows the migration seeds, yet curated names can be added, renamed or removed by an administrator at any time, and `EnableDynamicEval` may be off. Before relying on a curated name in a rule, the live `DictionaryEvalExpression` table (or the effective `RuleScriptToken` allowlist) should be checked on the target instance, keeping in mind that a rule that references an unregistered curated name fails the soft-parse integrity check exactly as any other unknown token does, with no other warning.

## Where it lives

There is no page in the user interface for Curated Dynamic Expressions. An expression is a named row that an administrator writes directly into the `DictionaryEvalExpression` table in the database, consistent with how [`RuleScriptToken`](../RuleCompilationAlgorithm/index.html) rows are managed today.

## The DictionaryEvalExpression Table

| Column | Description |
|--------|-------------|
| `Id` | Primary key. |
| `Name` | The bare identifier rules use, e.g. `IsGmailEmail`. Must not collide with a real extension method or another curated name. |
| `Expression` | The expression body, evaluated against a single bound parameter named `value`, e.g. `value.EndsWith("gmail.com")`. |
| `ResultTypeId` | `1`=String, `2`=Integer, `3`=Double, `4`=DateTime, `5`=Boolean, being the same `ReturnDataTypeId` convention already used by `EntityAnalysisModelInlineFunction` and `EntityAnalysisInlineScript`, reused here rather than introducing a second, differently-shaped type code. |
| `Compiled` | Written back on every background model synchronisation cycle: `1` if the row compiled successfully, `0` if it did not. |
| `CompileError` | The compiler error text when `Compiled` is `0`, otherwise `null`. The same `Compiled`/`CompileError` convention used by Activation, Gateway and Abstraction Rules. |

The migration that creates this table (`AddImplicitAsyncRecallAndTimeoutFeatures` in `Jube.Migrations`) also seeds one worked example per `ResultTypeId`, so there is always a concrete, testable row of each supported type present at install time:

| Name | Expression | ResultTypeId |
|------|------------|--------------|
| `ShoutString` | `value.ToUpper()` | 1 |
| `WhatIsLength` | `value.Length` | 2 |
| `HalfLength` | `value.Length / 2.0` | 3 |
| `ParseStringAsDate` | `DateTime.Parse(value)` | 4 |
| `IsGmailEmail` | `value.EndsWith("gmail.com")` | 5 |

The rows are present while `EnableDynamicEval` is off also, since being off only stops rows being registered as rule tokens, not their existence in the table. This is a fixed set from a single migration, though: nothing stops a later migration, or an administrator, from adding, renaming or removing rows, so even this seeded set should be confirmed against the live table rather than assumed to be there.

## Enabling the Feature

The whole feature is off by default, gated by the `EnableDynamicEval` Environment Variable, which is `False` by default (see [Environment Variables](../../../Concepts/EnvironmentVariables/index.html)). This is a single, blunt switch: with it off, no curated names exist as far as the rule parser and the runtime are concerned, no matter what is sitting in `DictionaryEvalExpression`.

## Example

To make a curated name available that tests whether a string ends with a particular domain, a row is inserted as follows:

```sql
insert into "DictionaryEvalExpression" ("Name", "Expression", "ResultTypeId")
values ('IsExampleEmail', 'value.EndsWith("example.com")', 5)
```

Upon the next background model synchronisation cycle the row is compiled, and the outcome can be inspected:

```sql
select "Name", "Compiled", "CompileError"
from "DictionaryEvalExpression"
```

The two columns are read as follows: a `Compiled` of 1 with an empty `CompileError` means the expression is held by the engine and may be used; a `Compiled` of 0 means it may not, the `CompileError` giving the reason in the words of the interpreter; and a row whose `Compiled` is empty has yet to be reached by a synchronisation cycle, in which case it is a matter of waiting for the next. In the user interface the outcome is seen only indirectly, in the Coder of a rule: a rule using a name that is not available, whether because the row did not compile, the cycle has yet to run or `EnableDynamicEval` is False, reads Errors beneath the Coder, as described in [Rule and Code Builder](../../../Navigation/RuleBuilderAndCoder/index.html), and is refused upon being saved.

With `Compiled` showing as 1, and `EnableDynamicEval` being True, the name is thereafter available to rules:

``` vb
If Payload.Email.IsExampleEmail Then
    Return True
End If
```

## How It Stays Safe

Rule text in Jube is soft-parsed against a token allowlist before being reflection-compiled to native code (see [Rule Compilation Tokens and Extensions](../RuleCompilationAlgorithm/index.html) for the full algorithm). Curated Dynamic Expressions are layered on top of that mechanism rather than around it, along two independent lines of defence:

1. The expression itself runs in a sandboxed interpreter, not the rule compiler. Each `Expression` is compiled once, at background model synchronisation time, by an embedded expression interpreter. Only members of `value` (the bound parameter) plus `System.Math`, `System.Convert` and LINQ `Enumerable` are nameable in an expression, such that a fully-qualified name such as `System.IO.File` or `System.Diagnostics.Process` is an unknown identifier, since no other namespace or type is referenced. Ordinary metadata calls such as `value.GetType()` work (every object has one), but nothing reachable from a `Type` can be used to discover or invoke members dynamically, in that `.Assembly`, `.GetMethods()` and similar are rejected outright by the interpreter with a reflection-not-allowed error. Taken together this is a materially smaller surface than the VB.NET rule compiler itself, which compiles to a real assembly with the full breadth of referenced .NET libraries available to it.
2. The underlying mechanism is never a reachable rule token, regardless of `EnableDynamicEval`. A curated name such as `IsGmailEmail` is only ever expanded into its real, generic-typed call after the soft parser has already validated the original, unexpanded text of the rule author against the token allowlist, so the expanded syntax never itself has to pass that check. The mechanism name that performs this expansion is permanently excluded from the token allowlist, in every configuration, so there is no way to reach it directly by typing it in a rule, and only a name already curated in `DictionaryEvalExpression` can ever be rewritten into a call to it. Enabling `EnableDynamicEval` only ever adds more named tokens (one per curated row) to the allowlist, the same way adding a [`RuleScriptToken`](../RuleCompilationAlgorithm/index.html) row does, and it never weakens the token check itself.

## Compilation and Synchronisation

Curated expressions follow the same "compile once, cache forever" principle as reflection-compiled rules: every row is compiled exactly once per background model synchronisation cycle (see [Environment Variables](../../../Concepts/EnvironmentVariables/index.html) for `ModelSynchronisationWait`) and the compiled result is held in memory, keyed by `Name`, for real-time recall, henceforth there is no compilation cost on the transaction path. A row that fails to compile (an unrecognised `ResultTypeId`, or an expression the interpreter cannot parse) has its `Compiled`/`CompileError` columns updated to record why, exactly as a failed Activation or Gateway Rule does, and is left unavailable to rules until corrected. It does not stop other rows, or any other part of model synchronisation, from proceeding.

A new or edited row only becomes usable in a rule once the token allowlist is rebuilt from the current set of curated names, which happens as part of the same background model synchronisation cycle, and it follows that a rule saved in the same cycle a row is added may not yet see it. This mirrors how a new `RuleScriptToken` row also only takes effect from the next synchronisation cycle onward.

## Limits

This is deliberately minimal, in the hope it is helpful, but being no substitute for promoting to proper extension methods in Jube:

- There is no administrative user interface, and rows are written directly to `DictionaryEvalExpression`, the same as `RuleScriptToken` today.
- Only the five `ResultTypeId` values already used elsewhere in the application (`1`=String, `2`=Integer, `3`=Double, `4`=DateTime, `5`=Boolean) are supported.
- An expression is a single statement-free body over one bound value. There is no equivalent of the multi-statement scripts of Inline Functions here, by design, as this feature is for small, easily reviewed pieces of logic, not general scripting.

## Permissions and security

No permission of the user interface governs Curated Dynamic Expressions, as the table is written only by an administrator having direct access to the database. A rule making use of a curated name is subject to the permission of the rule in question.

{% comment %}TODO(verify): the examples in this page have not been run against the current compiler. Confirm on a running system, with EnableDynamicEval set to True, that Payload.Email.IsGmailEmail compiles in an Activation Rule.{% endcomment %}
