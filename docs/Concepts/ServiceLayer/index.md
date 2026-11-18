---
layout: default
title: Service Layer & DTO Model
nav_order: 12
parent: Concepts
has_children: true
---

# Service Layer & DTO Model

[Validation Patterns](../SoftwareValidations/index.html) describes the original vertical slice of Jube, being `Controller > Repository > Data Context (ORM) > Database`, with the DTO existing solely as the request and response serialisation contract for a single MVC controller. The pattern is now `Endpoint (Minimal API) > Service > Repository > Data Context > Database`.

The service layer is not merely the logic of the controller moved sideways, and the DTO in front of it is not merely a request and response serialisation contract. The DTO is instead the single declarative source that a generic, entity-agnostic user interface and an Agentic AI tool host both read off directly, such that as little as possible of the grouping, layout, validation, permission shape and AI-tool metadata of an entity has to be written by hand for each entity ever again. This page explains that model. No MVC controllers exist, which is to say that the repository, query, helper, authentication, mock and invoke areas are all minimal endpoints.

## Where the pieces live

| Project              | Holds                                                                                                                                                                                                                                                     |
|----------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Jube.Dto`           | The DTOs themselves, the `Jube.Dto.Forms` declarative form attributes, and the `Jube.Dto.Interfaces` marker interfaces described below.                                                                                                                   |
| `Jube.Validations`   | One `AbstractValidator<TDto>` per DTO (FluentValidation). See [Validation Patterns](../SoftwareValidations/index.html) for how this fits the request pipeline.                                                                                         |
| `Jube.Service`       | One `<Area>Service` per area (permission checks, mapping, repository calls, `OperationScope`, see [Logging](../Logging/index.html)), the `Jube.Service.Agent` attributes, and the `ServiceToolCatalogue` that lists every operation exposed to an agent. |
| `Jube.App/Endpoints` | A thin Minimal API endpoint group per area. It has no business logic, in that it resolves identity from the authenticated principal, calls the service, and maps typed exceptions to the frozen HTTP status codes.                                               |

A DTO cannot live in `Jube.Service` because the service constructs its validator, and the validator needs the DTO. Putting the DTO in its own project (`Jube.Dto`), with no dependency on either, avoids a circular reference between `Jube.Service` and `Jube.Validations`.

## The DTO as the single declarative contract

Every field on a DTO carries a `[Description]` (also used verbatim as the agent-tool argument description, as described below) plus form layout metadata describing how it is grouped, laid out, and behaves in the user interface. Class-level attributes describe the entity as a whole, as follows, abbreviated from `Jube.Dto/EntityAnalysisModel/EntityAnalysisModelDto.cs`:

```csharp
[FormEndpoint("EntityAnalysisModel")]
[FormApproval]
[FormRoles]
[FormKeys(Id = nameof(Id), NaturalKey = nameof(Name))]
[LockField(nameof(Locked))]
[FormGroup("Identity", Order = 10)]
[FormGroup("Entry & Reference Date", Order = 20)]
[FormGroup("Cache", Order = 30, Collapsed = true)]
// ... one FormGroup per section ...
public class EntityAnalysisModelDto : IUpdated, IActivatable, ILockable, IGuidIdentified
{
    [Description("Display name of the model. Unique within the tenant (case-insensitive).")]
    [FormField(Group = "Identity", Order = 10)]
    [ListColumn(Order = 10, Title = "Name")]
    public string Name { get; init; }

    [Description("JSONPath specifying the location of the reference date in the HTTP POST body. Required " +
                 "unless Reference Date Payload Location is Now.")]
    [FormField(Group = "Entry & Reference Date", Order = 50)]
    [VisibleWhen(nameof(ReferenceDatePayloadLocationTypeId), (byte)1)]
    [RequiredWhen(nameof(ReferenceDatePayloadLocationTypeId), (byte)1)]
    public string ReferenceDateXPath { get; set; }

    // ...
}
```

Nothing here is transcribed by hand into a page for each entity. It is metadata that a single generic component of the user interface reads at runtime to render the form, being served to the browser by `GET /api/FormModel/{area}`, and that a single agent-tool host reads at startup to build the parameter schema of the tool, which is to say that the same attributes serve both consumers.

It is worth setting out what is a contract, and what is presentation. Field behaviour (`ReadOnly`, `VisibleWhen`, `EnabledWhen`, `RequiredWhen`, `Lookup`, `ListColumn`) and the keys and endpoint of the entity (`FormKeys`, `FormEndpoint`, `LockField`) are a contract, upon which the behaviour of the entity depends. Grouping, ordering and labelling (`FormGroup`, and the `Group`, `Order` and `Label` on each `FormField`) are not, being a matter of the user interface in which groups are chosen for task-based sense (AML and transaction monitoring norms).

## The Forms attribute set

The attributes are defined in `Jube.Dto/Forms/`, one type per file. All are opt-in and additive, and a DTO uses only the ones its shape needs. The principal attributes are as follows:

| Attribute                                                                           | Target               | Purpose                                                                                                                                            |
|-------------------------------------------------------------------------------------|----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|
| `[FormEndpoint(area)]`                                                              | class                | The API area the generic form talks to (`/api/<area>`).                                                                                            |
| `[FormKeys]`                                                                        | class                | Names the `Id` property, an optional `Parent` (for tree-shaped entities), and an optional `NaturalKey` used for duplicate-name checks and display. |
| `[LockField(property)]`                                                             | class, repeatable    | Names the boolean property that, when true, disables editing. Pairs with `ILockable`.                                                          |
| `[FormGroup(path)]`                                                                 | class, repeatable    | Declares one section of the form: `Order`, optional `Label`, `Collapsed`, and a section-level `Widget` override.                                   |
| `[FormField]`                                                                       | property             | Places a field in a `Group` at an `Order`, with an optional `Label`/`Widget`/`Placeholder`/`Help`, and `ReadOnly`.                                 |
| `[VisibleWhen(property, values...)]` / `[EnabledWhen(...)]` / `[RequiredWhen(...)]` | property, repeatable | Conditional show/enable/require, keyed off another property's value(s); `Op` (`ConditionOp`) and `MatchMode` control the comparison.               |
| `[Lookup(source)]`                                                                  | property             | Declares the field as a reference picker against another area's rows (`TextField`/`ValueField`, optional `ParentField` for cascading lookups).     |
| `[ListColumn]`                                                                      | property             | Declares the field as a column in the entity's list/grid view (`Order`, `Title`, `Sortable`, `Filterable`, `Hidden`).                              |
| `[NewDefault(value)]`                                                               | property             | The value a new (not-yet-saved) row starts with in the form.                                                                                       |


Further attributes in the same directory declare the remaining behaviour of an area, among them `[FormApproval]` and `[FormRoles]`, `[FormChild]` for the child types presented beneath a record, `[Options]` and `[FormValues]`, the grid attributes (`[GridEndpoint]`, `[GridFilter]`, `[GridSearch]`, `[GridTimeRange]`, `[GridRatio]`), and the editor attributes (`[Editor]`, `[RuleEditor]`, `[RuleTools]` and those particular to one area) by which the generic form defers a group of fields to a bespoke control.
## The marker interfaces

The interfaces are defined in `Jube.Dto/Interfaces/`, and are small, composable shape contracts that a DTO implements only when it genuinely has that shape, such that the generic user interface can key generic behaviour (an audit-trail panel, a lock toggle, a parent and child hierarchy, a rule builder) off the interface rather than off special-casing for each entity. They are deliberately never bundled onto one "do everything" interface, and a new one is only added once a second DTO needs the same shape, not speculatively.

| Interface          | Members                                                                                                   | Backs                                                                                                                                                            |
|--------------------|-----------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `IUpdated`         | `Id`, `CreatedUser`, `CreatedDate`, `UpdatedUser`, `UpdatedDate`, `Version`, `DeletedUser`, `DeletedDate` | The generic audit-trail display and optimistic-concurrency handling.                                                                                             |
| `IActivatable`     | `Active`                                                                                                  | The generic active/inactive toggle.                                                                                                                              |
| `ILockable`        | `Locked`                                                                                                  | The generic lock toggle. Pairs with the class-level `[LockField]`.                                                                                           |
| `IGuidIdentified`  | `Guid`                                                                                                    | Entities addressed externally by a stable GUID (for example a model's invocation endpoint).                                                                      |
| `ITreeChild`       | *(marker only, no members)*                                                                               | Entities with a parent/child hierarchy; the actual parent-FK property name is area-specific and comes from `[FormKeys(Parent = ...)]` rather than the interface. |
| `IRuleBuilderJson` | `Json`, `BuilderRuleScript`                                                                               | The rule Builder shape used by every rule-authoring field across the platform (Abstraction Rules, Activation Rules, and others).                       |

The following DTOs illustrate the interfaces in use:

* `EntityAnalysisModelDto` implements `IUpdated, IActivatable, ILockable, IGuidIdentified`.
* `EntityAnalysisModelRequestXPathDto` implements `IUpdated, IActivatable, ILockable, ITreeChild`, each row being a child of a Model (`EntityAnalysisModelId`, declared via `[FormKeys(Parent = nameof(EntityAnalysisModelId))]` rather than the interface itself).
* `EntityAnalysisModelInlineScriptDto` is likewise an `ITreeChild`, and makes use of `[Lookup]`, in that its `EntityAnalysisInlineScriptId` field is a reference picker against the `EntityAnalysisInlineScript` area (a read-only catalogue that is not tenant-scoped, whose rows are created and compiled outside the API, by direct database entry picked up by the engine). It is a lookup keyed by id rather than by guid, which is the default of the attribute: `[Lookup("/api/EntityAnalysisInlineScript", TextField = "name", ValueField = "id")]`.
* `EntityAnalysisModelGatewayRuleDto` is an `ITreeChild` and an `IRuleBuilderJson`. Its `BuilderRuleScript` and `Json` pair backs the same rule Builder that the Abstraction and Activation Rules use, and the class is marked `[RuleEditor]`, naming those two properties together with the sibling `RuleScriptTypeId` and `CoderRuleScript` that the interface does not cover, such that the generic form defers the whole group to the bespoke rule-authoring control rather than rendering individual inputs.
* `EntityAnalysisModelSanctionDto` is an `ITreeChild` with a further use of `[Lookup]`, albeit of a shape outside that for which `[Lookup]` is designed. Its `MultipartStringDataName` field is a picker in which the value and the label are the same string, against the `GetEntityAnalysisPotentialMultiPartStringNames` query, henceforth `TextField` and `ValueField` both point at the same bare `value` property rather than an id and name pair: `[Lookup("/api/GetEntityAnalysisPotentialMultiPartStringNames", TextField = "value", ValueField = "value", ParentField = nameof(EntityAnalysisModelId))]`.

## Agentic AI tooling

The same DTO and service metadata that drives the user interface also exposes every service operation as a tool that an LLM agent can call, with no behavioural change to the operation itself:

```csharp
[Description("Returns one Model by its numeric identifier, scoped to the calling user's tenant. Returns " +
             "null when the model does not exist or is not visible to the caller.")]
[ServiceOperation("EntityAnalysisModelGet", OperationKind.Read, Idempotent = true)]
public async Task<EntityAnalysisModelDto?> GetByIdAsync(
    [Description("Numeric identifier of the model.")]
    int id,
    CancellationToken token = default)
{
    using var op = OperationScope.Start("EntityAnalysisModel", "Get", userName, tenantRegistryId, auditLog, log,
        serviceChangeBus);
    // ... EnsurePermitted, repository call, OperationScope.Entity(id), map to DTO ...
}
```

The parts are as follows:

* `[ServiceOperation(name, kind)]`, where `name` is a globally unique, `PascalCase` tool name (`<Area><Operation>`, no underscores), `kind` is `OperationKind.Read`, `Write` or `Delete`, and `Idempotent` and `Destructive` are safety hints that an agent host uses to decide whether a call needs confirmation.
* `[Description]` on the method and each parameter, which is the same attribute used for the help text of a form field, and is reused verbatim as the tool and argument descriptions.
* The registration of every operation, once, in `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.<Area>.cs`:

```csharp
new ServiceToolDescriptor(
    "EntityAnalysisModelGet", OperationKind.Read, Idempotent: true, Destructive: false,
    "Returns one Model by id, scoped to the caller's tenant."),
```

Identity is never a tool argument. `userName` and `tenantRegistryId` are bound at the composition root from the authenticated principal (HTTP claims, or the authenticated service account of an agent session), and it follows that an LLM can influence which rows the business fields of a DTO describe, yet never the tenant or user as which it acts.

Tracing, metrics and the change-event stream that back this layer are documented in [Environment Variables](../EnvironmentVariables/index.html) (`EnableOpenTelemetry`, `EnableServiceChangeStream`), both of which are opt-in and do nothing when disabled, whereas the structured audit record written by `OperationScope` on every call (reads included) is unconditional.

## Status

The pattern is applied throughout, there being no MVC controllers or pages rendered in the server. The generic forms of the Vue user interface consume the Forms attributes at runtime, and the agent host consumes the `ServiceToolCatalogue`. The pure function tools offered to an agent alongside the service operations are described in [Extensions and Helpers Agent Tools](Extensions/index.html).

For the exhaustive, always-current member list of any attribute or interface described above, the source in `Jube.Dto/Forms/` and `Jube.Dto/Interfaces/` is authoritative, each being a small, single-purpose, low-churn type (one per file), quicker to read directly than to keep a second description of in sync here.
