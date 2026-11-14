---
layout: default
title: HTTP Adaptation
nav_order: 12
parent: Models
grand_parent: Configuration
---

# HTTP Adaptation

HTTP Adaptation refers to the dispatch of the full transaction payload in a POST body to a remote HTTP endpoint for the
receipt of a quantitative score, and optionally a structured explanation of it, in the JSON response body.

The request body is the entire `EntityAnalysisModelInstanceEntryPayload` for the transaction - Payload, Abstraction, TTL
Counters, Abstraction Calculations, Sanctions, and any HTTP Adaptations with a lower Priority that have already been
recalled for the same transaction.

The response body may still be a bare number for the simplest case:

``` json
0.91
```

Alternatively, a structured object carrying a score alongside model metadata, calibration, contribution and journey -
see the [HTTP Adaptation Protocol](../HTTPAdaptationProtocol/index.html) page for the full response specification,
consumption rules, and a set of mock reference endpoints to test against.

The intention of HTTP Adaptation is to recall R models via Plumber, Python models via Flask or make use of any HTTP
service that respects the payload specification set out above.

<!-- TODO(docs): confirm the Vue menu path and route for HTTP Adaptation; the Razor-era path "Models >> Machine Learning >> HTTP Adaptation" may be stale -->
To create a HTTP Adaptation, navigate Models >> Machine Learning >> HTTP Adaptation:

<!-- TODO(screenshot): file=model-http-adaptation-tree.png route=/models/http-adaptation state="HTTP Adaptation tree, one model expanded" -->
![Image](model-http-adaptation-tree.png)

Click on a model in the tree to create a new HTTP Adaptation:

<!-- TODO(screenshot): file=model-http-adaptation-new.png route=/models/http-adaptation state="New HTTP Adaptation form, empty, Endpoint group visible" -->
![Image](model-http-adaptation-new.png)

The HTTP Adaptation accepts the following parameters:

| Value         | Description                                                                                                                                                                                                                                                                                                                                                                                       | Example                                    |
|---------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------|
| HTTP Endpoint | <!-- TODO(docs): owner to confirm the label and help text shown in the Vue form --> Using the prefix specified in the HttpAdaptationUrl Environment Variable in concatenation with remainder of the URL for the HTTP Endpoint to POST to.  Assume no "/" terminating the HttpAdaptationUrl Environment Variable value, so this value must itself begin with "/". Required, up to 2048 characters. | /api/invoke/ExampleFraudScoreLocalEndpoint |
| Priority      | Ascending execution order among the adaptations on a model, to support boosting/model chaining - a lower Priority adaptation is recalled first, and its result is available to a later one via the request body. Defaults to 0. Must be zero or greater.                                                                                                                                          | 0.1                                        |

In this example an endpoint is available for the purpose of echoing back the Square Root of the ResponseCodeEqual0Volume
Abstraction Rule Value at https://localhost:5001/api/invoke/ExampleFraudScoreLocalEndpoint. Complete the page as
follows:

<!-- TODO(screenshot): file=model-http-adaptation-example.png route=/models/http-adaptation state="Form completed with /api/invoke/ExampleFraudScoreLocalEndpoint and priority 0" -->
![Image](model-http-adaptation-example.png)

Scroll down and click Add to create a version of the HTTP Adaptation:

<!-- TODO(screenshot): file=model-http-adaptation-version.png route=/models/http-adaptation state="Saved adaptation showing version 1 and the approval state" -->
![Image](model-http-adaptation-version.png)

Synchronise the model via Entity >> Synchronisation and repeat the HTTP POST to
endpoint [https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc](https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc)
for response as follows.

<!-- TODO(screenshot): file=model-http-adaptation-response.png route=/api/invoke/EntityAnalysisModel state="Invoke response JSON with the HttpAdaptation element populated" -->
![Image](model-http-adaptation-response.png)

Notice that the score has been returned for use in Activation Rules in the Adaptation entity.

## Security and operating behaviour

<!-- TODO(docs): owner to read this whole section against the deployed behaviour, it was written from code only -->

The destination is never typed in full by the person configuring the adaptation. The server prepends a base URL and the
configured HTTP Endpoint is only the remainder. To keep it that way the HTTP Endpoint must begin with a single `/` and
is refused when it contains a scheme or host (`https://evil.example/`), a backslash, whitespace, a control character
(including a carriage return or line feed), or a dot segment (`.` or `..`, including the percent-encoded forms), so it
cannot climb out of the path below the base URL. A query string is permitted. The same check is repeated when the model
synchronises, so an endpoint that arrived by a Preservation import rather than the form is skipped (and logged) if it
fails it.

### Base URL: installation-wide or per Tenant

| Source                                   | Who sets it                                           | Rules                                                                                                                                                                                                                           |
|------------------------------------------|-------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `HttpAdaptationUrl` environment variable | The operator                                          | Trusted. May be http and may be a loopback or private address, which is how the development default `http://localhost:5001` works. Userinfo, a query string, a fragment and schemes other than http or https are still refused. |
| HTTP Adaptation URL on the Tenant record | The landlord only (the Tenants page is landlord-only) | Must be `https`, a public host, with no credentials, query string or fragment. Checked when saved, checked again when the model synchronises, and checked again at connection time.                                             |

A Tenant with no URL of its own uses the installation-wide settings. A Tenant with its own URL uses its own URL, its own
timeout (or the installation-wide timeout when left at zero) and its own certificate validation switch (default on).
Because the base URL is per Tenant, one Tenant's adaptations no longer reach another Tenant's scoring service by
construction, unless the landlord points both at the same host.
<!-- TODO(screenshot): file=model-http-adaptation-tenant-settings.png route=/administration/tenants state="Tenant form, HTTP Adaptation group, landlord signed in" -->
<!-- TODO(docs): confirm the label text of the three Tenant fields and where they appear on the Tenants page -->
See [Enabling Multi Tenancy And Switching](../../../Concepts/MultiTenancy/EnablingMultiTenancyAndSwitching/index.html#http-adaptation-url) for the Tenant properties, the rules for the URL and when a change takes effect.

The guard on a Tenant URL, applied at connection time as well as on save so a name that is changed to point elsewhere
afterwards (DNS rebinding) is also caught: the host name is resolved when the call is made, every address is checked,
and the connection is made to a validated address itself. Refused: loopback (`127.0.0.0/8`, `::1`, `localhost`),
link-local including cloud metadata (`169.254.0.0/16` and `fe80::/10`, so `169.254.169.254`), private ranges, shared
address space, multicast, unspecified, reserved and tunnelled IPv6 forms of those, and unusual spellings such as
`2130706433` or `0x7f000001`. Redirects are never followed. Proxies are not used. The operator can relax the guard for
Tenant URLs for development only with `HttpAdaptationAllowInsecureTargets=True`.

What is sent: the full transaction payload for the model (see above). What is not sent: there is no credential, token or
custom header configured per adaptation, so nothing secret is stored in the adaptation, returned by the API or written
to the log. Authentication to the remote service, if needed, must be handled by the network or by the service itself.
<!-- TODO(docs): owner to decide whether a per-tenant credential header is wanted; none exists today -->

### How the call is made

| Behaviour           | Detail                                                                                                                                                                                                                                                                                                                                                                         |
|---------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Redirects           | Never followed. A 3xx answer is an unsuccessful call.                                                                                                                                                                                                                                                                                                                          |
| Unsuccessful status | Any status outside 200-299 is treated as a failure, the body is not parsed.                                                                                                                                                                                                                                                                                                    |
| Timeout             | The Tenant's timeout, else `HttpAdaptationTimeout` (default 1000 ms), for the whole call, and for the connection attempt. A Tenant timeout must be 0 or between 100 and 30000.                                                                                                                                                                                                 |
| Response size       | A response body over 4 MiB is refused.                                                                                                                                                                                                                                                                                                                                         |
| TLS                 | Certificates are validated by default (`HttpAdaptationValidateSsl` is `True`, and the Tenant switch defaults on). Turn validation off only against a development endpoint with a self-signed certificate.                                                                                                                                                                      |
| Cookies, proxies    | Not stored or sent; the environment proxy is not used.                                                                                                                                                                                                                                                                                                                         |
| Retries             | None by default (`ResilientHttpMaxRetries` is 0): the adaptation POST is not known to be idempotent, and a retry against an already slow endpoint multiplies the cost. A small budget with exponential backoff can be enabled, which retries only transient failures (5xx, 408, 429, network errors, timeouts).                                                                |
| Circuit breaker     | One breaker per Tenant and host. When the failure ratio in the sampling window reaches `ResilientHttpFailureRatio` with at least `ResilientHttpMinimumThroughput` calls, calls to that host are rejected without being sent for `ResilientHttpBreakDurationSeconds`, then one trial call is allowed. One Tenant's failing endpoint cannot open the breaker for another Tenant. |

When a call fails, times out, is refused or is rejected by an open breaker, the adaptation is recorded with an Error and
no Value, which is the suppressed state described in
the [HTTP Adaptation Protocol](../HTTPAdaptationProtocol/index.html): rules that read it see no value, the transaction
carries on, and the failure is written to the server log. Adaptations run one after another, so the worst case a failing
endpoint adds to a transaction before its breaker opens is the number of active adaptations on the model multiplied by
the timeout; once the breaker is open it adds nothing.
<!-- TODO(docs): confirm how a suppressed adaptation reads inside Builder and Coder rules, and whether Archive Key records a null -->
<!-- TODO(docs): the breaker is held in memory per engine process; confirm that is acceptable for a cluster -->
