---
layout: default
title: HTTP API Concepts
nav_order: 1
parent: API
grand_parent: Concepts
---

# HTTP API Concepts

There exists an embedded Swagger \ OpenAPI documentation available at the following location:

[https://localhost:5001/swagger](https://localhost:5001/swagger)

The landing page is titled Jube API v1, and lists every endpoint of the API grouped by its resource (for example Case, EntityAnalysisModel and Invoke), each of which expands to show its parameters, an example request body and the responses. The Swagger pages are themselves subject to authentication, which is to say that a request for /swagger which is not authenticated is challenged in the same manner as any other protected resource.

Swagger is the central resource for API documentation. The exception to self documentation is the /Invoke/EntityAnalysisModel endpoint which does not map to an object directly, with that request body, which also needs to be JSON, being parsed manually given model definitions (i.e. Request XPath). It follows that except the /Invoke/EntityAnalysisModel, which is documented extensively elsewhere, API endpoints benefit from strong typing.

The use of Swagger is outside the scope of this documentation, and it may be noted that an alternative HTTP request tool has been used throughout this documentation, however it is sufficed to say that in addition to the documentation of the API, there is also the functionality in Swagger to invoke any part of the API directly given JWT authentication token or API Key, both of which are offered by the Authorize function of the Swagger page.

## Authentication

For example, the following request seeks to return Case with ID 1:

```http
GET https://localhost:5001/api/Case/1
Authorization: Bearer <the JSON Web Token>
```

In the above example, it can be seen that the API parameter is accompanied by a JSON Web Token, Bearer token, without which a 401 http status code would be returned. Where a JSON Web Token, Bearer Token, is required, Swagger will display the parameter as required.

The api/Authentication/ByUserNamePassword endpoint is available for the validation of username and password returning a JSON Web Token in the response body:

```http
POST https://localhost:5001/api/Authentication/ByUserNamePassword
Content-Type: application/json

{
  "userName": "Administrator",
  "password": "<the password>"
}
```

```json
{
  "token": "<the JSON Web Token>",
  "expiration": "2026-01-01T00:15:00Z"
}
```

{% comment %}TODO(verify): replace the response above with one captured from a running system, confirming the casing of the token and expiration elements and the format of the expiration{% endcomment %}

As is standard, the HTTP header, which must be there on request, is named Authorization with the value being the string "Bearer " concatenated with the JSON Web Token. The HTTP cookie meanwhile is called "authentication-jwt" and contains only the JSON Web Token, although it is unlikely that an API led integration would choose to store or use a HTTP cookie, being more likely reserved for user interface page requests. Only the cookie is refreshed as it is used; a token sent in the header is valid for 15 minutes and is not replaced.

JWT is not intended for API integration, notwithstanding that the user interface is a fascia of the API's, and the use of API Key is encouraged, in which case the key is passed in a header named X-API-KEY in place of the Authorization header, as described in [Authentication Concepts](../../Authentication/index.html).

The JSON web token is signed (HMAC-SHA256), not encrypted, using Environment Variable values:

```text
JWTValidAudience=http://localhost:5001
JWTValidIssuer=http://localhost:5001
JWTKey=<a long random value, at least 32 characters>
```

| Value            | Description                                                                                                                                              |
|------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| JWTValidAudience | The server domain that is allowed the token.                                                                                                             |
| JWTValidIssuer   | The server domain that issued the token.                                                                                                                 |
| JWTKey           | The signing key for the JWT. It has no default and is not generated: it must be supplied as an Environment Variable, and Jube will not start without it. |

The token carries the username (e.g. Administrator) in the Name claim (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name) and the NameIdentifier claim, and the instant the session began. It follows that in knowing the key values for the JSON Web Token, a token can be created entirely out of band of the application, although every token is still checked against the database on each request and is refused if the user is not active, is locked, or has had their tokens revoked since it was issued.

## Status Codes

The following HTTP status codes are implemented by the API:

| Status Code | Implementation                                                                                                                                                                                                                                                                                                                                        |
|-------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 401         | Not authenticated.  JSON Web Token is either missing, expired or has been revoked (logout, password change or an administrator revoking the sessions).                                                                                                                                                                                                |
| 403         | Not authorised.  The authenticated user does not have access to the resource as configured in the permissions functionality.  Also returned to a state changing request (POST, PUT, PATCH, DELETE) authenticated only by the cookie that is not provably same-origin (`CsrfOriginCheck`).                                                             |
| 400         | Bad Request.  The request has not passed validation.  Error messages will be returned indicating the source of the failure in the JSON response body.  An empty, `null`, malformed or invalid UTF-8 JSON body, a NUL character, a number outside the range of a double and a value of the wrong type are all client errors and answered 400, not 500. |
| 204         | Key Not Found. In the case of PUT or DELETE methods,  the data was not found for modification.  This is often an indication of tenancy problems, where an attempt has been made to update data in a tenant which has not been allocated to the user.                                                                                                  |
| 500         | Internal Error.  An error fatal to the request.  The error messages will only be written to logs server side and not communicated to the end user                                                                                                                                                                                                     |
| 404         | Not Found.  The resource has not been found.  There are very few programmatic uses of Not Found,  except in the case of the Invoke APIs,  hence the first step should be to check the URL.                                                                                                                                                            |
| 408         | Timeout.  The resource has not been found in a timeout threshold.  In the case of the Invoke APIs callback on asynchronous processing.  A callback that belongs to another tenant is indistinguishable from one that never arrived, and also ends as a 408 after the same wait.                                                                       |
| 413         | Payload Too Large.  A JSON request body larger than the inspected limit (4 MB) is refused before it is processed.                                                                                                                                                                                                                                     |
| 429         | Too Many Requests.  On the Invoke APIs, the queue of asynchronous requests is full, or the caller has exceeded the rate allowed for sanctions checking.                                                                                                                                                                                              |
| 503         | Service Unavailable.  On the Invoke APIs, the engine has not started, so nothing can be processed yet.                                                                                                                                                                                                                                                |

## Authentication and Integration Endpoints

Resources are almost entirely subject to authentication, by JSON Web Token or API Key, and permission authorisation. The endpoints that exist for the purpose of gaining authentication, such as /api/Authentication/ByUserNamePassword, are by necessity outside of that authentication; it validates a user name and password, returning a JSON Web Token in the response body and placing the same in the cookie called "authentication-jwt".

The endpoints intended for the purpose of integration are prefixed with Invoke. They too require the caller to be authenticated, for which the API Key is intended, and they are furthermore subject to a configuration option of their own, in that the Environment Variable EnablePublicInvokeController must be set to True, which it is by default, without which each is answered 404:

| Endpoint                                          | Verb | Implementation                                                                                                   |
|---------------------------------------------------|------|------------------------------------------------------------------------------------------------------------------|
| /api/Invoke/Sanction                              | GET  | An endpoint for the purposes of integration to perform sanctions checking.                                       |
| /api/Invoke/Archive/Tag                           | PUT  | An endpoint for the purpose of adding tags to events stored in Archive.                                          |
| /api/Invoke/EntityAnalysisModel/{guid}            | POST | An endpoint for the invocation of models.                                                                        |
| /api/Invoke/EntityAnalysisModel/{guid}/Async      | POST | The same invocation of models, processed asynchronously.                                                         |
| /api/Invoke/Queue/EntityAnalysisModel/{guid}      | POST | An endpoint that places the request on the inbound queue, for the invocation of the model in the background.     |
| /api/Invoke/EntityAnalysisModel/Callback/{guid}   | GET  | An endpoint for the callback of the payload response given asynchronous model invocation.                        |
| /api/Invoke/ExhaustiveSearchInstance/{guid}       | POST | An endpoint for the invocation of exhaustive machine learning models beyond that available in model invocation.  |
| /api/Invoke/ExhaustiveSearchInstance/{guid}/Batch | POST | The same invocation of exhaustive machine learning models for a CSV or JSON file, uploaded as multipart form.    |
| /api/Invoke/AdvancedAnalytics/{guid}              | POST | An endpoint for the invocation of Advanced Analytics models.                                                     |

The mocking endpoints by which an HTTP Adaptation may be tried are served anonymously under /api/MockHttpAdaptation, and only where the Environment Variable EnableMockEndpoints is not False. The HTTP Adaptation of the default model, and the placeholder of the Http endpoint field, name /api/MockHttpAdaptation/BayesianNetworkBootstrapStrength.

The user interface is a single page application, written in Javascript, and served as static files. Given that the user interface is Javascript there must therefore be some form of API available to support the functions. Every function that can be enacted in the User Interface is available via the API. API which is prefixed with Invoke are not intended for use by the user interface and are instead for the purpose of integration.

## Verbs and Request Body

HTTP method verbs are implemented as customary:

| Verb   | Implementation                                                                                            |
|--------|-----------------------------------------------------------------------------------------------------------|
| GET    | HTTP requests to fetch data without a request body with parameterization in the URL stub or querystring.  |
| POST   | HTTP requests with a JSON request body for creation.                                                      |
| PUT    | HTTP requests with a JSON request body for update.                                                        |
| DELETE | HTTP requests to delete data without a request body with parameterization in the URL stub or querystring. |

HTTP request body is in all instances JSON for the purposes of integration, with media type header of application/json. The user interface, for its own purposes, speaks a binary form of the same payloads, as described in [Wire Format](../WireFormat/index.html), which does not alter the API as presented here. The following example is for PUT verb, targeting api/Case:

```json
{
    "id": 0,
    "diaryDate": "2022-10-18T07:09:16.686Z",
    "caseWorkflowStatusGuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "createdDate": "2022-10-18T07:09:16.686Z",
    "locked": true,
    "lockedUser": "string",
    "lockedDate": "2022-10-18T07:09:16.686Z",
    "closedStatusId": 0,
    "closedDate": "2022-10-18T07:09:16.686Z",
    "closedUser": "string",
    "caseKey": "string",
    "diary": true,
    "diaryUser": "string",
    "rating": 0,
    "json": "string",
    "caseKeyValue": "string",
    "lastClosedStatus": 0,
    "payload": "string",
    "closedReasonId": 0,
    "closedReasonNote": "string"
}
```

The response is of the same form as the request, returning the Case as it stands having been updated.

In all cases, examples are available in Swagger given the strong typing of the endpoints, where each endpoint is presented with an example value for the request body and for each response, in the manner of the listing above.

## Validation

All requests with the exception of Invoke endpoints are subject to validation. In the event validation fails, a Bad Request HTTP status is served, including details of the validation failure, as follows for PUT verb, to api/case, where a rating of 9 has been passed:

```json
{
  "isValid": false,
  "errors": [
    {
      "propertyName": "Rating",
      "errorMessage": "A rating from 0 (not yet rated) to 5 is required.",
      "attemptedValue": 9,
      "customState": null,
      "severity": 0,
      "errorCode": "RatingInclusiveBetween",
      "formattedMessagePlaceholderValues": {
        "From": 0,
        "To": 5,
        "PropertyName": "Rating",
        "PropertyValue": 9
      }
    }
  ],
  "ruleSetsExecuted": [
    "default"
  ]
}
```

{% comment %}TODO(verify): the listing above was composed from the validation of the Case rating, which must be from 0 to 5, its message and its error code; replace it with a response captured from a running system{% endcomment %}

In the above example validation has failed owing to the rating not being of a correct value.
