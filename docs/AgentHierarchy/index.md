---
layout: default
title: Agent Hierarchy
nav_order: 1
parent: Ask Jooby
has_children: true
---

# Agent hierarchy over the Jube service layer

Generated from the catalogue, not hand-listed: every agent below owns tools that exist in `ServiceToolCatalogue`, every
change appears exactly once in the whole tree, and no agent holds more than fifteen. A read may appear more than once,
because an agent that can change an object holds the reads of that object as well -- the alternative was an author whose
only way to look anything up was free-form SQL against a schema it had to guess.

Every operation is also listed flat, one entry each, in [tool-inventory.md](tool-inventory.md). That inventory is the
backstop for this tree: when routing by meaning fails, an exact operation name, a service name or a permission number
resolves there instead.

## Shape

|                                      |                                                                                                     |
|--------------------------------------|-----------------------------------------------------------------------------------------------------|
| tools assigned                       | 1046, over 718 distinct operations                                                                  |
| leaf agents                          | 166                                                                                                 |
| supervisors                          | 33                                                                                                  |
| leaves attached straight to a domain | 10                                                                                                  |
| domains                              | 9                                                                                                   |
| delegation hops                      | root → domain → supervisor → leaf is three, exactly `RunContext.MaxDepth`; a collapsed slice is two |
| widest fan-out                       | 9, counting cross-domain reach; `RunContext.MaxFanOut` caps one spawn call at 8, not the roster     |
| largest toolbox                      | 15                                                                                                  |
| shared reads                         | 328 assignments, all idempotent and non-destructive; no change is shared                            |

### Cross-domain reach

A leaf owns an operation outright and cannot delegate, so an agent whose job needs an operation filed in another slice
has no route to it unless one is named. Where that happened the failure was never a refusal: the agent routed correctly,
worked out exactly what it needed, found it was not holding it, and answered from whatever it could reach instead -- a
rule author quoting a documentation example as if it were the caller's own schema, a datasource author hanging its work
on `ExampleVisualisation` because it could not create the registry it meant.

Reach into another slice is widened by an edge, never by giving a second agent the same tool, because every change is
owned by exactly one leaf and that is what makes the inventory below an address book. Sharing is confined to reads of an
agent's own slice, which is not reach at all: it is the author of an object being able to see the object it is about to
change. The edges are declared in one table in `AgentDefinitions.cs` (`CrossDomainReach`), each carrying the sentence
the agent is told, and they are marked **Cross-Domain Reach** on each agent below. Four kinds recur:

* the **completions** leaf, which is the only authority on which payload fields a model actually carries and on which
  operators the rule builder offers -- reached by every domain that authors something written against a field name;
* the **invocation context** leaf, which builds the rule-evaluation context that every test bench's `Execute` operation
  takes and none of them can build;
* the **synchronisation schedule** leaf, which is the closing step of all nine rule taxonomy graphs and where every rule
  fragment is compiled, filed under platform reliability rather than beside the rules;
* **sibling slices of one artefact**, where a datasource needs the registry it hangs off and the parameters it binds to.

Domains follow the application's own navigation menu in `_Layout.cshtml`, because that is hand-authored information
architecture with the permission gate already attached to each entry. A slice is split three ways into Inspector, Test
Bench and Author **only where it exceeds seven tools**; smaller slices stay whole, and small related slices are packed
together up to the same budget. The split is by what an agent may change, not by what it may see: the Author of a slice
holds that slice's whole read surface too, including its validation, and the Inspector remains the safe default because
it holds no change at all.

### Root

#### Jooby

- **Hierarchy Placement:** Level 1, root
- **Semantic Persona:** The entry point for every request. It owns no tools of its own and exists to route a question to
  the one domain that owns the data behind it.
- **Semantic Routing Keywords:** (routes everything)
- **Toolbox (Max 15):** none — routing only
- **Downstream Dependencies:** access-admin, assistant-admin, case-analyst, case-workflow-admin, data-analyst,
  model-admin, model-risk, model-support, platform-ops

---

## Access Control and Tenancy (`access-admin`)

#### Access Control and Tenancy

- **Hierarchy Placement:** Level 2, child of Jooby
- **Semantic Persona:** Owns identity, roles, permissions, API keys and tenancy. Deliberately separated from every
  functional domain so that the agent which grants access is never the agent that uses it.
- **Semantic Routing Keywords:** user, role, permission, tenant, API key, login, logout, entitlement, segregation of
  duties, access
- **Toolbox (Max 15):** none — routes to 3 supervisors covering 45 tools
- **Downstream Dependencies:** `access-admin.entitlement`, `access-admin.identity`, `access-admin.tenancy`

#### Entitlement Supervisor

- **Hierarchy Placement:** Level 3, child of Access Control and Tenancy
- **Semantic Persona:** Routes within the entitlement slice of access control and tenancy. Holds no tools; it exists so
  the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** entitlement, role, registry, permission, specification
- **Toolbox (Max 15):** none — routes to 7 leaf agents covering 19 tools
- **Downstream Dependencies:** `role-registry-inspector`, `role-registry-test-bench`, `role-registry-author`,
  `role-registry-permission-inspector`, `role-registry-permission-test-bench`, `role-registry-permission-author`,
  `permission-specification`

#### Role Registry Inspector (`role-registry-inspector`)

- **Hierarchy Placement:** Level 4, child of Entitlement Supervisor
- **Semantic Persona:** Owns the role registry slice and reads only; it never changes anything and is the safe default
  for any question about current state.
- **Semantic Routing Keywords:** role, registry, inspector
- **Toolbox (Max 15):**
    * `RoleRegistryList`: Lists Roles visible to the caller's tenant, keyset-paged and capped. _(permissions from
      `RoleRegistryService.ListPermissions`)_
    * `RoleRegistryFilterFields`: Lists the fields a query builder JSON filter over Role Registrys may use. _(
      permissions from `RoleRegistryService.ListPermissions`)_
    * `RoleRegistryFilter`: Returns the Role Registrys matching query builder JSON, keyset-paged and capped. _(
      permissions from `RoleRegistryService.ListPermissions`)_
    * `RoleRegistryCount`: Counts the Role Registrys matching query builder JSON, optionally grouped by a field. _(
      permissions from `RoleRegistryService.ListPermissions`)_
    * `RoleRegistryGet`: Returns a single Role by Id. _(permissions from `RoleRegistryService.RequiredPermissions`)_
    * `RoleRegistryGetByGuid`: Returns a single Role by Id. _(permissions from
      `RoleRegistryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Role Registry Test Bench (`role-registry-test-bench`)

- **Hierarchy Placement:** Level 4, child of Entitlement Supervisor
- **Semantic Persona:** Owns the role registry slice and runs the side-effect-free rehearsals: validation, rule parsing
  and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** role, registry, test, bench
- **Toolbox (Max 15):**
    * `RoleRegistryValidate`: Validates a Role Registry without saving it and returns every failure. _(permissions from
      `RoleRegistryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Role Registry Author (`role-registry-author`)

- **Hierarchy Placement:** Level 4, child of Entitlement Supervisor
- **Semantic Persona:** Owns the role registry slice and is the only agent that mutates this slice; every call needs
  explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** role, registry, author
- **Toolbox (Max 15):**
    * `RoleRegistryList`: Lists Roles visible to the caller's tenant, keyset-paged and capped. _(permissions from
      `RoleRegistryService.ListPermissions`)_
    * `RoleRegistryFilterFields`: Lists the fields a query builder JSON filter over Role Registrys may use. _(
      permissions from `RoleRegistryService.ListPermissions`)_
    * `RoleRegistryFilter`: Returns the Role Registrys matching query builder JSON, keyset-paged and capped. _(
      permissions from `RoleRegistryService.ListPermissions`)_
    * `RoleRegistryCount`: Counts the Role Registrys matching query builder JSON, optionally grouped by a field. _(
      permissions from `RoleRegistryService.ListPermissions`)_
    * `RoleRegistryGet`: Returns a single Role by Id. _(permissions from `RoleRegistryService.RequiredPermissions`)_
    * `RoleRegistryGetByGuid`: Returns a single Role by Id. _(permissions from
      `RoleRegistryService.RequiredPermissions`)_
    * `RoleRegistryValidate`: Validates a Role Registry without saving it and returns every failure. _(permissions from
      `RoleRegistryService.RequiredPermissions`)_
    * `RoleRegistryCreate`: Creates a Role; calling twice creates two. **Write — confirmation required** _(permissions
      from `RoleRegistryService.RequiredPermissions`)_
    * `RoleRegistryUpdate`: Updates a Role; overwrites, writes a version-audit row. **Write — confirmation required** _(
      permissions from `RoleRegistryService.RequiredPermissions`)_
    * `RoleRegistryDelete`: Soft-deletes a Role; reversible only by direct database action. **Write — confirmation
      required** _(permissions from `RoleRegistryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Role Registry Permission Inspector (`role-registry-permission-inspector`)

- **Hierarchy Placement:** Level 4, child of Entitlement Supervisor
- **Semantic Persona:** Owns the role registry permission slice and reads only; it never changes anything and is the
  safe default for any question about current state.
- **Semantic Routing Keywords:** role, registry, permission, inspector
- **Toolbox (Max 15):**
    * `RoleRegistryPermissionList`: Lists Role Registry Permission grants visible to the caller's tenant, keyset-paged
      and capped. _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionFilterFields`: Lists the fields a query builder JSON filter over Role Registry Permissions
      may use. _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionFilter`: Returns the Role Registry Permissions matching query builder JSON, keyset-paged
      and capped. _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionCount`: Counts the Role Registry Permissions matching query builder JSON, optionally
      grouped by a field. _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionGet`: Returns a single Role Registry Permission grant by Id. _(permissions from
      `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionGetByGuid`: Returns a single Role Registry Permission grant by Id. _(permissions from
      `RoleRegistryPermissionService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Role Registry Permission Test Bench (`role-registry-permission-test-bench`)

- **Hierarchy Placement:** Level 4, child of Entitlement Supervisor
- **Semantic Persona:** Owns the role registry permission slice and runs the side-effect-free rehearsals: validation,
  rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** role, registry, permission, test, bench
- **Toolbox (Max 15):**
    * `RoleRegistryPermissionValidate`: Validates a Role Registry Permission without saving it and returns every
      failure. _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Role Registry Permission Author (`role-registry-permission-author`)

- **Hierarchy Placement:** Level 4, child of Entitlement Supervisor
- **Semantic Persona:** Owns the role registry permission slice and is the only agent that mutates this slice; every
  call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** role, registry, permission, author
- **Toolbox (Max 15):**
    * `RoleRegistryPermissionList`: Lists Role Registry Permission grants visible to the caller's tenant, keyset-paged
      and capped. _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionFilterFields`: Lists the fields a query builder JSON filter over Role Registry Permissions
      may use. _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionFilter`: Returns the Role Registry Permissions matching query builder JSON, keyset-paged
      and capped. _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionCount`: Counts the Role Registry Permissions matching query builder JSON, optionally
      grouped by a field. _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionGet`: Returns a single Role Registry Permission grant by Id. _(permissions from
      `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionGetByGuid`: Returns a single Role Registry Permission grant by Id. _(permissions from
      `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionValidate`: Validates a Role Registry Permission without saving it and returns every
      failure. _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionCreate`: Grants a Permission Specification to a Role Registry; calling twice creates two
      grants. **Write — confirmation required** _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionUpdate`: Updates a Role Registry Permission grant; writes a version-audit row. **Write —
      confirmation required** _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
    * `RoleRegistryPermissionDelete`: Soft-deletes a Role Registry Permission grant; reversible only by direct database
      action. **Write — confirmation required** _(permissions from `RoleRegistryPermissionService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Permission Specification (`permission-specification`)

- **Hierarchy Placement:** Level 4, child of Entitlement Supervisor
- **Semantic Persona:** Owns the permission specification slice end to end, which is small enough that reads and writes
  stay with one agent.
- **Semantic Routing Keywords:** permission, specification
- **Toolbox (Max 15):**
    * `PermissionSpecificationList`: Lists rows in the global Permission Specification catalogue, keyset-paged and
      capped. _(permissions from `PermissionSpecificationService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Identity Supervisor

- **Hierarchy Placement:** Level 3, child of Access Control and Tenancy
- **Semantic Persona:** Routes within the identity slice of access control and tenancy. Holds no tools; it exists so the
  domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** identity, user, registry, in, tenant, api, key, login, logout
- **Toolbox (Max 15):** none — routes to 4 leaf agents covering 17 tools
- **Downstream Dependencies:** `user-registry-inspector`, `user-registry-test-bench`, `user-registry-author`,
  `user-observability`

#### User Registry Inspector (`user-registry-inspector`)

- **Hierarchy Placement:** Level 4, child of Identity Supervisor
- **Semantic Persona:** Owns the user registry slice and reads only; it never changes anything and is the safe default
  for any question about current state.
- **Semantic Routing Keywords:** user, registry, inspector
- **Toolbox (Max 15):**
    * `UserRegistryList`: Lists user accounts for the caller's tenant, keyset-paged and capped. _(permissions from
      `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryFilterFields`: Lists the fields a query builder JSON filter over User Registrys may use. _(
      permissions from `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryFilter`: Returns the User Registrys matching query builder JSON, keyset-paged and capped. _(
      permissions from `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryCount`: Counts the User Registrys matching query builder JSON, optionally grouped by a field. _(
      permissions from `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryGet`: Returns one user account by id, scoped to the caller's tenant. _(permissions from
      `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryGetByGuid`: Returns one user account by Guid, scoped to the caller's tenant. _(permissions from
      `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryListByRoleRegistryGuid`: Lists the user accounts assigned to a given Role Registry, scoped to the
      caller's tenant. _(permissions from `UserRegistryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### User Registry Test Bench (`user-registry-test-bench`)

- **Hierarchy Placement:** Level 4, child of Identity Supervisor
- **Semantic Persona:** Owns the user registry slice and runs the side-effect-free rehearsals: validation, rule parsing
  and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** user, registry, test, bench
- **Toolbox (Max 15):**
    * `UserRegistryValidate`: Validates an User Registry without saving it and returns every failure. _(permissions from
      `UserRegistryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### User Registry Author (`user-registry-author`)

- **Hierarchy Placement:** Level 4, child of Identity Supervisor
- **Semantic Persona:** Owns the user registry slice and is the only agent that mutates this slice; every call needs
  explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** user, registry, author
- **Toolbox (Max 15):**
    * `UserRegistryList`: Lists user accounts for the caller's tenant, keyset-paged and capped. _(permissions from
      `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryFilterFields`: Lists the fields a query builder JSON filter over User Registrys may use. _(
      permissions from `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryFilter`: Returns the User Registrys matching query builder JSON, keyset-paged and capped. _(
      permissions from `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryCount`: Counts the User Registrys matching query builder JSON, optionally grouped by a field. _(
      permissions from `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryGet`: Returns one user account by id, scoped to the caller's tenant. _(permissions from
      `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryGetByGuid`: Returns one user account by Guid, scoped to the caller's tenant. _(permissions from
      `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryListByRoleRegistryGuid`: Lists the user accounts assigned to a given Role Registry, scoped to the
      caller's tenant. _(permissions from `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryValidate`: Validates an User Registry without saving it and returns every failure. _(permissions from
      `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryCreate`: Registers a user account under a Role Registry in the caller's tenant; calling twice creates
      two. **Write — confirmation required** _(permissions from `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryUpdate`: Updates a user account in the caller's tenant by id. Never touches the password. **Write —
      confirmation required** _(permissions from `UserRegistryService.RequiredPermissions`)_
    * `UserRegistryDelete`: Soft-deletes a user account in the caller's tenant by id. **Write — confirmation required**
      _(permissions from `UserRegistryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### User Observability (`user-observability`)

- **Hierarchy Placement:** Level 4, child of Identity Supervisor
- **Semantic Persona:** Owns the user observability slice end to end, which is small enough that reads and writes stay
  with one agent. It holds 2 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** user, in, tenant, registry, api, key, login, logout
- **Toolbox (Max 15):**
    * `UserInTenantList`: Lists the users allocated to the caller's own tenant, keyset-paged and capped. _(permissions
      from `UserInTenantService.RequiredPermissions`)_
    * `UserInTenantGetCurrentTenantRegistry`: Returns the caller's own tenant allocation, or nothing if unallocated. _(
      permissions from `UserInTenantService.RequiredPermissions`)_
    * `UserInTenantSwitchTenant`: Switches the caller to a different tenant; restricted to a Landlord caller. **Write —
      confirmation required** _(permissions from `UserInTenantService.RequiredPermissions`)_
    * `UserRegistryApiKeyListByUserRegistryId`: Lists the API keys issued against a UserRegistry, masked to an
      8-character display prefix. _(permissions from `UserRegistryApiKeyService.RequiredPermissions`)_
    * `UserRegistryApiKeyDelete`: Revokes an API key; reversible only by direct database action. A repeat call on an
      already-revoked key succeeds as a no-op if it was fully removed, or is reported not-found if it belongs to another
      tenant. **Write — confirmation required** _(permissions from `UserRegistryApiKeyService.RequiredPermissions`)_
    * `UserLoginList`: Lists the Login Audit Trail -- every sign-in attempt, successful or not, across all three schemes
      (Username/Password, Negotiate, OAuth/OpenID Connect), most recent first, capped at 100000, with optional
      date-range and substring search (Crea… _(permissions from `UserLoginService.RequiredPermissions`)_
    * `UserLogoutList`: Lists the Logout Audit Trail -- every time a session was cut (the user logged out, an
      administrator revoked the user's tokens, or a password change revoked the earlier sessions), most recent first,
      capped at 100000, with optional date-range… _(permissions from `UserLogoutService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Tenancy Supervisor

- **Hierarchy Placement:** Level 3, child of Access Control and Tenancy
- **Semantic Persona:** Routes within the tenancy slice of access control and tenancy. Holds no tools; it exists so the
  domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** tenancy, tenant, registry
- **Toolbox (Max 15):** none — routes to 3 leaf agents covering 9 tools
- **Downstream Dependencies:** `tenant-registry-inspector`, `tenant-registry-test-bench`, `tenant-registry-author`

#### Tenant Registry Inspector (`tenant-registry-inspector`)

- **Hierarchy Placement:** Level 4, child of Tenancy Supervisor
- **Semantic Persona:** Owns the tenant registry slice and reads only; it never changes anything and is the safe default
  for any question about current state.
- **Semantic Routing Keywords:** tenant, registry, inspector
- **Toolbox (Max 15):**
    * `TenantRegistryList`: Lists registered Tenants, Id-ascending, keyset-paged and capped. Landlord-only. _(
      permissions from `TenantRegistryService.RequiredPermissions`)_
    * `TenantRegistryFilterFields`: Lists the fields a query builder JSON filter over Tenant Registrys may use. _(
      permissions from `TenantRegistryService.RequiredPermissions`)_
    * `TenantRegistryFilter`: Returns the Tenant Registrys matching query builder JSON, keyset-paged and capped. _(
      permissions from `TenantRegistryService.RequiredPermissions`)_
    * `TenantRegistryCount`: Counts the Tenant Registrys matching query builder JSON, optionally grouped by a field. _(
      permissions from `TenantRegistryService.RequiredPermissions`)_
    * `TenantRegistryGet`: Returns a single Tenant by Id. Landlord-only. _(permissions from
      `TenantRegistryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Tenant Registry Test Bench (`tenant-registry-test-bench`)

- **Hierarchy Placement:** Level 4, child of Tenancy Supervisor
- **Semantic Persona:** Owns the tenant registry slice and runs the side-effect-free rehearsals: validation, rule
  parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** tenant, registry, test, bench
- **Toolbox (Max 15):**
    * `TenantRegistryValidate`: Validates a Tenant Registry without saving it and returns every failure. _(permissions
      from `TenantRegistryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Tenant Registry Author (`tenant-registry-author`)

- **Hierarchy Placement:** Level 4, child of Tenancy Supervisor
- **Semantic Persona:** Owns the tenant registry slice and is the only agent that mutates this slice; every call needs
  explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** tenant, registry, author
- **Toolbox (Max 15):**
    * `TenantRegistryList`: Lists registered Tenants, Id-ascending, keyset-paged and capped. Landlord-only. _(
      permissions from `TenantRegistryService.RequiredPermissions`)_
    * `TenantRegistryFilterFields`: Lists the fields a query builder JSON filter over Tenant Registrys may use. _(
      permissions from `TenantRegistryService.RequiredPermissions`)_
    * `TenantRegistryFilter`: Returns the Tenant Registrys matching query builder JSON, keyset-paged and capped. _(
      permissions from `TenantRegistryService.RequiredPermissions`)_
    * `TenantRegistryCount`: Counts the Tenant Registrys matching query builder JSON, optionally grouped by a field. _(
      permissions from `TenantRegistryService.RequiredPermissions`)_
    * `TenantRegistryGet`: Returns a single Tenant by Id. Landlord-only. _(permissions from
      `TenantRegistryService.RequiredPermissions`)_
    * `TenantRegistryValidate`: Validates a Tenant Registry without saving it and returns every failure. _(permissions
      from `TenantRegistryService.RequiredPermissions`)_
    * `TenantRegistryCreate`: Creates a Tenant; calling twice creates two. Landlord-only. **Write — confirmation
      required** _(permissions from `TenantRegistryService.RequiredPermissions`)_
    * `TenantRegistryUpdate`: Updates a Tenant; overwrites, writes a version-audit row. Landlord-only. **Write —
      confirmation required** _(permissions from `TenantRegistryService.RequiredPermissions`)_
    * `TenantRegistryDelete`: Soft-deletes a Tenant; reversible only by direct database action. Landlord-only. **Write —
      confirmation required** _(permissions from `TenantRegistryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

---

## Assistant Conversation Administration (`assistant-admin`)

#### Assistant Conversation Administration

- **Hierarchy Placement:** Level 2, child of Jooby
- **Semantic Persona:** Administers the assistant's own stored conversations and retrieval corpus. Strictly
  housekeeping: it can list and search what the assistant has recorded, never start a turn or spawn another agent.
- **Semantic Routing Keywords:** conversation history, transcript, retrieval corpus, RAG chunk, assistant housekeeping
- **Toolbox (Max 15):** none — routes to 1 leaf agent covering 5 tools
- **Downstream Dependencies:** `ask-jooby-observability`

#### Ask Jooby Observability (`ask-jooby-observability`)

- **Hierarchy Placement:** Level 3, child of Assistant Conversation Administration
- **Semantic Persona:** Owns the ask jooby observability slice end to end, which is small enough that reads and writes
  stay with one agent. It holds 2 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** ask, jooby, conversation, rag, chunk
- **Toolbox (Max 15):**
    * `AskJoobyConversationList`: Lists AskJooby conversations for the caller's tenant, most recently active first. _(
      permissions from `AskJoobyConversationService.RequiredPermissions`)_
    * `AskJoobyConversationCreate`: Starts a new, empty, untitled AskJooby conversation in the caller's tenant.
      **Write — confirmation required** _(permissions from `AskJoobyConversationService.RequiredPermissions`)_
    * `AskJoobyConversationGetMessages`: Returns the full transcript of one conversation, oldest first. _(permissions
      from `AskJoobyConversationService.RequiredPermissions`)_
    * `AskJoobyConversationSendMessage`: Sends a message to Jooby and streams the reply, persisting both sides of the
      exchange. **Write — confirmation required** _(permissions from `AskJoobyConversationService.RequiredPermissions`)_
    * `AskJoobyRagChunkSearch`: Searches the Pasture and returns the most relevant Bites, ranked by reciprocal-rank
      fusion of full-text and vector search. _(permissions from `AskJoobyRagChunkService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

---

## Case Investigation Desk (`case-analyst`)

#### Case Investigation Desk

- **Hierarchy Placement:** Level 2, child of Jooby
- **Semantic Persona:** Works live alert and case material for an investigator: the case itself, its notes, files,
  events and transaction journal, the activation watcher and sanctions screening. Never changes how the platform detects
  anything, only what is recorded about a case under investigation.
- **Semantic Routing Keywords:** case, alert, investigate, SAR, escalate, note, attachment, journal, watcher, sanctions
  screening, disposition
- **Toolbox (Max 15):** none — routes to 1 supervisor and 3 leaf agents covering 29 tools
- **Downstream Dependencies:** `case-analyst.investigation`, `activation-watcher-and-archive`, `sanctions-entry-source`,
  `session-case-observability`
- **Cross-Domain Reach:** `form-entries-and-macro-executions`

#### Investigation Supervisor

- **Hierarchy Placement:** Level 3, child of Case Investigation Desk
- **Semantic Persona:** Routes within the investigation slice of case investigation desk. Holds no tools; it exists so
  the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** investigation, case, file, note, by, key, value, id, session, search, compile,
  creation, stage, performance, counter, warning, event, journal
- **Toolbox (Max 15):** none — routes to 3 leaf agents covering 18 tools
- **Downstream Dependencies:** `case-record-and-files`, `case-lookup-and-notes`, `case-journal-and-events`

#### Case Record and Files (`case-record-and-files`)

- **Hierarchy Placement:** Level 4, child of Investigation Supervisor
- **Semantic Persona:** Owns the case record and files slice end to end, which is small enough that reads and writes
  stay with one agent. It holds 4 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** case, file
- **Toolbox (Max 15):**
    * `CaseList`: Lists active cases for the caller's tenant, keyset-paged and capped. _(permissions from
      `CaseService.RequiredPermissions`)_
    * `CaseGet`: Returns a single active case by Id, scoped to the caller's tenant and role. _(permissions from
      `CaseService.RequiredPermissions`)_
    * `CaseCreateFromCaseKeyValue`: Raises a new case (or promotes an existing one) from a matching archived
      transaction; dispatches the target status's notification/webhook. **Write — confirmation required** _(permissions
      from `CaseService.RequiredPermissions`)_
    * `CaseUpdate`: Updates a case's closed status, lock, diary, rating and workflow status; a status change dispatches
      that status's notification/webhook. **Write — confirmation required** _(permissions from
      `CaseService.RequiredPermissions`)_
    * `CaseFileRemove`: Removes (soft-deletes) a file attached to a Case. **Write — confirmation required** _(
      permissions from `CaseFileService.RequiredPermissions`)_
    * `CaseFileListByCaseKeyValue`: Lists the files attached to the Case for a given key field name and value, scoped to
      the caller's tenant and Case Workflow roles. _(permissions from `CaseFileService.RequiredPermissions`)_
    * `CaseFileUpload`: Uploads and attaches a file to a Case in the caller's tenant. **Write — confirmation required**
      _(permissions from `CaseFileService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Lookup and Notes (`case-lookup-and-notes`)

- **Hierarchy Placement:** Level 4, child of Investigation Supervisor
- **Semantic Persona:** Owns the case lookup and notes slice end to end, which is small enough that reads and writes
  stay with one agent. It holds 2 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** case, note, by, key, value, id, session, search, compile, creation, stage, performance,
  counter
- **Toolbox (Max 15):**
    * `CaseNoteInsert`: Adds a note to a case, filed against a CaseWorkflowAction; dispatches that action's
      notification/webhook if enabled. **Write — confirmation required** _(permissions from
      `CaseNoteService.RequiredPermissions`)_
    * `CaseNoteValidate`: Validates a Case Note without saving it and returns every failure. _(permissions from
      `CaseNoteService.RequiredPermissions`)_
    * `CaseNoteListByCaseKeyValue`: Lists case notes for a Case Key/Value pair, newest first, scoped to the caller's
      tenant. _(permissions from `CaseNoteService.RequiredPermissions`)_
    * `CaseByCaseKeyValueGet`: Gets the Cases matching a Case Key and Case Key Value that the caller's role may see. _(
      permissions from `CaseByCaseKeyValueService.RequiredPermissions`)_
    * `CaseByIdGet`: Gets one case by Id with its status colours, formatted payload and visible activations; records a
      case-viewed event. _(permissions from `CaseByIdService.RequiredPermissions`)_
    * `CaseBySessionCaseSearchCompileGet`: Retrieves the next case for a compiled Session Case Search, recording a
      case-opened event and locking the case to the calling user. **Write — confirmation required** _(permissions from
      `CaseBySessionCaseSearchCompileService.RequiredPermissions`)_
    * `CaseCreationStagePerformanceCounterList`: Lists background case creation stage performance rows --
      ExistingCasePriorityLookup, WorkflowStatusLookupAndPersist, Notification, HttpEndpoint -- most recent first,
      capped at 100000, with optional date-range and exact-match (StageId, e.g. _(permissions from
      `CaseCreationStagePerformanceCounterService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Journal and Events (`case-journal-and-events`)

- **Hierarchy Placement:** Level 4, child of Investigation Supervisor
- **Semantic Persona:** Owns the case journal and events slice end to end, which is small enough that reads and writes
  stay with one agent.
- **Semantic Routing Keywords:** case, creation, warning, event, by, key, value, journal, note
- **Toolbox (Max 15):**
    * `CaseCreationWarningList`: Lists captured case creation warn-threshold breaches -- the specific case (workflow,
      key, key value), the slow stage, and the notification/HTTP destination when relevant -- most recent first, capped
      at 100000, with optional date-range, exac… _(permissions from `CaseCreationWarningService.RequiredPermissions`)_
    * `CaseEventByCaseKeyValueGet`: Lists the audit events of a case identified by case key and case key value, newest
      first. _(permissions from `CaseEventByCaseKeyValueService.RequiredPermissions`)_
    * `CaseJournalGet`: Returns the tenant-scoped Case journal of Archive entries matching a payload key and value. _(
      permissions from `CaseJournalService.RequiredPermissions`)_
    * `CaseNoteByCaseKeyValueGet`: Lists the case notes for cases matching a case key and value, newest first, limited
      to workflows, statuses and actions the caller's roles may see within the caller's tenant. _(permissions from
      `CaseNoteByCaseKeyValueService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Activation Watcher and Archive (`activation-watcher-and-archive`)

- **Hierarchy Placement:** Level 3, child of Case Investigation Desk
- **Semantic Persona:** Owns the activation watcher and archive slice end to end, which is small enough that reads and
  writes stay with one agent. It holds 1 mutating tool, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** activation, watcher, archive, archiver, stage, performance, counter, warning
- **Toolbox (Max 15):**
    * `ActivationWatcherReplay`: Replays ActivationWatcher rows created within a date range, scoped to the caller's
      tenant. _(permissions from `ActivationWatcherService.RequiredPermissions`)_
    * `ArchiveTag`: Replaces the full Tag set on an archived transaction, scoped to the caller's tenant. **Write —
      confirmation required** _(permissions from `ArchiveService.RequiredPermissions`)_
    * `ArchiverStagePerformanceCounterList`: Lists background archiver stage performance rows across every Model --
      BuildArchiveJson, CaseCreationDispatch, RdbmsArchiveWrite, BulkCopyArchiveBuffer -- most recent first, capped at
      100000, with optional date-range and exact-match (StageI… _(permissions from
      `ArchiverStagePerformanceCounterService.RequiredPermissions`)_
    * `ArchiverWarningList`: Lists captured archiver warn-threshold breaches -- the specific invocation (when the stage
      operates on one item; null for BulkCopyArchiveBuffer), the model, and the slow stage -- most recent first, capped
      at 100000, with optional date-range… _(permissions from `ArchiverWarningService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Sanctions Entry Source (`sanctions-entry-source`)

- **Hierarchy Placement:** Level 3, child of Case Investigation Desk
- **Semantic Persona:** Owns the sanctions entry source slice end to end, which is small enough that reads and writes
  stay with one agent.
- **Semantic Routing Keywords:** sanctions, entry, source
- **Toolbox (Max 15):**
    * `SanctionEntrySourceList`: Lists the configured Sanction Entry Sources (e.g. SDN, BOE, EU) the Sanctions Loader
      can poll or manually import from, each identified by Id and Name. _(permissions from
      `SanctionEntrySourceService.RequiredPermissions`)_
    * `SanctionEntrySourceFilterFields`: Lists the fields a query builder JSON filter over Sanction Entry Sources may
      use. _(permissions from `SanctionEntrySourceService.RequiredPermissions`)_
    * `SanctionEntrySourceFilter`: Returns the Sanction Entry Sources matching query builder JSON, keyset-paged and
      capped. _(permissions from `SanctionEntrySourceService.RequiredPermissions`)_
    * `SanctionEntrySourceCount`: Counts the Sanction Entry Sources matching query builder JSON, optionally grouped by a
      field. _(permissions from `SanctionEntrySourceService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Session Case Observability (`session-case-observability`)

- **Hierarchy Placement:** Level 3, child of Case Investigation Desk
- **Semantic Persona:** Owns the session case observability slice end to end, which is small enough that reads and
  writes stay with one agent. It holds 1 mutating tool, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** session, case, journal, search, compiled, sql
- **Toolbox (Max 15):**
    * `SessionCaseJournalGetByCaseWorkflowGuid`: Gets the calling user's own saved journal (e.g. case grid column
      layout) for a CaseWorkflow. _(permissions from `SessionCaseJournalService.RequiredPermissions`)_
    * `SessionCaseJournalCreate`: Saves the calling user's own journal for a CaseWorkflow, replacing any earlier save.
      **Write — confirmation required** _(permissions from `SessionCaseJournalService.RequiredPermissions`)_
    * `SessionCaseSearchCompiledSqlGetLast`: Returns the caller's own most recently compiled Session Case Search (or a
      NotFound placeholder); a search flagged for rebuild is recompiled first. _(permissions from
      `SessionCaseSearchCompiledSqlService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

---

## Case Workflow Governance (`case-workflow-admin`)

#### Case Workflow Governance

- **Hierarchy Placement:** Level 2, child of Jooby
- **Semantic Persona:** Owns the definitions that shape how cases behave: statuses, routing filters, XPath extraction,
  forms, actions and macros, and the role grants over each. It designs the workflow an investigator later works inside,
  and never touches a live case.
- **Semantic Routing Keywords:** case workflow, status, queue, routing, form, macro, action, display, XPath extraction,
  workflow role
- **Toolbox (Max 15):** none — routes to 7 supervisors covering 121 tools
- **Downstream Dependencies:** `case-workflow-admin.definition`, `case-workflow-admin.entitlement`,
  `case-workflow-admin.interaction.actions`, `case-workflow-admin.interaction.forms`,
  `case-workflow-admin.interaction.macros`, `case-workflow-admin.routing.extraction`,
  `case-workflow-admin.routing.status`
- **Cross-Domain Reach:** `completions`

#### Definition Supervisor

- **Hierarchy Placement:** Level 3, child of Case Workflow Governance
- **Semantic Persona:** Routes within the definition slice of case workflow governance. Holds no tools; it exists so the
  domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** definition, case, workflow
- **Toolbox (Max 15):** none — routes to 3 leaf agents covering 10 tools
- **Downstream Dependencies:** `case-workflow-inspector`, `case-workflow-test-bench`, `case-workflow-author`

#### Case Workflow Inspector (`case-workflow-inspector`)

- **Hierarchy Placement:** Level 4, child of Definition Supervisor
- **Semantic Persona:** Owns the case workflow slice and reads only; it never changes anything and is the safe default
  for any question about current state.
- **Semantic Routing Keywords:** case, workflow, inspector
- **Toolbox (Max 15):**
    * `CaseWorkflowList`: Lists Case Workflows visible to the caller's tenant, keyset-paged and capped. _(permissions
      from `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowFilterFields`: Lists the fields a query builder JSON filter over Case Workflows may use. _(
      permissions from `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowFilter`: Returns the Case Workflows matching query builder JSON, keyset-paged and capped. _(
      permissions from `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowCount`: Counts the Case Workflows matching query builder JSON, optionally grouped by a field. _(
      permissions from `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowListByModelActiveOnly`: Lists active Case Workflows for a parent model that the caller holds a role
      grant on. _(permissions from `CaseWorkflowService.ActiveOnlyPermissions`)_
    * `CaseWorkflowGet`: Returns a single Case Workflow by Id. _(permissions from
      `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowGetByGuid`: Returns a single Case Workflow by Id. _(permissions from
      `CaseWorkflowService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Test Bench (`case-workflow-test-bench`)

- **Hierarchy Placement:** Level 4, child of Definition Supervisor
- **Semantic Persona:** Owns the case workflow slice and runs the side-effect-free rehearsals: validation, rule parsing
  and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** case, workflow, test, bench
- **Toolbox (Max 15):**
    * `CaseWorkflowValidate`: Validates a Case Workflow without saving it and returns every failure. _(permissions from
      `CaseWorkflowService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Author (`case-workflow-author`)

- **Hierarchy Placement:** Level 4, child of Definition Supervisor
- **Semantic Persona:** Owns the case workflow slice and is the only agent that mutates this slice; every call needs
  explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** case, workflow, author
- **Toolbox (Max 15):**
    * `CaseWorkflowList`: Lists Case Workflows visible to the caller's tenant, keyset-paged and capped. _(permissions
      from `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowFilterFields`: Lists the fields a query builder JSON filter over Case Workflows may use. _(
      permissions from `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowFilter`: Returns the Case Workflows matching query builder JSON, keyset-paged and capped. _(
      permissions from `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowCount`: Counts the Case Workflows matching query builder JSON, optionally grouped by a field. _(
      permissions from `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowListByModelActiveOnly`: Lists active Case Workflows for a parent model that the caller holds a role
      grant on. _(permissions from `CaseWorkflowService.ActiveOnlyPermissions`)_
    * `CaseWorkflowGet`: Returns a single Case Workflow by Id. _(permissions from
      `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowGetByGuid`: Returns a single Case Workflow by Id. _(permissions from
      `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowValidate`: Validates a Case Workflow without saving it and returns every failure. _(permissions from
      `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowCreate`: Creates a Case Workflow; calling twice creates two. **Write — confirmation required** _(
      permissions from `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowUpdate`: Updates a Case Workflow; overwrites, writes a version-audit row. **Write — confirmation
      required** _(permissions from `CaseWorkflowService.RequiredPermissions`)_
    * `CaseWorkflowDelete`: Soft-deletes a Case Workflow; reversible only by direct database action. **Write —
      confirmation required** _(permissions from `CaseWorkflowService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entitlement Supervisor

- **Hierarchy Placement:** Level 3, child of Case Workflow Governance
- **Semantic Persona:** Routes within the entitlement slice of case workflow governance. Holds no tools; it exists so
  the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** entitlement, case, workflow, action, role, display, filter, form, macro, status, path
- **Toolbox (Max 15):** none — routes to 4 leaf agents covering 24 tools
- **Downstream Dependencies:** `action-and-display-role-grants`, `filter-and-form-role-grants`,
  `macro-and-workflow-role-grants`, `status-and-xpath-role-grants`

#### Action and Display Role Grants (`action-and-display-role-grants`)

- **Hierarchy Placement:** Level 4, child of Entitlement Supervisor
- **Semantic Persona:** Owns the action and display role grants slice end to end, which is small enough that reads and
  writes stay with one agent. It holds 4 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** case, workflow, action, role, display
- **Toolbox (Max 15):**
    * `CaseWorkflowActionRoleListByCaseWorkflowActionGuid`: Lists the Role grants for a Case Workflow Action. _(
      permissions from `CaseWorkflowActionRoleService.RequiredPermissions`)_
    * `CaseWorkflowActionRoleCreate`: Grants a Role access to a Case Workflow Action; calling twice creates two grants.
      **Write — confirmation required** _(permissions from `CaseWorkflowActionRoleService.RequiredPermissions`)_
    * `CaseWorkflowActionRoleDelete`: Removes a Role's grant on a Case Workflow Action; reversible only by direct
      database action. **Write — confirmation required** _(permissions from
      `CaseWorkflowActionRoleService.RequiredPermissions`)_
    * `CaseWorkflowDisplayRoleListByCaseWorkflowDisplayGuid`: Lists the Role grants for a Case Workflow Display. _(
      permissions from `CaseWorkflowDisplayRoleService.RequiredPermissions`)_
    * `CaseWorkflowDisplayRoleCreate`: Grants a Role access to a Case Workflow Display; calling twice creates two
      grants. **Write — confirmation required** _(permissions from
      `CaseWorkflowDisplayRoleService.RequiredPermissions`)_
    * `CaseWorkflowDisplayRoleDelete`: Removes a Role's grant on a Case Workflow Display; reversible only by direct
      database action. **Write — confirmation required** _(permissions from
      `CaseWorkflowDisplayRoleService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Filter and Form Role Grants (`filter-and-form-role-grants`)

- **Hierarchy Placement:** Level 4, child of Entitlement Supervisor
- **Semantic Persona:** Owns the filter and form role grants slice end to end, which is small enough that reads and
  writes stay with one agent. It holds 4 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** case, workflow, filter, role, form
- **Toolbox (Max 15):**
    * `CaseWorkflowFilterRoleListByCaseWorkflowFilterGuid`: Lists the Role grants for a Case Workflow Filter. _(
      permissions from `CaseWorkflowFilterRoleService.RequiredPermissions`)_
    * `CaseWorkflowFilterRoleCreate`: Grants a Role access to a Case Workflow Filter; calling twice creates two grants.
      **Write — confirmation required** _(permissions from `CaseWorkflowFilterRoleService.RequiredPermissions`)_
    * `CaseWorkflowFilterRoleDelete`: Removes a Role's grant on a Case Workflow Filter; reversible only by direct
      database action. **Write — confirmation required** _(permissions from
      `CaseWorkflowFilterRoleService.RequiredPermissions`)_
    * `CaseWorkflowFormRoleListByCaseWorkflowFormGuid`: Lists the Role grants for a Case Workflow Form. _(permissions
      from `CaseWorkflowFormRoleService.RequiredPermissions`)_
    * `CaseWorkflowFormRoleCreate`: Grants a Role access to a Case Workflow Form; calling twice creates two grants.
      **Write — confirmation required** _(permissions from `CaseWorkflowFormRoleService.RequiredPermissions`)_
    * `CaseWorkflowFormRoleDelete`: Removes a Role's grant on a Case Workflow Form; reversible only by direct database
      action. **Write — confirmation required** _(permissions from `CaseWorkflowFormRoleService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Macro and Workflow Role Grants (`macro-and-workflow-role-grants`)

- **Hierarchy Placement:** Level 4, child of Entitlement Supervisor
- **Semantic Persona:** Owns the macro and workflow role grants slice end to end, which is small enough that reads and
  writes stay with one agent. It holds 4 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** case, workflow, macro, role
- **Toolbox (Max 15):**
    * `CaseWorkflowMacroRoleListByCaseWorkflowMacroGuid`: Lists the Role grants for a Case Workflow Macro. _(permissions
      from `CaseWorkflowMacroRoleService.RequiredPermissions`)_
    * `CaseWorkflowMacroRoleCreate`: Grants a Role access to a Case Workflow Macro; calling twice creates two grants.
      **Write — confirmation required** _(permissions from `CaseWorkflowMacroRoleService.RequiredPermissions`)_
    * `CaseWorkflowMacroRoleDelete`: Removes a Role's grant on a Case Workflow Macro; reversible only by direct database
      action. **Write — confirmation required** _(permissions from `CaseWorkflowMacroRoleService.RequiredPermissions`)_
    * `CaseWorkflowRoleListByCaseWorkflowGuid`: Lists the Role grants for a Case Workflow. _(permissions from
      `CaseWorkflowRoleService.RequiredPermissions`)_
    * `CaseWorkflowRoleCreate`: Grants a Role access to a Case Workflow; calling twice creates two grants. **Write —
      confirmation required** _(permissions from `CaseWorkflowRoleService.RequiredPermissions`)_
    * `CaseWorkflowRoleDelete`: Removes a Role's grant on a Case Workflow; reversible only by direct database action.
      **Write — confirmation required** _(permissions from `CaseWorkflowRoleService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Status and XPath Role Grants (`status-and-xpath-role-grants`)

- **Hierarchy Placement:** Level 4, child of Entitlement Supervisor
- **Semantic Persona:** Owns the status and xpath role grants slice end to end, which is small enough that reads and
  writes stay with one agent. It holds 4 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** case, workflow, status, role, path
- **Toolbox (Max 15):**
    * `CaseWorkflowStatusRoleListByCaseWorkflowStatusGuid`: Lists the Role grants for a Case Workflow Status. _(
      permissions from `CaseWorkflowStatusRoleService.RequiredPermissions`)_
    * `CaseWorkflowStatusRoleCreate`: Grants a Role access to a Case Workflow Status; calling twice creates two grants.
      **Write — confirmation required** _(permissions from `CaseWorkflowStatusRoleService.RequiredPermissions`)_
    * `CaseWorkflowStatusRoleDelete`: Removes a Role's grant on a Case Workflow Status; reversible only by direct
      database action. **Write — confirmation required** _(permissions from
      `CaseWorkflowStatusRoleService.RequiredPermissions`)_
    * `CaseWorkflowXPathRoleListByCaseWorkflowXPathGuid`: Lists the Role grants for a Case Workflow XPath. _(permissions
      from `CaseWorkflowXPathRoleService.RequiredPermissions`)_
    * `CaseWorkflowXPathRoleCreate`: Grants a Role access to a Case Workflow XPath; calling twice creates two grants.
      **Write — confirmation required** _(permissions from `CaseWorkflowXPathRoleService.RequiredPermissions`)_
    * `CaseWorkflowXPathRoleDelete`: Removes a Role's grant on a Case Workflow XPath; reversible only by direct database
      action. **Write — confirmation required** _(permissions from `CaseWorkflowXPathRoleService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Interaction Actions Supervisor

- **Hierarchy Placement:** Level 3, child of Case Workflow Governance
- **Semantic Persona:** Routes within the interaction actions slice of case workflow governance. Holds no tools; it
  exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** interaction actions, case, workflow, action, display
- **Toolbox (Max 15):** none — routes to 6 leaf agents covering 22 tools
- **Downstream Dependencies:** `case-workflow-action-inspector`, `case-workflow-action-test-bench`,
  `case-workflow-action-author`, `case-workflow-display-inspector`, `case-workflow-display-test-bench`,
  `case-workflow-display-author`

#### Case Workflow Action Inspector (`case-workflow-action-inspector`)

- **Hierarchy Placement:** Level 4, child of Interaction Actions Supervisor
- **Semantic Persona:** Owns the case workflow action slice and reads only; it never changes anything and is the safe
  default for any question about current state.
- **Semantic Routing Keywords:** case, workflow, action, inspector
- **Toolbox (Max 15):**
    * `CaseWorkflowActionList`: Lists Case Workflow Actions visible to the caller's tenant, keyset-paged and capped. _(
      permissions from `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionFilterFields`: Lists the fields a query builder JSON filter over Case Workflow Actions may use.
      _(permissions from `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionFilter`: Returns the Case Workflow Actions matching query builder JSON, keyset-paged and
      capped. _(permissions from `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionCount`: Counts the Case Workflow Actions matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionListByWorkflowIdActiveOnly`: Lists active Case Workflow Actions for a parent Case Workflow that
      the caller holds a role grant on. _(permissions from `CaseWorkflowActionService.ActiveOnlyPermissions`)_
    * `CaseWorkflowActionListByWorkflowGuidActiveOnly`: Lists active Case Workflow Actions for a parent Case Workflow
      (by Guid) that the caller holds a role grant on. _(permissions from
      `CaseWorkflowActionService.ActiveOnlyPermissions`)_
    * `CaseWorkflowActionGet`: Returns a single Case Workflow Action by Id. _(permissions from
      `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionGetByGuid`: Returns a single Case Workflow Action by Id. _(permissions from
      `CaseWorkflowActionService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Action Test Bench (`case-workflow-action-test-bench`)

- **Hierarchy Placement:** Level 4, child of Interaction Actions Supervisor
- **Semantic Persona:** Owns the case workflow action slice and runs the side-effect-free rehearsals: validation, rule
  parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** case, workflow, action, test, bench
- **Toolbox (Max 15):**
    * `CaseWorkflowActionValidate`: Validates a Case Workflow Action without saving it and returns every failure. _(
      permissions from `CaseWorkflowActionService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Action Author (`case-workflow-action-author`)

- **Hierarchy Placement:** Level 4, child of Interaction Actions Supervisor
- **Semantic Persona:** Owns the case workflow action slice and is the only agent that mutates this slice; every call
  needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** case, workflow, action, author
- **Toolbox (Max 15):**
    * `CaseWorkflowActionList`: Lists Case Workflow Actions visible to the caller's tenant, keyset-paged and capped. _(
      permissions from `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionFilterFields`: Lists the fields a query builder JSON filter over Case Workflow Actions may use.
      _(permissions from `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionFilter`: Returns the Case Workflow Actions matching query builder JSON, keyset-paged and
      capped. _(permissions from `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionCount`: Counts the Case Workflow Actions matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionListByWorkflowIdActiveOnly`: Lists active Case Workflow Actions for a parent Case Workflow that
      the caller holds a role grant on. _(permissions from `CaseWorkflowActionService.ActiveOnlyPermissions`)_
    * `CaseWorkflowActionListByWorkflowGuidActiveOnly`: Lists active Case Workflow Actions for a parent Case Workflow
      (by Guid) that the caller holds a role grant on. _(permissions from
      `CaseWorkflowActionService.ActiveOnlyPermissions`)_
    * `CaseWorkflowActionGet`: Returns a single Case Workflow Action by Id. _(permissions from
      `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionGetByGuid`: Returns a single Case Workflow Action by Id. _(permissions from
      `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionValidate`: Validates a Case Workflow Action without saving it and returns every failure. _(
      permissions from `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionCreate`: Creates a Case Workflow Action; calling twice creates two. **Write — confirmation
      required** _(permissions from `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionUpdate`: Updates a Case Workflow Action; overwrites, writes a version-audit row. **Write —
      confirmation required** _(permissions from `CaseWorkflowActionService.RequiredPermissions`)_
    * `CaseWorkflowActionDelete`: Soft-deletes a Case Workflow Action; reversible only by direct database action.
      **Write — confirmation required** _(permissions from `CaseWorkflowActionService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Display Inspector (`case-workflow-display-inspector`)

- **Hierarchy Placement:** Level 4, child of Interaction Actions Supervisor
- **Semantic Persona:** Owns the case workflow display slice and reads only; it never changes anything and is the safe
  default for any question about current state.
- **Semantic Routing Keywords:** case, workflow, display, inspector
- **Toolbox (Max 15):**
    * `CaseWorkflowDisplayList`: Lists Case Workflow Displays visible to the caller's tenant, keyset-paged and capped.
      _(permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayFilterFields`: Lists the fields a query builder JSON filter over Case Workflow Displays may
      use. _(permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayFilter`: Returns the Case Workflow Displays matching query builder JSON, keyset-paged and
      capped. _(permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayCount`: Counts the Case Workflow Displays matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayListByWorkflowIdActiveOnly`: Lists active Case Workflow Displays for a parent Case Workflow
      that the caller holds a role grant on. _(permissions from `CaseWorkflowDisplayService.ActiveOnlyPermissions`)_
    * `CaseWorkflowDisplayListByWorkflowGuidActiveOnly`: Lists active Case Workflow Displays for a parent Case Workflow
      (by Guid) that the caller holds a role grant on. _(permissions from
      `CaseWorkflowDisplayService.ActiveOnlyPermissions`)_
    * `CaseWorkflowDisplayGet`: Returns a single Case Workflow Display by Id. _(permissions from
      `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayGetByGuid`: Returns a single Case Workflow Display by Id. _(permissions from
      `CaseWorkflowDisplayService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Display Test Bench (`case-workflow-display-test-bench`)

- **Hierarchy Placement:** Level 4, child of Interaction Actions Supervisor
- **Semantic Persona:** Owns the case workflow display slice and runs the side-effect-free rehearsals: validation, rule
  parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** case, workflow, display, test, bench
- **Toolbox (Max 15):**
    * `CaseWorkflowDisplayValidate`: Validates a Case Workflow Display without saving it and returns every failure. _(
      permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Display Author (`case-workflow-display-author`)

- **Hierarchy Placement:** Level 4, child of Interaction Actions Supervisor
- **Semantic Persona:** Owns the case workflow display slice and is the only agent that mutates this slice; every call
  needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** case, workflow, display, author
- **Toolbox (Max 15):**
    * `CaseWorkflowDisplayList`: Lists Case Workflow Displays visible to the caller's tenant, keyset-paged and capped.
      _(permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayFilterFields`: Lists the fields a query builder JSON filter over Case Workflow Displays may
      use. _(permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayFilter`: Returns the Case Workflow Displays matching query builder JSON, keyset-paged and
      capped. _(permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayCount`: Counts the Case Workflow Displays matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayListByWorkflowIdActiveOnly`: Lists active Case Workflow Displays for a parent Case Workflow
      that the caller holds a role grant on. _(permissions from `CaseWorkflowDisplayService.ActiveOnlyPermissions`)_
    * `CaseWorkflowDisplayListByWorkflowGuidActiveOnly`: Lists active Case Workflow Displays for a parent Case Workflow
      (by Guid) that the caller holds a role grant on. _(permissions from
      `CaseWorkflowDisplayService.ActiveOnlyPermissions`)_
    * `CaseWorkflowDisplayGet`: Returns a single Case Workflow Display by Id. _(permissions from
      `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayGetByGuid`: Returns a single Case Workflow Display by Id. _(permissions from
      `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayValidate`: Validates a Case Workflow Display without saving it and returns every failure. _(
      permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayCreate`: Creates a Case Workflow Display; calling twice creates two. **Write — confirmation
      required** _(permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayUpdate`: Updates a Case Workflow Display; overwrites, writes a version-audit row. **Write —
      confirmation required** _(permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
    * `CaseWorkflowDisplayDelete`: Soft-deletes a Case Workflow Display; reversible only by direct database action.
      **Write — confirmation required** _(permissions from `CaseWorkflowDisplayService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Interaction Forms Supervisor

- **Hierarchy Placement:** Level 3, child of Case Workflow Governance
- **Semantic Persona:** Routes within the interaction forms slice of case workflow governance. Holds no tools; it exists
  so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** interaction forms, case, workflow, form, entry, display, execution, by, key, value,
  macro
- **Toolbox (Max 15):** none — routes to 4 leaf agents covering 18 tools
- **Downstream Dependencies:** `case-workflow-form-inspector`, `case-workflow-form-test-bench`,
  `case-workflow-form-author`, `form-entries-and-macro-executions`

#### Case Workflow Form Inspector (`case-workflow-form-inspector`)

- **Hierarchy Placement:** Level 4, child of Interaction Forms Supervisor
- **Semantic Persona:** Owns the case workflow form slice and reads only; it never changes anything and is the safe
  default for any question about current state.
- **Semantic Routing Keywords:** case, workflow, form, inspector
- **Toolbox (Max 15):**
    * `CaseWorkflowFormList`: Lists Case Workflow Forms visible to the caller's tenant, keyset-paged and capped. _(
      permissions from `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormFilterFields`: Lists the fields a query builder JSON filter over Case Workflow Forms may use. _(
      permissions from `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormFilter`: Returns the Case Workflow Forms matching query builder JSON, keyset-paged and capped. _(
      permissions from `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormCount`: Counts the Case Workflow Forms matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormListByWorkflowIdActiveOnly`: Lists active Case Workflow Forms for a parent Case Workflow that the
      caller holds a role grant on. _(permissions from `CaseWorkflowFormService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFormListByWorkflowGuidActiveOnly`: Lists active Case Workflow Forms for a parent Case Workflow (by
      Guid) that the caller holds a role grant on. _(permissions from `CaseWorkflowFormService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFormGet`: Returns a single Case Workflow Form by Id. _(permissions from
      `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormGetByGuid`: Returns a single Case Workflow Form by Id. _(permissions from
      `CaseWorkflowFormService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Form Test Bench (`case-workflow-form-test-bench`)

- **Hierarchy Placement:** Level 4, child of Interaction Forms Supervisor
- **Semantic Persona:** Owns the case workflow form slice and runs the side-effect-free rehearsals: validation, rule
  parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** case, workflow, form, test, bench
- **Toolbox (Max 15):**
    * `CaseWorkflowFormValidate`: Validates a Case Workflow Form without saving it and returns every failure. _(
      permissions from `CaseWorkflowFormService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Form Author (`case-workflow-form-author`)

- **Hierarchy Placement:** Level 4, child of Interaction Forms Supervisor
- **Semantic Persona:** Owns the case workflow form slice and is the only agent that mutates this slice; every call
  needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** case, workflow, form, author
- **Toolbox (Max 15):**
    * `CaseWorkflowFormList`: Lists Case Workflow Forms visible to the caller's tenant, keyset-paged and capped. _(
      permissions from `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormFilterFields`: Lists the fields a query builder JSON filter over Case Workflow Forms may use. _(
      permissions from `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormFilter`: Returns the Case Workflow Forms matching query builder JSON, keyset-paged and capped. _(
      permissions from `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormCount`: Counts the Case Workflow Forms matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormListByWorkflowIdActiveOnly`: Lists active Case Workflow Forms for a parent Case Workflow that the
      caller holds a role grant on. _(permissions from `CaseWorkflowFormService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFormListByWorkflowGuidActiveOnly`: Lists active Case Workflow Forms for a parent Case Workflow (by
      Guid) that the caller holds a role grant on. _(permissions from `CaseWorkflowFormService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFormGet`: Returns a single Case Workflow Form by Id. _(permissions from
      `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormGetByGuid`: Returns a single Case Workflow Form by Id. _(permissions from
      `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormValidate`: Validates a Case Workflow Form without saving it and returns every failure. _(
      permissions from `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormCreate`: Creates a Case Workflow Form; calling twice creates two. **Write — confirmation
      required** _(permissions from `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormUpdate`: Updates a Case Workflow Form; overwrites, writes a version-audit row. **Write —
      confirmation required** _(permissions from `CaseWorkflowFormService.RequiredPermissions`)_
    * `CaseWorkflowFormDelete`: Soft-deletes a Case Workflow Form; reversible only by direct database action. **Write —
      confirmation required** _(permissions from `CaseWorkflowFormService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Form Entries and Macro Executions (`form-entries-and-macro-executions`)

- **Hierarchy Placement:** Level 4, child of Interaction Forms Supervisor
- **Semantic Persona:** Owns the form entries and macro executions slice end to end, which is small enough that reads
  and writes stay with one agent. It holds 2 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** case, workflow, form, entry, display, execution, by, key, value, macro
- **Toolbox (Max 15):**
    * `CaseWorkflowFormEntryInsert`: Submits a CaseWorkflowForm entry for a case; dispatches that form's
      notification/webhook if enabled. **Write — confirmation required** _(permissions from
      `CaseWorkflowFormEntryService.RequiredPermissions`)_
    * `CaseWorkflowFormEntryValidate`: Validates a Case Workflow Form Entry without saving it and returns every failure.
      _(permissions from `CaseWorkflowFormEntryService.RequiredPermissions`)_
    * `CaseWorkflowFormEntryListByCaseKeyValue`: Lists case workflow form entries for a Case Key/Value pair, newest
      first, scoped to the caller's tenant and role grants. _(permissions from
      `CaseWorkflowFormEntryService.RequiredPermissions`)_
    * `CaseWorkflowDisplayExecutionExecute`: Renders a case workflow display HTML template with the case's stored
      payload tokens replaced. _(permissions from `CaseWorkflowDisplayExecutionService.RequiredPermissions`)_
    * `CaseWorkflowFormEntryByCaseKeyValueGet`: Lists Case Workflow Form Entries for a Case Key and Case Key Value,
      newest case first, limited to the caller's tenant and to Case Workflows and Statuses the caller's role holds. _(
      permissions from `CaseWorkflowFormEntryByCaseKeyValueService.RequiredPermissions`)_
    * `CaseWorkflowFormEntryValueListByCaseWorkflowFormEntryId`: Lists the submitted field name/value rows for a
      CaseWorkflowFormEntry, newest first, scoped to the caller's tenant and role grants. _(permissions from
      `CaseWorkflowFormEntryValueService.RequiredPermissions`)_
    * `CaseWorkflowMacroExecutionExecute`: Executes an active case workflow macro against a case, sending the macro's
      configured notification and/or HTTP endpoint call. **Write — confirmation required** _(permissions from
      `CaseWorkflowMacroExecutionService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Interaction Macros Supervisor

- **Hierarchy Placement:** Level 3, child of Case Workflow Governance
- **Semantic Persona:** Routes within the interaction macros slice of case workflow governance. Holds no tools; it
  exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** interaction macros, case, workflow, macro
- **Toolbox (Max 15):** none — routes to 3 leaf agents covering 11 tools
- **Downstream Dependencies:** `case-workflow-macro-inspector`, `case-workflow-macro-test-bench`,
  `case-workflow-macro-author`

#### Case Workflow Macro Inspector (`case-workflow-macro-inspector`)

- **Hierarchy Placement:** Level 4, child of Interaction Macros Supervisor
- **Semantic Persona:** Owns the case workflow macro slice and reads only; it never changes anything and is the safe
  default for any question about current state.
- **Semantic Routing Keywords:** case, workflow, macro, inspector
- **Toolbox (Max 15):**
    * `CaseWorkflowMacroList`: Lists Case Workflow Macros visible to the caller's tenant, keyset-paged and capped. _(
      permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroFilterFields`: Lists the fields a query builder JSON filter over Case Workflow Macros may use.
      _(permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroFilter`: Returns the Case Workflow Macros matching query builder JSON, keyset-paged and capped.
      _(permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroCount`: Counts the Case Workflow Macros matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroListByWorkflowIdActiveOnly`: Lists active Case Workflow Macros for a parent Case Workflow that
      the caller holds a role grant on. _(permissions from `CaseWorkflowMacroService.ActiveOnlyPermissions`)_
    * `CaseWorkflowMacroListByWorkflowGuidActiveOnly`: Lists active Case Workflow Macros for a parent Case Workflow (by
      Guid) that the caller holds a role grant on. _(permissions from `CaseWorkflowMacroService.ActiveOnlyPermissions`)_
    * `CaseWorkflowMacroGet`: Returns a single Case Workflow Macro by Id. _(permissions from
      `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroGetByGuid`: Returns a single Case Workflow Macro by Id. _(permissions from
      `CaseWorkflowMacroService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Macro Test Bench (`case-workflow-macro-test-bench`)

- **Hierarchy Placement:** Level 4, child of Interaction Macros Supervisor
- **Semantic Persona:** Owns the case workflow macro slice and runs the side-effect-free rehearsals: validation, rule
  parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** case, workflow, macro, test, bench
- **Toolbox (Max 15):**
    * `CaseWorkflowMacroValidate`: Validates a Case Workflow Macro without saving it and returns every failure. _(
      permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Macro Author (`case-workflow-macro-author`)

- **Hierarchy Placement:** Level 4, child of Interaction Macros Supervisor
- **Semantic Persona:** Owns the case workflow macro slice and is the only agent that mutates this slice; every call
  needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** case, workflow, macro, author
- **Toolbox (Max 15):**
    * `CaseWorkflowMacroList`: Lists Case Workflow Macros visible to the caller's tenant, keyset-paged and capped. _(
      permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroFilterFields`: Lists the fields a query builder JSON filter over Case Workflow Macros may use.
      _(permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroFilter`: Returns the Case Workflow Macros matching query builder JSON, keyset-paged and capped.
      _(permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroCount`: Counts the Case Workflow Macros matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroListByWorkflowIdActiveOnly`: Lists active Case Workflow Macros for a parent Case Workflow that
      the caller holds a role grant on. _(permissions from `CaseWorkflowMacroService.ActiveOnlyPermissions`)_
    * `CaseWorkflowMacroListByWorkflowGuidActiveOnly`: Lists active Case Workflow Macros for a parent Case Workflow (by
      Guid) that the caller holds a role grant on. _(permissions from `CaseWorkflowMacroService.ActiveOnlyPermissions`)_
    * `CaseWorkflowMacroGet`: Returns a single Case Workflow Macro by Id. _(permissions from
      `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroGetByGuid`: Returns a single Case Workflow Macro by Id. _(permissions from
      `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroValidate`: Validates a Case Workflow Macro without saving it and returns every failure. _(
      permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroCreate`: Creates a Case Workflow Macro; calling twice creates two. **Write — confirmation
      required** _(permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroUpdate`: Updates a Case Workflow Macro; overwrites, writes a version-audit row. **Write —
      confirmation required** _(permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
    * `CaseWorkflowMacroDelete`: Soft-deletes a Case Workflow Macro; reversible only by direct database action.
      **Write — confirmation required** _(permissions from `CaseWorkflowMacroService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Routing Extraction Supervisor

- **Hierarchy Placement:** Level 3, child of Case Workflow Governance
- **Semantic Persona:** Routes within the routing extraction slice of case workflow governance. Holds no tools; it
  exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** routing extraction, case, workflow, filter, path
- **Toolbox (Max 15):** none — routes to 7 leaf agents covering 23 tools
- **Downstream Dependencies:** `case-workflow-filter-inspector-1`, `case-workflow-filter-inspector-2`,
  `case-workflow-filter-test-bench`, `case-workflow-filter-author`, `case-workflow-x-path-inspector`,
  `case-workflow-x-path-test-bench`, `case-workflow-x-path-author`
- **Cross-Domain Reach:** `completions`

#### Case Workflow Filter Inspector 1 (`case-workflow-filter-inspector-1`)

- **Hierarchy Placement:** Level 4, child of Routing Extraction Supervisor
- **Semantic Persona:** Owns the case workflow filter slice end to end, which is small enough that reads and writes stay
  with one agent.
- **Semantic Routing Keywords:** case, workflow, filter, inspector, 1
- **Toolbox (Max 15):**
    * `CaseWorkflowFilterList`: Lists Case Workflow Filters visible to the caller's tenant, keyset-paged and capped. _(
      permissions from `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterFilterFields`: Lists the fields a query builder JSON filter over Case Workflow Filters may use.
      _(permissions from `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterFilter`: Returns the Case Workflow Filters matching query builder JSON, keyset-paged and
      capped. _(permissions from `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterCount`: Counts the Case Workflow Filters matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterListByWorkflowIdActiveOnly`: Lists active Case Workflow Filters for a parent Case Workflow that
      the caller holds a role grant on. _(permissions from `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterListByWorkflowGuidActiveOnly`: Lists active Case Workflow Filters for a parent Case Workflow
      (by Guid) that the caller holds a role grant on. _(permissions from
      `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterGet`: Returns a single Case Workflow Filter by Id. _(permissions from
      `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Filter Inspector 2 (`case-workflow-filter-inspector-2`)

- **Hierarchy Placement:** Level 4, child of Routing Extraction Supervisor
- **Semantic Persona:** Owns the case workflow filter slice end to end, which is small enough that reads and writes stay
  with one agent.
- **Semantic Routing Keywords:** case, workflow, filter, inspector, 2
- **Toolbox (Max 15):**
    * `CaseWorkflowFilterGetByGuid`: Returns a single Case Workflow Filter by Guid. _(permissions from
      `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Filter Test Bench (`case-workflow-filter-test-bench`)

- **Hierarchy Placement:** Level 4, child of Routing Extraction Supervisor
- **Semantic Persona:** Owns the case workflow filter slice and runs the side-effect-free rehearsals: validation, rule
  parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** case, workflow, filter, test, bench
- **Toolbox (Max 15):**
    * `CaseWorkflowFilterValidate`: Validates a Case Workflow Filter without saving it and returns every failure. _(
      permissions from `CaseWorkflowFilterService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Filter Author (`case-workflow-filter-author`)

- **Hierarchy Placement:** Level 4, child of Routing Extraction Supervisor
- **Semantic Persona:** Owns the case workflow filter slice and is the only agent that mutates this slice; every call
  needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** case, workflow, filter, author
- **Toolbox (Max 15):**
    * `CaseWorkflowFilterList`: Lists Case Workflow Filters visible to the caller's tenant, keyset-paged and capped. _(
      permissions from `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterFilterFields`: Lists the fields a query builder JSON filter over Case Workflow Filters may use.
      _(permissions from `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterFilter`: Returns the Case Workflow Filters matching query builder JSON, keyset-paged and
      capped. _(permissions from `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterCount`: Counts the Case Workflow Filters matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterListByWorkflowIdActiveOnly`: Lists active Case Workflow Filters for a parent Case Workflow that
      the caller holds a role grant on. _(permissions from `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterListByWorkflowGuidActiveOnly`: Lists active Case Workflow Filters for a parent Case Workflow
      (by Guid) that the caller holds a role grant on. _(permissions from
      `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterGet`: Returns a single Case Workflow Filter by Id. _(permissions from
      `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterGetByGuid`: Returns a single Case Workflow Filter by Guid. _(permissions from
      `CaseWorkflowFilterService.ActiveOnlyPermissions`)_
    * `CaseWorkflowFilterValidate`: Validates a Case Workflow Filter without saving it and returns every failure. _(
      permissions from `CaseWorkflowFilterService.RequiredPermissions`)_
    * `CaseWorkflowFilterCreate`: Creates a Case Workflow Filter; calling twice creates two. **Write — confirmation
      required** _(permissions from `CaseWorkflowFilterService.RequiredPermissions`)_
    * `CaseWorkflowFilterUpdate`: Updates a Case Workflow Filter; overwrites, writes a version-audit row. **Write —
      confirmation required** _(permissions from `CaseWorkflowFilterService.RequiredPermissions`)_
    * `CaseWorkflowFilterDelete`: Soft-deletes a Case Workflow Filter; reversible only by direct database action.
      **Write — confirmation required** _(permissions from `CaseWorkflowFilterService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow X Path Inspector (`case-workflow-x-path-inspector`)

- **Hierarchy Placement:** Level 4, child of Routing Extraction Supervisor
- **Semantic Persona:** Owns the case workflow x path slice and reads only; it never changes anything and is the safe
  default for any question about current state.
- **Semantic Routing Keywords:** case, workflow, path, inspector
- **Toolbox (Max 15):**
    * `CaseWorkflowXPathList`: Lists Case Workflow XPath extractions visible to the caller's tenant, keyset-paged and
      capped. _(permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathFilterFields`: Lists the fields a query builder JSON filter over Case WorkflowXPaths may use. _(
      permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathFilter`: Returns the Case WorkflowXPaths matching query builder JSON, keyset-paged and capped.
      _(permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathCount`: Counts the Case WorkflowXPaths matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathListByWorkflowIdActiveDrillOnly`: Lists active, Drill-enabled Case Workflow XPath extractions
      for a parent Case Workflow that the caller holds a role grant on. _(permissions from
      `CaseWorkflowXPathService.ActiveOnlyPermissions`)_
    * `CaseWorkflowXPathListByWorkflowGuidActiveDrillOnly`: Lists active, Drill-enabled Case Workflow XPath extractions
      for a parent Case Workflow (by Guid) that the caller holds a role grant on. _(permissions from
      `CaseWorkflowXPathService.ActiveOnlyPermissions`)_
    * `CaseWorkflowXPathGet`: Returns a single Case Workflow XPath extraction by Id. _(permissions from
      `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathGetByGuid`: Returns a single Case Workflow XPath extraction by Id. _(permissions from
      `CaseWorkflowXPathService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow X Path Test Bench (`case-workflow-x-path-test-bench`)

- **Hierarchy Placement:** Level 4, child of Routing Extraction Supervisor
- **Semantic Persona:** Owns the case workflow x path slice and runs the side-effect-free rehearsals: validation, rule
  parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** case, workflow, path, test, bench
- **Toolbox (Max 15):**
    * `CaseWorkflowXPathValidate`: Validates a Case WorkflowXPath without saving it and returns every failure. _(
      permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow X Path Author (`case-workflow-x-path-author`)

- **Hierarchy Placement:** Level 4, child of Routing Extraction Supervisor
- **Semantic Persona:** Owns the case workflow x path slice and is the only agent that mutates this slice; every call
  needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** case, workflow, path, author
- **Toolbox (Max 15):**
    * `CaseWorkflowXPathList`: Lists Case Workflow XPath extractions visible to the caller's tenant, keyset-paged and
      capped. _(permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathFilterFields`: Lists the fields a query builder JSON filter over Case WorkflowXPaths may use. _(
      permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathFilter`: Returns the Case WorkflowXPaths matching query builder JSON, keyset-paged and capped.
      _(permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathCount`: Counts the Case WorkflowXPaths matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathListByWorkflowIdActiveDrillOnly`: Lists active, Drill-enabled Case Workflow XPath extractions
      for a parent Case Workflow that the caller holds a role grant on. _(permissions from
      `CaseWorkflowXPathService.ActiveOnlyPermissions`)_
    * `CaseWorkflowXPathListByWorkflowGuidActiveDrillOnly`: Lists active, Drill-enabled Case Workflow XPath extractions
      for a parent Case Workflow (by Guid) that the caller holds a role grant on. _(permissions from
      `CaseWorkflowXPathService.ActiveOnlyPermissions`)_
    * `CaseWorkflowXPathGet`: Returns a single Case Workflow XPath extraction by Id. _(permissions from
      `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathGetByGuid`: Returns a single Case Workflow XPath extraction by Id. _(permissions from
      `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathValidate`: Validates a Case WorkflowXPath without saving it and returns every failure. _(
      permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathCreate`: Creates a Case Workflow XPath extraction; calling twice creates two. **Write —
      confirmation required** _(permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathUpdate`: Updates a Case Workflow XPath extraction; overwrites, writes a version-audit row.
      **Write — confirmation required** _(permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
    * `CaseWorkflowXPathDelete`: Soft-deletes a Case Workflow XPath extraction; reversible only by direct database
      action. **Write — confirmation required** _(permissions from `CaseWorkflowXPathService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Routing Status Supervisor

- **Hierarchy Placement:** Level 3, child of Case Workflow Governance
- **Semantic Persona:** Routes within the routing status slice of case workflow governance. Holds no tools; it exists so
  the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** routing status, case, workflow, status, priority
- **Toolbox (Max 15):** none — routes to 5 leaf agents covering 13 tools
- **Downstream Dependencies:** `case-workflow-status-inspector-1`, `case-workflow-status-inspector-2`,
  `case-workflow-status-test-bench`, `case-workflow-status-author`, `case-workflow-priority`

#### Case Workflow Status Inspector 1 (`case-workflow-status-inspector-1`)

- **Hierarchy Placement:** Level 4, child of Routing Status Supervisor
- **Semantic Persona:** Owns the case workflow status slice end to end, which is small enough that reads and writes stay
  with one agent.
- **Semantic Routing Keywords:** case, workflow, status, inspector, 1
- **Toolbox (Max 15):**
    * `CaseWorkflowStatusList`: Lists Case Workflow Statuses visible to the caller's tenant, keyset-paged and capped. _(
      permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusFilterFields`: Lists the fields a query builder JSON filter over Case Workflow Statuses may
      use. _(permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusFilter`: Returns the Case Workflow Statuses matching query builder JSON, keyset-paged and
      capped. _(permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusCount`: Counts the Case Workflow Statuses matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusListByWorkflowGuid`: Lists Case Workflow Statuses for a parent Case Workflow (by Guid),
      including inactive ones. _(permissions from `CaseWorkflowStatusService.ListByWorkflowPermissions`)_
    * `CaseWorkflowStatusListByWorkflowIdActiveOnly`: Lists active Case Workflow Statuses for a parent Case Workflow
      that the caller holds a role grant on. _(permissions from `CaseWorkflowStatusService.ActiveOnlyPermissions`)_
    * `CaseWorkflowStatusListByWorkflowGuidActiveOnly`: Lists active Case Workflow Statuses for a parent Case Workflow
      (by Guid) that the caller holds a role grant on. _(permissions from
      `CaseWorkflowStatusService.ActiveOnlyPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Status Inspector 2 (`case-workflow-status-inspector-2`)

- **Hierarchy Placement:** Level 4, child of Routing Status Supervisor
- **Semantic Persona:** Owns the case workflow status slice end to end, which is small enough that reads and writes stay
  with one agent.
- **Semantic Routing Keywords:** case, workflow, status, inspector, 2
- **Toolbox (Max 15):**
    * `CaseWorkflowStatusGet`: Returns a single Case Workflow Status by Id. _(permissions from
      `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusGetByGuid`: Returns a single Case Workflow Status by Id. _(permissions from
      `CaseWorkflowStatusService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Status Test Bench (`case-workflow-status-test-bench`)

- **Hierarchy Placement:** Level 4, child of Routing Status Supervisor
- **Semantic Persona:** Owns the case workflow status slice and runs the side-effect-free rehearsals: validation, rule
  parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** case, workflow, status, test, bench
- **Toolbox (Max 15):**
    * `CaseWorkflowStatusValidate`: Validates a Case Workflow Status without saving it and returns every failure. _(
      permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Status Author (`case-workflow-status-author`)

- **Hierarchy Placement:** Level 4, child of Routing Status Supervisor
- **Semantic Persona:** Owns the case workflow status slice and is the only agent that mutates this slice; every call
  needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** case, workflow, status, author
- **Toolbox (Max 15):**
    * `CaseWorkflowStatusList`: Lists Case Workflow Statuses visible to the caller's tenant, keyset-paged and capped. _(
      permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusFilterFields`: Lists the fields a query builder JSON filter over Case Workflow Statuses may
      use. _(permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusFilter`: Returns the Case Workflow Statuses matching query builder JSON, keyset-paged and
      capped. _(permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusCount`: Counts the Case Workflow Statuses matching query builder JSON, optionally grouped by a
      field. _(permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusListByWorkflowGuid`: Lists Case Workflow Statuses for a parent Case Workflow (by Guid),
      including inactive ones. _(permissions from `CaseWorkflowStatusService.ListByWorkflowPermissions`)_
    * `CaseWorkflowStatusListByWorkflowIdActiveOnly`: Lists active Case Workflow Statuses for a parent Case Workflow
      that the caller holds a role grant on. _(permissions from `CaseWorkflowStatusService.ActiveOnlyPermissions`)_
    * `CaseWorkflowStatusListByWorkflowGuidActiveOnly`: Lists active Case Workflow Statuses for a parent Case Workflow
      (by Guid) that the caller holds a role grant on. _(permissions from
      `CaseWorkflowStatusService.ActiveOnlyPermissions`)_
    * `CaseWorkflowStatusGet`: Returns a single Case Workflow Status by Id. _(permissions from
      `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusGetByGuid`: Returns a single Case Workflow Status by Id. _(permissions from
      `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusValidate`: Validates a Case Workflow Status without saving it and returns every failure. _(
      permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusCreate`: Creates a Case Workflow Status; calling twice creates two. **Write — confirmation
      required** _(permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusUpdate`: Updates a Case Workflow Status; overwrites, writes a version-audit row. **Write —
      confirmation required** _(permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
    * `CaseWorkflowStatusDelete`: Soft-deletes a Case Workflow Status; reversible only by direct database action.
      **Write — confirmation required** _(permissions from `CaseWorkflowStatusService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Case Workflow Priority (`case-workflow-priority`)

- **Hierarchy Placement:** Level 4, child of Routing Status Supervisor
- **Semantic Persona:** Owns the case workflow priority slice end to end, which is small enough that reads and writes
  stay with one agent.
- **Semantic Routing Keywords:** case, workflow, priority
- **Toolbox (Max 15):**
    * `CaseWorkflowPriorityList`: Lists the fixed Case priority levels (High, Medium, Low) -- the same for every Case
      Workflow and every tenant. _(permissions from `CaseWorkflowPriorityService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

---

## Analytics Reporting (`data-analyst`)

#### Analytics Reporting

- **Hierarchy Placement:** Level 2, child of Jooby
- **Semantic Persona:** Owns the visualisation registry, its datasources, parameters and the role grants over them.
  Produces and governs the reporting surface, and reads detection output without ever altering it.
- **Semantic Routing Keywords:** visualisation, dashboard, chart, report, datasource, parameter, series, directory
- **Toolbox (Max 15):** none — routes to 3 supervisors covering 44 tools
- **Downstream Dependencies:** `data-analyst.visualisation.definition`, `data-analyst.visualisation.parameters`,
  `data-analyst.visualisation.registry`

#### Visualisation Definition Supervisor

- **Hierarchy Placement:** Level 3, child of Analytics Reporting
- **Semantic Persona:** Routes within the visualisation definition slice of analytics reporting. Holds no tools; it
  exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** visualisation definition, visualisation, registry, role, datasource, command,
  execution, series
- **Toolbox (Max 15):** none — routes to 4 leaf agents covering 16 tools
- **Downstream Dependencies:** `visualisation-registry-inspector`, `visualisation-registry-test-bench`,
  `visualisation-registry-author`, `visualisation-role-grants-and-execution`

#### Visualisation Registry Inspector (`visualisation-registry-inspector`)

- **Hierarchy Placement:** Level 4, child of Visualisation Definition Supervisor
- **Semantic Persona:** Owns the visualisation registry slice and reads only; it never changes anything and is the safe
  default for any question about current state.
- **Semantic Routing Keywords:** visualisation, registry, inspector
- **Toolbox (Max 15):**
    * `VisualisationRegistryList`: Lists Visualisation Registries visible to the caller's tenant, keyset-paged and
      capped. _(permissions from `VisualisationRegistryService.ListPermissions`)_
    * `VisualisationRegistryFilterFields`: Lists the fields a query builder JSON filter over Visualisation Registrys may
      use. _(permissions from `VisualisationRegistryService.ListPermissions`)_
    * `VisualisationRegistryFilter`: Returns the Visualisation Registrys matching query builder JSON, keyset-paged and
      capped. _(permissions from `VisualisationRegistryService.ListPermissions`)_
    * `VisualisationRegistryCount`: Counts the Visualisation Registrys matching query builder JSON, optionally grouped
      by a field. _(permissions from `VisualisationRegistryService.ListPermissions`)_
    * `VisualisationRegistryGet`: Returns a single Visualisation Registry by Id. _(permissions from
      `VisualisationRegistryService.ReadPermissions`)_
    * `VisualisationRegistryGetByGuid`: Returns a single Visualisation Registry by Id. _(permissions from
      `VisualisationRegistryService.ReadPermissions`)_
    * `VisualisationRegistryGetByGuidActiveOnly`: Returns a single active Visualisation Registry by Guid, for embedded
      recall, provided the caller holds a role grant on it. _(permissions from
      `VisualisationRegistryService.ReadPermissions`)_
    * `VisualisationRegistryListByShowInDirectory`: Lists active, Show In Directory Visualisation Registries that the
      caller holds a role grant on. _(permissions from `VisualisationRegistryService.ShowInDirectoryPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Visualisation Registry Test Bench (`visualisation-registry-test-bench`)

- **Hierarchy Placement:** Level 4, child of Visualisation Definition Supervisor
- **Semantic Persona:** Owns the visualisation registry slice and runs the side-effect-free rehearsals: validation, rule
  parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** visualisation, registry, test, bench
- **Toolbox (Max 15):**
    * `VisualisationRegistryValidate`: Validates a Visualisation Registry without saving it and returns every failure.
      _(permissions from `VisualisationRegistryService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Visualisation Registry Author (`visualisation-registry-author`)

- **Hierarchy Placement:** Level 4, child of Visualisation Definition Supervisor
- **Semantic Persona:** Owns the visualisation registry slice and is the only agent that mutates this slice; every call
  needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** visualisation, registry, author
- **Toolbox (Max 15):**
    * `VisualisationRegistryList`: Lists Visualisation Registries visible to the caller's tenant, keyset-paged and
      capped. _(permissions from `VisualisationRegistryService.ListPermissions`)_
    * `VisualisationRegistryFilterFields`: Lists the fields a query builder JSON filter over Visualisation Registrys may
      use. _(permissions from `VisualisationRegistryService.ListPermissions`)_
    * `VisualisationRegistryFilter`: Returns the Visualisation Registrys matching query builder JSON, keyset-paged and
      capped. _(permissions from `VisualisationRegistryService.ListPermissions`)_
    * `VisualisationRegistryCount`: Counts the Visualisation Registrys matching query builder JSON, optionally grouped
      by a field. _(permissions from `VisualisationRegistryService.ListPermissions`)_
    * `VisualisationRegistryGet`: Returns a single Visualisation Registry by Id. _(permissions from
      `VisualisationRegistryService.ReadPermissions`)_
    * `VisualisationRegistryGetByGuid`: Returns a single Visualisation Registry by Id. _(permissions from
      `VisualisationRegistryService.ReadPermissions`)_
    * `VisualisationRegistryGetByGuidActiveOnly`: Returns a single active Visualisation Registry by Guid, for embedded
      recall, provided the caller holds a role grant on it. _(permissions from
      `VisualisationRegistryService.ReadPermissions`)_
    * `VisualisationRegistryListByShowInDirectory`: Lists active, Show In Directory Visualisation Registries that the
      caller holds a role grant on. _(permissions from `VisualisationRegistryService.ShowInDirectoryPermissions`)_
    * `VisualisationRegistryValidate`: Validates a Visualisation Registry without saving it and returns every failure.
      _(permissions from `VisualisationRegistryService.WritePermissions`)_
    * `VisualisationRegistryCreate`: Creates a Visualisation Registry; calling twice creates two. **Write — confirmation
      required** _(permissions from `VisualisationRegistryService.WritePermissions`)_
    * `VisualisationRegistryUpdate`: Updates a Visualisation Registry; overwrites, writes a version-audit row. **Write —
      confirmation required** _(permissions from `VisualisationRegistryService.WritePermissions`)_
    * `VisualisationRegistryDelete`: Soft-deletes a Visualisation Registry; reversible only by direct database action.
      **Write — confirmation required** _(permissions from `VisualisationRegistryService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Visualisation Role Grants and Execution (`visualisation-role-grants-and-execution`)

- **Hierarchy Placement:** Level 4, child of Visualisation Definition Supervisor
- **Semantic Persona:** Owns the visualisation role grants and execution slice end to end, which is small enough that
  reads and writes stay with one agent. It holds 2 mutating tools, so a confirmation is required before any of them
  runs.
- **Semantic Routing Keywords:** visualisation, registry, role, datasource, command, execution, series
- **Toolbox (Max 15):**
    * `VisualisationRegistryRoleListByVisualisationRegistryGuid`: Lists the Role grants for a Visualisation Registry. _(
      permissions from `VisualisationRegistryRoleService.RequiredPermissions`)_
    * `VisualisationRegistryRoleCreate`: Grants a Role access to a Visualisation Registry; calling twice creates two
      grants. **Write — confirmation required** _(permissions from
      `VisualisationRegistryRoleService.RequiredPermissions`)_
    * `VisualisationRegistryRoleDelete`: Removes a Role's grant on a Visualisation Registry; reversible only by direct
      database action. **Write — confirmation required** _(permissions from
      `VisualisationRegistryRoleService.RequiredPermissions`)_
    * `VisualisationRegistryDatasourceCommandExecutionExecute`: Executes a Visualisation Registry Datasource command
      with caller-supplied parameter values and returns its rows. _(permissions from
      `VisualisationRegistryDatasourceCommandExecutionService.RequiredPermissions`)_
    * `VisualisationRegistryDatasourceSeriesListByVisualisationRegistryDatasourceId`: Lists the series definitions (name
      and inferred data type of each SQL result column) of a Visualisation Registry Datasource; server-derived, never
      user-created. _(permissions from `VisualisationRegistryDatasourceSeriesService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Visualisation Parameters Supervisor

- **Hierarchy Placement:** Level 3, child of Analytics Reporting
- **Semantic Persona:** Routes within the visualisation parameters slice of analytics reporting. Holds no tools; it
  exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** visualisation parameters, visualisation, registry, parameter
- **Toolbox (Max 15):** none — routes to 3 leaf agents covering 11 tools
- **Downstream Dependencies:** `visualisation-registry-parameter-inspector`,
  `visualisation-registry-parameter-test-bench`, `visualisation-registry-parameter-author`

#### Visualisation Registry Parameter Inspector (`visualisation-registry-parameter-inspector`)

- **Hierarchy Placement:** Level 4, child of Visualisation Parameters Supervisor
- **Semantic Persona:** Owns the visualisation registry parameter slice and reads only; it never changes anything and is
  the safe default for any question about current state.
- **Semantic Routing Keywords:** visualisation, registry, parameter, inspector
- **Toolbox (Max 15):**
    * `VisualisationRegistryParameterList`: Lists Visualisation Registry Parameters for the caller's tenant,
      keyset-paged and capped. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterFilterFields`: Lists the fields a query builder JSON filter over Visualisation
      Registry Parameters may use. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterFilter`: Returns the Visualisation Registry Parameters matching query builder JSON,
      keyset-paged and capped. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterCount`: Counts the Visualisation Registry Parameters matching query builder JSON,
      optionally grouped by a field. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterGet`: Returns one Visualisation Registry Parameter by its id, scoped to the
      caller's tenant. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterGetByGuid`: Returns one Visualisation Registry Parameter by its id, scoped to the
      caller's tenant. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterGetByVisualisationRegistryId`: Lists the Parameters belonging to a given
      Visualisation Registry, ordered by Id, scoped to the caller's tenant. _(permissions from
      `VisualisationRegistryParameterService.ActiveOnlyPermissions`)_
    * `VisualisationRegistryParameterGetByVisualisationRegistryIdActiveOnly`: Lists the active Parameters belonging to a
      given Visualisation Registry that the caller holds a role grant on, scoped to the caller's tenant. _(permissions
      from `VisualisationRegistryParameterService.ActiveOnlyPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Visualisation Registry Parameter Test Bench (`visualisation-registry-parameter-test-bench`)

- **Hierarchy Placement:** Level 4, child of Visualisation Parameters Supervisor
- **Semantic Persona:** Owns the visualisation registry parameter slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** visualisation, registry, parameter, test, bench
- **Toolbox (Max 15):**
    * `VisualisationRegistryParameterValidate`: Validates a Visualisation Registry Parameter without saving it and
      returns every failure. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Visualisation Registry Parameter Author (`visualisation-registry-parameter-author`)

- **Hierarchy Placement:** Level 4, child of Visualisation Parameters Supervisor
- **Semantic Persona:** Owns the visualisation registry parameter slice and is the only agent that mutates this slice;
  every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** visualisation, registry, parameter, author
- **Toolbox (Max 15):**
    * `VisualisationRegistryParameterList`: Lists Visualisation Registry Parameters for the caller's tenant,
      keyset-paged and capped. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterFilterFields`: Lists the fields a query builder JSON filter over Visualisation
      Registry Parameters may use. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterFilter`: Returns the Visualisation Registry Parameters matching query builder JSON,
      keyset-paged and capped. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterCount`: Counts the Visualisation Registry Parameters matching query builder JSON,
      optionally grouped by a field. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterGet`: Returns one Visualisation Registry Parameter by its id, scoped to the
      caller's tenant. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterGetByGuid`: Returns one Visualisation Registry Parameter by its id, scoped to the
      caller's tenant. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterGetByVisualisationRegistryId`: Lists the Parameters belonging to a given
      Visualisation Registry, ordered by Id, scoped to the caller's tenant. _(permissions from
      `VisualisationRegistryParameterService.ActiveOnlyPermissions`)_
    * `VisualisationRegistryParameterGetByVisualisationRegistryIdActiveOnly`: Lists the active Parameters belonging to a
      given Visualisation Registry that the caller holds a role grant on, scoped to the caller's tenant. _(permissions
      from `VisualisationRegistryParameterService.ActiveOnlyPermissions`)_
    * `VisualisationRegistryParameterValidate`: Validates a Visualisation Registry Parameter without saving it and
      returns every failure. _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterCreate`: Registers a Parameter under a Visualisation Registry in the caller's
      tenant; calling twice creates two. **Write — confirmation required** _(permissions from
      `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterUpdate`: Updates a Parameter in the caller's tenant by id. **Write — confirmation
      required** _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
    * `VisualisationRegistryParameterDelete`: Soft-deletes a Parameter in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `VisualisationRegistryParameterService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Visualisation Registry Supervisor

- **Hierarchy Placement:** Level 3, child of Analytics Reporting
- **Semantic Persona:** Routes within the visualisation registry slice of analytics reporting. Holds no tools; it exists
  so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** visualisation registry, visualisation, registry, datasource, role, parameter
- **Toolbox (Max 15):** none — routes to 4 leaf agents covering 17 tools
- **Downstream Dependencies:** `visualisation-registry-datasource-inspector`,
  `visualisation-registry-datasource-test-bench`, `visualisation-registry-datasource-author`,
  `datasource-and-parameter-role-grants`
- **Cross-Domain Reach:** `visualisation-registry-inspector`, `visualisation-registry-author`,
  `visualisation-registry-parameter-inspector`, `visualisation-registry-parameter-author`

#### Visualisation Registry Datasource Inspector (`visualisation-registry-datasource-inspector`)

- **Hierarchy Placement:** Level 4, child of Visualisation Registry Supervisor
- **Semantic Persona:** Owns the visualisation registry datasource slice and reads only; it never changes anything and
  is the safe default for any question about current state.
- **Semantic Routing Keywords:** visualisation, registry, datasource, inspector
- **Toolbox (Max 15):**
    * `VisualisationRegistryDatasourceList`: Lists Datasources for the caller's tenant, keyset-paged and capped. _(
      permissions from `VisualisationRegistryDatasourceService.ListPermissions`)_
    * `VisualisationRegistryDatasourceFilterFields`: Lists the fields a query builder JSON filter over Visualisation
      Registry Datasources may use. _(permissions from `VisualisationRegistryDatasourceService.ListPermissions`)_
    * `VisualisationRegistryDatasourceFilter`: Returns the Visualisation Registry Datasources matching query builder
      JSON, keyset-paged and capped. _(permissions from `VisualisationRegistryDatasourceService.ListPermissions`)_
    * `VisualisationRegistryDatasourceCount`: Counts the Visualisation Registry Datasources matching query builder JSON,
      optionally grouped by a field. _(permissions from `VisualisationRegistryDatasourceService.ListPermissions`)_
    * `VisualisationRegistryDatasourceGet`: Returns one Datasource by id, scoped to the caller's tenant. _(permissions
      from `VisualisationRegistryDatasourceService.ReadPermissions`)_
    * `VisualisationRegistryDatasourceGetByGuid`: Returns one Datasource by Guid, scoped to the caller's tenant. _(
      permissions from `VisualisationRegistryDatasourceService.ReadPermissions`)_
    * `VisualisationRegistryDatasourceGetByVisualisationRegistryId`: Lists Datasources belonging to a given
      Visualisation Registry, scoped to the caller's tenant. _(permissions from
      `VisualisationRegistryDatasourceService.ReadByParentPermissions`)_
    * `VisualisationRegistryDatasourceGetByVisualisationRegistryIdActiveOnly`: Lists active, role-visible Datasources
      belonging to a given Visualisation Registry for the caller. _(permissions from
      `VisualisationRegistryDatasourceService.ReadByParentPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Visualisation Registry Datasource Test Bench (`visualisation-registry-datasource-test-bench`)

- **Hierarchy Placement:** Level 4, child of Visualisation Registry Supervisor
- **Semantic Persona:** Owns the visualisation registry datasource slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** visualisation, registry, datasource, test, bench
- **Toolbox (Max 15):**
    * `VisualisationRegistryDatasourceValidate`: Validates a Visualisation Registry Datasource without saving it and
      returns every failure. _(permissions from `VisualisationRegistryDatasourceService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Visualisation Registry Datasource Author (`visualisation-registry-datasource-author`)

- **Hierarchy Placement:** Level 4, child of Visualisation Registry Supervisor
- **Semantic Persona:** Owns the visualisation registry datasource slice and is the only agent that mutates this slice;
  every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** visualisation, registry, datasource, author
- **Toolbox (Max 15):**
    * `VisualisationRegistryDatasourceList`: Lists Datasources for the caller's tenant, keyset-paged and capped. _(
      permissions from `VisualisationRegistryDatasourceService.ListPermissions`)_
    * `VisualisationRegistryDatasourceFilterFields`: Lists the fields a query builder JSON filter over Visualisation
      Registry Datasources may use. _(permissions from `VisualisationRegistryDatasourceService.ListPermissions`)_
    * `VisualisationRegistryDatasourceFilter`: Returns the Visualisation Registry Datasources matching query builder
      JSON, keyset-paged and capped. _(permissions from `VisualisationRegistryDatasourceService.ListPermissions`)_
    * `VisualisationRegistryDatasourceCount`: Counts the Visualisation Registry Datasources matching query builder JSON,
      optionally grouped by a field. _(permissions from `VisualisationRegistryDatasourceService.ListPermissions`)_
    * `VisualisationRegistryDatasourceGet`: Returns one Datasource by id, scoped to the caller's tenant. _(permissions
      from `VisualisationRegistryDatasourceService.ReadPermissions`)_
    * `VisualisationRegistryDatasourceGetByGuid`: Returns one Datasource by Guid, scoped to the caller's tenant. _(
      permissions from `VisualisationRegistryDatasourceService.ReadPermissions`)_
    * `VisualisationRegistryDatasourceGetByVisualisationRegistryId`: Lists Datasources belonging to a given
      Visualisation Registry, scoped to the caller's tenant. _(permissions from
      `VisualisationRegistryDatasourceService.ReadByParentPermissions`)_
    * `VisualisationRegistryDatasourceGetByVisualisationRegistryIdActiveOnly`: Lists active, role-visible Datasources
      belonging to a given Visualisation Registry for the caller. _(permissions from
      `VisualisationRegistryDatasourceService.ReadByParentPermissions`)_
    * `VisualisationRegistryDatasourceValidate`: Validates a Visualisation Registry Datasource without saving it and
      returns every failure. _(permissions from `VisualisationRegistryDatasourceService.WritePermissions`)_
    * `VisualisationRegistryDatasourceCreate`: Registers a Datasource under a Visualisation Registry in the caller's
      tenant, validating its SQL Command; calling twice creates two. **Write — confirmation required** _(permissions
      from `VisualisationRegistryDatasourceService.WritePermissions`)_
    * `VisualisationRegistryDatasourceUpdate`: Updates a Datasource in the caller's tenant by id, re-validating its SQL
      Command. **Write — confirmation required** _(permissions from
      `VisualisationRegistryDatasourceService.WritePermissions`)_
    * `VisualisationRegistryDatasourceDelete`: Soft-deletes a Datasource in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `VisualisationRegistryDatasourceService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Datasource and Parameter Role Grants (`datasource-and-parameter-role-grants`)

- **Hierarchy Placement:** Level 4, child of Visualisation Registry Supervisor
- **Semantic Persona:** Owns the datasource and parameter role grants slice end to end, which is small enough that reads
  and writes stay with one agent. It holds 4 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** visualisation, registry, datasource, role, parameter
- **Toolbox (Max 15):**
    * `VisualisationRegistryDatasourceRoleListByVisualisationRegistryDatasourceGuid`: Lists the Role grants for a
      Visualisation Registry Datasource. _(permissions from
      `VisualisationRegistryDatasourceRoleService.RequiredPermissions`)_
    * `VisualisationRegistryDatasourceRoleCreate`: Grants a Role access to a Visualisation Registry Datasource; calling
      twice creates two grants. **Write — confirmation required** _(permissions from
      `VisualisationRegistryDatasourceRoleService.RequiredPermissions`)_
    * `VisualisationRegistryDatasourceRoleDelete`: Removes a Role's grant on a Visualisation Registry Datasource;
      reversible only by direct database action. **Write — confirmation required** _(permissions from
      `VisualisationRegistryDatasourceRoleService.RequiredPermissions`)_
    * `VisualisationRegistryParameterRoleListByVisualisationRegistryParameterGuid`: Lists the Role grants for a
      Visualisation Registry Parameter. _(permissions from
      `VisualisationRegistryParameterRoleService.RequiredPermissions`)_
    * `VisualisationRegistryParameterRoleCreate`: Grants a Role access to a Visualisation Registry Parameter; calling
      twice creates two grants. **Write — confirmation required** _(permissions from
      `VisualisationRegistryParameterRoleService.RequiredPermissions`)_
    * `VisualisationRegistryParameterRoleDelete`: Removes a Role's grant on a Visualisation Registry Parameter;
      reversible only by direct database action. **Write — confirmation required** _(permissions from
      `VisualisationRegistryParameterRoleService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

---

## Detection Model Authoring (`model-admin`)

#### Detection Model Authoring

- **Hierarchy Placement:** Level 2, child of Jooby
- **Semantic Persona:** Authors the detection model itself and the reference and abstraction material it is built from:
  request payload contract, derivations, gateway and activation rules, counters and aggregations. This is where fraud
  and AML typologies become executable rules.
- **Semantic Routing Keywords:** model, rule, activation, gateway, abstraction, TTL counter, request XPath, inline
  function, inline script, tag, sanction list, threshold
- **Toolbox (Max 15):** none — routes to 8 supervisors covering 153 tools
- **Downstream Dependencies:** `model-admin.abstraction.calculations`, `model-admin.abstraction.counters`,
  `model-admin.activation`, `model-admin.core`, `model-admin.references.derivation`, `model-admin.references.gateway`,
  `model-admin.references.payload-contract`, `model-admin.references.watchlists`
- **Cross-Domain Reach:** `completions`

#### Abstraction Calculations Supervisor

- **Hierarchy Placement:** Level 3, child of Detection Model Authoring
- **Semantic Persona:** Routes within the abstraction calculations slice of detection model authoring. Holds no tools;
  it exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** abstraction calculations, entity, analysis, model, abstraction, calculation, rule
- **Toolbox (Max 15):** none — routes to 6 leaf agents covering 25 tools
- **Downstream Dependencies:** `entity-analysis-model-abstraction-calculation-inspector`,
  `entity-analysis-model-abstraction-calculation-test-bench`, `entity-analysis-model-abstraction-calculation-author`,
  `entity-analysis-model-abstraction-rule-inspector`, `entity-analysis-model-abstraction-rule-test-bench`,
  `entity-analysis-model-abstraction-rule-author`
- **Cross-Domain Reach:** `completions`, `model-invocation-context-and-dependencies`

#### Entity Analysis Model Abstraction Calculation Inspector (`entity-analysis-model-abstraction-calculation-inspector`)

- **Hierarchy Placement:** Level 4, child of Abstraction Calculations Supervisor
- **Semantic Persona:** Owns the entity analysis model abstraction calculation slice and reads only; it never changes
  anything and is the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, abstraction, calculation, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelAbstractionCalculationList`: Lists Abstraction Calculations for the caller's tenant,
      keyset-paged and capped. _(permissions from `EntityAnalysisModelAbstractionCalculationService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationFilterFields`: Lists the fields a query builder JSON filter over
      Abstraction Calculations may use. _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationFilter`: Returns the Abstraction Calculations matching query builder
      JSON, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationCount`: Counts the Abstraction Calculations matching query builder JSON,
      optionally grouped by a field. _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationGet`: Returns one Abstraction Calculation by id, scoped to the caller's
      tenant. _(permissions from `EntityAnalysisModelAbstractionCalculationService.ReadPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationGetByGuid`: Returns one Abstraction Calculation by Guid, scoped to the
      caller's tenant. _(permissions from `EntityAnalysisModelAbstractionCalculationService.ReadPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationGetByEntityAnalysisModelId`: Lists Abstraction Calculations belonging to
      a given Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.ReadPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Abstraction Calculation Test Bench (`entity-analysis-model-abstraction-calculation-test-bench`)

- **Hierarchy Placement:** Level 4, child of Abstraction Calculations Supervisor
- **Semantic Persona:** Owns the entity analysis model abstraction calculation slice and runs the side-effect-free
  rehearsals: validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, abstraction, calculation, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelAbstractionCalculationValidate`: Validates an Abstraction Calculation without saving it and
      returns every failure. _(permissions from `EntityAnalysisModelAbstractionCalculationService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionCalculationParseRule`: Parses and compiles an Abstraction Calculation's rule text
      as the engine would and returns each located error. _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionCalculationExecute`: Runs an Abstraction Calculation against an invocation context
      as the engine would and returns the result. _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Abstraction Calculation Author (`entity-analysis-model-abstraction-calculation-author`)

- **Hierarchy Placement:** Level 4, child of Abstraction Calculations Supervisor
- **Semantic Persona:** Owns the entity analysis model abstraction calculation slice and is the only agent that mutates
  this slice; every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change
  snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, abstraction, calculation, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelAbstractionCalculationList`: Lists Abstraction Calculations for the caller's tenant,
      keyset-paged and capped. _(permissions from `EntityAnalysisModelAbstractionCalculationService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationFilterFields`: Lists the fields a query builder JSON filter over
      Abstraction Calculations may use. _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationFilter`: Returns the Abstraction Calculations matching query builder
      JSON, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationCount`: Counts the Abstraction Calculations matching query builder JSON,
      optionally grouped by a field. _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationGet`: Returns one Abstraction Calculation by id, scoped to the caller's
      tenant. _(permissions from `EntityAnalysisModelAbstractionCalculationService.ReadPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationGetByGuid`: Returns one Abstraction Calculation by Guid, scoped to the
      caller's tenant. _(permissions from `EntityAnalysisModelAbstractionCalculationService.ReadPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationGetByEntityAnalysisModelId`: Lists Abstraction Calculations belonging to
      a given Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.ReadPermissions`)_
    * `EntityAnalysisModelAbstractionCalculationValidate`: Validates an Abstraction Calculation without saving it and
      returns every failure. _(permissions from `EntityAnalysisModelAbstractionCalculationService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionCalculationParseRule`: Parses and compiles an Abstraction Calculation's rule text
      as the engine would and returns each located error. _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionCalculationExecute`: Runs an Abstraction Calculation against an invocation context
      as the engine would and returns the result. _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionCalculationCreate`: Registers an Abstraction Calculation under a Model in the
      caller's tenant; calling twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionCalculationUpdate`: Updates an Abstraction Calculation in the caller's tenant by
      id. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionCalculationDelete`: Soft-deletes an Abstraction Calculation in the caller's tenant
      by id. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionCalculationResubmit`: Resubmits a rejected Abstraction Calculation for approval
      again, unchanged, opening a fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionCalculationRetract`: Retracts a rejected Abstraction Calculation, restoring it to
      its last approved version without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelAbstractionCalculationService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Abstraction Rule Inspector (`entity-analysis-model-abstraction-rule-inspector`)

- **Hierarchy Placement:** Level 4, child of Abstraction Calculations Supervisor
- **Semantic Persona:** Owns the entity analysis model abstraction rule slice and reads only; it never changes anything
  and is the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, abstraction, rule, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelAbstractionRuleList`: Lists Abstraction Rules for the caller's tenant, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelAbstractionRuleService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionRuleFilterFields`: Lists the fields a query builder JSON filter over Abstraction
      Rules may use. _(permissions from `EntityAnalysisModelAbstractionRuleService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionRuleFilter`: Returns the Abstraction Rules matching query builder JSON,
      keyset-paged and capped. _(permissions from `EntityAnalysisModelAbstractionRuleService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionRuleCount`: Counts the Abstraction Rules matching query builder JSON, optionally
      grouped by a field. _(permissions from `EntityAnalysisModelAbstractionRuleService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionRuleGet`: Returns one Abstraction Rule by id, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelAbstractionRuleService.ReadPermissions`)_
    * `EntityAnalysisModelAbstractionRuleGetByGuid`: Returns one Abstraction Rule by Guid, scoped to the caller's
      tenant. _(permissions from `EntityAnalysisModelAbstractionRuleService.ReadPermissions`)_
    * `EntityAnalysisModelAbstractionRuleGetByEntityAnalysisModelId`: Lists Abstraction Rules belonging to a given
      Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelAbstractionRuleService.ReadPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Abstraction Rule Test Bench (`entity-analysis-model-abstraction-rule-test-bench`)

- **Hierarchy Placement:** Level 4, child of Abstraction Calculations Supervisor
- **Semantic Persona:** Owns the entity analysis model abstraction rule slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, abstraction, rule, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelAbstractionRuleValidate`: Validates an Abstraction Rule without saving it and returns every
      failure. _(permissions from `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionRuleParseRule`: Parses and compiles an Abstraction Rule's rule text as the engine
      would and returns each located error. _(permissions from
      `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionRuleExecute`: Runs an Abstraction Rule against an invocation context as the engine
      would and returns the result. _(permissions from `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionRuleBuildRuleFromBuilderJson`: Turns query builder JSON into an Abstraction Rule's
      rule text as the browser builder would, and parses it. _(permissions from
      `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Abstraction Rule Author (`entity-analysis-model-abstraction-rule-author`)

- **Hierarchy Placement:** Level 4, child of Abstraction Calculations Supervisor
- **Semantic Persona:** Owns the entity analysis model abstraction rule slice and is the only agent that mutates this
  slice; every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, abstraction, rule, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelAbstractionRuleList`: Lists Abstraction Rules for the caller's tenant, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelAbstractionRuleService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionRuleFilterFields`: Lists the fields a query builder JSON filter over Abstraction
      Rules may use. _(permissions from `EntityAnalysisModelAbstractionRuleService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionRuleFilter`: Returns the Abstraction Rules matching query builder JSON,
      keyset-paged and capped. _(permissions from `EntityAnalysisModelAbstractionRuleService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionRuleCount`: Counts the Abstraction Rules matching query builder JSON, optionally
      grouped by a field. _(permissions from `EntityAnalysisModelAbstractionRuleService.ListPermissions`)_
    * `EntityAnalysisModelAbstractionRuleGet`: Returns one Abstraction Rule by id, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelAbstractionRuleService.ReadPermissions`)_
    * `EntityAnalysisModelAbstractionRuleGetByGuid`: Returns one Abstraction Rule by Guid, scoped to the caller's
      tenant. _(permissions from `EntityAnalysisModelAbstractionRuleService.ReadPermissions`)_
    * `EntityAnalysisModelAbstractionRuleGetByEntityAnalysisModelId`: Lists Abstraction Rules belonging to a given
      Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelAbstractionRuleService.ReadPermissions`)_
    * `EntityAnalysisModelAbstractionRuleValidate`: Validates an Abstraction Rule without saving it and returns every
      failure. _(permissions from `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionRuleParseRule`: Parses and compiles an Abstraction Rule's rule text as the engine
      would and returns each located error. _(permissions from
      `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionRuleExecute`: Runs an Abstraction Rule against an invocation context as the engine
      would and returns the result. _(permissions from `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionRuleBuildRuleFromBuilderJson`: Turns query builder JSON into an Abstraction Rule's
      rule text as the browser builder would, and parses it. _(permissions from
      `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionRuleCreate`: Registers an Abstraction Rule under a Model in the caller's tenant;
      calling twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionRuleUpdate`: Updates an Abstraction Rule in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionRuleDelete`: Soft-deletes an Abstraction Rule in the caller's tenant by id.
      **Write — confirmation required** _(permissions from
      `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Abstraction Rule Approvals (`entity-analysis-model-abstraction-rule-approvals`)

- **Hierarchy Placement:** Level 4, child of Abstraction Calculations Supervisor
- **Semantic Persona:** Owns the maker-checker aftermath of the entity analysis model abstraction rule slice: resolves a
  rejected submission by resubmitting it unchanged or retracting it to its last approved version. Never creates, updates
  or deletes it.
- **Semantic Routing Keywords:** entity, analysis, model, abstraction, rule, approvals, resubmit, retract
- **Toolbox (Max 15):**
    * `EntityAnalysisModelAbstractionRuleResubmit`: Resubmits a rejected Abstraction Rule for approval again, unchanged,
      opening a fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
    * `EntityAnalysisModelAbstractionRuleRetract`: Retracts a rejected Abstraction Rule, restoring it to its last
      approved version without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelAbstractionRuleService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Abstraction Counters Supervisor

- **Hierarchy Placement:** Level 3, child of Detection Model Authoring
- **Semantic Persona:** Routes within the abstraction counters slice of detection model authoring. Holds no tools; it
  exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** abstraction counters, entity, analysis, model, ttl, counter, time, window
- **Toolbox (Max 15):** none — routes to 4 leaf agents covering 15 tools
- **Downstream Dependencies:** `entity-analysis-model-ttl-counter-inspector`,
  `entity-analysis-model-ttl-counter-test-bench`, `entity-analysis-model-ttl-counter-author`,
  `entity-analysis-model-time-window`
- **Cross-Domain Reach:** `completions`

#### Entity Analysis Model Ttl Counter Inspector (`entity-analysis-model-ttl-counter-inspector`)

- **Hierarchy Placement:** Level 4, child of Abstraction Counters Supervisor
- **Semantic Persona:** Owns the entity analysis model ttl counter slice and reads only; it never changes anything and
  is the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, ttl, counter, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelTtlCounterList`: Lists TTL Counters for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelTtlCounterService.ListPermissions`)_
    * `EntityAnalysisModelTtlCounterFilterFields`: Lists the fields a query builder JSON filter over TTL Counters may
      use. _(permissions from `EntityAnalysisModelTtlCounterService.ListPermissions`)_
    * `EntityAnalysisModelTtlCounterFilter`: Returns the TTL Counters matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelTtlCounterService.ListPermissions`)_
    * `EntityAnalysisModelTtlCounterCount`: Counts the TTL Counters matching query builder JSON, optionally grouped by a
      field. _(permissions from `EntityAnalysisModelTtlCounterService.ListPermissions`)_
    * `EntityAnalysisModelTtlCounterGet`: Returns one TTL Counter by id, scoped to the caller's tenant. _(permissions
      from `EntityAnalysisModelTtlCounterService.ReadPermissions`)_
    * `EntityAnalysisModelTtlCounterGetByGuid`: Returns one TTL Counter by Guid, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelTtlCounterService.ReadPermissions`)_
    * `EntityAnalysisModelTtlCounterGetByEntityAnalysisModelId`: Lists TTL Counters belonging to a given Model, scoped
      to the caller's tenant. _(permissions from `EntityAnalysisModelTtlCounterService.ReadByModelPermissions`)_
    * `EntityAnalysisModelTtlCounterGetByEntityAnalysisModelGuid`: Lists TTL Counters belonging to the Model with the
      given Guid, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelTtlCounterService.ReadByModelPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Ttl Counter Test Bench (`entity-analysis-model-ttl-counter-test-bench`)

- **Hierarchy Placement:** Level 4, child of Abstraction Counters Supervisor
- **Semantic Persona:** Owns the entity analysis model ttl counter slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, ttl, counter, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelTtlCounterValidate`: Validates a TTL Counter without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelTtlCounterService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Ttl Counter Author (`entity-analysis-model-ttl-counter-author`)

- **Hierarchy Placement:** Level 4, child of Abstraction Counters Supervisor
- **Semantic Persona:** Owns the entity analysis model ttl counter slice and is the only agent that mutates this slice;
  every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, ttl, counter, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelTtlCounterList`: Lists TTL Counters for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelTtlCounterService.ListPermissions`)_
    * `EntityAnalysisModelTtlCounterFilterFields`: Lists the fields a query builder JSON filter over TTL Counters may
      use. _(permissions from `EntityAnalysisModelTtlCounterService.ListPermissions`)_
    * `EntityAnalysisModelTtlCounterFilter`: Returns the TTL Counters matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelTtlCounterService.ListPermissions`)_
    * `EntityAnalysisModelTtlCounterCount`: Counts the TTL Counters matching query builder JSON, optionally grouped by a
      field. _(permissions from `EntityAnalysisModelTtlCounterService.ListPermissions`)_
    * `EntityAnalysisModelTtlCounterGet`: Returns one TTL Counter by id, scoped to the caller's tenant. _(permissions
      from `EntityAnalysisModelTtlCounterService.ReadPermissions`)_
    * `EntityAnalysisModelTtlCounterGetByGuid`: Returns one TTL Counter by Guid, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelTtlCounterService.ReadPermissions`)_
    * `EntityAnalysisModelTtlCounterGetByEntityAnalysisModelId`: Lists TTL Counters belonging to a given Model, scoped
      to the caller's tenant. _(permissions from `EntityAnalysisModelTtlCounterService.ReadByModelPermissions`)_
    * `EntityAnalysisModelTtlCounterGetByEntityAnalysisModelGuid`: Lists TTL Counters belonging to the Model with the
      given Guid, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelTtlCounterService.ReadByModelPermissions`)_
    * `EntityAnalysisModelTtlCounterValidate`: Validates a TTL Counter without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelTtlCounterService.WritePermissions`)_
    * `EntityAnalysisModelTtlCounterCreate`: Registers a TTL Counter under a Model in the caller's tenant; calling twice
      creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelTtlCounterService.WritePermissions`)_
    * `EntityAnalysisModelTtlCounterUpdate`: Updates a TTL Counter in the caller's tenant by id. **Write — confirmation
      required** _(permissions from `EntityAnalysisModelTtlCounterService.WritePermissions`)_
    * `EntityAnalysisModelTtlCounterDelete`: Soft-deletes a TTL Counter in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelTtlCounterService.WritePermissions`)_
    * `EntityAnalysisModelTtlCounterResubmit`: Resubmits a rejected TTL Counter for approval again, unchanged, opening a
      fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelTtlCounterService.WritePermissions`)_
    * `EntityAnalysisModelTtlCounterRetract`: Retracts a rejected TTL Counter, restoring it to its last approved version
      without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelTtlCounterService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Time Window (`entity-analysis-model-time-window`)

- **Hierarchy Placement:** Level 4, child of Abstraction Counters Supervisor
- **Semantic Persona:** Owns the entity analysis model time window slice end to end, which is small enough that reads
  and writes stay with one agent.
- **Semantic Routing Keywords:** entity, analysis, model, time, window
- **Toolbox (Max 15):**
    * `EntityAnalysisModelTimeWindowIntervals`: Lists the kinds of time window and the interval codes each allows. _(
      permissions from `EntityAnalysisModelTimeWindowService.RequiredPermissions`)_
    * `EntityAnalysisModelTimeWindowCalculate`: Works out a time window's exact start and end as the engine does. _(
      permissions from `EntityAnalysisModelTimeWindowService.RequiredPermissions`)_
    * `EntityAnalysisModelTimeWindowAbstractionRule`: Works out the window an abstraction rule aggregates over for a
      reference date. _(permissions from `EntityAnalysisModelTimeWindowService.RequiredPermissions`)_
    * `EntityAnalysisModelTimeWindowTtlCounter`: Works out the window a TTL counter counts over for a reference date. _(
      permissions from `EntityAnalysisModelTimeWindowService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Activation Supervisor

- **Hierarchy Placement:** Level 3, child of Detection Model Authoring
- **Semantic Persona:** Routes within the activation slice of detection model authoring. Holds no tools; it exists so
  the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** activation, entity, analysis, model, rule, override, query
- **Toolbox (Max 15):** none — routes to 7 leaf agents covering 27 tools
- **Downstream Dependencies:** `entity-analysis-model-activation-rule-inspector`,
  `entity-analysis-model-activation-rule-test-bench`, `entity-analysis-model-activation-rule-author`,
  `entity-analysis-model-activation-rule-override-inspector`,
  `entity-analysis-model-activation-rule-override-test-bench`, `entity-analysis-model-activation-rule-override-author`,
  `entity-analysis-model-activation-rule-override-query`
- **Cross-Domain Reach:** `completions`, `model-invocation-context-and-dependencies`

#### Entity Analysis Model Activation Rule Inspector (`entity-analysis-model-activation-rule-inspector`)

- **Hierarchy Placement:** Level 4, child of Activation Supervisor
- **Semantic Persona:** Owns the entity analysis model activation rule slice and reads only; it never changes anything
  and is the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, activation, rule, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelActivationRuleList`: Lists Activation Rules for the caller's tenant, keyset-paged and capped.
      _(permissions from `EntityAnalysisModelActivationRuleService.ListPermissions`)_
    * `EntityAnalysisModelActivationRuleFilterFields`: Lists the fields a query builder JSON filter over Activation
      Rules may use. _(permissions from `EntityAnalysisModelActivationRuleService.ListPermissions`)_
    * `EntityAnalysisModelActivationRuleFilter`: Returns the Activation Rules matching query builder JSON, keyset-paged
      and capped. _(permissions from `EntityAnalysisModelActivationRuleService.ListPermissions`)_
    * `EntityAnalysisModelActivationRuleCount`: Counts the Activation Rules matching query builder JSON, optionally
      grouped by a field. _(permissions from `EntityAnalysisModelActivationRuleService.ListPermissions`)_
    * `EntityAnalysisModelActivationRuleGet`: Returns one Activation Rule by id, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelActivationRuleService.ReadPermissions`)_
    * `EntityAnalysisModelActivationRuleGetByGuid`: Returns one Activation Rule by Guid, scoped to the caller's tenant.
      _(permissions from `EntityAnalysisModelActivationRuleService.ReadPermissions`)_
    * `EntityAnalysisModelActivationRuleGetByEntityAnalysisModelId`: Lists Activation Rules belonging to a given Model,
      scoped to the caller's tenant. _(permissions from `EntityAnalysisModelActivationRuleService.ReadPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Activation Rule Test Bench (`entity-analysis-model-activation-rule-test-bench`)

- **Hierarchy Placement:** Level 4, child of Activation Supervisor
- **Semantic Persona:** Owns the entity analysis model activation rule slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, activation, rule, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelActivationRuleValidate`: Validates an Activation Rule without saving it and returns every
      failure. _(permissions from `EntityAnalysisModelActivationRuleService.WritePermissions`)_
    * `EntityAnalysisModelActivationRuleParseRule`: Parses and compiles an Activation Rule's rule text as the engine
      would and returns each located error. _(permissions from
      `EntityAnalysisModelActivationRuleService.WritePermissions`)_
    * `EntityAnalysisModelActivationRuleExecute`: Runs an Activation Rule against an invocation context as the engine
      would and returns the result. _(permissions from `EntityAnalysisModelActivationRuleService.WritePermissions`)_
    * `EntityAnalysisModelActivationRuleBuildRuleFromBuilderJson`: Turns query builder JSON into an Activation Rule's
      rule text as the browser builder would, and parses it. _(permissions from
      `EntityAnalysisModelActivationRuleService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Activation Rule Author (`entity-analysis-model-activation-rule-author`)

- **Hierarchy Placement:** Level 4, child of Activation Supervisor
- **Semantic Persona:** Owns the entity analysis model activation rule slice and is the only agent that mutates this
  slice; every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, activation, rule, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelActivationRuleList`: Lists Activation Rules for the caller's tenant, keyset-paged and capped.
      _(permissions from `EntityAnalysisModelActivationRuleService.ListPermissions`)_
    * `EntityAnalysisModelActivationRuleFilterFields`: Lists the fields a query builder JSON filter over Activation
      Rules may use. _(permissions from `EntityAnalysisModelActivationRuleService.ListPermissions`)_
    * `EntityAnalysisModelActivationRuleFilter`: Returns the Activation Rules matching query builder JSON, keyset-paged
      and capped. _(permissions from `EntityAnalysisModelActivationRuleService.ListPermissions`)_
    * `EntityAnalysisModelActivationRuleCount`: Counts the Activation Rules matching query builder JSON, optionally
      grouped by a field. _(permissions from `EntityAnalysisModelActivationRuleService.ListPermissions`)_
    * `EntityAnalysisModelActivationRuleGet`: Returns one Activation Rule by id, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelActivationRuleService.ReadPermissions`)_
    * `EntityAnalysisModelActivationRuleGetByGuid`: Returns one Activation Rule by Guid, scoped to the caller's tenant.
      _(permissions from `EntityAnalysisModelActivationRuleService.ReadPermissions`)_
    * `EntityAnalysisModelActivationRuleGetByEntityAnalysisModelId`: Lists Activation Rules belonging to a given Model,
      scoped to the caller's tenant. _(permissions from `EntityAnalysisModelActivationRuleService.ReadPermissions`)_
    * `EntityAnalysisModelActivationRuleValidate`: Validates an Activation Rule without saving it and returns every
      failure. _(permissions from `EntityAnalysisModelActivationRuleService.WritePermissions`)_
    * `EntityAnalysisModelActivationRuleParseRule`: Parses and compiles an Activation Rule's rule text as the engine
      would and returns each located error. _(permissions from
      `EntityAnalysisModelActivationRuleService.WritePermissions`)_
    * `EntityAnalysisModelActivationRuleExecute`: Runs an Activation Rule against an invocation context as the engine
      would and returns the result. _(permissions from `EntityAnalysisModelActivationRuleService.WritePermissions`)_
    * `EntityAnalysisModelActivationRuleBuildRuleFromBuilderJson`: Turns query builder JSON into an Activation Rule's
      rule text as the browser builder would, and parses it. _(permissions from
      `EntityAnalysisModelActivationRuleService.WritePermissions`)_
    * `EntityAnalysisModelActivationRuleCreate`: Registers an Activation Rule under a Model in the caller's tenant;
      calling twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelActivationRuleService.WritePermissions`)_
    * `EntityAnalysisModelActivationRuleUpdate`: Updates an Activation Rule in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelActivationRuleService.WritePermissions`)_
    * `EntityAnalysisModelActivationRuleDelete`: Soft-deletes an Activation Rule in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelActivationRuleService.WritePermissions`)_
    * `EntityAnalysisModelActivationRuleReset`: Resets the ActivationCounter and EvaluationCounter of an Activation Rule
      to zero. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelActivationRuleService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Activation Rule Approvals (`entity-analysis-model-activation-rule-approvals`)

- **Hierarchy Placement:** Level 4, child of Core Supervisor (the Activation Supervisor's own fan-out was already at
  budget)
- **Semantic Persona:** Owns the maker-checker aftermath of the entity analysis model activation rule slice: resolves a
  rejected submission by resubmitting it unchanged or retracting it to its last approved version. Never creates, updates
  or deletes it.
- **Semantic Routing Keywords:** entity, analysis, model, activation, rule, approvals, resubmit, retract
- **Toolbox (Max 15):**
    * `EntityAnalysisModelActivationRuleResubmit`: Resubmits a rejected Activation Rule for approval again, unchanged,
      opening a fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelActivationRuleService.WritePermissions`)_
    * `EntityAnalysisModelActivationRuleRetract`: Retracts a rejected Activation Rule, restoring it to its last approved
      version without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelActivationRuleService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Activation Rule Override Inspector (`entity-analysis-model-activation-rule-override-inspector`)

- **Hierarchy Placement:** Level 4, child of Activation Supervisor
- **Semantic Persona:** Owns the entity analysis model activation rule override slice and reads only; it never changes
  anything and is the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, activation, rule, override, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelActivationRuleOverrideList`: Lists Activation-Rule-scoped Overrides for the caller's tenant,
      keyset-paged and capped. _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideFilterFields`: Lists the fields a query builder JSON filter over
      Activation Rule Overrides may use. _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideFilter`: Returns the Activation Rule Overrides matching query builder
      JSON, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideCount`: Counts the Activation Rule Overrides matching query builder
      JSON, optionally grouped by a field. _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideGet`: Returns one Activation-Rule-scoped Override by id, scoped to the
      caller's tenant. _(permissions from `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideGetByEntityAnalysisModelGuid`: Lists Activation-Rule-scoped Overrides
      belonging to a given Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Activation Rule Override Test Bench (`entity-analysis-model-activation-rule-override-test-bench`)

- **Hierarchy Placement:** Level 4, child of Activation Supervisor
- **Semantic Persona:** Owns the entity analysis model activation rule override slice and runs the side-effect-free
  rehearsals: validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, activation, rule, override, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelActivationRuleOverrideValidate`: Validates an Activation Rule Override without saving it and
      returns every failure. _(permissions from `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Activation Rule Override Author (`entity-analysis-model-activation-rule-override-author`)

- **Hierarchy Placement:** Level 4, child of Activation Supervisor
- **Semantic Persona:** Owns the entity analysis model activation rule override slice and is the only agent that mutates
  this slice; every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change
  snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, activation, rule, override, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelActivationRuleOverrideList`: Lists Activation-Rule-scoped Overrides for the caller's tenant,
      keyset-paged and capped. _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideFilterFields`: Lists the fields a query builder JSON filter over
      Activation Rule Overrides may use. _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideFilter`: Returns the Activation Rule Overrides matching query builder
      JSON, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideCount`: Counts the Activation Rule Overrides matching query builder
      JSON, optionally grouped by a field. _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideGet`: Returns one Activation-Rule-scoped Override by id, scoped to the
      caller's tenant. _(permissions from `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideGetByEntityAnalysisModelGuid`: Lists Activation-Rule-scoped Overrides
      belonging to a given Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideValidate`: Validates an Activation Rule Override without saving it and
      returns every failure. _(permissions from `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideCreate`: Registers a Override against a Model/Activation Rule in the
      caller's tenant; calling twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideUpdate`: Toggles a Override on or off by row existence; NOT
      idempotent -- repeat calls flip state. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideUpdateDeleteExpiryDate`: Updates the Delete Expiry Date of an existing
      active Override in the caller's tenant. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideDelete`: Soft-deletes a Override in the caller's tenant by id. **Write —
      confirmation required** _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelActivationRuleOverrideUpdateOverrideKind`: Updates the Override Kind of an active Activation
      Rule Override in the caller's tenant. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Activation Rule Override Query (`entity-analysis-model-activation-rule-override-query`)

- **Hierarchy Placement:** Level 4, child of Activation Supervisor
- **Semantic Persona:** Owns the entity analysis model activation rule override query slice end to end, which is small
  enough that reads and writes stay with one agent.
- **Semantic Routing Keywords:** entity, analysis, model, activation, rule, override, query
- **Toolbox (Max 15):**
    * `EntityAnalysisModelActivationRuleOverrideQueryGet`: Lists the override-enabled Activation Rules of an Entity
      Analysis Model and whether each is currently overridden for a override key and value. _(permissions from
      `EntityAnalysisModelActivationRuleOverrideQueryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Core Supervisor

- **Hierarchy Placement:** Level 3, child of Detection Model Authoring
- **Semantic Persona:** Routes within the core slice of detection model authoring. Holds no tools; it exists so the
  domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** core, entity, analysis, model
- **Toolbox (Max 15):** none — routes to 3 leaf agents covering 9 tools
- **Downstream Dependencies:** `entity-analysis-model-inspector`, `entity-analysis-model-test-bench`,
  `entity-analysis-model-author`
- **Cross-Domain Reach:** `entity-analysis-model-synchronisation-schedule`

#### Entity Analysis Model Inspector (`entity-analysis-model-inspector`)

- **Hierarchy Placement:** Level 4, child of Core Supervisor
- **Semantic Persona:** Owns the entity analysis model slice and reads only; it never changes anything and is the safe
  default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelList`: Lists Models for the caller's tenant, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelService.RequiredPermissions`)_
    * `EntityAnalysisModelFilterFields`: Lists the fields a query builder JSON filter over Entity Analysis Models may
      use. _(permissions from `EntityAnalysisModelService.ListPermissions`)_
    * `EntityAnalysisModelFilter`: Returns the Entity Analysis Models matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelService.ListPermissions`)_
    * `EntityAnalysisModelCount`: Counts the Entity Analysis Models matching query builder JSON, optionally grouped by a
      field. _(permissions from `EntityAnalysisModelService.ListPermissions`)_
    * `EntityAnalysisModelGet`: Returns one Model by id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Test Bench (`entity-analysis-model-test-bench`)

- **Hierarchy Placement:** Level 4, child of Core Supervisor
- **Semantic Persona:** Owns the entity analysis model slice and runs the side-effect-free rehearsals: validation, rule
  parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelValidate`: Validates an Entity Analysis Model without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Author (`entity-analysis-model-author`)

- **Hierarchy Placement:** Level 4, child of Core Supervisor
- **Semantic Persona:** Owns the entity analysis model slice and is the only agent that mutates this slice; every call
  needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelList`: Lists Models for the caller's tenant, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelService.RequiredPermissions`)_
    * `EntityAnalysisModelFilterFields`: Lists the fields a query builder JSON filter over Entity Analysis Models may
      use. _(permissions from `EntityAnalysisModelService.ListPermissions`)_
    * `EntityAnalysisModelFilter`: Returns the Entity Analysis Models matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelService.ListPermissions`)_
    * `EntityAnalysisModelCount`: Counts the Entity Analysis Models matching query builder JSON, optionally grouped by a
      field. _(permissions from `EntityAnalysisModelService.ListPermissions`)_
    * `EntityAnalysisModelGet`: Returns one Model by id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelService.RequiredPermissions`)_
    * `EntityAnalysisModelValidate`: Validates an Entity Analysis Model without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelService.RequiredPermissions`)_
    * `EntityAnalysisModelCreate`: Creates a Model in the caller's tenant; calling twice creates two. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelService.RequiredPermissions`)_
    * `EntityAnalysisModelUpdate`: Updates a Model in the caller's tenant by id. **Write — confirmation required** _(
      permissions from `EntityAnalysisModelService.RequiredPermissions`)_
    * `EntityAnalysisModelDelete`: Soft-deletes a Model in the caller's tenant by id. **Write — confirmation required**
      _(permissions from `EntityAnalysisModelService.RequiredPermissions`)_
    * `EntityAnalysisModelResubmit`: Resubmits a rejected Model for approval again, unchanged, opening a fresh approval
      cycle. **Write — confirmation required** _(permissions from `EntityAnalysisModelService.RequiredPermissions`)_
    * `EntityAnalysisModelRetract`: Retracts a rejected Model, restoring it to its last approved version without a new
      approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### References Derivation Supervisor

- **Hierarchy Placement:** Level 3, child of Detection Model Authoring
- **Semantic Persona:** Routes within the references derivation slice of detection model authoring. Holds no tools; it
  exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** references derivation, entity, analysis, model, inline, function, script
- **Toolbox (Max 15):** none — routes to 6 leaf agents covering 23 tools
- **Downstream Dependencies:** `entity-analysis-model-inline-function-inspector`,
  `entity-analysis-model-inline-function-test-bench`, `entity-analysis-model-inline-function-author`,
  `entity-analysis-model-inline-script-inspector`, `entity-analysis-model-inline-script-test-bench`,
  `entity-analysis-model-inline-script-author`
- **Cross-Domain Reach:** `completions`, `model-invocation-context-and-dependencies`

#### Entity Analysis Model Inline Function Inspector (`entity-analysis-model-inline-function-inspector`)

- **Hierarchy Placement:** Level 4, child of References Derivation Supervisor
- **Semantic Persona:** Owns the entity analysis model inline function slice and reads only; it never changes anything
  and is the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, inline, function, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelInlineFunctionList`: Lists Inline Functions for the caller's tenant, keyset-paged and capped.
      _(permissions from `EntityAnalysisModelInlineFunctionService.ListPermissions`)_
    * `EntityAnalysisModelInlineFunctionFilterFields`: Lists the fields a query builder JSON filter over Inline
      Functions may use. _(permissions from `EntityAnalysisModelInlineFunctionService.ListPermissions`)_
    * `EntityAnalysisModelInlineFunctionFilter`: Returns the Inline Functions matching query builder JSON, keyset-paged
      and capped. _(permissions from `EntityAnalysisModelInlineFunctionService.ListPermissions`)_
    * `EntityAnalysisModelInlineFunctionCount`: Counts the Inline Functions matching query builder JSON, optionally
      grouped by a field. _(permissions from `EntityAnalysisModelInlineFunctionService.ListPermissions`)_
    * `EntityAnalysisModelInlineFunctionGet`: Returns one Inline Function by id, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelInlineFunctionService.ReadPermissions`)_
    * `EntityAnalysisModelInlineFunctionGetByGuid`: Returns one Inline Function by Guid, scoped to the caller's tenant.
      _(permissions from `EntityAnalysisModelInlineFunctionService.ReadPermissions`)_
    * `EntityAnalysisModelInlineFunctionGetByEntityAnalysisModelId`: Lists Inline Functions belonging to a given Model,
      scoped to the caller's tenant. _(permissions from `EntityAnalysisModelInlineFunctionService.ReadPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Inline Function Test Bench (`entity-analysis-model-inline-function-test-bench`)

- **Hierarchy Placement:** Level 4, child of References Derivation Supervisor
- **Semantic Persona:** Owns the entity analysis model inline function slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, inline, function, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelInlineFunctionValidate`: Validates an Inline Function without saving it and returns every
      failure. _(permissions from `EntityAnalysisModelInlineFunctionService.WritePermissions`)_
    * `EntityAnalysisModelInlineFunctionParseRule`: Parses and compiles an Inline Function's rule text as the engine
      would and returns each located error. _(permissions from
      `EntityAnalysisModelInlineFunctionService.WritePermissions`)_
    * `EntityAnalysisModelInlineFunctionExecute`: Runs an Inline Function against an invocation context as the engine
      would and returns the result. _(permissions from `EntityAnalysisModelInlineFunctionService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Inline Function Author (`entity-analysis-model-inline-function-author`)

- **Hierarchy Placement:** Level 4, child of References Derivation Supervisor
- **Semantic Persona:** Owns the entity analysis model inline function slice and is the only agent that mutates this
  slice; every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, inline, function, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelInlineFunctionList`: Lists Inline Functions for the caller's tenant, keyset-paged and capped.
      _(permissions from `EntityAnalysisModelInlineFunctionService.ListPermissions`)_
    * `EntityAnalysisModelInlineFunctionFilterFields`: Lists the fields a query builder JSON filter over Inline
      Functions may use. _(permissions from `EntityAnalysisModelInlineFunctionService.ListPermissions`)_
    * `EntityAnalysisModelInlineFunctionFilter`: Returns the Inline Functions matching query builder JSON, keyset-paged
      and capped. _(permissions from `EntityAnalysisModelInlineFunctionService.ListPermissions`)_
    * `EntityAnalysisModelInlineFunctionCount`: Counts the Inline Functions matching query builder JSON, optionally
      grouped by a field. _(permissions from `EntityAnalysisModelInlineFunctionService.ListPermissions`)_
    * `EntityAnalysisModelInlineFunctionGet`: Returns one Inline Function by id, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelInlineFunctionService.ReadPermissions`)_
    * `EntityAnalysisModelInlineFunctionGetByGuid`: Returns one Inline Function by Guid, scoped to the caller's tenant.
      _(permissions from `EntityAnalysisModelInlineFunctionService.ReadPermissions`)_
    * `EntityAnalysisModelInlineFunctionGetByEntityAnalysisModelId`: Lists Inline Functions belonging to a given Model,
      scoped to the caller's tenant. _(permissions from `EntityAnalysisModelInlineFunctionService.ReadPermissions`)_
    * `EntityAnalysisModelInlineFunctionValidate`: Validates an Inline Function without saving it and returns every
      failure. _(permissions from `EntityAnalysisModelInlineFunctionService.WritePermissions`)_
    * `EntityAnalysisModelInlineFunctionParseRule`: Parses and compiles an Inline Function's rule text as the engine
      would and returns each located error. _(permissions from
      `EntityAnalysisModelInlineFunctionService.WritePermissions`)_
    * `EntityAnalysisModelInlineFunctionExecute`: Runs an Inline Function against an invocation context as the engine
      would and returns the result. _(permissions from `EntityAnalysisModelInlineFunctionService.WritePermissions`)_
    * `EntityAnalysisModelInlineFunctionCreate`: Creates an Inline Function under a Model in the caller's tenant;
      calling twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelInlineFunctionService.WritePermissions`)_
    * `EntityAnalysisModelInlineFunctionUpdate`: Updates an Inline Function in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelInlineFunctionService.WritePermissions`)_
    * `EntityAnalysisModelInlineFunctionDelete`: Soft-deletes an Inline Function in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelInlineFunctionService.WritePermissions`)_
    * `EntityAnalysisModelInlineFunctionResubmit`: Resubmits a rejected Inline Function for approval again, unchanged,
      opening a fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelInlineFunctionService.WritePermissions`)_
    * `EntityAnalysisModelInlineFunctionRetract`: Retracts a rejected Inline Function, restoring it to its last approved
      version without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelInlineFunctionService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Inline Script Inspector (`entity-analysis-model-inline-script-inspector`)

- **Hierarchy Placement:** Level 4, child of References Derivation Supervisor
- **Semantic Persona:** Owns the entity analysis model inline script slice and reads only; it never changes anything and
  is the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, inline, script, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelInlineScriptList`: Lists Inline Script registrations for the caller's tenant, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelInlineScriptService.ListPermissions`)_
    * `EntityAnalysisModelInlineScriptFilterFields`: Lists the fields a query builder JSON filter over Inline Scripts
      may use. _(permissions from `EntityAnalysisModelInlineScriptService.ListPermissions`)_
    * `EntityAnalysisModelInlineScriptFilter`: Returns the Inline Scripts matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelInlineScriptService.ListPermissions`)_
    * `EntityAnalysisModelInlineScriptCount`: Counts the Inline Scripts matching query builder JSON, optionally grouped
      by a field. _(permissions from `EntityAnalysisModelInlineScriptService.ListPermissions`)_
    * `EntityAnalysisModelInlineScriptGet`: Returns one Inline Script registration by id, scoped to the caller's tenant.
      _(permissions from `EntityAnalysisModelInlineScriptService.ReadPermissions`)_
    * `EntityAnalysisModelInlineScriptGetByGuid`: Returns one Inline Script registration by Guid, scoped to the caller's
      tenant. _(permissions from `EntityAnalysisModelInlineScriptService.ReadPermissions`)_
    * `EntityAnalysisModelInlineScriptGetByEntityAnalysisModelId`: Lists Inline Script registrations belonging to a
      given Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelInlineScriptService.ReadPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Inline Script Test Bench (`entity-analysis-model-inline-script-test-bench`)

- **Hierarchy Placement:** Level 4, child of References Derivation Supervisor
- **Semantic Persona:** Owns the entity analysis model inline script slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, inline, script, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelInlineScriptValidate`: Validates an Inline Script without saving it and returns every failure.
      _(permissions from `EntityAnalysisModelInlineScriptService.WritePermissions`)_
    * `EntityAnalysisModelInlineScriptExecute`: Runs a model's inline script against an invocation context when enabled;
      returns its properties. _(permissions from `EntityAnalysisModelInlineScriptService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Inline Script Author (`entity-analysis-model-inline-script-author`)

- **Hierarchy Placement:** Level 4, child of References Derivation Supervisor
- **Semantic Persona:** Owns the entity analysis model inline script slice and is the only agent that mutates this
  slice; every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, inline, script, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelInlineScriptList`: Lists Inline Script registrations for the caller's tenant, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelInlineScriptService.ListPermissions`)_
    * `EntityAnalysisModelInlineScriptFilterFields`: Lists the fields a query builder JSON filter over Inline Scripts
      may use. _(permissions from `EntityAnalysisModelInlineScriptService.ListPermissions`)_
    * `EntityAnalysisModelInlineScriptFilter`: Returns the Inline Scripts matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelInlineScriptService.ListPermissions`)_
    * `EntityAnalysisModelInlineScriptCount`: Counts the Inline Scripts matching query builder JSON, optionally grouped
      by a field. _(permissions from `EntityAnalysisModelInlineScriptService.ListPermissions`)_
    * `EntityAnalysisModelInlineScriptGet`: Returns one Inline Script registration by id, scoped to the caller's tenant.
      _(permissions from `EntityAnalysisModelInlineScriptService.ReadPermissions`)_
    * `EntityAnalysisModelInlineScriptGetByGuid`: Returns one Inline Script registration by Guid, scoped to the caller's
      tenant. _(permissions from `EntityAnalysisModelInlineScriptService.ReadPermissions`)_
    * `EntityAnalysisModelInlineScriptGetByEntityAnalysisModelId`: Lists Inline Script registrations belonging to a
      given Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelInlineScriptService.ReadPermissions`)_
    * `EntityAnalysisModelInlineScriptValidate`: Validates an Inline Script without saving it and returns every failure.
      _(permissions from `EntityAnalysisModelInlineScriptService.WritePermissions`)_
    * `EntityAnalysisModelInlineScriptExecute`: Runs a model's inline script against an invocation context when enabled;
      returns its properties. _(permissions from `EntityAnalysisModelInlineScriptService.WritePermissions`)_
    * `EntityAnalysisModelInlineScriptCreate`: Registers an Inline Script under a Model in the caller's tenant; calling
      twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelInlineScriptService.WritePermissions`)_
    * `EntityAnalysisModelInlineScriptUpdate`: Updates an Inline Script registration in the caller's tenant by id.
      **Write — confirmation required** _(permissions from `EntityAnalysisModelInlineScriptService.WritePermissions`)_
    * `EntityAnalysisModelInlineScriptDelete`: Soft-deletes an Inline Script registration in the caller's tenant by id.
      **Write — confirmation required** _(permissions from `EntityAnalysisModelInlineScriptService.WritePermissions`)_
    * `EntityAnalysisModelInlineScriptResubmit`: Resubmits a rejected Inline Script registration for approval again,
      unchanged, opening a fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelInlineScriptService.WritePermissions`)_
    * `EntityAnalysisModelInlineScriptRetract`: Retracts a rejected Inline Script registration, restoring it to its last
      approved version without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelInlineScriptService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### References Gateway Supervisor

- **Hierarchy Placement:** Level 3, child of Detection Model Authoring
- **Semantic Persona:** Routes within the references gateway slice of detection model authoring. Holds no tools; it
  exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** references gateway, entity, analysis, model, gateway, rule
- **Toolbox (Max 15):** none — routes to 3 leaf agents covering 14 tools
- **Downstream Dependencies:** `entity-analysis-model-gateway-rule-inspector`,
  `entity-analysis-model-gateway-rule-test-bench`, `entity-analysis-model-gateway-rule-author`
- **Cross-Domain Reach:** `completions`, `model-invocation-context-and-dependencies`

#### Entity Analysis Model Gateway Rule Inspector (`entity-analysis-model-gateway-rule-inspector`)

- **Hierarchy Placement:** Level 4, child of References Gateway Supervisor
- **Semantic Persona:** Owns the entity analysis model gateway rule slice and reads only; it never changes anything and
  is the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, gateway, rule, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelGatewayRuleList`: Lists Gateway Rules for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelGatewayRuleService.ListPermissions`)_
    * `EntityAnalysisModelGatewayRuleFilterFields`: Lists the fields a query builder JSON filter over Gateway Rules may
      use. _(permissions from `EntityAnalysisModelGatewayRuleService.ListPermissions`)_
    * `EntityAnalysisModelGatewayRuleFilter`: Returns the Gateway Rules matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelGatewayRuleService.ListPermissions`)_
    * `EntityAnalysisModelGatewayRuleCount`: Counts the Gateway Rules matching query builder JSON, optionally grouped by
      a field. _(permissions from `EntityAnalysisModelGatewayRuleService.ListPermissions`)_
    * `EntityAnalysisModelGatewayRuleGet`: Returns one Gateway Rule by id, scoped to the caller's tenant. _(permissions
      from `EntityAnalysisModelGatewayRuleService.ReadPermissions`)_
    * `EntityAnalysisModelGatewayRuleGetByGuid`: Returns one Gateway Rule by Guid, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelGatewayRuleService.ReadPermissions`)_
    * `EntityAnalysisModelGatewayRuleGetByEntityAnalysisModelId`: Lists Gateway Rules belonging to a given Model, scoped
      to the caller's tenant. _(permissions from `EntityAnalysisModelGatewayRuleService.ReadPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Gateway Rule Test Bench (`entity-analysis-model-gateway-rule-test-bench`)

- **Hierarchy Placement:** Level 4, child of References Gateway Supervisor
- **Semantic Persona:** Owns the entity analysis model gateway rule slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, gateway, rule, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelGatewayRuleValidate`: Validates a Gateway Rule without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
    * `EntityAnalysisModelGatewayRuleParseRule`: Parses and compiles a Gateway Rule's rule text as the engine would and
      returns each located error. _(permissions from `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
    * `EntityAnalysisModelGatewayRuleExecute`: Runs a Gateway Rule against an invocation context as the engine would and
      returns the result. _(permissions from `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
    * `EntityAnalysisModelGatewayRuleBuildRuleFromBuilderJson`: Turns query builder JSON into a Gateway Rule's rule text
      as the browser builder would, and parses it. _(permissions from
      `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Gateway Rule Author (`entity-analysis-model-gateway-rule-author`)

- **Hierarchy Placement:** Level 4, child of References Gateway Supervisor
- **Semantic Persona:** Owns the entity analysis model gateway rule slice and is the only agent that mutates this slice;
  every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, gateway, rule, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelGatewayRuleList`: Lists Gateway Rules for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelGatewayRuleService.ListPermissions`)_
    * `EntityAnalysisModelGatewayRuleFilterFields`: Lists the fields a query builder JSON filter over Gateway Rules may
      use. _(permissions from `EntityAnalysisModelGatewayRuleService.ListPermissions`)_
    * `EntityAnalysisModelGatewayRuleFilter`: Returns the Gateway Rules matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelGatewayRuleService.ListPermissions`)_
    * `EntityAnalysisModelGatewayRuleCount`: Counts the Gateway Rules matching query builder JSON, optionally grouped by
      a field. _(permissions from `EntityAnalysisModelGatewayRuleService.ListPermissions`)_
    * `EntityAnalysisModelGatewayRuleGet`: Returns one Gateway Rule by id, scoped to the caller's tenant. _(permissions
      from `EntityAnalysisModelGatewayRuleService.ReadPermissions`)_
    * `EntityAnalysisModelGatewayRuleGetByGuid`: Returns one Gateway Rule by Guid, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelGatewayRuleService.ReadPermissions`)_
    * `EntityAnalysisModelGatewayRuleGetByEntityAnalysisModelId`: Lists Gateway Rules belonging to a given Model, scoped
      to the caller's tenant. _(permissions from `EntityAnalysisModelGatewayRuleService.ReadPermissions`)_
    * `EntityAnalysisModelGatewayRuleValidate`: Validates a Gateway Rule without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
    * `EntityAnalysisModelGatewayRuleParseRule`: Parses and compiles a Gateway Rule's rule text as the engine would and
      returns each located error. _(permissions from `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
    * `EntityAnalysisModelGatewayRuleExecute`: Runs a Gateway Rule against an invocation context as the engine would and
      returns the result. _(permissions from `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
    * `EntityAnalysisModelGatewayRuleBuildRuleFromBuilderJson`: Turns query builder JSON into a Gateway Rule's rule text
      as the browser builder would, and parses it. _(permissions from
      `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
    * `EntityAnalysisModelGatewayRuleCreate`: Registers a Gateway Rule under a Model in the caller's tenant; calling
      twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
    * `EntityAnalysisModelGatewayRuleUpdate`: Updates a Gateway Rule in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
    * `EntityAnalysisModelGatewayRuleDelete`: Soft-deletes a Gateway Rule in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
    * `EntityAnalysisModelGatewayRuleResetCounter`: Resets the activation and evaluation counters for a Gateway Rule in
      the caller's tenant to zero. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Gateway Rule Approvals (`entity-analysis-model-gateway-rule-approvals`)

- **Hierarchy Placement:** Level 4, child of References Gateway Supervisor
- **Semantic Persona:** Owns the maker-checker aftermath of the entity analysis model gateway rule slice: resolves a
  rejected submission by resubmitting it unchanged or retracting it to its last approved version. Never creates, updates
  or deletes it.
- **Semantic Routing Keywords:** entity, analysis, model, gateway, rule, approvals, resubmit, retract
- **Toolbox (Max 15):**
    * `EntityAnalysisModelGatewayRuleResubmit`: Resubmits a rejected Gateway Rule for approval again, unchanged, opening
      a fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
    * `EntityAnalysisModelGatewayRuleRetract`: Retracts a rejected Gateway Rule, restoring it to its last approved
      version without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelGatewayRuleService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### References Payload Contract Supervisor

- **Hierarchy Placement:** Level 3, child of Detection Model Authoring
- **Semantic Persona:** Routes within the references payload contract slice of detection model authoring. Holds no
  tools; it exists so the domain above it chooses between a handful of coherent specialists rather than dozens of
  leaves.
- **Semantic Routing Keywords:** references payload contract, entity, analysis, model, request, path, inline, script,
  potential, multi, part, string, names, by, integer, float, data, type
- **Toolbox (Max 15):** none — routes to 5 leaf agents covering 20 tools
- **Downstream Dependencies:** `entity-analysis-model-request-x-path-inspector-1`,
  `entity-analysis-model-request-x-path-inspector-2`, `entity-analysis-model-request-x-path-test-bench`,
  `entity-analysis-model-request-x-path-author`, `entity-analysis-observability`

#### Entity Analysis Model Request X Path Inspector 1 (`entity-analysis-model-request-x-path-inspector-1`)

- **Hierarchy Placement:** Level 4, child of References Payload Contract Supervisor
- **Semantic Persona:** Owns the entity analysis model request x path slice end to end, which is small enough that reads
  and writes stay with one agent.
- **Semantic Routing Keywords:** entity, analysis, model, request, path, inspector, 1
- **Toolbox (Max 15):**
    * `EntityAnalysisModelRequestXPathList`: Lists Request XPaths for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelRequestXPathService.ListPermissions`)_
    * `EntityAnalysisModelRequestXPathFilterFields`: Lists the fields a query builder JSON filter over RequestXPaths may
      use. _(permissions from `EntityAnalysisModelRequestXPathService.ListPermissions`)_
    * `EntityAnalysisModelRequestXPathFilter`: Returns the RequestXPaths matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelRequestXPathService.ListPermissions`)_
    * `EntityAnalysisModelRequestXPathCount`: Counts the RequestXPaths matching query builder JSON, optionally grouped
      by a field. _(permissions from `EntityAnalysisModelRequestXPathService.ListPermissions`)_
    * `EntityAnalysisModelRequestXPathGet`: Returns one Request XPath by id, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelRequestXPathService.ReadPermissions`)_
    * `EntityAnalysisModelRequestXPathGetByEntityAnalysisModelId`: Lists Request XPaths belonging to a given Model,
      scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelRequestXPathService.ReadByEntityAnalysisModelPermissions`)_
    * `EntityAnalysisModelRequestXPathGetByCasesWorkflowId`: Lists Request XPaths reachable from a given Case Workflow's
      Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelRequestXPathService.ReadPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Request X Path Inspector 2 (`entity-analysis-model-request-x-path-inspector-2`)

- **Hierarchy Placement:** Level 4, child of References Payload Contract Supervisor
- **Semantic Persona:** Owns the entity analysis model request x path slice end to end, which is small enough that reads
  and writes stay with one agent.
- **Semantic Routing Keywords:** entity, analysis, model, request, path, inspector, 2
- **Toolbox (Max 15):**
    * `EntityAnalysisModelRequestXPathGetByOverrideKey`: Lists Request XPaths flagged for Override, scoped to the
      caller's tenant. _(permissions from `EntityAnalysisModelRequestXPathService.OverridePermissions`)_
    * `EntityAnalysisModelRequestXPathGetByEntityAnalysisModelIdByDataType`: Lists String, Integer or Float Request
      XPaths belonging to a given Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelRequestXPathService.ReadByDataTypePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Request X Path Test Bench (`entity-analysis-model-request-x-path-test-bench`)

- **Hierarchy Placement:** Level 4, child of References Payload Contract Supervisor
- **Semantic Persona:** Owns the entity analysis model request x path slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, request, path, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelRequestXPathValidate`: Validates a RequestXPath without saving it and returns every failure.
      _(permissions from `EntityAnalysisModelRequestXPathService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Request X Path Author (`entity-analysis-model-request-x-path-author`)

- **Hierarchy Placement:** Level 4, child of References Payload Contract Supervisor
- **Semantic Persona:** Owns the entity analysis model request x path slice and is the only agent that mutates this
  slice; every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, request, path, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelRequestXPathList`: Lists Request XPaths for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelRequestXPathService.ListPermissions`)_
    * `EntityAnalysisModelRequestXPathFilterFields`: Lists the fields a query builder JSON filter over RequestXPaths may
      use. _(permissions from `EntityAnalysisModelRequestXPathService.ListPermissions`)_
    * `EntityAnalysisModelRequestXPathFilter`: Returns the RequestXPaths matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelRequestXPathService.ListPermissions`)_
    * `EntityAnalysisModelRequestXPathCount`: Counts the RequestXPaths matching query builder JSON, optionally grouped
      by a field. _(permissions from `EntityAnalysisModelRequestXPathService.ListPermissions`)_
    * `EntityAnalysisModelRequestXPathGet`: Returns one Request XPath by id, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelRequestXPathService.ReadPermissions`)_
    * `EntityAnalysisModelRequestXPathGetByEntityAnalysisModelId`: Lists Request XPaths belonging to a given Model,
      scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelRequestXPathService.ReadByEntityAnalysisModelPermissions`)_
    * `EntityAnalysisModelRequestXPathGetByCasesWorkflowId`: Lists Request XPaths reachable from a given Case Workflow's
      Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelRequestXPathService.ReadPermissions`)_
    * `EntityAnalysisModelRequestXPathGetByOverrideKey`: Lists Request XPaths flagged for Override, scoped to the
      caller's tenant. _(permissions from `EntityAnalysisModelRequestXPathService.OverridePermissions`)_
    * `EntityAnalysisModelRequestXPathGetByEntityAnalysisModelIdByDataType`: Lists String, Integer or Float Request
      XPaths belonging to a given Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelRequestXPathService.ReadByDataTypePermissions`)_
    * `EntityAnalysisModelRequestXPathValidate`: Validates a RequestXPath without saving it and returns every failure.
      _(permissions from `EntityAnalysisModelRequestXPathService.WritePermissions`)_
    * `EntityAnalysisModelRequestXPathCreate`: Creates a Request XPath under a Model in the caller's tenant; calling
      twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelRequestXPathService.WritePermissions`)_
    * `EntityAnalysisModelRequestXPathUpdate`: Updates a Request XPath in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelRequestXPathService.WritePermissions`)_
    * `EntityAnalysisModelRequestXPathDelete`: Soft-deletes a Request XPath in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelRequestXPathService.WritePermissions`)_
    * `EntityAnalysisModelRequestXPathResubmit`: Resubmits a rejected Request XPath for approval again, unchanged,
      opening a fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelRequestXPathService.WritePermissions`)_
    * `EntityAnalysisModelRequestXPathRetract`: Retracts a rejected Request XPath, restoring it to its last approved
      version without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelRequestXPathService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Observability (`entity-analysis-observability`)

- **Hierarchy Placement:** Level 4, child of References Payload Contract Supervisor
- **Semantic Persona:** Owns the entity analysis observability slice end to end, which is small enough that reads and
  writes stay with one agent.
- **Semantic Routing Keywords:** entity, analysis, inline, script, potential, multi, part, string, names, request, path,
  by, integer, float, data, type
- **Toolbox (Max 15):**
    * `EntityAnalysisInlineScriptList`: Lists registered Inline Scripts from the system-wide catalogue, keyset-paged and
      capped. _(permissions from `EntityAnalysisInlineScriptService.ListPermissions`)_
    * `EntityAnalysisInlineScriptFilterFields`: Lists the fields a query builder JSON filter over Entity Analysis Inline
      Scripts may use. _(permissions from `EntityAnalysisInlineScriptService.ListPermissions`)_
    * `EntityAnalysisInlineScriptFilter`: Returns the Entity Analysis Inline Scripts matching query builder JSON,
      keyset-paged and capped. _(permissions from `EntityAnalysisInlineScriptService.ListPermissions`)_
    * `EntityAnalysisInlineScriptCount`: Counts the Entity Analysis Inline Scripts matching query builder JSON,
      optionally grouped by a field. _(permissions from `EntityAnalysisInlineScriptService.ListPermissions`)_
    * `EntityAnalysisPotentialMultiPartStringNamesGetById`: Lists the names of potential multi-part string fields for an
      entity analysis model, by model Id. _(permissions from
      `EntityAnalysisPotentialMultiPartStringNamesService.RequiredPermissions`)_
    * `EntityAnalysisPotentialMultiPartStringNamesGetByGuid`: Lists the names of potential multi-part string fields for
      an entity analysis model, by model Guid. _(permissions from
      `EntityAnalysisPotentialMultiPartStringNamesService.RequiredPermissions`)_
    * `EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeGet`: Lists the String and Float fields of
      an Entity Analysis Model usable as search keys. _(permissions from
      `EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### References Watchlists Supervisor

- **Hierarchy Placement:** Level 3, child of Detection Model Authoring
- **Semantic Persona:** Routes within the references watchlists slice of detection model authoring. Holds no tools; it
  exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** references watchlists, entity, analysis, model, sanction, tag
- **Toolbox (Max 15):** none — routes to 6 leaf agents covering 20 tools
- **Downstream Dependencies:** `entity-analysis-model-sanction-inspector`, `entity-analysis-model-sanction-test-bench`,
  `entity-analysis-model-sanction-author`, `entity-analysis-model-tag-inspector`,
  `entity-analysis-model-tag-test-bench`, `entity-analysis-model-tag-author`
- **Cross-Domain Reach:** `completions`

#### Entity Analysis Model Sanction Inspector (`entity-analysis-model-sanction-inspector`)

- **Hierarchy Placement:** Level 4, child of References Watchlists Supervisor
- **Semantic Persona:** Owns the entity analysis model sanction slice and reads only; it never changes anything and is
  the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, sanction, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelSanctionList`: Lists Sanction checks for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelSanctionService.ListPermissions`)_
    * `EntityAnalysisModelSanctionFilterFields`: Lists the fields a query builder JSON filter over Sanctions may use. _(
      permissions from `EntityAnalysisModelSanctionService.ListPermissions`)_
    * `EntityAnalysisModelSanctionFilter`: Returns the Sanctions matching query builder JSON, keyset-paged and capped.
      _(permissions from `EntityAnalysisModelSanctionService.ListPermissions`)_
    * `EntityAnalysisModelSanctionCount`: Counts the Sanctions matching query builder JSON, optionally grouped by a
      field. _(permissions from `EntityAnalysisModelSanctionService.ListPermissions`)_
    * `EntityAnalysisModelSanctionGet`: Returns one Sanction check by id, scoped to the caller's tenant. _(permissions
      from `EntityAnalysisModelSanctionService.ReadPermissions`)_
    * `EntityAnalysisModelSanctionGetByGuid`: Returns one Sanction check by Guid, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelSanctionService.ReadPermissions`)_
    * `EntityAnalysisModelSanctionGetByEntityAnalysisModelId`: Lists Sanction checks belonging to a given Model, scoped
      to the caller's tenant. _(permissions from `EntityAnalysisModelSanctionService.ReadPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Sanction Test Bench (`entity-analysis-model-sanction-test-bench`)

- **Hierarchy Placement:** Level 4, child of References Watchlists Supervisor
- **Semantic Persona:** Owns the entity analysis model sanction slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, sanction, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelSanctionValidate`: Validates a Sanction without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelSanctionService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Sanction Author (`entity-analysis-model-sanction-author`)

- **Hierarchy Placement:** Level 4, child of References Watchlists Supervisor
- **Semantic Persona:** Owns the entity analysis model sanction slice and is the only agent that mutates this slice;
  every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, sanction, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelSanctionList`: Lists Sanction checks for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelSanctionService.ListPermissions`)_
    * `EntityAnalysisModelSanctionFilterFields`: Lists the fields a query builder JSON filter over Sanctions may use. _(
      permissions from `EntityAnalysisModelSanctionService.ListPermissions`)_
    * `EntityAnalysisModelSanctionFilter`: Returns the Sanctions matching query builder JSON, keyset-paged and capped.
      _(permissions from `EntityAnalysisModelSanctionService.ListPermissions`)_
    * `EntityAnalysisModelSanctionCount`: Counts the Sanctions matching query builder JSON, optionally grouped by a
      field. _(permissions from `EntityAnalysisModelSanctionService.ListPermissions`)_
    * `EntityAnalysisModelSanctionGet`: Returns one Sanction check by id, scoped to the caller's tenant. _(permissions
      from `EntityAnalysisModelSanctionService.ReadPermissions`)_
    * `EntityAnalysisModelSanctionGetByGuid`: Returns one Sanction check by Guid, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelSanctionService.ReadPermissions`)_
    * `EntityAnalysisModelSanctionGetByEntityAnalysisModelId`: Lists Sanction checks belonging to a given Model, scoped
      to the caller's tenant. _(permissions from `EntityAnalysisModelSanctionService.ReadPermissions`)_
    * `EntityAnalysisModelSanctionValidate`: Validates a Sanction without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelSanctionService.WritePermissions`)_
    * `EntityAnalysisModelSanctionCreate`: Registers a Sanction check under a Model in the caller's tenant; calling
      twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelSanctionService.WritePermissions`)_
    * `EntityAnalysisModelSanctionUpdate`: Updates a Sanction check in the caller's tenant by id. **Write — confirmation
      required** _(permissions from `EntityAnalysisModelSanctionService.WritePermissions`)_
    * `EntityAnalysisModelSanctionDelete`: Soft-deletes a Sanction check in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelSanctionService.WritePermissions`)_
    * `EntityAnalysisModelSanctionResubmit`: Resubmits a rejected Sanction check for approval again, unchanged, opening
      a fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelSanctionService.WritePermissions`)_
    * `EntityAnalysisModelSanctionRetract`: Retracts a rejected Sanction check, restoring it to its last approved
      version without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelSanctionService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Tag Inspector (`entity-analysis-model-tag-inspector`)

- **Hierarchy Placement:** Level 4, child of References Watchlists Supervisor
- **Semantic Persona:** Owns the entity analysis model tag slice and reads only; it never changes anything and is the
  safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, tag, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelTagList`: Lists Tags for the caller's tenant, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagFilterFields`: Lists the fields a query builder JSON filter over Tags may use. _(
      permissions from `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagFilter`: Returns the Tags matching query builder JSON, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagCount`: Counts the Tags matching query builder JSON, optionally grouped by a field. _(
      permissions from `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagGet`: Returns one Tag by id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagGetByGuid`: Returns one Tag by Guid, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagGetByEntityAnalysisModelId`: Lists the Active Tags belonging to a given Model, ordered by
      Name, scoped to the caller's tenant. _(permissions from `EntityAnalysisModelTagService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Tag Test Bench (`entity-analysis-model-tag-test-bench`)

- **Hierarchy Placement:** Level 4, child of References Watchlists Supervisor
- **Semantic Persona:** Owns the entity analysis model tag slice and runs the side-effect-free rehearsals: validation,
  rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, tag, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelTagValidate`: Validates a Tag without saving it and returns every failure. _(permissions from
      `EntityAnalysisModelTagService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Tag Author (`entity-analysis-model-tag-author`)

- **Hierarchy Placement:** Level 4, child of References Watchlists Supervisor
- **Semantic Persona:** Owns the entity analysis model tag slice and is the only agent that mutates this slice; every
  call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, tag, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelTagList`: Lists Tags for the caller's tenant, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagFilterFields`: Lists the fields a query builder JSON filter over Tags may use. _(
      permissions from `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagFilter`: Returns the Tags matching query builder JSON, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagCount`: Counts the Tags matching query builder JSON, optionally grouped by a field. _(
      permissions from `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagGet`: Returns one Tag by id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagGetByGuid`: Returns one Tag by Guid, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagGetByEntityAnalysisModelId`: Lists the Active Tags belonging to a given Model, ordered by
      Name, scoped to the caller's tenant. _(permissions from `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagValidate`: Validates a Tag without saving it and returns every failure. _(permissions from
      `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagCreate`: Registers a Tag under a Model in the caller's tenant; calling twice creates two.
      **Write — confirmation required** _(permissions from `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagUpdate`: Updates a Tag in the caller's tenant by id. **Write — confirmation required** _(
      permissions from `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagDelete`: Soft-deletes a Tag in the caller's tenant by id. **Write — confirmation required**
      _(permissions from `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagResubmit`: Resubmits a rejected Tag for approval again, unchanged, opening a fresh approval
      cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelTagService.RequiredPermissions`)_
    * `EntityAnalysisModelTagRetract`: Retracts a rejected Tag, restoring it to its last approved version without a new
      approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelTagService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

---

## Model Risk and Analytics (`model-risk`)

#### Model Risk and Analytics

- **Hierarchy Placement:** Level 2, child of Jooby
- **Semantic Persona:** Owns everything that trains, validates, promotes and explains a model: Exhaustive search,
  Advanced Analytics, backtesting, integrity and HTTP adaptation. This is the model-risk-management function and the
  only place a model's predictive behaviour changes.
- **Semantic Routing Keywords:** train, training run, promote, rollback, backtest, calibration, anomaly, classifier,
  regressor, clusterer, model integrity, drift, explainability
- **Toolbox (Max 15):** none — routes to 4 supervisors and 1 leaf agent covering 133 tools
- **Downstream Dependencies:** `model-risk.advanced-analytics`, `model-risk.advanced-analytics-training`,
  `model-risk.exhaustive`, `model-risk.validation`, `advanced-analytics-training-data`
- **Cross-Domain Reach:** `completions`

#### Advanced Analytics Training Data (`advanced-analytics-training-data`)

- **Hierarchy Placement:** Level 3, child of Model Risk and Analytics
- **Semantic Persona:** Turns a table somebody has queried into the numeric training data a run reads, choosing columns
  by name and packing the outcome last. It trains nothing. It hangs off the domain rather than off the training
  supervisor because a leaf cannot delegate, so a pointer prepared below the supervisor could never be handed back up to
  it.
- **Semantic Routing Keywords:** training data, data pointer, cache pointer, label column, predictors, prepare, matrix
- **Toolbox (Max 15):**
    * `AdvancedAnalyticsTrainingDataPut`: Turns a cached table into the numeric training data a run reads, by named
      columns, and returns the cache pointer to submit against. **Write — confirmation required** _(permissions from
      `AdvancedAnalyticsPermissionSpecifications.TrainingOnly`)_
    * `AdvancedAnalyticsTrainingDataGet`: Reports the shape, size and expiry of training data already put in the cache,
      or nothing when it has expired. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Supervisor

- **Hierarchy Placement:** Level 3, child of Model Risk and Analytics
- **Semantic Persona:** Routes within the advanced analytics slice of model risk and analytics. Holds no tools; it
  exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** advanced analytics, advanced, analytics, instance, model
- **Toolbox (Max 15):** none — routes to 5 leaf agents covering 19 tools
- **Downstream Dependencies:** `advanced-analytics-instance-inspector`, `advanced-analytics-instance-test-bench`,
  `advanced-analytics-instance-author`, `advanced-analytics-model-inspector`, `advanced-analytics-model-author`

#### Advanced Analytics Instance Inspector (`advanced-analytics-instance-inspector`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Supervisor
- **Semantic Persona:** Owns the advanced analytics instance slice and reads only; it never changes anything and is the
  safe default for any question about current state.
- **Semantic Routing Keywords:** advanced, analytics, instance, inspector
- **Toolbox (Max 15):**
    * `AdvancedAnalyticsInstanceGet`: Returns one Advanced Analytics instance by id, scoped to the caller's tenant. _(
      permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceGetByGuid`: Returns one Advanced Analytics instance by Guid, scoped to the caller's
      tenant. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceList`: Lists Advanced Analytics instances for the caller's tenant, keyset-paged and
      capped. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceFilterFields`: Lists the fields a query builder JSON filter over Advanced Analytics
      instances may use. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceFilter`: Returns the Advanced Analytics instances matching query builder JSON,
      keyset-paged and capped. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceCount`: Counts the Advanced Analytics instances matching query builder JSON, optionally
      grouped by a field. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Instance Test Bench (`advanced-analytics-instance-test-bench`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Supervisor
- **Semantic Persona:** Owns the advanced analytics instance slice and runs the side-effect-free rehearsals: validation,
  rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** advanced, analytics, instance, test, bench
- **Toolbox (Max 15):**
    * `AdvancedAnalyticsInstanceValidate`: Validates an Advanced Analytics instance without saving it and returns every
      failure. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Instance Author (`advanced-analytics-instance-author`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Supervisor
- **Semantic Persona:** Owns the advanced analytics instance slice and is the only agent that mutates this slice; every
  call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** advanced, analytics, instance, author
- **Toolbox (Max 15):**
    * `AdvancedAnalyticsInstanceGet`: Returns one Advanced Analytics instance by id, scoped to the caller's tenant. _(
      permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceGetByGuid`: Returns one Advanced Analytics instance by Guid, scoped to the caller's
      tenant. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceList`: Lists Advanced Analytics instances for the caller's tenant, keyset-paged and
      capped. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceFilterFields`: Lists the fields a query builder JSON filter over Advanced Analytics
      instances may use. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceFilter`: Returns the Advanced Analytics instances matching query builder JSON,
      keyset-paged and capped. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceCount`: Counts the Advanced Analytics instances matching query builder JSON, optionally
      grouped by a field. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceValidate`: Validates an Advanced Analytics instance without saving it and returns every
      failure. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceCreate`: Creates an unallocated Advanced Analytics instance in the caller's tenant.
      **Write — confirmation required** _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceUpdate`: Renames an instance or changes its output flags; never changes its allocation.
      **Write — confirmation required** _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceDelete`: Soft-deletes an Advanced Analytics instance, taking it out of the live
      pipeline. **Write — confirmation required** _(permissions from
      `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsInstanceAllocateToModel`: Allocates an instance to a Jube Model of the caller's tenant; needs
      the Promote Model permission. **Write — confirmation required** _(permissions from
      `AdvancedAnalyticsPermissionSpecifications.PromoteModelOnly`)_
    * `AdvancedAnalyticsInstanceDeallocate`: Removes an instance's allocation to a Jube Model; needs the Promote Model
      permission. **Write — confirmation required** _(permissions from
      `AdvancedAnalyticsPermissionSpecifications.PromoteModelOnly`)_
    * `AdvancedAnalyticsInstanceResubmit`: Resubmits a rejected change to an instance for approval again; needs the
      Promote Model permission. **Write — confirmation required** _(permissions from
      `AdvancedAnalyticsPermissionSpecifications.PromoteModelOnly`)_
    * `AdvancedAnalyticsInstanceRetract`: Retracts a rejected change to an instance, restoring its last approved state;
      needs the Promote Model permission. **Write — confirmation required** _(permissions from
      `AdvancedAnalyticsPermissionSpecifications.PromoteModelOnly`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Model Inspector (`advanced-analytics-model-inspector`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Supervisor
- **Semantic Persona:** Owns the advanced analytics model slice and reads only; it never changes anything and is the
  safe default for any question about current state.
- **Semantic Routing Keywords:** advanced, analytics, model, inspector
- **Toolbox (Max 15):**
    * `AdvancedAnalyticsModelList`: Lists trained models for the caller's tenant with their live version, keyset-paged
      and capped. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsModelGet`: Returns one trained model with all its versions and their headline metrics. _(
      permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsModelListVersions`: Lists the versions of a trained model with headline metrics and warning
      codes. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsModelGetVersion`: Returns one model version with its complete stored evidence. _(permissions
      from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsModelCompareVersions`: Compares two model versions on held-out metrics and says whether the
      comparison is fair. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsModelGetPromotionEvidence`: Returns the evidence needed to decide whether a model version
      deserves promotion. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Model Author (`advanced-analytics-model-author`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Supervisor
- **Semantic Persona:** Owns the advanced analytics model slice and is the only agent that mutates this slice; every
  call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** advanced, analytics, model, author
- **Toolbox (Max 15):**
    * `AdvancedAnalyticsModelList`: Lists trained models for the caller's tenant with their live version, keyset-paged
      and capped. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsModelGet`: Returns one trained model with all its versions and their headline metrics. _(
      permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsModelListVersions`: Lists the versions of a trained model with headline metrics and warning
      codes. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsModelGetVersion`: Returns one model version with its complete stored evidence. _(permissions
      from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsModelCompareVersions`: Compares two model versions on held-out metrics and says whether the
      comparison is fair. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsModelGetPromotionEvidence`: Returns the evidence needed to decide whether a model version
      deserves promotion. _(permissions from `AdvancedAnalyticsPermissionSpecifications.AnalyticsOnly`)_
    * `AdvancedAnalyticsModelPromoteVersion`: Makes a model version live, recording its evidence; needs the Promote
      Model permission. **Write — confirmation required** _(permissions from
      `AdvancedAnalyticsPermissionSpecifications.PromoteModelOnly`)_
    * `AdvancedAnalyticsModelRollback`: Puts a model back on an earlier version; needs the Promote Model permission.
      **Write — confirmation required** _(permissions from
      `AdvancedAnalyticsPermissionSpecifications.PromoteModelOnly`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Training Supervisor

- **Hierarchy Placement:** Level 3, child of Model Risk and Analytics
- **Semantic Persona:** Routes within the advanced analytics training slice of model risk and analytics by academic
  model kind rather than by what sounds closest to the request. Holds no tools; every run below belongs to an Advanced
  Analytics instance that must already exist, and training data must already be a cache pointer (via the training data
  specialist) before a run can be opened.
- **Semantic Routing Keywords:** train, training run, classifier, regressor, clusterer, anomaly, latent structure, model
  family, academic kind
- **Toolbox (Max 15):** none — routes to 8 leaf agents covering 56 tools
- **Downstream Dependencies:** `advanced-analytics-training-classifier-trees`,
  `advanced-analytics-training-classifier-probabilistic`, `advanced-analytics-training-classifier-margin`,
  `advanced-analytics-training-clusterer-centroid`, `advanced-analytics-training-clusterer-density`,
  `advanced-analytics-training-regressor`, `advanced-analytics-training-anomaly`,
  `advanced-analytics-training-latent-structure`
- **Cross-Domain Reach:** `advanced-analytics-instance-inspector`

#### Advanced Analytics Training Classifier Trees (`advanced-analytics-training-classifier-trees`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Training Supervisor
- **Semantic Persona:** Trains the tree and ensemble classifiers of advanced analytics: a single decision tree, a random
  forest, a bagged ensemble, AdaBoost and gradient boosting. All five fit axis-parallel splits and differ in how many
  trees they grow and how they weight them.
- **Semantic Routing Keywords:** decision tree, random forest, bagging, adaboost, gradient boosting, classifier,
  ensemble, tree
- **Toolbox (Max 15):**
    * `SubmitDecisionTreeClassifierTraining`: Queues a decision tree classification training run: fits nested yes/no
      threshold splits that best separate the event of interest, with the leaf's calibrated share of positive rows as
      the recalled probability. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitRandomForestClassifierTraining`: Queues a random forest classification training run: averages many
      bootstrap-sampled decision trees' votes into a steadier, Platt-calibrated probability than any single tree's. _(
      permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitBaggingClassifierTraining`: Queues a bagging classification training run: averages many unrestricted
      decision trees, each grown on its own bootstrap sample, into a Platt-calibrated probability. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitAdaBoostClassifierTraining`: Queues an AdaBoost classification training run: fits shallow trees one after
      another, each reweighted onto the previous rounds' mistakes, and Platt-calibrates the alpha-weighted vote margin.
      _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitGradientBoostingClassifierTraining`: Queues a gradient boosting classification training run: fits shallow
      trees successively to the current log-odds residual and Platt-calibrates the resulting margin. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunStatus`: Reads where a queued training run has got to: Pending, Running, then
      Succeeded, Failed or Cancelled; works for a run submitted by any family's Submit tool. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
    * `CancelAdvancedAnalyticsTrainingRun`: Cancels a training run submitted by any family's Submit tool; a finished run
      is returned unchanged. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunResult`: Reads the outcome of a finished training run: its final status and, once
      succeeded, the trained model and version plus family-specific evidence. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Training Classifier Probabilistic (`advanced-analytics-training-classifier-probabilistic`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Training Supervisor
- **Semantic Persona:** Trains the probabilistic classifiers of advanced analytics: logistic regression, naive Bayes and
  tree-augmented naive Bayes. These three estimate a probability as part of the fit rather than having one scaled on
  afterwards, so their evidence reads as coefficients or conditional tables rather than as a vote.
- **Semantic Routing Keywords:** logistic regression, naive bayes, tree augmented naive bayes, probabilistic classifier,
  coefficients
- **Toolbox (Max 15):**
    * `SubmitLogisticRegressionTraining`: Queues a logistic regression training run against inline rows or a cache
      pointer, fitting a ridge-penalised coefficient per predictor. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitNaiveBayesClassifierTraining`: Queues a Gaussian naive Bayes classifier training run, fitting one
      distribution per predictor per class and Platt-calibrating the resulting posterior. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitTreeAugmentedNaiveBayesClassifierTraining`: Queues a tree-augmented naive Bayes training run: a
      Chow-Liu-discovered one-parent-per-predictor structure relaxes naive Bayes' independence assumption, with the
      posterior Platt-calibrated. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunStatus`: Reads where a queued training run has got to: Pending, Running, then
      Succeeded, Failed or Cancelled; works for a run submitted by any family's Submit tool. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
    * `CancelAdvancedAnalyticsTrainingRun`: Cancels a training run submitted by any family's Submit tool; a finished run
      is returned unchanged. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunResult`: Reads the outcome of a finished training run: its final status and, once
      succeeded, the trained model and version plus family-specific evidence. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Training Classifier Margin (`advanced-analytics-training-classifier-margin`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Training Supervisor
- **Semantic Persona:** Trains the margin, instance and connectionist classifiers of advanced analytics: linear and
  kernel support vector machines, k nearest neighbours and a neural network. None emits a probability of its own, so
  every one relies on calibration fitted on the validation split.
- **Semantic Routing Keywords:** support vector machine, svm, kernel, k nearest neighbours, neural network, classifier,
  margin
- **Toolbox (Max 15):**
    * `SubmitLinearSupportVectorClassifierTraining`: Queues a linear support vector machine classifier training run,
      fitting a maximum-margin linear boundary by dual coordinate descent and Platt-calibrating its signed distance. _(
      permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitKernelSupportVectorClassifierTraining`: Queues a kernel support vector machine classifier training run,
      fitting a maximum-margin boundary in a kernel-reshaped space by sequential minimal optimisation and
      Platt-calibrating its signed distance. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitKNearestNeighboursClassifierTraining`: Queues a k nearest neighbours classifier training run, scoring a row
      by the Platt-calibrated vote share of its k nearest standardised training rows. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitNeuralNetworkClassifierTraining`: Queues a small feedforward neural network classifier training run,
      fitting one hidden layer and Platt-calibrating its sigmoid output. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunStatus`: Reads where a queued training run has got to: Pending, Running, then
      Succeeded, Failed or Cancelled; works for a run submitted by any family's Submit tool. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
    * `CancelAdvancedAnalyticsTrainingRun`: Cancels a training run submitted by any family's Submit tool; a finished run
      is returned unchanged. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunResult`: Reads the outcome of a finished training run: its final status and, once
      succeeded, the trained model and version plus family-specific evidence. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Training Clusterer Centroid (`advanced-analytics-training-clusterer-centroid`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Training Supervisor
- **Semantic Persona:** Trains the centroid and medoid clusterers of advanced analytics: k means, mini batch k means,
  balanced k means, k medoids and k modes. Every one is told how many groups to find and summarises each by one
  representative point.
- **Semantic Routing Keywords:** k means, mini batch k means, balanced k means, k medoids, k modes, clusterer, centroid,
  medoid
- **Toolbox (Max 15):**
    * `SubmitKMeansClustererTraining`: Queues a k-means clustering training run, assigning rows to the nearest of k
      iteratively-recentred means with no labels. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitMiniBatchKMeansClustererTraining`: Queues a mini-batch k-means clustering training run: a faster k-means
      approximation for large row counts that nudges centres from small random batches. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitBalancedKMeansClustererTraining`: Queues a balanced k-means clustering training run, constraining cluster
      sizes to as close to equal as the row count allows via an optimal-assignment step. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitKMedoidsClustererTraining`: Queues a k-medoids clustering training run, requiring each cluster centre to be
      an actual training row rather than an averaged point. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitKModesClustererTraining`: Queues a k-modes clustering training run over categorical, integer-coded columns,
      clustering by per-cluster modal category rather than an averaged centre. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunStatus`: Reads where a queued training run has got to: Pending, Running, then
      Succeeded, Failed or Cancelled; works for a run submitted by any family's Submit tool. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
    * `CancelAdvancedAnalyticsTrainingRun`: Cancels a training run submitted by any family's Submit tool; a finished run
      is returned unchanged. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunResult`: Reads the outcome of a finished training run: its final status and, once
      succeeded, the trained model and version plus family-specific evidence. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Training Clusterer Density (`advanced-analytics-training-clusterer-density`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Training Supervisor
- **Semantic Persona:** Trains the hierarchical, density and mixture clusterers of advanced analytics: binary split,
  mean shift and a Gaussian mixture. These discover how many groups there are rather than being told.
- **Semantic Routing Keywords:** binary split, mean shift, gaussian mixture, clusterer, density, hierarchical
- **Toolbox (Max 15):**
    * `SubmitBinarySplitClustererTraining`: Queues a binary split clustering training run, repeatedly bisecting
      whichever existing cluster has the largest distortion until k clusters exist. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitMeanShiftClustererTraining`: Queues a mean shift clustering training run, letting rows climb to the nearest
      density peak so the cluster count is discovered rather than chosen. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitGaussianMixtureClustererTraining`: Queues a Gaussian mixture clustering training run, fitting overlapping
      bell-shaped components and reporting each row's per-component membership probability. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunStatus`: Reads where a queued training run has got to: Pending, Running, then
      Succeeded, Failed or Cancelled; works for a run submitted by any family's Submit tool. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
    * `CancelAdvancedAnalyticsTrainingRun`: Cancels a training run submitted by any family's Submit tool; a finished run
      is returned unchanged. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunResult`: Reads the outcome of a finished training run: its final status and, once
      succeeded, the trained model and version plus family-specific evidence. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Training Regressor (`advanced-analytics-training-regressor`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Training Supervisor
- **Semantic Persona:** Trains every regressor in advanced analytics: linear regression, kernel ridge, a decision tree,
  a random forest, gradient boosting, k nearest neighbours and a neural network. All seven predict a number rather than
  a class or a group.
- **Semantic Routing Keywords:** linear regression, kernel ridge, decision tree, random forest, gradient boosting, k
  nearest neighbours, neural network, regressor
- **Toolbox (Max 15):**
    * `SubmitLinearRegressionTraining`: Queues an ordinary least squares linear regression training run against inline
      rows. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitKernelRidgeRegressorTraining`: Queues a kernel ridge regression training run, solving a ridge-penalised fit
      in a kernel-reshaped space by a single closed-form linear solve. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitDecisionTreeRegressorTraining`: Queues a decision tree regression training run, fitting nested threshold
      splits that most reduce the outcome's variance. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitRandomForestRegressorTraining`: Queues a random forest regression training run, averaging many
      bootstrap-sampled regression trees into a steadier continuous prediction. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitGradientBoostingRegressorTraining`: Queues a gradient boosting regression training run, fitting shallow
      trees successively to the current residual with no calibration needed. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitKNearestNeighboursRegressorTraining`: Queues a k nearest neighbours regression training run, predicting the
      mean target of a row's k nearest standardised training rows. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitNeuralNetworkRegressorTraining`: Queues a small feedforward neural network regression training run, mapping
      its bounded output back onto the outcome's original scale. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunStatus`: Reads where a queued training run has got to: Pending, Running, then
      Succeeded, Failed or Cancelled; works for a run submitted by any family's Submit tool. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
    * `CancelAdvancedAnalyticsTrainingRun`: Cancels a training run submitted by any family's Submit tool; a finished run
      is returned unchanged. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunResult`: Reads the outcome of a finished training run: its final status and, once
      succeeded, the trained model and version plus family-specific evidence. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Training Anomaly (`advanced-analytics-training-anomaly`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Training Supervisor
- **Semantic Persona:** Trains the anomaly detectors of advanced analytics: isolation forest, Mahalanobis distance and a
  one-class support vector machine. All three learn what normal looks like and score distance from it with no label
  required.
- **Semantic Routing Keywords:** isolation forest, mahalanobis, one-class support vector machine, anomaly, outlier
- **Toolbox (Max 15):**
    * `SubmitIsolationForestAnomalyTraining`: Queues an isolation forest anomaly training run, scoring a row by how few
      random splits it takes to isolate it; unsupervised, with labels only optionally calibrating the score afterwards.
      _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitMahalanobisAnomalyTraining`: Queues a Mahalanobis-distance anomaly training run, scoring a row by its
      correlation-aware distance from the fitted mean. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitOneClassSupportVectorAnomalyTraining`: Queues a one-class support vector machine anomaly training run,
      fitting a boundary that wraps snugly around the bulk of unlabelled rows in a kernel-reshaped space. _(permissions
      from `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunStatus`: Reads where a queued training run has got to: Pending, Running, then
      Succeeded, Failed or Cancelled; works for a run submitted by any family's Submit tool. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
    * `CancelAdvancedAnalyticsTrainingRun`: Cancels a training run submitted by any family's Submit tool; a finished run
      is returned unchanged. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunResult`: Reads the outcome of a finished training run: its final status and, once
      succeeded, the trained model and version plus family-specific evidence. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
- **Downstream Dependencies:** none (leaf)

#### Advanced Analytics Training Latent Structure (`advanced-analytics-training-latent-structure`)

- **Hierarchy Placement:** Level 4, child of Advanced Analytics Training Supervisor
- **Semantic Persona:** Trains the two families of advanced analytics that describe the shape of the data rather than
  predicting from it: principal components, which reduces dimension, and a Gaussian mixture density, which estimates the
  distribution itself. Neither predicts anything.
- **Semantic Routing Keywords:** principal component, pca, gaussian mixture density, dimension reduction, density
  estimation, latent structure
- **Toolbox (Max 15):**
    * `SubmitPrincipalComponentTraining`: Queues a principal component training run, re-expressing correlated variables
      as a smaller set of uncorrelated, variance-ranked components. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `SubmitGaussianMixtureDensityTraining`: Queues a Gaussian mixture density training run, describing the rows' shape
      as a weighted sum of components and scoring a row by its log-density. _(permissions from
      `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunStatus`: Reads where a queued training run has got to: Pending, Running, then
      Succeeded, Failed or Cancelled; works for a run submitted by any family's Submit tool. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
    * `CancelAdvancedAnalyticsTrainingRun`: Cancels a training run submitted by any family's Submit tool; a finished run
      is returned unchanged. _(permissions from `AdvancedAnalyticsPermissions.Training`)_
    * `GetAdvancedAnalyticsTrainingRunResult`: Reads the outcome of a finished training run: its final status and, once
      succeeded, the trained model and version plus family-specific evidence. _(permissions from
      `AdvancedAnalyticsPermissions.Analytics`)_
- **Downstream Dependencies:** none (leaf)

#### Exhaustive Supervisor

- **Hierarchy Placement:** Level 3, child of Model Risk and Analytics
- **Semantic Persona:** Routes within the exhaustive slice of model risk and analytics. Holds no tools; it exists so the
  domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** exhaustive, search, instance, trial, list, query, variable, promoted, confusion, error,
  histogram, learning, curve, predicted, actual, roc, prescription, promotion, history, variance, statistics
- **Toolbox (Max 15):** none — routes to 6 leaf agents covering 28 tools
- **Downstream Dependencies:** `exhaustive-search-instance-inspector`, `exhaustive-search-instance-test-bench`,
  `exhaustive-search-instance-author`, `exhaustive-trials-and-confusion`, `exhaustive-promotion-curves-and-history`,
  `exhaustive-variable-statistics`
- **Cross-Domain Reach:** `completions`

#### Exhaustive Search Instance Inspector (`exhaustive-search-instance-inspector`)

- **Hierarchy Placement:** Level 4, child of Exhaustive Supervisor
- **Semantic Persona:** Owns the exhaustive search instance slice and reads only; it never changes anything and is the
  safe default for any question about current state.
- **Semantic Routing Keywords:** exhaustive, search, instance, inspector
- **Toolbox (Max 15):**
    * `ExhaustiveSearchInstanceList`: Lists Exhaustive Adaptations for the caller's tenant, keyset-paged and capped. _(
      permissions from `ExhaustiveSearchInstanceService.ListPermissions`)_
    * `ExhaustiveSearchInstanceFilterFields`: Lists the fields a query builder JSON filter over Exhaustive Search
      Instances may use. _(permissions from `ExhaustiveSearchInstanceService.ListPermissions`)_
    * `ExhaustiveSearchInstanceFilter`: Returns the Exhaustive Search Instances matching query builder JSON,
      keyset-paged and capped. _(permissions from `ExhaustiveSearchInstanceService.ListPermissions`)_
    * `ExhaustiveSearchInstanceCount`: Counts the Exhaustive Search Instances matching query builder JSON, optionally
      grouped by a field. _(permissions from `ExhaustiveSearchInstanceService.ListPermissions`)_
    * `ExhaustiveSearchInstanceGet`: Returns one Exhaustive Adaptation by id, scoped to the caller's tenant. _(
      permissions from `ExhaustiveSearchInstanceService.ReadPermissions`)_
    * `ExhaustiveSearchInstanceGetByGuid`: Returns one Exhaustive Adaptation by Guid, scoped to the caller's tenant. _(
      permissions from `ExhaustiveSearchInstanceService.ReadPermissions`)_
    * `ExhaustiveSearchInstanceGetByEntityAnalysisModelId`: Lists Exhaustive Adaptations belonging to a given Model,
      scoped to the caller's tenant. _(permissions from `ExhaustiveSearchInstanceService.ReadPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Exhaustive Search Instance Test Bench (`exhaustive-search-instance-test-bench`)

- **Hierarchy Placement:** Level 4, child of Exhaustive Supervisor
- **Semantic Persona:** Owns the exhaustive search instance slice and runs the side-effect-free rehearsals: validation,
  rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** exhaustive, search, instance, test, bench
- **Toolbox (Max 15):**
    * `ExhaustiveSearchInstanceValidate`: Validates an Exhaustive Search Instance without saving it and returns every
      failure. _(permissions from `ExhaustiveSearchInstanceService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Exhaustive Search Instance Author (`exhaustive-search-instance-author`)

- **Hierarchy Placement:** Level 4, child of Exhaustive Supervisor
- **Semantic Persona:** Owns the exhaustive search instance slice and is the only agent that mutates this slice; every
  call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** exhaustive, search, instance, author
- **Toolbox (Max 15):**
    * `ExhaustiveSearchInstanceList`: Lists Exhaustive Adaptations for the caller's tenant, keyset-paged and capped. _(
      permissions from `ExhaustiveSearchInstanceService.ListPermissions`)_
    * `ExhaustiveSearchInstanceFilterFields`: Lists the fields a query builder JSON filter over Exhaustive Search
      Instances may use. _(permissions from `ExhaustiveSearchInstanceService.ListPermissions`)_
    * `ExhaustiveSearchInstanceFilter`: Returns the Exhaustive Search Instances matching query builder JSON,
      keyset-paged and capped. _(permissions from `ExhaustiveSearchInstanceService.ListPermissions`)_
    * `ExhaustiveSearchInstanceCount`: Counts the Exhaustive Search Instances matching query builder JSON, optionally
      grouped by a field. _(permissions from `ExhaustiveSearchInstanceService.ListPermissions`)_
    * `ExhaustiveSearchInstanceGet`: Returns one Exhaustive Adaptation by id, scoped to the caller's tenant. _(
      permissions from `ExhaustiveSearchInstanceService.ReadPermissions`)_
    * `ExhaustiveSearchInstanceGetByGuid`: Returns one Exhaustive Adaptation by Guid, scoped to the caller's tenant. _(
      permissions from `ExhaustiveSearchInstanceService.ReadPermissions`)_
    * `ExhaustiveSearchInstanceGetByEntityAnalysisModelId`: Lists Exhaustive Adaptations belonging to a given Model,
      scoped to the caller's tenant. _(permissions from `ExhaustiveSearchInstanceService.ReadPermissions`)_
    * `ExhaustiveSearchInstanceValidate`: Validates an Exhaustive Search Instance without saving it and returns every
      failure. _(permissions from `ExhaustiveSearchInstanceService.WritePermissions`)_
    * `ExhaustiveSearchInstanceCreate`: Registers an Exhaustive Adaptation under a Model in the caller's tenant; calling
      twice creates two. **Write — confirmation required** _(permissions from
      `ExhaustiveSearchInstanceService.WritePermissions`)_
    * `ExhaustiveSearchInstanceUpdate`: Updates an Exhaustive Adaptation in the caller's tenant by id, only while
      training has not started. **Write — confirmation required** _(permissions from
      `ExhaustiveSearchInstanceService.WritePermissions`)_
    * `ExhaustiveSearchInstanceStop`: Requests that an in-progress or queued training run stop, by Guid. **Write —
      confirmation required** _(permissions from `ExhaustiveSearchInstanceService.WritePermissions`)_
    * `ExhaustiveSearchInstanceDelete`: Soft-deletes an Exhaustive Adaptation in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `ExhaustiveSearchInstanceService.WritePermissions`)_
    * `ExhaustiveSearchInstanceSetActive`: Sets whether an Exhaustive Adaptation is Active in the caller's tenant.
      **Write — confirmation required** _(permissions from `ExhaustiveSearchInstanceService.WritePermissions`)_
    * `ExhaustiveSearchInstanceResubmit`: Resubmits a rejected Exhaustive Adaptation for approval again, unchanged,
      opening a fresh approval cycle. **Write — confirmation required** _(permissions from
      `ExhaustiveSearchInstanceService.WritePermissions`)_
    * `ExhaustiveSearchInstanceRetract`: Retracts a rejected Exhaustive Adaptation, restoring it to its last approved
      version without a new approval cycle. **Write — confirmation required** _(permissions from
      `ExhaustiveSearchInstanceService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Exhaustive Trials and Confusion (`exhaustive-trials-and-confusion`)

- **Hierarchy Placement:** Level 4, child of Exhaustive Supervisor
- **Semantic Persona:** Owns the exhaustive trials and confusion slice end to end, which is small enough that reads and
  writes stay with one agent. It holds 2 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** exhaustive, search, instance, trial, list, query, variable, promoted, confusion, error,
  histogram
- **Toolbox (Max 15):**
    * `ExhaustiveSearchInstanceTrialInstanceListQueryGet`: Lists every trial attempt of one Exhaustive Search Instance,
      scoped to the caller's tenant. _(permissions from
      `ExhaustiveSearchInstanceTrialInstanceListQueryService.RequiredPermissions`)_
    * `ExhaustiveSearchInstanceTrialInstanceDetailGet`: Returns the detail of one Exhaustive trial attempt, scoped to
      the caller's tenant. _(permissions from
      `ExhaustiveSearchInstanceTrialInstanceListQueryService.RequiredPermissions`)_
    * `ExhaustiveSearchInstanceVariableGet`: Lists the variables of an Exhaustive Search Instance with distribution
      statistics and histogram bins. _(permissions from `ExhaustiveSearchInstanceVariableService.RequiredPermissions`)_
    * `ExhaustiveSearchInstanceVariableReallocate`: Reallocates a trained variable's live name and processing type,
      without retraining. **Write — confirmation required** _(permissions from
      `ExhaustiveSearchInstanceVariableService.RequiredPermissions`)_
    * `ExhaustiveSearchInstancePromotedTrialInstanceUpdate`: Sets whether a promoted trial instance is promoted for
      production use; setting the same value twice has no further effect. **Write — confirmation required** _(
      permissions from `ExhaustiveSearchInstancePromotedTrialInstanceService.WritePermissions`)_
    * `ExhaustiveSearchInstancePromotedTrialInstanceConfusionGet`: Returns the confusion matrix and derived ratios for
      the active promoted trial instance of an Exhaustive Search Instance. _(permissions from
      `ExhaustiveSearchInstancePromotedTrialInstanceConfusionService.RequiredPermissions`)_
    * `ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramGet`: Returns a ten-bin histogram of prediction errors
      of the active promoted trial instance of an exhaustive search instance. _(permissions from
      `ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Exhaustive Promotion Curves and History (`exhaustive-promotion-curves-and-history`)

- **Hierarchy Placement:** Level 4, child of Exhaustive Supervisor
- **Semantic Persona:** Owns the exhaustive promotion curves and history slice end to end, which is small enough that
  reads and writes stay with one agent.
- **Semantic Routing Keywords:** exhaustive, search, instance, promoted, trial, learning, curve, predicted, actual,
  query, roc, variable, prescription, promotion, history
- **Toolbox (Max 15):**
    * `ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveGet`: Returns the score-over-time learning curve of an
      exhaustive search instance's promoted trial instances. _(permissions from
      `ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveService.RequiredPermissions`)_
    * `ExhaustiveSearchInstancePromotedTrialInstancePredictedActualGet`: Returns predicted, actual and error values for
      the promoted trial instance of an exhaustive search instance. _(permissions from
      `ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService.RequiredPermissions`)_
    * `ExhaustiveSearchInstancePromotedTrialInstanceQueryGet`: Lists the promoted trial instances of one Exhaustive
      Search Instance, newest first. _(permissions from
      `ExhaustiveSearchInstancePromotedTrialInstanceQueryService.RequiredPermissions`)_
    * `ExhaustiveSearchInstancePromotedTrialInstanceRocGet`: Returns the ROC curve points (score, false positive rate,
      true positive rate) of an Exhaustive Search Instance's active promoted trial instance. _(permissions from
      `ExhaustiveSearchInstancePromotedTrialInstanceRocService.RequiredPermissions`)_
    * `ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionGet`: Lists the variables of a promoted trial
      instance with variable and prescription statistics and sensitivity, ordered by sensitivity descending. _(
      permissions from `ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService.RequiredPermissions`)_
    * `ExhaustiveSearchInstancePromotionHistoryGet`: Returns the promotion history of an exhaustive search instance:
      score, topology complexity and trials explored at each promotion. _(permissions from
      `ExhaustiveSearchInstancePromotionHistoryService.RequiredPermissions`)_
    * `ExhaustiveSearchInstanceTrialInstanceVariableGet`: Lists the variables of the promoted trial instance of an
      Exhaustive Search Instance with normalisation statistics, ordered by variable sequence. _(permissions from
      `ExhaustiveSearchInstanceTrialInstanceVariableService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Exhaustive Variable Statistics (`exhaustive-variable-statistics`)

- **Hierarchy Placement:** Level 4, child of Exhaustive Supervisor
- **Semantic Persona:** Owns the exhaustive variable statistics slice end to end, which is small enough that reads and
  writes stay with one agent.
- **Semantic Routing Keywords:** exhaustive, search, instance, trial, variable, variance, statistics
- **Toolbox (Max 15):**
    * `ExhaustiveSearchInstanceTrialInstanceVariableVarianceGet`: Lists the multi-collinearity correlations of one
      Exhaustive Search Instance Variable against its test variables, ordered by absolute correlation rank. _(
      permissions from `ExhaustiveSearchInstanceTrialInstanceVariableVarianceService.RequiredPermissions`)_
    * `ExhaustiveSearchInstanceVariableStatisticsGet`: Lists the variables of an Exhaustive Search Instance, or one
      variable, with the population, positive class, negative class and Monte Carlo prescription histograms. _(
      permissions from `ExhaustiveSearchInstanceVariableStatisticsService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Validation Supervisor

- **Hierarchy Placement:** Level 3, child of Model Risk and Analytics
- **Semantic Persona:** Routes within the validation slice of model risk and analytics. Holds no tools; it exists so the
  domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** validation, entity, analysis, model, backtest, http, adaptation, integrity, history,
  simulation, sample
- **Toolbox (Max 15):** none — routes to 7 leaf agents covering 25 tools
- **Downstream Dependencies:** `entity-analysis-model-backtest-inspector`, `entity-analysis-model-backtest-test-bench`,
  `entity-analysis-model-backtest-author`, `entity-analysis-model-http-adaptation-inspector`,
  `entity-analysis-model-http-adaptation-test-bench`, `entity-analysis-model-http-adaptation-author`,
  `model-integrity-and-sampling`
- **Cross-Domain Reach:** `completions`

#### Entity Analysis Model Backtest Inspector (`entity-analysis-model-backtest-inspector`)

- **Hierarchy Placement:** Level 4, child of Validation Supervisor
- **Semantic Persona:** Owns the entity analysis model backtest slice and reads only; it never changes anything and is
  the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, backtest, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelBacktestFields`: Lists the fields a backtest's filter and class may use, including Tag.<Name>
      for the class. _(permissions from `EntityAnalysisModelBacktestService.RequiredPermissions`)_
    * `EntityAnalysisModelBacktestRunRule`: Backtests a rule immediately over a filtered archive against a class;
      returns raw counts. _(permissions from `EntityAnalysisModelBacktestService.RequiredPermissions`)_
    * `EntityAnalysisModelBacktestStatus`: Returns a submitted backtest's status, progress and headline counts. _(
      permissions from `EntityAnalysisModelBacktestService.RequiredPermissions`)_
    * `EntityAnalysisModelBacktestList`: Lists a model's submitted backtests, optionally for one rule. _(permissions
      from `EntityAnalysisModelBacktestService.RequiredPermissions`)_
    * `EntityAnalysisModelBacktestResult`: Returns a finished backtest's counts and examples. _(permissions from
      `EntityAnalysisModelBacktestService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Backtest Test Bench (`entity-analysis-model-backtest-test-bench`)

- **Hierarchy Placement:** Level 4, child of Validation Supervisor
- **Semantic Persona:** Owns the entity analysis model backtest slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, backtest, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelBacktestExplain`: Explains why one archived transaction was or was not counted, with the
      values read. _(permissions from `EntityAnalysisModelBacktestService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Backtest Author (`entity-analysis-model-backtest-author`)

- **Hierarchy Placement:** Level 4, child of Validation Supervisor
- **Semantic Persona:** Owns the entity analysis model backtest slice and is the only agent that mutates this slice;
  every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, backtest, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelBacktestFields`: Lists the fields a backtest's filter and class may use, including Tag.<Name>
      for the class. _(permissions from `EntityAnalysisModelBacktestService.RequiredPermissions`)_
    * `EntityAnalysisModelBacktestRunRule`: Backtests a rule immediately over a filtered archive against a class;
      returns raw counts. _(permissions from `EntityAnalysisModelBacktestService.RequiredPermissions`)_
    * `EntityAnalysisModelBacktestStatus`: Returns a submitted backtest's status, progress and headline counts. _(
      permissions from `EntityAnalysisModelBacktestService.RequiredPermissions`)_
    * `EntityAnalysisModelBacktestList`: Lists a model's submitted backtests, optionally for one rule. _(permissions
      from `EntityAnalysisModelBacktestService.RequiredPermissions`)_
    * `EntityAnalysisModelBacktestResult`: Returns a finished backtest's counts and examples. _(permissions from
      `EntityAnalysisModelBacktestService.RequiredPermissions`)_
    * `EntityAnalysisModelBacktestExplain`: Explains why one archived transaction was or was not counted, with the
      values read. _(permissions from `EntityAnalysisModelBacktestService.RequiredPermissions`)_
    * `EntityAnalysisModelBacktestSubmit`: Submits a large backtest to the engine's backtest threads; returns the run to
      poll. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelBacktestService.RequiredPermissions`)_
    * `EntityAnalysisModelBacktestStop`: Stops a submitted backtest. **Write — confirmation required** _(permissions
      from `EntityAnalysisModelBacktestService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Http Adaptation Inspector (`entity-analysis-model-http-adaptation-inspector`)

- **Hierarchy Placement:** Level 4, child of Validation Supervisor
- **Semantic Persona:** Owns the entity analysis model http adaptation slice and reads only; it never changes anything
  and is the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, http, adaptation, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelHttpAdaptationList`: Lists HTTP Adaptations for the caller's tenant, keyset-paged and capped.
      _(permissions from `EntityAnalysisModelHttpAdaptationService.ListPermissions`)_
    * `EntityAnalysisModelHttpAdaptationFilterFields`: Lists the fields a query builder JSON filter over HTTP
      Adaptations may use. _(permissions from `EntityAnalysisModelHttpAdaptationService.ListPermissions`)_
    * `EntityAnalysisModelHttpAdaptationFilter`: Returns the HTTP Adaptations matching query builder JSON, keyset-paged
      and capped. _(permissions from `EntityAnalysisModelHttpAdaptationService.ListPermissions`)_
    * `EntityAnalysisModelHttpAdaptationCount`: Counts the HTTP Adaptations matching query builder JSON, optionally
      grouped by a field. _(permissions from `EntityAnalysisModelHttpAdaptationService.ListPermissions`)_
    * `EntityAnalysisModelHttpAdaptationGet`: Returns one HTTP Adaptation by id, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelHttpAdaptationService.ReadPermissions`)_
    * `EntityAnalysisModelHttpAdaptationGetByGuid`: Returns one HTTP Adaptation by Guid, scoped to the caller's tenant.
      _(permissions from `EntityAnalysisModelHttpAdaptationService.ReadPermissions`)_
    * `EntityAnalysisModelHttpAdaptationGetByEntityAnalysisModelId`: Lists HTTP Adaptations belonging to a given Model,
      scoped to the caller's tenant. _(permissions from `EntityAnalysisModelHttpAdaptationService.ReadPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Http Adaptation Test Bench (`entity-analysis-model-http-adaptation-test-bench`)

- **Hierarchy Placement:** Level 4, child of Validation Supervisor
- **Semantic Persona:** Owns the entity analysis model http adaptation slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, http, adaptation, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelHttpAdaptationValidate`: Validates a HTTP Adaptation without saving it and returns every
      failure. _(permissions from `EntityAnalysisModelHttpAdaptationService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Http Adaptation Author (`entity-analysis-model-http-adaptation-author`)

- **Hierarchy Placement:** Level 4, child of Validation Supervisor
- **Semantic Persona:** Owns the entity analysis model http adaptation slice and is the only agent that mutates this
  slice; every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, http, adaptation, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelHttpAdaptationList`: Lists HTTP Adaptations for the caller's tenant, keyset-paged and capped.
      _(permissions from `EntityAnalysisModelHttpAdaptationService.ListPermissions`)_
    * `EntityAnalysisModelHttpAdaptationFilterFields`: Lists the fields a query builder JSON filter over HTTP
      Adaptations may use. _(permissions from `EntityAnalysisModelHttpAdaptationService.ListPermissions`)_
    * `EntityAnalysisModelHttpAdaptationFilter`: Returns the HTTP Adaptations matching query builder JSON, keyset-paged
      and capped. _(permissions from `EntityAnalysisModelHttpAdaptationService.ListPermissions`)_
    * `EntityAnalysisModelHttpAdaptationCount`: Counts the HTTP Adaptations matching query builder JSON, optionally
      grouped by a field. _(permissions from `EntityAnalysisModelHttpAdaptationService.ListPermissions`)_
    * `EntityAnalysisModelHttpAdaptationGet`: Returns one HTTP Adaptation by id, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelHttpAdaptationService.ReadPermissions`)_
    * `EntityAnalysisModelHttpAdaptationGetByGuid`: Returns one HTTP Adaptation by Guid, scoped to the caller's tenant.
      _(permissions from `EntityAnalysisModelHttpAdaptationService.ReadPermissions`)_
    * `EntityAnalysisModelHttpAdaptationGetByEntityAnalysisModelId`: Lists HTTP Adaptations belonging to a given Model,
      scoped to the caller's tenant. _(permissions from `EntityAnalysisModelHttpAdaptationService.ReadPermissions`)_
    * `EntityAnalysisModelHttpAdaptationValidate`: Validates a HTTP Adaptation without saving it and returns every
      failure. _(permissions from `EntityAnalysisModelHttpAdaptationService.WritePermissions`)_
    * `EntityAnalysisModelHttpAdaptationCreate`: Registers an HTTP Adaptation under a Model in the caller's tenant;
      calling twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelHttpAdaptationService.WritePermissions`)_
    * `EntityAnalysisModelHttpAdaptationUpdate`: Updates an HTTP Adaptation in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelHttpAdaptationService.WritePermissions`)_
    * `EntityAnalysisModelHttpAdaptationDelete`: Soft-deletes an HTTP Adaptation in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelHttpAdaptationService.WritePermissions`)_
    * `EntityAnalysisModelHttpAdaptationResubmit`: Resubmits a rejected HTTP Adaptation for approval again, unchanged,
      opening a fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelHttpAdaptationService.WritePermissions`)_
    * `EntityAnalysisModelHttpAdaptationRetract`: Retracts a rejected HTTP Adaptation, restoring it to its last approved
      version without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelHttpAdaptationService.WritePermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Model Integrity and Sampling (`model-integrity-and-sampling`)

- **Hierarchy Placement:** Level 4, child of Validation Supervisor
- **Semantic Persona:** Owns the model integrity and sampling slice end to end, which is small enough that reads and
  writes stay with one agent.
- **Semantic Routing Keywords:** entity, analysis, model, integrity, history, simulation, sample
- **Toolbox (Max 15):**
    * `EntityAnalysisModelIntegrityModels`: Lists the tenant's models that can be checked. _(permissions from
      `EntityAnalysisModelIntegrityService.RequiredPermissions`)_
    * `EntityAnalysisModelIntegrityCheck`: Checks a model's dependencies, compilation, configuration and engine state;
      returns coded findings. _(permissions from `EntityAnalysisModelIntegrityService.RequiredPermissions`)_
    * `EntityAnalysisModelIntegrityEngineState`: Returns what the engine instances have loaded and started for a model.
      _(permissions from `EntityAnalysisModelIntegrityService.RequiredPermissions`)_
    * `EntityAnalysisModelIntegrityGraph`: Returns a model's dependency graph as nodes and edges, optionally centred on
      one entity. _(permissions from `EntityAnalysisModelIntegrityService.RequiredPermissions`)_
    * `EntityAnalysisModelHistorySimulationAbstractionAggregation`: Works out an abstraction rule's value for a
      transaction from cached history, as the engine does. _(permissions from
      `EntityAnalysisModelHistorySimulationService.RequiredPermissions`)_
    * `EntityAnalysisModelHistorySimulationTtlCounterValue`: Reads a TTL counter's value for a transaction from the
      cache, as the engine does; increments nothing. _(permissions from
      `EntityAnalysisModelHistorySimulationService.RequiredPermissions`)_
    * `EntityAnalysisModelSampleExecute`: Samples archived transactions of an Entity Analysis Model over a date window
      and returns them as a CSV file. _(permissions from `EntityAnalysisModelSampleService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

---

## Model Supporting Structures (`model-support`)

#### Model Supporting Structures

- **Hierarchy Placement:** Level 2, child of Jooby
- **Semantic Persona:** Holds the lookup and operational scaffolding a detection model leans on: lists and dictionaries,
  overrides, reprocessing, the configuration navigation tree and the authoring aids. Separate from the model builder
  because these are shared structures with their own lifecycles.
- **Semantic Routing Keywords:** list, dictionary, lookup, override, suppression, reprocessing, replay, tree,
  completion, parser, rule vocabulary
- **Toolbox (Max 15):** none — routes to 6 supervisors covering 128 tools
- **Downstream Dependencies:** `model-support.authoring-support`, `model-support.data-structures.dictionaries`,
  `model-support.data-structures.lists`, `model-support.navigation`, `model-support.overrides`,
  `model-support.reprocessing`
- **Cross-Domain Reach:** `entity-analysis-model-synchronisation-schedule`

#### Authoring Support Supervisor

- **Hierarchy Placement:** Level 3, child of Model Supporting Structures
- **Semantic Persona:** Routes within the authoring support slice of model supporting structures. Holds no tools; it
  exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** authoring support, completions, entity, analysis, model, invocation, context,
  dependency, role, rule, vocabulary, icons, parser
- **Toolbox (Max 15):** none — routes to 3 leaf agents covering 21 tools
- **Downstream Dependencies:** `completions`, `model-invocation-context-and-dependencies`,
  `authoring-aids-and-model-role-grants`

#### Completions (`completions`)

- **Hierarchy Placement:** Level 4, child of Authoring Support Supervisor
- **Semantic Persona:** Owns the completions slice end to end, which is small enough that reads and writes stay with one
  agent.
- **Semantic Routing Keywords:** completions
- **Toolbox (Max 15):**
    * `CompletionsGetByCaseWorkflowId`: Gets the payload field completions for the model behind a case workflow, by
      workflow Id. _(permissions from `CompletionsService.ByCaseWorkflowIdPermissions`)_
    * `CompletionsGetByCaseWorkflowGuidIncludingDeleted`: Gets the payload field completions for the model behind a case
      workflow, by workflow Guid, including deleted workflows. _(permissions from
      `CompletionsService.IncludingDeletedPermissions`)_
    * `CompletionsGetByCaseWorkflowIdIncludingDeleted`: Gets the payload field completions for the model behind a case
      workflow, by workflow Id, including deleted workflows. _(permissions from
      `CompletionsService.IncludingDeletedPermissions`)_
    * `CompletionsGetByEntityAnalysisModelId`: Gets the payload field completions for an entity analysis model, for
      reporting. _(permissions from `CompletionsService.ByEntityAnalysisModelIdPermissions`)_
    * `CompletionsGetByEntityAnalysisModelIdParseTypeId`: Gets the payload field completions for an entity analysis
      model and parse type. _(permissions from `CompletionsService.ByParseTypePermissions`)_
    * `CompletionsGetByExhaustiveInstanceDataset`: Gets filter builder completions for every column of a confirmed
      uploaded Exhaustive dataset. _(permissions from `CompletionsService.ByExhaustiveInstanceDatasetPermissions`)_
    * `QueryBuilderOperatorsList`: Lists every operator the rule builder offers, with the rule text each produces, the
      values it takes and the field data types it applies to. _(permissions from
      `QueryBuilderService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Model Invocation Context and Dependencies (`model-invocation-context-and-dependencies`)

- **Hierarchy Placement:** Level 4, child of Authoring Support Supervisor
- **Semantic Persona:** Owns the model invocation context and dependencies slice end to end, which is small enough that
  reads and writes stay with one agent.
- **Semantic Routing Keywords:** entity, analysis, model, invocation, context, dependency
- **Toolbox (Max 15):**
    * `EntityAnalysisModelInvocationContextBlank`: Returns a blank rule-evaluation context for a model, with every name
      a rule can use and its defaults. _(permissions from
      `EntityAnalysisModelInvocationContextService.AuthoringPermissions`)_
    * `EntityAnalysisModelInvocationContextFromRequestJson`: Builds a rule-evaluation context from request JSON with the
      engine's own extraction; stores nothing. _(permissions from
      `EntityAnalysisModelInvocationContextService.AuthoringPermissions`)_
    * `EntityAnalysisModelInvocationContextFromArchive`: Builds a rule-evaluation context from an archived transaction,
      with every value as it was. _(permissions from `EntityAnalysisModelInvocationContextService.ArchivePermissions`)_
    * `EntityAnalysisModelInvocationContextOverlay`: Sets values in a rule-evaluation context by completion name,
      checking each against its data type. _(permissions from
      `EntityAnalysisModelInvocationContextService.AuthoringPermissions`)_
    * `EntityAnalysisModelDependencyDependents`: Lists every entity that uses an entity, directly or indirectly, and
      whether deleting it would be refused. _(permissions from
      `EntityAnalysisModelDependencyService.RequiredPermissions`)_
    * `EntityAnalysisModelDependencyDependencies`: Lists what an entity uses, resolved to the entities it names. _(
      permissions from `EntityAnalysisModelDependencyService.RequiredPermissions`)_
    * `EntityAnalysisModelDependencyIssues`: Finds dangling names, uses of inactive entities and unused entities in a
      model. _(permissions from `EntityAnalysisModelDependencyService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Authoring Aids and Model Role Grants (`authoring-aids-and-model-role-grants`)

- **Hierarchy Placement:** Level 4, child of Authoring Support Supervisor
- **Semantic Persona:** Owns the authoring aids and model role grants slice end to end, which is small enough that reads
  and writes stay with one agent. It holds 2 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** entity, analysis, model, role, rule, vocabulary, icons, parser
- **Toolbox (Max 15):**
    * `EntityAnalysisModelRoleListByEntityAnalysisModelGuid`: Lists the Role grants for an Entity Analysis Model. _(
      permissions from `EntityAnalysisModelRoleService.RequiredPermissions`)_
    * `EntityAnalysisModelRoleCreate`: Grants a Role access to an Entity Analysis Model; calling twice creates two
      grants. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelRoleService.RequiredPermissions`)_
    * `EntityAnalysisModelRoleDelete`: Removes a Role's grant on an Entity Analysis Model; reversible only by direct
      database action. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelRoleService.RequiredPermissions`)_
    * `RuleVocabularyList`: Lists the keywords, allowed words and rule functions rule text may use. _(permissions from
      `RuleVocabularyService.RequiredPermissions`)_
    * `RuleVocabularyDescribe`: Describes a rule word, with every function signature. _(permissions from
      `RuleVocabularyService.RequiredPermissions`)_
    * `IconsGet`: Lists the icon files available to the user interface. _(permissions from
      `IconsService.RequiredPermissions`)_
    * `ParserParse`: Parses and compiles a user-authored rule against an Entity Analysis Model and returns any errors.
      _(permissions from `ParserService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Data Structures Dictionaries Supervisor

- **Hierarchy Placement:** Level 3, child of Model Supporting Structures
- **Semantic Persona:** Routes within the data structures dictionaries slice of model supporting structures. Holds no
  tools; it exists so the domain above it chooses between a handful of coherent specialists rather than dozens of
  leaves.
- **Semantic Routing Keywords:** data structures dictionaries, entity, analysis, model, dictionary, kvp
- **Toolbox (Max 15):** none — routes to 6 leaf agents covering 21 tools
- **Downstream Dependencies:** `entity-analysis-model-dictionary-inspector`,
  `entity-analysis-model-dictionary-test-bench`, `entity-analysis-model-dictionary-author`,
  `entity-analysis-model-dictionary-kvp-inspector`, `entity-analysis-model-dictionary-kvp-test-bench`,
  `entity-analysis-model-dictionary-kvp-author`

#### Entity Analysis Model Dictionary Inspector (`entity-analysis-model-dictionary-inspector`)

- **Hierarchy Placement:** Level 4, child of Data Structures Dictionaries Supervisor
- **Semantic Persona:** Owns the entity analysis model dictionary slice and reads only; it never changes anything and is
  the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, dictionary, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelDictionaryList`: Lists Dictionaries for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryFilterFields`: Lists the fields a query builder JSON filter over Dictionarys may
      use. _(permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryFilter`: Returns the Dictionarys matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryCount`: Counts the Dictionarys matching query builder JSON, optionally grouped by a
      field. _(permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryGet`: Returns one Dictionary by id, scoped to the caller's tenant. _(permissions
      from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryGetByGuid`: Returns one Dictionary by Guid, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryGetByEntityAnalysisModelId`: Lists the Dictionaries belonging to a given Model,
      ordered by Id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Dictionary Test Bench (`entity-analysis-model-dictionary-test-bench`)

- **Hierarchy Placement:** Level 4, child of Data Structures Dictionaries Supervisor
- **Semantic Persona:** Owns the entity analysis model dictionary slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, dictionary, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelDictionaryValidate`: Validates a Dictionary without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Dictionary Author (`entity-analysis-model-dictionary-author`)

- **Hierarchy Placement:** Level 4, child of Data Structures Dictionaries Supervisor
- **Semantic Persona:** Owns the entity analysis model dictionary slice and is the only agent that mutates this slice;
  every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, dictionary, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelDictionaryList`: Lists Dictionaries for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryFilterFields`: Lists the fields a query builder JSON filter over Dictionarys may
      use. _(permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryFilter`: Returns the Dictionarys matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryCount`: Counts the Dictionarys matching query builder JSON, optionally grouped by a
      field. _(permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryGet`: Returns one Dictionary by id, scoped to the caller's tenant. _(permissions
      from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryGetByGuid`: Returns one Dictionary by Guid, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryGetByEntityAnalysisModelId`: Lists the Dictionaries belonging to a given Model,
      ordered by Id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryValidate`: Validates a Dictionary without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryCreate`: Registers a Dictionary under a Model in the caller's tenant; calling twice
      creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryUpdate`: Updates a Dictionary in the caller's tenant by id. **Write — confirmation
      required** _(permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryDelete`: Soft-deletes a Dictionary in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryResubmit`: Resubmits a rejected Dictionary for approval again, unchanged, opening a
      fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryRetract`: Retracts a rejected Dictionary, restoring it to its last approved version
      without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelDictionaryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Dictionary Kvp Inspector (`entity-analysis-model-dictionary-kvp-inspector`)

- **Hierarchy Placement:** Level 4, child of Data Structures Dictionaries Supervisor
- **Semantic Persona:** Owns the entity analysis model dictionary kvp slice and reads only; it never changes anything
  and is the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, dictionary, kvp, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelDictionaryKvpList`: Lists Key Value Pairs for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpFilterFields`: Lists the fields a query builder JSON filter over Dictionary KVPs
      may use. _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpFilter`: Returns the Dictionary KVPs matching query builder JSON, keyset-paged
      and capped. _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpCount`: Counts the Dictionary KVPs matching query builder JSON, optionally
      grouped by a field. _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpGet`: Returns one Key Value Pair, matched on the parent Dictionary's id, scoped
      to the caller's tenant. _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpGetByGuid`: Returns one Key Value Pair, matched on the parent Dictionary's id,
      scoped to the caller's tenant. _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpGetByEntityAnalysisModelDictionaryId`: Lists the Key Value Pairs belonging to a
      given Dictionary, ordered by Id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Dictionary Kvp Test Bench (`entity-analysis-model-dictionary-kvp-test-bench`)

- **Hierarchy Placement:** Level 4, child of Data Structures Dictionaries Supervisor
- **Semantic Persona:** Owns the entity analysis model dictionary kvp slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, dictionary, kvp, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelDictionaryKvpValidate`: Validates a Dictionary KVP without saving it and returns every
      failure. _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Dictionary Kvp Author (`entity-analysis-model-dictionary-kvp-author`)

- **Hierarchy Placement:** Level 4, child of Data Structures Dictionaries Supervisor
- **Semantic Persona:** Owns the entity analysis model dictionary kvp slice and is the only agent that mutates this
  slice; every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, dictionary, kvp, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelDictionaryKvpList`: Lists Key Value Pairs for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpFilterFields`: Lists the fields a query builder JSON filter over Dictionary KVPs
      may use. _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpFilter`: Returns the Dictionary KVPs matching query builder JSON, keyset-paged
      and capped. _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpCount`: Counts the Dictionary KVPs matching query builder JSON, optionally
      grouped by a field. _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpGet`: Returns one Key Value Pair, matched on the parent Dictionary's id, scoped
      to the caller's tenant. _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpGetByGuid`: Returns one Key Value Pair, matched on the parent Dictionary's id,
      scoped to the caller's tenant. _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpGetByEntityAnalysisModelDictionaryId`: Lists the Key Value Pairs belonging to a
      given Dictionary, ordered by Id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpValidate`: Validates a Dictionary KVP without saving it and returns every
      failure. _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpCreate`: Registers a Key Value Pair under a Dictionary in the caller's tenant;
      calling twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpUpdate`: Updates a Key Value Pair in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpDelete`: Soft-deletes a Key Value Pair in the caller's tenant by id. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpUploadCsv`: Imports CSV files of key,value[,deleteExpiryDate] lines into a
      Dictionary in the caller's tenant; existing keys are updated, lines that fail are skipped, and one upload record
      is written per file. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpResubmit`: Resubmits a rejected Key Value Pair for approval again, unchanged,
      opening a fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
    * `EntityAnalysisModelDictionaryKvpRetract`: Retracts a rejected Key Value Pair, restoring it to its last approved
      version without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelDictionaryKvpService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Data Structures Lists Supervisor

- **Hierarchy Placement:** Level 3, child of Model Supporting Structures
- **Semantic Persona:** Routes within the data structures lists slice of model supporting structures. Holds no tools; it
  exists so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** data structures lists, entity, analysis, model, list, value
- **Toolbox (Max 15):** none — routes to 6 leaf agents covering 21 tools
- **Downstream Dependencies:** `entity-analysis-model-list-inspector`, `entity-analysis-model-list-test-bench`,
  `entity-analysis-model-list-author`, `entity-analysis-model-list-value-inspector`,
  `entity-analysis-model-list-value-test-bench`, `entity-analysis-model-list-value-author`

#### Entity Analysis Model List Inspector (`entity-analysis-model-list-inspector`)

- **Hierarchy Placement:** Level 4, child of Data Structures Lists Supervisor
- **Semantic Persona:** Owns the entity analysis model list slice and reads only; it never changes anything and is the
  safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, list, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelListList`: Lists Lists for the caller's tenant, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListFilterFields`: Lists the fields a query builder JSON filter over Lists may use. _(
      permissions from `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListFilter`: Returns the Lists matching query builder JSON, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListCount`: Counts the Lists matching query builder JSON, optionally grouped by a field. _(
      permissions from `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListGet`: Returns one List by id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListGetByGuid`: Returns one List by Guid, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListGetByEntityAnalysisModelId`: Lists the Lists belonging to a given Model, ordered by Id,
      scoped to the caller's tenant. _(permissions from `EntityAnalysisModelListService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model List Test Bench (`entity-analysis-model-list-test-bench`)

- **Hierarchy Placement:** Level 4, child of Data Structures Lists Supervisor
- **Semantic Persona:** Owns the entity analysis model list slice and runs the side-effect-free rehearsals: validation,
  rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, list, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelListValidate`: Validates a List without saving it and returns every failure. _(permissions
      from `EntityAnalysisModelListService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model List Author (`entity-analysis-model-list-author`)

- **Hierarchy Placement:** Level 4, child of Data Structures Lists Supervisor
- **Semantic Persona:** Owns the entity analysis model list slice and is the only agent that mutates this slice; every
  call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, list, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelListList`: Lists Lists for the caller's tenant, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListFilterFields`: Lists the fields a query builder JSON filter over Lists may use. _(
      permissions from `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListFilter`: Returns the Lists matching query builder JSON, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListCount`: Counts the Lists matching query builder JSON, optionally grouped by a field. _(
      permissions from `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListGet`: Returns one List by id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListGetByGuid`: Returns one List by Guid, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListGetByEntityAnalysisModelId`: Lists the Lists belonging to a given Model, ordered by Id,
      scoped to the caller's tenant. _(permissions from `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListValidate`: Validates a List without saving it and returns every failure. _(permissions
      from `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListCreate`: Registers a List under a Model in the caller's tenant; calling twice creates two.
      **Write — confirmation required** _(permissions from `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListUpdate`: Updates a List in the caller's tenant by id. **Write — confirmation required** _(
      permissions from `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListDelete`: Soft-deletes a List in the caller's tenant by id. **Write — confirmation
      required** _(permissions from `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListResubmit`: Resubmits a rejected List for approval again, unchanged, opening a fresh
      approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelListService.RequiredPermissions`)_
    * `EntityAnalysisModelListRetract`: Retracts a rejected List, restoring it to its last approved version without a
      new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelListService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model List Value Inspector (`entity-analysis-model-list-value-inspector`)

- **Hierarchy Placement:** Level 4, child of Data Structures Lists Supervisor
- **Semantic Persona:** Owns the entity analysis model list value slice and reads only; it never changes anything and is
  the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, list, value, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelListValueList`: Lists List Values for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueFilterFields`: Lists the fields a query builder JSON filter over List Values may use.
      _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueFilter`: Returns the List Values matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueCount`: Counts the List Values matching query builder JSON, optionally grouped by a
      field. _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueGet`: Returns one List Value matched against its parent List's id, scoped to the
      caller's tenant. _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueGetByGuid`: Returns one List Value matched against its parent List's id, scoped to
      the caller's tenant. _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueGetByEntityAnalysisModelListId`: Lists the values belonging to a given List, ordered
      by Id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelListValueService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model List Value Test Bench (`entity-analysis-model-list-value-test-bench`)

- **Hierarchy Placement:** Level 4, child of Data Structures Lists Supervisor
- **Semantic Persona:** Owns the entity analysis model list value slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, list, value, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelListValueValidate`: Validates a List Value without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model List Value Author (`entity-analysis-model-list-value-author`)

- **Hierarchy Placement:** Level 4, child of Data Structures Lists Supervisor
- **Semantic Persona:** Owns the entity analysis model list value slice and is the only agent that mutates this slice;
  every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, list, value, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelListValueList`: Lists List Values for the caller's tenant, keyset-paged and capped. _(
      permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueFilterFields`: Lists the fields a query builder JSON filter over List Values may use.
      _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueFilter`: Returns the List Values matching query builder JSON, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueCount`: Counts the List Values matching query builder JSON, optionally grouped by a
      field. _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueGet`: Returns one List Value matched against its parent List's id, scoped to the
      caller's tenant. _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueGetByGuid`: Returns one List Value matched against its parent List's id, scoped to
      the caller's tenant. _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueGetByEntityAnalysisModelListId`: Lists the values belonging to a given List, ordered
      by Id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueValidate`: Validates a List Value without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueCreate`: Registers a value under a List in the caller's tenant; calling twice creates
      two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueUpdate`: Updates a value in the caller's tenant by id. **Write — confirmation
      required** _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueDelete`: Soft-deletes a value in the caller's tenant by id. **Write — confirmation
      required** _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueUploadCsv`: Loads one or more CSV files of values into a List in the caller's tenant
      in a single transaction, recording an upload entry per file; calling twice loads the values twice. **Write —
      confirmation required** _(permissions from `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueResubmit`: Resubmits a rejected List Value for approval again, unchanged, opening a
      fresh approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelListValueService.RequiredPermissions`)_
    * `EntityAnalysisModelListValueRetract`: Retracts a rejected List Value, restoring it to its last approved version
      without a new approval cycle. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelListValueService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Navigation Supervisor

- **Hierarchy Placement:** Level 3, child of Model Supporting Structures
- **Semantic Persona:** Routes within the navigation slice of model supporting structures. Holds no tools; it exists so
  the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** navigation, tree, children
- **Toolbox (Max 15):** none — routes to 4 leaf agents covering 27 tools
- **Downstream Dependencies:** `tree-children-inspector-1`, `tree-children-inspector-2`, `tree-children-inspector-3`,
  `tree-children-inspector-4`

#### Tree Children Inspector 1 (`tree-children-inspector-1`)

- **Hierarchy Placement:** Level 4, child of Navigation Supervisor
- **Semantic Persona:** Owns the tree children slice end to end, which is small enough that reads and writes stay with
  one agent.
- **Semantic Routing Keywords:** tree, children, inspector, 1
- **Toolbox (Max 15):**
    * `TreeChildrenRequestXPathGet`: Lists the request XPath nodes of an Entity Analysis Model as tree children, ordered
      by Id. _(permissions from `TreeChildrenNodes.RequestXPath`)_
    * `TreeChildrenVisualisationRegistryDatasourceGet`: Lists the datasource nodes of a Visualisation Registry as tree
      children, ordered by priority. _(permissions from `TreeChildrenNodes.VisualisationRegistryDatasource`)_
    * `TreeChildrenUserRegistryGet`: Lists the users of a Role Registry as tree children. _(permissions from
      `TreeChildrenNodes.UserRegistry`)_
    * `TreeChildrenRoleRegistryPermissionGet`: Lists the permission nodes granted to a Role Registry as tree children;
      the name is the permission specification name. _(permissions from `TreeChildrenNodes.RoleRegistryPermission`)_
    * `TreeChildrenVisualisationRegistryParameterGet`: Lists the parameter nodes of a Visualisation Registry as tree
      children, ordered by Id. _(permissions from `TreeChildrenNodes.VisualisationRegistryParameter`)_
    * `TreeChildrenInlineFunctionGet`: Lists the inline function nodes of an Entity Analysis Model as tree children,
      ordered by Id. _(permissions from `TreeChildrenNodes.InlineFunction`)_
    * `TreeChildrenTagGet`: Lists the tag nodes of an Entity Analysis Model as tree children, ordered by Id. _(
      permissions from `TreeChildrenNodes.Tag`)_
- **Downstream Dependencies:** none (leaf)

#### Tree Children Inspector 2 (`tree-children-inspector-2`)

- **Hierarchy Placement:** Level 4, child of Navigation Supervisor
- **Semantic Persona:** Owns the tree children slice end to end, which is small enough that reads and writes stay with
  one agent.
- **Semantic Routing Keywords:** tree, children, inspector, 2
- **Toolbox (Max 15):**
    * `TreeChildrenGatewayRuleGet`: Lists the gateway rule nodes of an Entity Analysis Model as tree children, ordered
      by priority. _(permissions from `TreeChildrenNodes.GatewayRule`)_
    * `TreeChildrenExhaustiveGet`: Lists the exhaustive search instance nodes of an Entity Analysis Model as tree
      children, ordered by Id. _(permissions from `TreeChildrenNodes.Exhaustive`)_
    * `TreeChildrenReprocessingGet`: Lists the reprocessing rule nodes of an Entity Analysis Model as tree children. _(
      permissions from `TreeChildrenNodes.Reprocessing`)_
    * `TreeChildrenAdaptationGet`: Lists the HTTP adaptation nodes of an Entity Analysis Model as tree children, ordered
      by Id. _(permissions from `TreeChildrenNodes.Adaptation`)_
    * `TreeChildrenCaseWorkflowGet`: Lists the case workflow nodes of an Entity Analysis Model as tree children, ordered
      by Id. _(permissions from `TreeChildrenNodes.CaseWorkflow`)_
    * `TreeChildrenAbstractionCalculationGet`: Lists the abstraction calculation nodes of an Entity Analysis Model as
      tree children. _(permissions from `TreeChildrenNodes.AbstractionCalculation`)_
    * `TreeChildrenAbstractionRuleGet`: Lists the abstraction rule nodes of an Entity Analysis Model as tree children.
      _(permissions from `TreeChildrenNodes.AbstractionRule`)_
- **Downstream Dependencies:** none (leaf)

#### Tree Children Inspector 3 (`tree-children-inspector-3`)

- **Hierarchy Placement:** Level 4, child of Navigation Supervisor
- **Semantic Persona:** Owns the tree children slice end to end, which is small enough that reads and writes stay with
  one agent.
- **Semantic Routing Keywords:** tree, children, inspector, 3
- **Toolbox (Max 15):**
    * `TreeChildrenActivationRuleGet`: Lists the activation rule nodes of an Entity Analysis Model as tree children,
      ordered by priority. _(permissions from `TreeChildrenNodes.ActivationRule`)_
    * `TreeChildrenTTLCounterGet`: Lists the TTL counter nodes of an Entity Analysis Model as tree children, ordered by
      Id. _(permissions from `TreeChildrenNodes.TtlCounter`)_
    * `TreeChildrenInlineScriptGet`: Lists the inline script nodes of an Entity Analysis Model as tree children, ordered
      by Id. _(permissions from `TreeChildrenNodes.InlineScript`)_
    * `TreeChildrenSanctionsGet`: Lists the sanction nodes of an Entity Analysis Model as tree children, ordered by Id.
      _(permissions from `TreeChildrenNodes.Sanctions`)_
    * `TreeChildrenListGet`: Lists the list nodes of an Entity Analysis Model, addressed by model Guid, as tree
      children. _(permissions from `TreeChildrenNodes.List`)_
    * `TreeChildrenDictionaryGet`: Lists the dictionary nodes of an Entity Analysis Model, addressed by model Guid, as
      tree children. _(permissions from `TreeChildrenNodes.Dictionary`)_
    * `TreeChildrenCaseWorkflowXPathGet`: Lists the xpath nodes of a Case Workflow as tree children. _(permissions from
      `TreeChildrenNodes.CaseWorkflowXPath`)_
- **Downstream Dependencies:** none (leaf)

#### Tree Children Inspector 4 (`tree-children-inspector-4`)

- **Hierarchy Placement:** Level 4, child of Navigation Supervisor
- **Semantic Persona:** Owns the tree children slice end to end, which is small enough that reads and writes stay with
  one agent.
- **Semantic Routing Keywords:** tree, children, inspector, 4
- **Toolbox (Max 15):**
    * `TreeChildrenCaseWorkflowFormGet`: Lists the form nodes of a Case Workflow as tree children. _(permissions from
      `TreeChildrenNodes.CaseWorkflowForm`)_
    * `TreeChildrenCaseWorkflowActionGet`: Lists the action nodes of a Case Workflow as tree children. _(permissions
      from `TreeChildrenNodes.CaseWorkflowAction`)_
    * `TreeChildrenCaseWorkflowMacroGet`: Lists the macro nodes of a Case Workflow as tree children. _(permissions from
      `TreeChildrenNodes.CaseWorkflowMacro`)_
    * `TreeChildrenCaseWorkflowFilterGet`: Lists the filter nodes of a Case Workflow as tree children. _(permissions
      from `TreeChildrenNodes.CaseWorkflowFilter`)_
    * `TreeChildrenCaseWorkflowDisplayGet`: Lists the display nodes of a Case Workflow as tree children. _(permissions
      from `TreeChildrenNodes.CaseWorkflowDisplay`)_
    * `TreeChildrenCaseWorkflowStatusGet`: Lists the status nodes of a Case Workflow as tree children. _(permissions
      from `TreeChildrenNodes.CaseWorkflowStatus`)_
- **Downstream Dependencies:** none (leaf)

#### Overrides Supervisor

- **Hierarchy Placement:** Level 3, child of Model Supporting Structures
- **Semantic Persona:** Routes within the overrides slice of model supporting structures. Holds no tools; it exists so
  the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** overrides, entity, analysis, model, override, query
- **Toolbox (Max 15):** none — routes to 4 leaf agents covering 14 tools
- **Downstream Dependencies:** `entity-analysis-model-override-inspector`, `entity-analysis-model-override-test-bench`,
  `entity-analysis-model-override-author`, `entity-analysis-model-override-query`

#### Entity Analysis Model Override Inspector (`entity-analysis-model-override-inspector`)

- **Hierarchy Placement:** Level 4, child of Overrides Supervisor
- **Semantic Persona:** Owns the entity analysis model override slice and reads only; it never changes anything and is
  the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, override, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelOverrideList`: Lists Overrides for the caller's tenant, keyset-paged and capped. _(permissions
      from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideFilterFields`: Lists the fields a query builder JSON filter over Overrides may use. _(
      permissions from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideFilter`: Returns the Overrides matching query builder JSON, keyset-paged and capped.
      _(permissions from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideCount`: Counts the Overrides matching query builder JSON, optionally grouped by a
      field. _(permissions from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideGet`: Returns one Override by id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideGetByEntityAnalysisModelId`: Lists Overrides belonging to a given Model, scoped to the
      caller's tenant. _(permissions from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Override Test Bench (`entity-analysis-model-override-test-bench`)

- **Hierarchy Placement:** Level 4, child of Overrides Supervisor
- **Semantic Persona:** Owns the entity analysis model override slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, override, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelOverrideValidate`: Validates a Override without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Override Author (`entity-analysis-model-override-author`)

- **Hierarchy Placement:** Level 4, child of Overrides Supervisor
- **Semantic Persona:** Owns the entity analysis model override slice and is the only agent that mutates this slice;
  every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, override, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelOverrideList`: Lists Overrides for the caller's tenant, keyset-paged and capped. _(permissions
      from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideFilterFields`: Lists the fields a query builder JSON filter over Overrides may use. _(
      permissions from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideFilter`: Returns the Overrides matching query builder JSON, keyset-paged and capped.
      _(permissions from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideCount`: Counts the Overrides matching query builder JSON, optionally grouped by a
      field. _(permissions from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideGet`: Returns one Override by id, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideGetByEntityAnalysisModelId`: Lists Overrides belonging to a given Model, scoped to the
      caller's tenant. _(permissions from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideValidate`: Validates a Override without saving it and returns every failure. _(
      permissions from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideCreate`: Registers a Override against a Model in the caller's tenant; calling twice
      creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideUpdate`: Toggles a Override on or off by row existence; NOT idempotent -- repeat calls
      flip state. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideUpdateDeleteExpiryDate`: Updates the Delete Expiry Date of an existing active Override
      in the caller's tenant. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelOverrideService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideDelete`: Soft-deletes a Override in the caller's tenant by id. **Write — confirmation
      required** _(permissions from `EntityAnalysisModelOverrideService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Override Query (`entity-analysis-model-override-query`)

- **Hierarchy Placement:** Level 4, child of Overrides Supervisor
- **Semantic Persona:** Owns the entity analysis model override query slice end to end, which is small enough that reads
  and writes stay with one agent.
- **Semantic Routing Keywords:** entity, analysis, model, override, query
- **Toolbox (Max 15):**
    * `EntityAnalysisModelOverrideQueryGet`: Lists tenant models supporting override on a key, flagging active overrides
      for a key value. _(permissions from `EntityAnalysisModelOverrideQueryService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideQueryGetKeys`: Lists the override keys available for a Model in the caller's tenant.
      _(permissions from `EntityAnalysisModelOverrideQueryService.RequiredPermissions`)_
    * `EntityAnalysisModelOverrideQueryGetValues`: Lists the values held against an override key for a Model in the
      caller's tenant. _(permissions from `EntityAnalysisModelOverrideQueryService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Reprocessing Supervisor

- **Hierarchy Placement:** Level 3, child of Model Supporting Structures
- **Semantic Persona:** Routes within the reprocessing slice of model supporting structures. Holds no tools; it exists
  so the domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** reprocessing, entity, analysis, model, rule, instance
- **Toolbox (Max 15):** none — routes to 6 leaf agents covering 24 tools
- **Downstream Dependencies:** `entity-analysis-model-reprocessing-rule-inspector`,
  `entity-analysis-model-reprocessing-rule-test-bench`, `entity-analysis-model-reprocessing-rule-author`,
  `entity-analysis-model-reprocessing-rule-instance-inspector`,
  `entity-analysis-model-reprocessing-rule-instance-test-bench`,
  `entity-analysis-model-reprocessing-rule-instance-author`
- **Cross-Domain Reach:** `completions`, `model-invocation-context-and-dependencies`

#### Entity Analysis Model Reprocessing Rule Inspector (`entity-analysis-model-reprocessing-rule-inspector`)

- **Hierarchy Placement:** Level 4, child of Reprocessing Supervisor
- **Semantic Persona:** Owns the entity analysis model reprocessing rule slice and reads only; it never changes anything
  and is the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, reprocessing, rule, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelReprocessingRuleList`: Lists Reprocessing Rules for the caller's tenant, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleFilterFields`: Lists the fields a query builder JSON filter over Reprocessing
      Rules may use. _(permissions from `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleFilter`: Returns the Reprocessing Rules matching query builder JSON,
      keyset-paged and capped. _(permissions from `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleCount`: Counts the Reprocessing Rules matching query builder JSON, optionally
      grouped by a field. _(permissions from `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleGet`: Returns one Reprocessing Rule by id, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleGetByEntityAnalysisModelId`: Lists Reprocessing Rules belonging to a given
      Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Reprocessing Rule Test Bench (`entity-analysis-model-reprocessing-rule-test-bench`)

- **Hierarchy Placement:** Level 4, child of Reprocessing Supervisor
- **Semantic Persona:** Owns the entity analysis model reprocessing rule slice and runs the side-effect-free rehearsals:
  validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, reprocessing, rule, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelReprocessingRuleValidate`: Validates a Reprocessing Rule without saving it and returns every
      failure. _(permissions from `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleParseRule`: Parses and compiles a Reprocessing Rule's rule text as the engine
      would and returns each located error. _(permissions from
      `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleExecute`: Runs a Reprocessing Rule against an invocation context as the engine
      would and returns the result. _(permissions from
      `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleBuildRuleFromBuilderJson`: Turns query builder JSON into a Reprocessing Rule's
      rule text as the browser builder would, and parses it. _(permissions from
      `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Reprocessing Rule Author (`entity-analysis-model-reprocessing-rule-author`)

- **Hierarchy Placement:** Level 4, child of Reprocessing Supervisor
- **Semantic Persona:** Owns the entity analysis model reprocessing rule slice and is the only agent that mutates this
  slice; every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, reprocessing, rule, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelReprocessingRuleList`: Lists Reprocessing Rules for the caller's tenant, keyset-paged and
      capped. _(permissions from `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleFilterFields`: Lists the fields a query builder JSON filter over Reprocessing
      Rules may use. _(permissions from `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleFilter`: Returns the Reprocessing Rules matching query builder JSON,
      keyset-paged and capped. _(permissions from `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleCount`: Counts the Reprocessing Rules matching query builder JSON, optionally
      grouped by a field. _(permissions from `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleGet`: Returns one Reprocessing Rule by id, scoped to the caller's tenant. _(
      permissions from `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleGetByEntityAnalysisModelId`: Lists Reprocessing Rules belonging to a given
      Model, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleValidate`: Validates a Reprocessing Rule without saving it and returns every
      failure. _(permissions from `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleParseRule`: Parses and compiles a Reprocessing Rule's rule text as the engine
      would and returns each located error. _(permissions from
      `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleExecute`: Runs a Reprocessing Rule against an invocation context as the engine
      would and returns the result. _(permissions from
      `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleBuildRuleFromBuilderJson`: Turns query builder JSON into a Reprocessing Rule's
      rule text as the browser builder would, and parses it. _(permissions from
      `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleCreate`: Registers a Reprocessing Rule under a Model in the caller's tenant;
      calling twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleUpdate`: Supersedes a Reprocessing Rule in the caller's tenant by id,
      assigning it a new id. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleDelete`: Soft-deletes a Reprocessing Rule in the caller's tenant by id.
      **Write — confirmation required** _(permissions from
      `EntityAnalysisModelReprocessingRuleService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Reprocessing Rule Instance Inspector (`entity-analysis-model-reprocessing-rule-instance-inspector`)

- **Hierarchy Placement:** Level 4, child of Reprocessing Supervisor
- **Semantic Persona:** Owns the entity analysis model reprocessing rule instance slice and reads only; it never changes
  anything and is the safe default for any question about current state.
- **Semantic Routing Keywords:** entity, analysis, model, reprocessing, rule, instance, inspector
- **Toolbox (Max 15):**
    * `EntityAnalysisModelReprocessingRuleInstanceList`: Lists Reprocessing Rule instances for the caller's tenant,
      keyset-paged and capped. _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceFilterFields`: Lists the fields a query builder JSON filter over
      Reprocessing Rule Instances may use. _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceFilter`: Returns the Reprocessing Rule Instances matching query
      builder JSON, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceCount`: Counts the Reprocessing Rule Instances matching query builder
      JSON, optionally grouped by a field. _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceGet`: Returns one Reprocessing Rule instance by id, scoped to the
      caller's tenant. _(permissions from `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceGetByEntityAnalysisModelReprocessingId`: Lists Reprocessing Rule
      instances belonging to a given Reprocessing Rule, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Reprocessing Rule Instance Test Bench (`entity-analysis-model-reprocessing-rule-instance-test-bench`)

- **Hierarchy Placement:** Level 4, child of Reprocessing Supervisor
- **Semantic Persona:** Owns the entity analysis model reprocessing rule instance slice and runs the side-effect-free
  rehearsals: validation, rule parsing and trial execution, so a change can be proven before it is made.
- **Semantic Routing Keywords:** entity, analysis, model, reprocessing, rule, instance, test, bench
- **Toolbox (Max 15):**
    * `EntityAnalysisModelReprocessingRuleInstanceValidate`: Validates a Reprocessing Rule Instance without saving it
      and returns every failure. _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Entity Analysis Model Reprocessing Rule Instance Author (`entity-analysis-model-reprocessing-rule-instance-author`)

- **Hierarchy Placement:** Level 4, child of Reprocessing Supervisor
- **Semantic Persona:** Owns the entity analysis model reprocessing rule instance slice and is the only agent that
  mutates this slice; every call needs explicit confirmation and, where the slice is preservation-managed, a pre-change
  snapshot.
- **Semantic Routing Keywords:** entity, analysis, model, reprocessing, rule, instance, author
- **Toolbox (Max 15):**
    * `EntityAnalysisModelReprocessingRuleInstanceList`: Lists Reprocessing Rule instances for the caller's tenant,
      keyset-paged and capped. _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceFilterFields`: Lists the fields a query builder JSON filter over
      Reprocessing Rule Instances may use. _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceFilter`: Returns the Reprocessing Rule Instances matching query
      builder JSON, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceCount`: Counts the Reprocessing Rule Instances matching query builder
      JSON, optionally grouped by a field. _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceGet`: Returns one Reprocessing Rule instance by id, scoped to the
      caller's tenant. _(permissions from `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceGetByEntityAnalysisModelReprocessingId`: Lists Reprocessing Rule
      instances belonging to a given Reprocessing Rule, scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceValidate`: Validates a Reprocessing Rule Instance without saving it
      and returns every failure. _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceCreate`: Registers a Reprocessing Rule instance under a Reprocessing
      Rule in the caller's tenant; calling twice creates two. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceInsertByExistingUpdateUncompleted`: Triggers a new reprocessing run
      for a Reprocessing Rule; fails if one is already uncompleted. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceUpdate`: Supersedes a Reprocessing Rule instance in the caller's
      tenant by id, assigning it a new id. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
    * `EntityAnalysisModelReprocessingRuleInstanceDelete`: Soft-deletes a Reprocessing Rule instance in the caller's
      tenant by id. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

---

## Platform Reliability (`platform-ops`)

#### Platform Reliability

- **Hierarchy Placement:** Level 2, child of Jooby
- **Semantic Persona:** Reads the operational health of the platform: Postgres, Redis, etcd and Patroni, Docker and the
  overlay network, OpenTelemetry, logs and engine throughput counters. Almost entirely landlord-only and almost entirely
  read-only.
- **Semantic Routing Keywords:** postgres, redis, etcd, patroni, docker, haproxy, overlay network, telemetry, OTLP, log,
  queue balance, latency, throughput, replication lag, WAF
- **Toolbox (Max 15):** none — routes to 2 supervisors and 5 leaf agents covering 59 tools
- **Downstream Dependencies:** `platform-ops.engine-health`, `platform-ops.telemetry`, `application-and-container-logs`,
  `containers-load-balancer-and-overlay-network`, `etcd-and-patroni-cluster-state`, `postgres-observability`,
  `redis-observability`

#### Engine Health Supervisor

- **Hierarchy Placement:** Level 3, child of Platform Reliability
- **Semantic Persona:** Routes within the engine health slice of platform reliability. Holds no tools; it exists so the
  domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** engine health, entity, analysis, model, synchronisation, schedule, capture, queue,
  health, asynchronous, balance, processing, counter, response, time, pipeline, stage, performance, node, status,
  entries, task, http, invoke, warning
- **Toolbox (Max 15):** none — routes to 3 leaf agents covering 17 tools
- **Downstream Dependencies:** `entity-analysis-model-synchronisation-schedule`, `engine-queue-and-throughput-counters`,
  `invoke-path-performance-and-warnings`

#### Entity Analysis Model Synchronisation Schedule (`entity-analysis-model-synchronisation-schedule`)

- **Hierarchy Placement:** Level 4, child of Engine Health Supervisor
- **Semantic Persona:** Owns the entity analysis model synchronisation schedule slice end to end, which is small enough
  that reads and writes stay with one agent. It holds 1 mutating tool, so a confirmation is required before any of them
  runs.
- **Semantic Routing Keywords:** entity, analysis, model, synchronisation, schedule
- **Toolbox (Max 15):**
    * `EntityAnalysisModelSynchronisationScheduleGetCurrent`: Returns the most recently scheduled model-synchronisation
      row for the caller's tenant. _(permissions from
      `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`)_
    * `EntityAnalysisModelSynchronisationScheduleList`: Lists scheduled model-synchronisation rows for the caller's
      tenant, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`)_
    * `EntityAnalysisModelSynchronisationScheduleFilterFields`: Lists the fields a query builder JSON filter over
      Synchronisation Schedules may use. _(permissions from
      `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`)_
    * `EntityAnalysisModelSynchronisationScheduleFilter`: Returns the Synchronisation Schedules matching query builder
      JSON, keyset-paged and capped. _(permissions from
      `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`)_
    * `EntityAnalysisModelSynchronisationScheduleCount`: Counts the Synchronisation Schedules matching query builder
      JSON, optionally grouped by a field. _(permissions from
      `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`)_
    * `EntityAnalysisModelSynchronisationScheduleCreate`: Schedules model synchronisation across the cluster; calling
      twice creates two rows. **Write — confirmation required** _(permissions from
      `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`)_
    * `EntityAnalysisModelSynchronisationScheduleValidate`: Validates a Synchronisation Schedule without saving it and
      returns every failure. _(permissions from
      `EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Engine Queue and Throughput Counters (`engine-queue-and-throughput-counters`)

- **Hierarchy Placement:** Level 4, child of Engine Health Supervisor
- **Semantic Persona:** Owns the engine queue and throughput counters slice end to end, which is small enough that reads
  and writes stay with one agent.
- **Semantic Routing Keywords:** capture, queue, health, entity, analysis, asynchronous, balance, model, processing,
  counter, response, time, pipeline, stage, performance, synchronisation, node, status, entries
- **Toolbox (Max 15):**
    * `CaptureQueueHealthList`: Lists per-cycle depth and dropped-count snapshots for every bounded in-memory capture
      queue in the observability suite (ModelInvokeWarning, CaseCreationWarning, ArchiverWarning, RedisSentinelEvent,
      RedisConnectionEvent, OpenTelemetryMetric)… _(permissions from `CaptureQueueHealthService.RequiredPermissions`)_
    * `EntityAnalysisAsynchronousQueueBalanceList`: Lists platform-wide asynchronous processing queue balance rows, most
      recent first, capped at 100000, with optional date-range filters. _(permissions from
      `EntityAnalysisAsynchronousQueueBalanceService.RequiredPermissions`)_
    * `EntityAnalysisModelAsynchronousQueueBalanceList`: Lists per-Model asynchronous processing queue balance rows,
      most recent first, capped at 100000, with optional date-range and Model-name substring filters. _(permissions from
      `EntityAnalysisModelAsynchronousQueueBalanceService.RequiredPermissions`)_
    * `EntityAnalysisModelProcessingCounterList`: Lists per-Model processing counter rows, most recent first, capped at
      100000, with optional date-range and Model-name substring filters. _(permissions from
      `EntityAnalysisModelProcessingCounterService.RequiredPermissions`)_
    * `EntityAnalysisModelResponseTimePipelineCounterList`: Lists per-minute Response Time Pipeline Counter rows for a
      Model, most recent first, capped at 100000, with optional date-range and substring search (StageName) filters,
      scoped to the caller's tenant. _(permissions from
      `EntityAnalysisModelResponseTimePipelineCounterService.RequiredPermissions`)_
    * `EntityAnalysisModelStagePerformanceCounterList`: Lists per-minute Stage Performance Counter rows for a Model,
      most recent first, capped at 100000, with optional date-range and substring search (StageName) filters, scoped to
      the caller's tenant. _(permissions from `EntityAnalysisModelStagePerformanceCounterService.RequiredPermissions`)_
    * `EntityAnalysisModelSynchronisationNodeStatusEntriesGet`: Lists the tenant's model-synchronisation node statuses
      seen in the last hour, with pending and availability flags. _(permissions from
      `EntityAnalysisModelSynchronisationNodeStatusEntriesService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Invoke Path Performance and Warnings (`invoke-path-performance-and-warnings`)

- **Hierarchy Placement:** Level 4, child of Engine Health Supervisor
- **Semantic Persona:** Owns the invoke path performance and warnings slice end to end, which is small enough that reads
  and writes stay with one agent.
- **Semantic Routing Keywords:** entity, analysis, model, task, performance, counter, http, processing, invoke, warning
- **Toolbox (Max 15):**
    * `EntityAnalysisModelTaskPerformanceCounterList`: Lists per-minute Task Performance Counter rows for a Model, most
      recent first, capped at 100000, with optional date-range and substring search (TaskName/Direction) filters, scoped
      to the caller's tenant -- useful for cloud compute-cost attr… _(permissions from
      `EntityAnalysisModelTaskPerformanceCounterService.RequiredPermissions`)_
    * `HttpProcessingCounterList`: Lists platform-wide HTTP processing counters, most recent first, capped at 100000,
      with optional date-range filters. _(permissions from `HttpProcessingCounterService.RequiredPermissions`)_
    * `ModelInvokeWarningList`: Lists captured invocation warn-threshold breaches -- the trace point message and timing
      gap that exceeded a model's Warn Threshold Milliseconds, most recent first, capped at 100000, with optional
      model/date-range/substring filters. _(permissions from `ModelInvokeWarningService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Telemetry Supervisor

- **Hierarchy Placement:** Level 3, child of Platform Reliability
- **Semantic Persona:** Routes within the telemetry slice of platform reliability. Holds no tools; it exists so the
  domain above it chooses between a handful of coherent specialists rather than dozens of leaves.
- **Semantic Routing Keywords:** telemetry, open, exclude, log, counter, dot, net, runtime, metric, otlp, dispatch
- **Toolbox (Max 15):** none — routes to 3 leaf agents covering 15 tools
- **Downstream Dependencies:** `open-telemetry-exclude`, `telemetry-log-counters-and-runtime-metrics`,
  `telemetry-metrics-and-otlp-dispatch`

#### Open Telemetry Exclude (`open-telemetry-exclude`)

- **Hierarchy Placement:** Level 4, child of Telemetry Supervisor
- **Semantic Persona:** Owns the open telemetry exclude slice end to end, which is small enough that reads and writes
  stay with one agent. It holds 3 mutating tools, so a confirmation is required before any of them runs.
- **Semantic Routing Keywords:** open, telemetry, exclude
- **Toolbox (Max 15):**
    * `OpenTelemetryExcludeList`: Lists every OpenTelemetryExclude row, most recent first. Not scoped to any tenant. _(
      permissions from `OpenTelemetryExcludeService.RequiredPermissions`)_
    * `OpenTelemetryExcludeGet`: Returns one OpenTelemetryExclude row by id. _(permissions from
      `OpenTelemetryExcludeService.RequiredPermissions`)_
    * `OpenTelemetryExcludeCreate`: Registers an OpenTelemetry instrument/counter name to stop exporting; calling twice
      creates two rows. **Write — confirmation required** _(permissions from
      `OpenTelemetryExcludeService.RequiredPermissions`)_
    * `OpenTelemetryExcludeValidate`: Validates an Open Telemetry Exclude without saving it and returns every failure.
      _(permissions from `OpenTelemetryExcludeService.RequiredPermissions`)_
    * `OpenTelemetryExcludeUpdate`: Updates an existing exclusion row in place. **Write — confirmation required** _(
      permissions from `OpenTelemetryExcludeService.RequiredPermissions`)_
    * `OpenTelemetryExcludeDelete`: Soft-deletes an exclusion row by id; the named instrument becomes eligible for
      export again. **Write — confirmation required** _(permissions from
      `OpenTelemetryExcludeService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Telemetry Log Counters and Runtime Metrics (`telemetry-log-counters-and-runtime-metrics`)

- **Hierarchy Placement:** Level 4, child of Telemetry Supervisor
- **Semantic Persona:** Owns the telemetry log counters and runtime metrics slice end to end, which is small enough that
  reads and writes stay with one agent. It holds 3 mutating tools, so a confirmation is required before any of them
  runs.
- **Semantic Routing Keywords:** open, telemetry, log, counter, dot, net, runtime, metric
- **Toolbox (Max 15):**
    * `OpenTelemetryLogCounterList`: Lists every OpenTelemetryLogCounter rule (Name + Regex), most recent first. Not
      scoped to any tenant. _(permissions from `OpenTelemetryLogCounterService.RequiredPermissions`)_
    * `OpenTelemetryLogCounterGet`: Returns one OpenTelemetryLogCounter rule by id. _(permissions from
      `OpenTelemetryLogCounterService.RequiredPermissions`)_
    * `OpenTelemetryLogCounterCreate`: Registers a rule that increments an OpenTelemetry counter named Name whenever
      Regex matches an incoming unstructured log line; calling twice creates two rows. **Write — confirmation required**
      _(permissions from `OpenTelemetryLogCounterService.RequiredPermissions`)_
    * `OpenTelemetryLogCounterValidate`: Validates an Open Telemetry Log Counter without saving it and returns every
      failure. _(permissions from `OpenTelemetryLogCounterService.RequiredPermissions`)_
    * `OpenTelemetryLogCounterUpdate`: Updates an existing rule in place. **Write — confirmation required** _(
      permissions from `OpenTelemetryLogCounterService.RequiredPermissions`)_
    * `OpenTelemetryLogCounterDelete`: Soft-deletes a rule by id; it immediately stops being checked against incoming
      log lines. **Write — confirmation required** _(permissions from
      `OpenTelemetryLogCounterService.RequiredPermissions`)_
    * `DotNetRuntimeMetricList`: Lists per-minute .NET runtime metric snapshots across every node, most recent first,
      capped at 100000, with optional date-range and substring search (Instance) filters, not scoped to any tenant. _(
      permissions from `DotNetRuntimeMetricService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Telemetry Metrics and OTLP Dispatch (`telemetry-metrics-and-otlp-dispatch`)

- **Hierarchy Placement:** Level 4, child of Telemetry Supervisor
- **Semantic Persona:** Owns the telemetry metrics and otlp dispatch slice end to end, which is small enough that reads
  and writes stay with one agent.
- **Semantic Routing Keywords:** open, telemetry, metric, otlp, dispatch, counter
- **Toolbox (Max 15):**
    * `OpenTelemetryMetricList`: Lists locally-aggregated OpenTelemetry metrics -- everything already recorded onto
      ServiceDiagnostics/EngineDiagnostics' Meters (jube.service.operation. *, jube.engine.stage.duration,
      jube.engine.sampler.*, ...), summarised once a minute int… _(permissions from
      `OpenTelemetryMetricService.RequiredPermissions`)_
    * `OtlpDispatchCounterList`: Lists per-minute OTLP export dispatch counters -- one row per distinct Signal (
      "traces"/"metrics"/"logs") per minute, with Count/SuccessCount/FailureCount/ItemCount/DroppedCount and
      Total/Min/MaxMicroseconds for that window, most recent fir… _(permissions from
      `OtlpDispatchCounterService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Application and Container Logs (`application-and-container-logs`)

- **Hierarchy Placement:** Level 3, child of Platform Reliability
- **Semantic Persona:** Owns the application and container logs slice end to end, which is small enough that reads and
  writes stay with one agent.
- **Semantic Routing Keywords:** application, log, entry, container, waf, attack
- **Toolbox (Max 15):**
    * `ApplicationLogEntryList`: Lists application-wide WARN/ERROR/FATAL log entries, most recent first, capped at
      100000, with optional date-range and substring search (Message/LoggerName/Exception) filters. _(permissions from
      `ApplicationLogEntryService.RequiredPermissions`)_
    * `ContainerLogEntryList`: Lists every container's own stdout/stderr on the Docker host (no name filtering -- covers
      etcd/Patroni/Redis/Sentinel and every other container), most recent first, capped at 100000, with optional
      date-range and substring search (ContainerN… _(permissions from `ContainerLogEntryService.RequiredPermissions`)_
    * `WafAttackList`: Lists captured Web Application Firewall signature matches, most recent first, capped at 100000,
      with optional date-range and substring search
      (MatchedValue/Route/SignatureName/Category/MatchedField/UserName/RemoteIp) filters. _(permissions from
      `WafAttackService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Containers, Load Balancer and Overlay Network (`containers-load-balancer-and-overlay-network`)

- **Hierarchy Placement:** Level 3, child of Platform Reliability
- **Semantic Persona:** Owns the containers, load balancer and overlay network slice end to end, which is small enough
  that reads and writes stay with one agent.
- **Semantic Routing Keywords:** docker, container, metric, event, host, ha, proxy, reachability, probe, server, status,
  overlay, network, task, drift
- **Toolbox (Max 15):**
    * `DockerContainerListNow`: Lists every container Docker currently knows about right now (running or not), live --
      not a stored snapshot, over DockerProxyClient/the Jube.Monitoring sidecar's own `/proxy` endpoint, never
      docker.sock directly. Read-only. _(permissions from `DockerAdministratorService.RequiredPermissions`)_
    * `DockerContainerMetricList`: Lists per-minute, per-container Docker metrics (state, health, restart count,
      CPU/memory/network/block-io usage, pids) captured by the Jube.Monitoring sidecar, across every host, most recent
      first, capped at 100000, with optional date-range… _(permissions from
      `DockerContainerMetricService.RequiredPermissions`)_
    * `DockerEventList`: Lists Docker Engine lifecycle events on the host (container die/kill/oom/restart/health_status,
      network/volume/image events, docker exec audit trail), most recent first, capped at 100000, with optional
      date-range and substring search (Event… _(permissions from `DockerEventService.RequiredPermissions`)_
    * `DockerHostInfoNow`: Reads the Docker host's own live info/version (container/image counts, CPU, memory, kernel,
      OS, Docker/API version) right now, over the same sidecar proxy path as `DockerContainerListNow`. Read-only. _(
      permissions from `DockerAdministratorService.RequiredPermissions`)_
    * `DockerHostMetricList`: Lists per-minute, per-host Docker host metrics (container/image counts, daemon version,
      image/layer/container/volume/build-cache disk usage) captured by the Jube.Monitoring sidecar, across every host,
      most recent first, capped at 100000, wi… _(permissions from `DockerHostMetricService.RequiredPermissions`)_
    * `HAProxyReachabilityProbeList`: Lists per-minute synthetic HAProxy reachability probes. Landlord only. _(
      permissions from `HaProxyReachabilityProbeService.RequiredPermissions`)_
    * `HAProxyServerStatusList`: Lists per-minute HAProxy backend server status snapshots. Landlord only. _(permissions
      from `HaProxyServerStatusService.RequiredPermissions`)_
    * `OverlayNetworkTaskDriftList`: Lists per-minute overlay network and DNS reconciliation checks. Landlord only. _(
      permissions from `OverlayNetworkTaskDriftService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Etcd and Patroni Cluster State (`etcd-and-patroni-cluster-state`)

- **Hierarchy Placement:** Level 3, child of Platform Reliability
- **Semantic Persona:** Owns the etcd and patroni cluster state slice end to end, which is small enough that reads and
  writes stay with one agent.
- **Semantic Routing Keywords:** etcd, cluster, event, member, status, patroni
- **Toolbox (Max 15):**
    * `EtcdClearAlarm`: Clears every currently-raised etcd alarm (NOSPACE/CORRUPT) on the given endpoint; acknowledges
      the flag, does not fix whatever raised it. **Write — confirmation required** _(permissions from
      `EtcdAdministratorService.RequiredPermissions`)_
    * `EtcdClusterEventList`: Lists captured etcd cluster events (LeaderChanged, HealthChanged, AlarmRaised,
      AlarmCleared), detected by diffing consecutive one-minute EtcdMemberStatus samples, most recent first, capped at
        100000. _(permissions from `EtcdClusterEventService.RequiredPermissions`)_
    * `EtcdClusterStatusNow`: Reads etcd's current member status, health, alarms and version right now over its own REST
      API, for every discoverable endpoint -- not the background sampler's last-stored row. Read-only. _(permissions
      from `EtcdAdministratorService.RequiredPermissions`)_
    * `EtcdDefragment`: Defragments one etcd member's own backend database file to reclaim freed space; briefly blocks
      that member, so call it on one member at a time, never the whole cluster at once. **Write — confirmation
      required** _(permissions from `EtcdAdministratorService.RequiredPermissions`)_
    * `EtcdHashKv`: Reads one etcd member's own keyspace consistency hash at its current revision, for comparing across
      members to catch a diverged member -- two members at the same revision reporting different hashes have disagreed
      about the keyspace. Read-only. _(permissions from `EtcdAdministratorService.RequiredPermissions`)_
    * `EtcdMaintenanceStatus`: Reads one etcd member's raft term/index, current believed leader, and backend database
      size both total and in-use -- the figure to check before running EtcdDefragment, rather than running it blind.
      Read-only. _(permissions from `EtcdAdministratorService.RequiredPermissions`)_
    * `EtcdMemberList`: Reads etcd's own authoritative membership list from the given endpoint -- member id, name,
      peer/client URLs, and voting vs learner status. Any one reachable member answers for the whole cluster, since
      membership is Raft-replicated state. Read-only. _(permissions from
      `EtcdAdministratorService.RequiredPermissions`)_
    * `EtcdMemberStatusList`: Lists per-minute etcd cluster member snapshots, one row per discovered/configured member,
      most recent first, capped at 100000. _(permissions from `EtcdMemberStatusService.RequiredPermissions`)_
    * `PatroniClusterEventList`: Lists captured Patroni cluster events: RoleChanged/StateChanged/TimelineChanged
      (detected by diffing consecutive one-minute PatroniMemberStatus samples), and Failover (read directly from
      Patroni's own DCS 'history' key in etcd, with Reason/… _(permissions from
      `PatroniClusterEventService.RequiredPermissions`)_
    * `PatroniClusterStatusNow`: Reads every discoverable Patroni member's status right now over its own REST API --
      role, state, timeline, lag, pending restart -- not the background sampler's last-stored row. Read-only. _(
      permissions from `PatroniAdministratorService.RequiredPermissions`)_
    * `PatroniConfigGet`: Reads a Patroni member's own resolved dynamic configuration (DCS-stored config merged with
      whatever is local to that node) as plain JSON text. Read-only. _(permissions from
      `PatroniAdministratorService.RequiredPermissions`)_
    * `PatroniMemberStatusList`: Lists per-minute Patroni-managed Postgres member snapshots, one row per
      discovered/configured member, most recent first, capped at 100000. _(permissions from
      `PatroniMemberStatusService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Postgres Observability (`postgres-observability`)

- **Hierarchy Placement:** Level 3, child of Platform Reliability
- **Semantic Persona:** Owns the postgres observability slice end to end, which is small enough that reads and writes
  stay with one agent.
- **Semantic Routing Keywords:** postgres, activity, index, statistics, log, entry, metric, replication, status,
  statement, table
- **Toolbox (Max 15):**
    * `PostgresActivityList`: Lists every backend currently known to PostgreSQL (pg_stat_activity), live -- not a stored
      snapshot, so results reflect only the instant this is called. _(permissions from
      `PostgresActivityService.RequiredPermissions`)_
    * `PostgresIndexStatisticsList`: Lists live per-index Postgres statistics for every user index (scan counts, size,
      unique/primary-key/unused flags), queried live -- not stored history, not scoped to any tenant. _(permissions from
      `PostgresIndexStatisticsService.RequiredPermissions`)_
    * `PostgresLockList`: Lists every lock PostgreSQL currently knows about (pg_locks), live. Ungranted rows -- a
      backend waiting on another one -- sort first. Complements PostgresActivityList's BlockedByPids (who is blocked)
      with what is actually being contended (which relation, which lock mode). Optional case-insensitive substring
      search against username, query text or relation name. Read-only. _(permissions from
      `PostgresLockService.RequiredPermissions`)_
    * `PostgresLogEntryList`: Lists Postgres's own server log lines (checkpoints, connection issues, deadlocks, crashes,
      slow autovacuum, ...), most recent first, capped at 100000, with optional date-range and substring search
      (Message/Level) filters. _(permissions from `PostgresLogEntryService.RequiredPermissions`)_
    * `PostgresMetricList`: Lists per-minute Postgres metric snapshots across every node, most recent first, capped at
      100000, with optional date-range and substring search (Instance) filters, not scoped to any tenant. _(permissions
      from `PostgresMetricService.RequiredPermissions`)_
    * `PostgresReplicationStatusList`: Lists per-minute pg_stat_replication snapshots, one row per connected standby,
      most recent first, capped at 100000, with optional date-range and substring search
      (UserName/ApplicationName/ClientAddress/State/SyncState) filters, not scoped t… _(permissions from
      `PostgresReplicationStatusService.RequiredPermissions`)_
    * `PostgresStatementStatisticsList`: Lists per-query-shape aggregate statistics from pg_stat_statements -- calls,
      rows, execution time min/mean/max/stddev, cache-hit vs disk-read bytes, temp-file spill bytes and WAL bytes,
      accumulated across all history since the extension was… _(permissions from
      `PostgresStatementStatisticsService.RequiredPermissions`)_
    * `PostgresTableStatisticsList`: Lists live per-table Postgres statistics for every user table (scans, row
      estimates, sizes, vacuum/analyze history, a LikelyMissingIndex heuristic), queried live -- not stored history, not
      scoped to any tenant. _(permissions from `PostgresTableStatisticsService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

#### Redis Observability (`redis-observability`)

- **Hierarchy Placement:** Level 3, child of Platform Reliability
- **Semantic Persona:** Owns the redis observability slice end to end, which is small enough that reads and writes stay
  with one agent.
- **Semantic Routing Keywords:** redis, call, counter, connection, event, multiplexer, metric, sentinel, status, slow,
  operation
- **Toolbox (Max 15):**
    * `RedisCallCounterList`: Lists per-minute Redis call counters -- one row per distinct "call" label (a Jube.Cache
      repository method, e.g. _(permissions from `RedisCallCounterService.RequiredPermissions`)_
    * `RedisConnectionEventList`: Lists captured CacheService ConnectionMultiplexer events (ConnectionFailed,
      ConnectionRestored, ErrorMessage, InternalError, ConfigurationChanged, ConfigurationChangedBroadcast), most recent
      first, capped at 100000. _(permissions from `RedisConnectionEventService.RequiredPermissions`)_
    * `RedisConnectionMultiplexerMetricList`: Lists per-minute snapshots of the application's own live Redis connection
      health and queue depth across every node, most recent first, capped at 100000, with optional date-range and
      substring search (Instance/ClientName) filters, not scoped… _(permissions from
      `RedisConnectionMultiplexerMetricService.RequiredPermissions`)_
    * `RedisKeyActivityList`: Samples up to 100 keys from the live Redis keyspace (SCAN, never KEYS) and reports each
      one's type, idle time, and -- only under an LFU maxmemory-policy -- its approximate access frequency. The 100-key
      cap is fixed. Optional glob pattern narrows the sample. For spotting a hot/cold key design problem, never for
      enumerating the keyspace. Read-only. _(permissions from `RedisKeyActivityService.RequiredPermissions`)_
    * `RedisMetricList`: Lists per-minute Redis metric snapshots across every node, most recent first, capped at 100000,
      with optional date-range and substring search (Instance) filters, not scoped to any tenant. _(permissions from
      `RedisMetricService.RequiredPermissions`)_
    * `RedisSentinelCheckQuorum`: Reads whether enough Sentinels currently agree a failover of the given master could
      actually succeed right now -- the real pre-flight check before reaching for RedisSentinelFailover. Read-only. _(
      permissions from `RedisSentinelAdministratorService.RequiredPermissions`)_
    * `RedisSentinelClusterStatusNow`: Reads every master/slave/sentinel Sentinel currently reports right now over its
      own protocol, not the background sampler's last-stored row, which can be up to a minute stale. Read-only. _(
      permissions from `RedisSentinelAdministratorService.RequiredPermissions`)_
    * `RedisSentinelEventList`: Lists captured Redis Sentinel pub/sub events (every channel Sentinel publishes --
      +sdown, -sdown, +odown, +switch-master, +failover-state, +slave, +sentinel, and more), most recent first, capped
      at 100000, with optional date-range and subst… _(permissions from
      `RedisSentinelEventService.RequiredPermissions`)_
    * `RedisSentinelFailover`: Forces a manual failover of the given master right now -- the action Sentinel's own
      design exists to let an operator trigger under a real incident. **Write — confirmation required** _(permissions
      from `RedisSentinelAdministratorService.RequiredPermissions`)_
    * `RedisSentinelGetMasterAddress`: Reads Sentinel's own current, authoritative address for the given master, as
      "host:port". Read-only. _(permissions from `RedisSentinelAdministratorService.RequiredPermissions`)_
    * `RedisSentinelReset`: Forgets Sentinel's current state of every master matching the given glob pattern, forcing
      Sentinel to rediscover it from scratch; self-healing and non-destructive to data but still a real mutating call.
      **Write — confirmation required** _(permissions from `RedisSentinelAdministratorService.RequiredPermissions`)_
    * `RedisSentinelStatusList`: Lists per-minute Redis Sentinel topology snapshots (SENTINEL MASTERS/SLAVES/SENTINELS),
      one row per master/slave/sentinel entity, most recent first, capped at 100000. _(permissions from
      `RedisSentinelStatusService.RequiredPermissions`)_
    * `RedisSlowOperationList`: Lists Redis SLOWLOG entries across every node, most recent first, capped at 100000, with
      optional date-range and substring search (Command/CommandName/KeyName/ClientAddress/ClientName) filters, not
      scoped to any tenant. _(permissions from `RedisSlowOperationService.RequiredPermissions`)_
- **Downstream Dependencies:** none (leaf)

---

## Fitting this to the runtime

The tree above is a design, not a wiring. Four things in `Jube.AskJooby` decide whether it can execute as drawn:

* `RunContext.MaxDepth` defaults to 3 and `AgentFactory` only offers a delegation tool while `depth < context.MaxDepth`,
  so root to domain to supervisor to leaf is exactly the budget and there is no room for a fifth tier; the nine slices
  that reduced to a single leaf are attached straight to their domain rather than kept behind a supervisor that would
  route to one child, which also buys back a hop for them.
* `RunContext.MaxFanOut` defaults to 8 and `SpawnAgentTool` rejects a *single spawn call* that exceeds it, which is a
  cap on how many specialists are asked at once and not on how many an agent may choose between; no roster here exceeds
  nine, counting cross-domain reach, and `model-admin` and `model-support` are separate domains specifically so that the
  slice rosters stay within eight before any cross-domain edge is added.
* `AgentToolDefinition.RequiresSnapshot` is already derived from the owning operation being `Write` or `Delete`, and
  `SnapshotOnWriteAiFunction` wraps those tools; the 174 tools flagged above are that same set, so the flag is a
  restatement of what the runtime already computes and must not be reintroduced as a second, hand-maintained source of
  truth.
* `AgentDefinitions.IsPermitted` checks `RequiresLandlord` before it considers `RequiredPermissions`, because 49
  services are landlord-only with an empty permission list; every permission shown above is read from the owning
  service's own `RequiredPermissions` and is never retyped here, so a service that changes its guard changes this tree
  with it.

Each agent's toolbox is the authority for what it can call. Nothing above grants a tool that the catalogue does not
already carry, and the eighteen operations the catalogue withholds on purpose stay withheld: they are absent from this
tree by the same decision.
