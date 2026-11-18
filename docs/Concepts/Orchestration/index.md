---
layout: default
title: Orchestration
nav_order: 9
parent: Concepts
---

# Orchestration

Jube is designed to be started, probed and stopped by an orchestrator, which is to say tooling such as Kubernetes, Docker Swarm or Docker Compose, without the loss of data and without the orchestrator needing any knowledge of the application beyond a single HTTP endpoint. Two behaviours make this possible: the manner in which the application handles SIGTERM, and the Ready Endpoint.

## SIGTERM Handling

SIGTERM is handled robustly via the centralisation of a cancellation token in the application. The application background tasks are all dismantled on the basis of instruction to the singular cancellation token. The application is designed to shut down very quickly and very safely to fully support dynamic scaling of the application with tooling such as Kubernetes or Docker Swarm.

On cancellation all background tasks monitor for cancellation, and will gracefully exit if sensitive, otherwise cause immediate cancellation if inconsequential. In the case of background tasks that are responsible for draining concurrency queues, asynchronous database writes and the pruning of the Resp cache, these queues will be drained before cancellation (the application won't drop data on SIGTERM). On SIGTERM, in the event that a background task can't be stopped for reasons of draining, its name will be written out to console as a blocking task, about every second, for observability, as follows:

```text
Blocking task: <the name of the background task>
```

Upon SIGTERM, any inflight requests via the embedded Kestrel server or AMQP will be drained also, being allowed to fully conclude. In the case of HTTP requests, on SIGTERM, any requests being served as part of drainage will include a disconnect header, which is to say `Connection: close`, instructing the client to consider the connection as closed, notwithstanding the response.

## Ready Endpoint

To determine if the application is ready to serve requests, a Ready Endpoint is available, and it is a simple matter of making an HTTP GET request with no headers, no querystring and no body:

```http
GET http://localhost:5001/api/ready
```

The Ready Endpoint will return HTTP Status Code 200 when ready, otherwise HTTP Status Code 503. In both cases there is no response body, henceforth it is only the status code that is to be relied upon.

In the case of SIGTERM having been received, the Ready Endpoint will immediately return HTTP Status Code 503.

The Ready Endpoint checks for the instantiation of the following services:

* Implicitly the instantiation of the Kestrel Web Server.
* In the case of the EnableEngine environment variable being true, the Engine instantiation, which implies models, sanctions and any other dependencies have been loaded.
* In the case of the StreamingActivationWatcher environment variable being true, the Relay instantiation, which implies readiness to relay information to the Activation Watcher.

The checks are made in a fixed order, stopping at the first that fails: firstly that shutdown has not been requested, then the Engine (only when EnableEngine is True), then the Relay (only when StreamingActivationWatcher is True). Any failure is HTTP Status Code 503, which has the effect of taking the node out of rotation, while a ready node is HTTP Status Code 200. In the event that EnableEngine is True yet the Engine was never registered at all, the Ready Endpoint is not in a position to answer and HTTP Status Code 500 is returned instead.

The Ready Endpoint is anonymous by design, as an orchestrator or load balancer has no user to authenticate as. Keeping in mind that a probe may call the Ready Endpoint every few seconds, it reads no data, needs no permission and writes no audit record.

## Docker Health Check

The Ready Endpoint is also the backing for the Docker health check, which is included in the Docker File of Jube.App. It follows that Docker communication of health status rests upon receiving a 200 HTTP Status from this endpoint.

The health check is a small program by the name Jube.HealthCheck, published into the same image, which makes the request to `http://localhost:5001/api/ready` set out above, allowing five seconds for a response, and exits with 0 given a successful HTTP Status Code and 1 otherwise (including where no response is received at all). The health check is declared in the Docker File as follows:

```dockerfile
HEALTHCHECK --interval=10s --timeout=5s --retries=3 --start-period=30s \
    CMD dotnet /app/HealthCheck/Jube.HealthCheck.dll || exit 1
```

| Value        | Description                                                                                                         |
|--------------|---------------------------------------------------------------------------------------------------------------------|
| interval     | The Ready Endpoint is probed every ten seconds.                                                                     |
| timeout      | A probe not concluding inside five seconds is taken to have failed.                                                 |
| retries      | Three consecutive failures are required before the container is reported as unhealthy.                              |
| start-period | Failures in the first thirty seconds are not counted, giving the application time to start and the Engine to load. |

In the case of Kubernetes, the Helm chart in Jube.Helm points its probes at the same `/api/ready` path, hence the behaviour described for Docker holds there also.
