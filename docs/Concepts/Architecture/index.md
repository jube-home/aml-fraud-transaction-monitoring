---
layout: default
title: Architecture
nav_order: 1
parent: Concepts
---

# Architecture

Jube is open source transaction and event monitoring software. Jube is software for real-time data wrangling, artificial intelligence, decision-making and case management.

Jube has wider utility; Abstraction values are returned in the real-time response payload, hence Jube can facilitate data wrangling in a Function as a Service (FaaS) pattern. Response payload data is also stored in an addressable fashion, to the effect of improving user experience for advanced analytical reporting, while also reducing database resource \ compute cost.

Jube can be used in a variety of monitoring use cases, but is particularly suited for:

* Fraud or Abuse Detection and Prevention.
* IoT Service Efficiency Assurance.

Jube has only open source dependencies; It runs on commodity Linux Operating Systems and with Postgres databases. The stateless nature of Jube allows for scaling to massive extremes.

## Software Architecture

Jube has a highly scalable architecture. Jube performs as much processing as possible in memory in predictably sized thread capacity. Where database interactions are required, it is at a minimum against entities which are optimised to return datasets quickly based on indexed key value pairs using a Resp instance, which is to say anything RESP wire compatible in practice, with Garnet being the server provided in the Docker Compose file and Redis, KeyDB and DragonflyDB being alternatives. Configuration and rule compilation (this is .Net reflection) is done perpetually in background threads so that real-time request processing is not interrupted with computationally expensive language interpretation. Archive storage of the response payload happens asynchronously using internal concurrent queues, sometime after the response payload has been dispatched (and usually in bulk). The Archive payload is comprehensive; When exploited via Postgres binary JSON functions and the functionality inside Jube to promote keys, analytical reporting capability is enhanced to a great extent.

The software is written in C# and is presented as a single binary application, although it can be taken to be stateless for the purposes of clustering. While a single binary, Environment Variables are used to enable or disable threads, allocating threads based on desired architecture and work allocation.

Jube is intended to run on a commodity Linux open source infrastructure and has the following software dependencies:

* .Net 10 Runtime.
* Postgres database version 13 onwards (tested on 15.4 but no significant database development to cause a breaking change). The Docker Compose file provides Postgres 17 by way of the pgvector image.
* A Resp server, RESP being the wire protocol made commonplace by Redis. The Docker Compose file provides Garnet, with an alternative Docker Compose file, docker-compose.redis.yml, providing Redis in its place. In the case of Redis, version 6 or above is suggested (it probably works fine on earlier versions, as the commands used are basic), and RESP wire compatibility implies that it is possible to use KeyDB, DragonflyDB or any database compliant with the RESP wire protocol.

{% comment %}TODO(verify): the minimum Postgres version of 13 and the tested version of 15.4 predate this branch, which creates vector columns in its migrations and ships the pgvector/pgvector:pg17 image; confirm the minimum version supported and whether the pgvector extension is now a hard dependency.{% endcomment %}

Jube will of course run on Windows, or indeed any platform that is supported by the .Net runtime, however, alternative platforms are not recommended for production use. Windows is not tested. Furthermore, Redis, KeyDB and DragonflyDB do not support Windows, so Cache options there are limited to Garnet (although there may well be other RESP wire compatible databases that have been overlooked that work on Windows). Unclear why Windows would be a first choice, however.

## Solution Layout

While presented as a single binary, the solution is separated into projects by responsibility, and it is helpful to understand the principal ones as follows:

| Project          | Responsibility                                                                                                                                                              |
|------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Jube.App         | The host process, embedding the Kestrel HTTP web server, the API endpoints (in the Endpoints directory) and the user interface (in the Vue directory).                      |
| Jube.Service     | The service layer backing the API endpoints, where permissions are enforced.                                                                                                |
| Jube.Dto         | The data transfer objects exchanged with the API, which also carry the form layout metadata from which the forms of the user interface are drawn.                           |
| Jube.Validations | The validation applied to the data transfer objects in the server.                                                                                                          |
| Jube.Data        | Access to the Postgres database.                                                                                                                                            |
| Jube.Cache       | Access to the Resp cache.                                                                                                                                                   |
| Jube.Engine      | The background threads collectively referred to as the engine, being model synchronisation, real-time model invocation and the background services described further below. |
| Jube.Migrations  | The database Migrations.                                                                                                                                                    |

The user interface is a single page application written in Vue, which is compiled to static content served by the Kestrel HTTP web server, and it is a fascia of the API endpoints, relying upon them for all data. There are no pages rendered in the server.

## Migration

Jube handles its own database model installation and upgrade, a process known more commonly as Migration. When Jube starts, and after establishing a connection to the Postgres database, a check is performed against a database table called "VersionInfo" to establish what database changes need to be made to harmonise:

```sql
select *
from "VersionInfo"
```

The VersionInfo table has a row for each Migration that has been applied, comprising the Version (a number expressing the date and time at which the Migration was written), the date and time at which it was applied and the name of the Migration as a Description. An abbreviated example is as follows:

| Version        | AppliedOn           | Description                                        |
|----------------|---------------------|----------------------------------------------------|
| 202209091323   | 2022-09-20 05:18:14 | AddExampleCaseVolumeEntryTableIndex                |
| 20220426071427 | 2022-09-20 05:18:14 | AddEntityAnalysisModelAsynchronousQueueBalanceTable |
| 20220426071437 | 2022-09-20 05:18:14 | AddEntityAnalysisModelListValueTableIndex          |
| 20220429124900 | 2022-09-20 05:18:14 | AddActivationWatcherTableIndex                     |
| 20220429124901 | 2022-09-20 05:18:14 | AddArchiveKeyTableIndex                            |
| 20220429124902 | 2022-09-20 05:18:15 | AddArchiveTableIndex                               |

The list of required database changes, determined by cross-referencing the table VersionInfo with the available Migrations compiled to the Jube instance, are subsequently executed in the specified order.

Migration requires the following DDL permissions:

* CREATE TABLE.
* CREATE INDEX.
* INSERT.

DDL permissions cannot always be assured in production configurations, especially as the implementation becomes more distributed. For this reason the Migration will use the connection string in the MigrationConnectionString Environment Variable where it is provided, allowing the elevated permissions to be kept apart from the ConnectionString used otherwise, which it falls back to (with a warning being logged) in its absence.

Migration can be overridden by Environment Variable:

```text
EnableMigration=False
```

The default EnableMigration is set to True, and is taken to be the case throughout this documentation.

In the event Migration is disabled, the [Fluent Migrator Command Line Runner](https://fluentmigrator.github.io/articles/runners/runner-console.html) will need to be executed separately, targeting the Jube.Migrations.dll binary for Postgres 11.0.

Most migrations only add structure, but some tighten integrity by adding unique indexes, and those repair existing data first so that the index can be created:

* Names that must be unique among live (not soft deleted) rows, compared case insensitively, are enforced by the database: a User name across the whole instance, a Case Workflow name within its Model, a Case Workflow Action, Display or Filter name within its Case Workflow, a Visualisation Registry name within its tenant and a Visualisation Datasource or Parameter name within its Visualisation Registry. A row that duplicates an earlier one is renamed by appending ` [dup Id]` (the Id of the row), so nothing is deleted and the row can be found and corrected afterwards.
* A Role can be granted to an entity only once. On the twelve Role Allocation tables (Case Workflow, Status, XPath, Action, Display, Filter, Form and Macro, Visualisation Registry, Datasource and Parameter, and Entity Analysis Model) a repeated live grant is soft deleted, keeping the earliest, with `DeletedUser` set to `Migration`. Granting the same Role to the same entity again returns the grant that exists.
* `UserRegistry` gains a nullable `TokensValidFrom` column, used for session revocation (see Authentication Concepts).

Where the data of an existing instance matters, it is advised to run the migration against a copy first and look at the rows renamed with ` [dup ` in the name, since the migration takes the place of a person deciding which duplicate to keep.

Migrations do not exist as a concept for Resp in the same manner, as this is a schemaless Key \ Value Pair in memory database where Keys are upserted (inserted or updated on existence). Henceforth, Resp does not have a schema, although values do have a very specific structure native to Jube, as described in [Cache Schema](../Cache/CacheSchema/index.html). Where a prefix has been renamed between releases, the keys are carried across by a Migration in the same list as those for the database, hence no separate step is required.

## Embedded Kestrel HTTP Web Server

The Jube instance exposes an embedded web server by the name Kestrel. As long as the application started without error, itself a simple matter of ensuring the .Net runtime and Postgres database connection, then HTTP requests can be made.

All HTTP requests will be targeted at the Kestrel webserver, and include:

* Static content, being the compiled user interface (its HTML shell, javascript, styles, fonts and images), which is not subject to authentication. The user interface is entirely dependent on the API endpoints, and presents nothing of substance until the user has authenticated.
* The API endpoints (Minimal API, backed by the service layer) which are subject to authentication, except for the login and logout endpoints and the readiness probe. The invocation endpoints are intended for integration and also require authentication (a JWT or an API Key). The API is a variety of endpoints that are parametrized as JSON post bodies, and less frequently querystring values. The API is documented via Swagger at [http://localhost:5001/swagger](http://localhost:5001/swagger), once the Jube instance is running and responding to HTTP requests, and subject to authentication. It follows that any interaction that can be achieved in the user interface, can also be achieved via API directly, subject to authentication.

Authentication relies on a standard implementation of JSON web tokens sent in the HTTP header called Authorization (as a Bearer token), or an http cookie called authentication-jwt, one of which must always be present except for unauthenticated resources (an API Key in the X-API-KEY header is the alternative for integration).

The JSON web token is signed (HMAC-SHA256), not encrypted, using Environment Variables:

```text
JWTValidAudience=http://localhost:5001
JWTValidIssuer=http://localhost:5001
JWTKey=<a long random value, at least 32 characters>
```

| Value            | Description                                                                                                                                                |
|------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| JWTValidAudience | The server domain that is allowed the token.                                                                                                               |
| JWTValidIssuer   | The server domain that issued the token.                                                                                                                   |
| JWTKey           | The signing key for the token. It has no default and is not generated: it must be supplied as an Environment Variable, and Jube will not start without it. |

Notwithstanding a stateless architecture, the token contains a single claim type of Name (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name), which will contain only the username (e.g. Administrator).

## Custom HTTP Response Headers

Arbitrary HTTP response headers can be injected into every response by inserting rows directly into the `HttpResponseHeader` table (Header, Value columns). There is no page in the user interface for this, as rows are read once into an in-memory dictionary at startup and applied to every response by middleware, so a change to the table requires a restart of the instance to take effect.

A baseline set, being `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Cross-Origin-Resource-Policy` and a `Content-Security-Policy`, is seeded by a FluentMigrator migration (`Jube.Migrations.Branches.PenTestGbkRemediation.SeedDefaultHttpResponseHeaders`), with `Cross-Origin-Opener-Policy` and `Cross-Origin-Embedder-Policy` added by a later one, so a fresh instance already carries them without any intervention. `X-Content-Type-Options: nosniff` is set unconditionally by `RequestHardeningMiddleware` and `Strict-Transport-Security` by `UseHsts()`/`AddHsts()` (365 days, `IncludeSubDomains`), hence neither needs a row in this table.

The `Content-Security-Policy` as it stands after all migrations is as follows, keeping in mind that it continues to allow `'unsafe-inline'` for script and style, and `'unsafe-eval'` for script:

```text
default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; connect-src 'self'; worker-src 'self' blob:; child-src blob:; font-src 'self' data:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'
```

{% comment %}TODO(verify): the page formerly gave the remaining Razor pages as the reason for 'unsafe-inline'. The Razor pages are gone, yet the seeded policy still carries 'unsafe-inline' and 'unsafe-eval'; owner to confirm whether the Vue user interface still requires them or whether the policy can now be tightened.{% endcomment %}

Anything beyond the baseline, such as a stricter `Content-Security-Policy` or a header specific to one deployment, is a matter of updating or inserting a row, for example:

```sql
INSERT INTO public."HttpResponseHeader" ("Header", "Value")
VALUES ('Content-Security-Policy',
        'default-src ''self''; script-src ''self''; style-src ''self''; img-src ''self'' data:');
```

## Containerisation

To the extent that it is thought by the technical end user that Containerisation differs from lightweight Linux Virtual Machines, it is fully supported.

Environment Variables required by a Jube instance are passed as an operating system Environment Variable (the default way to pass invocation values to a container image).

The Docker Compose file in the root of the repository provides a complete arrangement, comprising the Postgres database (postgres), the Resp cache (resp, being Garnet) and the Jube instance (jube), alongside supporting services for monitoring (jube-monitoring), the execution of R scripts (jube-r) and the receipt of OpenTelemetry (jube-otel-listener). The behaviour of a Jube instance under an orchestrator is described in [Orchestration](../Orchestration/index.html).

## Cache

Resp is the cache in the Jube software architecture and assures consistent and real-time responses. Ensure the RespConnectionString environment variable string (the host name or IP of the remote Resp server or cluster, being localhost by default and resp in the Docker Compose file) as follows:

```text
RespConnectionString=?
```

Cache has extreme performance demands and should be entirely uncontested when real-time tolerances are demanded. Furthermore, cache should be forced almost entirely into memory, which in the case of Redis is the only option anyway. Ensuring that cache has an uncontested in-memory habitat is not all that achievable without offloading these entities to a separately sized and memory optimal process and server (this is complicated by Redis being a single thread, which makes alternatives such as Garnet, DragonflyDB and KeyDB attractive, offering better performance and greater consolidation, and is the reason for Garnet being provided in the Docker Compose file).

Given Resp being used as a cache the installation by its nature will be entirely in memory and writing to disk, which is asynchronous in the form of an Append Only File (AOF), exists for recovery only. In memory databases need meticulous capacity planning and monitoring, so not to allow for a situation where memory runs out and keys are evicted (a disastrous situation for Cache integrity). Key eviction is so problematic, it is suggested to consider KeyDB and its flash capability allowing for fall back to flash, not to mention the benefits of multithreaded performance which Redis has fallen far behind on.

## Load Balancing

A Jube instance is designed in an entirely stateless manner such to sit behind a standard HTTP load balancer. In HTTP load balancing, requests to the load balancer are sent to different backend servers in rotation, allowing for horizontal scalability. There is no session state within the Jube instance for any HTTP interaction, hence server affinity is not required (state is offloaded to the Postgres databases where required).

There is no specific configuration required of a Jube instance to achieve load balancing, with the configuration being offloaded to the load balancer itself.

A Jube instance exposes a self-hosted and embedded HTTP web server called Kestrel which supports SSL \ HTTPS schemes, however by default the certificate is self-signed and intended to secure internal transport only. In the event a Jube instance is made available over a public or corporate network, it is advisable to implement a reverse proxy or load balancer for the purposes of offloading the SSL \ HTTPS schemes, even if there exists only a single backend server. While the Kestrel embedded HTTP web server can support SSL with own certificates, it is not commonly used to support own SSL \ HTTP schemes in production use.

## RabbitMQ Load Balanced Distributed Installation with Cache Offload

In addition to HTTP interfaces to a Jube instance, RabbitMQ (supporting AMQP protocols) can be used for model invocation. The purpose of the RabbitMQ is to provide the following additional capabilities:

* The response payload available otherwise by HTTP response is emitted on an exchange called 'jubeOutbound' that can be subscribed to by consumers as required. This may take place even if model invocation has taken place via HTTP.
* The request payload otherwise sent by HTTP request can be placed into a queue called 'jubeInbound' along with certain routing headers that would otherwise be in the HTTP URL stub.
* Activation Rule matches are sent to an exchange called 'jubeActivations' for consumption by the Watcher in the user interface, but can be otherwise subscribed to by consumers as required (noting this data is largely a reduced version of what is available in jubeOutbound exchange). If RabbitMQ is not enabled, the same effect is achieved for supporting Watcher functionality by relaying through the Publish \ Subscribe functionality of the cache or polling the ActivationWatcher table in the database.
* Notifications dispatched as a consequence of an Activation Rule match can be placed to a queue called "jubeNotifications". If RabbitMQ is not enabled, the same effect is achieved for supporting Activation Rule Match Notification functionality by polling the Notifications table in the database. (notifications sent via the case management system are synchronous).

To enable RabbitMQ, the following Environment Variable needs to be updated:

```text
AMQP=True
AMQPUri=amqps://hostname:secret@cow.moo.cloudamqp.com/hostname
```

| Value   | Description                                                                                                                          |
|---------|--------------------------------------------------------------------------------------------------------------------------------------|
| AMQP    | True or False to signify that connections should be established via the AMQPUri connection string to the RabbitMQ server or cluster. |
| AMQPUri | The connection string to the RabbitMQ server.                                                                                        |

## HTTPS Highly Available Load Balanced Distributed

Jube is a single process, but given the stateless nature it can be clustered and arranged in a highly available manner. Given the comprehensive nature of the Jube instance, it is foreseeable that certain services, which would be background threads consuming resources, may not be used despite being instantiated by the default Environment Variable configuration (although they will be wait sleeping if not in use, so the effect is minimal).

Jube instances can be arranged in a highly available fashion as backend servers to a load balancer, yet also with services allocated to individual servers. In such an arrangement, clients and integrating systems make HTTPS requests to a load balancer, which offloads SSL and sends the requests in rotation to two or more Jube instances provisioned for the Invocation API and Engine, and separately to two or more Jube instances provisioned for the user interface and its API functions. Further Jube instances, receiving no requests from the load balancer save for the Ready Endpoint, are each provisioned for a single background purpose, being Exhaustive training, Reprocessing and the remaining low footprint threads. Every instance connects to the same Postgres database and the same Resp cache, and, where enabled, to the same RabbitMQ server.

{% comment %}DIAGRAM id=concepts-architecture-highly-available-distributed
     conveys=one binary provisioned by Environment Variable into differing roles behind a load balancer, all sharing the same database and cache
     description=Top: a box "Clients and integrating systems" with an arrow labelled "HTTPS" down to a box "Load balancer (SSL offload)". From the load balancer two groups of arrows labelled "HTTP, in rotation": the first to a group of two identical boxes "Jube instance: Invocation API and Engine"; the second to a group of two identical boxes "Jube instance: User Interface static content and CRUD type API". To the right, not connected to the load balancer, a column of three single boxes: "Jube instance: Exhaustive training (EnableExhaustiveTraining)", "Jube instance: Reprocessing (EnableReprocessing)", "Jube instance: other low footprint threads (Sanction, Cases Automation, Search Key Cache, TTL Counter, Notification, Cache Prune)". Bottom row of three shared stores: "Postgres database", "Resp cache (Garnet)", "RabbitMQ (optional, AMQP=True)". Every Jube instance box has an arrow down to Postgres and to Resp; the Invocation instances and the low footprint instance have dashed arrows to RabbitMQ, labelled with the queue jubeInbound inbound and the exchanges jubeOutbound, jubeActivations and queue jubeNotifications outbound. A dashed arrow labelled "GET /api/ready" from the load balancer to each instance it balances.{% endcomment %}

The above example would be overly distributed for most use cases, but serves to explain concepts.

The instances are provisioned for their purpose using Environment Variables. In the above arrangement there is provisioning for the following:

* Invocation API and Engine.
* Other CRUD type API functions for the User Interface.
* Other low footprint threads.
* Exhaustive training.
* Reprocessing.
* User Interface HTML, js and Static Content.

Although presented in more detail in subsequent sections of this documentation the real-time model data is processed via either an HTTP endpoint or an AMQP queue called jubeInbound.

The invocation HTTP API endpoints are intended for integration and, in the manner of every other API endpoint, require authentication by a JSON Web Token or, as is encouraged for integration, an API Key. The invocation HTTP API endpoints are available by default, hence an Environment Variable change is not required. Invocation over HTTP can be switched off for an instance altogether, in which case the invocation endpoints return the Not Found HTTP Status, by updating an Environment Variable as:

```text
EnablePublicInvokeController=False
```

{% comment %}TODO(verify): the page formerly stated that the invocation endpoint carries no authentication and is restricted by network security alone, and the name EnablePublicInvokeController reflects that. On this branch the invocation endpoints require authorisation in all cases (Jube.App/Endpoints/InvokeEndpoints.cs) and the variable only decides between serving and returning 404. Owner to confirm that anonymous invocation is retired for good, as the description in Environment Variables still speaks of an open basis.{% endcomment %}

Model processing relies on a complex set of background threads collectively referred to as the engine.

While the embedded Kestrel HTTP web service is responsible for processing requests, it is transposed to the engine.

AMQP meanwhile is handled directly via the engine.

Generally speaking, all but the most rudimentary user interface serving requires the engine to be enabled in the Environment Variable. The engine is enabled by default but can be disabled by Environment Variable:

```text
EnableEngine=False
```

Several threads are instantiated by the engine as default, but can be disabled from their default via Environment Variables:

```text
EnableSanction=False
EnableReprocessing=False
ReprocessingThreads=1
EnableExhaustiveTraining=False
EnableCasesAutomation=False
EnableSearchKeyCache=False
EnableTtlCounter=False
EnableNotification=False
```

| Value                    | Description                                                                                                                                                                                                                                                                                                  |
|--------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| EnableSanction           | Responsible for loading and serving sanctions check requests on the instance (in both model processing and direct request via HTTP).                                                                                                                                                                         |
| EnableReprocessing       | Responsible for reprocessing data from the archive through the models in a similar manner as real-time. Useful when new model configuration has been put in place and data needs to be modified. It is extremely intensive. Has provision for concurrency between instances and multiple ReprocessingThreads. |
| ReprocessingThreads      | The number of threads allocated to reprocessing.                                                                                                                                                                                                                                                             |
| EnableExhaustiveTraining | Responsible for training anomaly detection and neural network models. A computationally expensive process and care should be taken to isolate from real-time processes for risk of degradation of real-time model invocation response time.                                                                  |
| EnableCasesAutomation    | Responsible for case scheduling, specifically the reopening and closing of cases on lapse of expiry date. This routine has poor concurrency and it is suggested that there be only a single instance at this time.                                                                                           |
| EnableSearchKeyCache     | Responsible for the background processing of Abstraction Rules designated as being cached (this is aggregations having been precomputed rather than being online in the real-time model invocation). There may only be a single instance as there is no concurrency available.                               |
| EnableTtlCounter         | Responsible for the background maintenance of TTLCounters. There may only be a single instance.                                                                                                                                                                                                              |
| EnableNotification       | Responsible for the background dispatch of Notifications, either by connecting to RabbitMQ or maintaining a polling thread against a Notifications in memory concurrent queue.                                                                                                                               |
| CachePruneServer         | Responsible for the deletion of data from the Payload and PayloadLatest structures of the cache. True by default.                                                                                                                                                                                            |
| WaitCachePrune           | The time in milliseconds between the cache being pruned for expired payload and latest entries, being 10000 by default.                                                                                                                                                                                      |

The engine has gained many further background services, each enabled or disabled in the same manner, and the complete list is maintained in [Environment Variables](../EnvironmentVariables/index.html).

Such is the reliance of Jube upon the Kestrel HTTP web server, there is no means to disable it, henceforth, and subject to authentication in many cases, the User Interface HTML, js and Static Content and other CRUD type API functions will always be available.

## Database and Cache High Availability Considerations

A Jube instance is stateless however the platform application itself would be stateful given a dependence on the database and cache.

High availability of neither the Postgres database nor the Resp cache is within the scope of this documentation, as the manner in which high availability is achieved is standard and generic.

It is suggested that storage be offloaded to highly durable external storage in the case of archive and highly performant local storage in the case of Cache. WAL should be written to separate high performance disks and asynchronous write usage maximised. In the context of resilient failover with disks offloaded, the failure of the Postgres instance is quite implausible, and the implementation can be scaled up, rather than out, which is both easier and cheaper.
