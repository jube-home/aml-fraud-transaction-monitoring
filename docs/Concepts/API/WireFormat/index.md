---
layout: default
title: Wire Format
nav_order: 14
parent: API
grand_parent: Concepts
---

# Wire Format

The browser user interface does not, by default, speak plain JSON to the Jube API. It instead negotiates a compact wire format, being MessagePack, to which compression and a second layer of encryption may be added. The format is negotiated by the caller, which is to say that a caller not asking for it (a script, the Swagger page, an integration) is always served JSON, whatever the settings below. The `/api/invoke` endpoints write their responses as JSON directly and are never transcoded, henceforth the public API remains JSON, as described in [HTTP API Concepts](../HTTPAPIConcepts/index.html).

The mechanism is a filter applied at the root of the API, which transcodes a successful response to the format asked for, together with middleware which transcodes a MessagePack request body to the equivalent JSON before it reaches the endpoint. It follows that no endpoint is aware of the wire format, and that the data rides the serialisation alone.

## Negotiation

The browser sends `Accept: application/x-msgpack, application/vnd.jube.columnar+json;q=0.95, application/json;q=0.9` on every authenticated call, and `Accept-Language` set to the interface language (`en`, `pl`, `ar`, `ka`, `id`, `sv`, `ur`, `hi` or `fr`). Anonymous calls, such as sign-in, are always JSON.

| Setting                                           | Default           | Effect                                                                                                                                                     |
|---------------------------------------------------|-------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `WireSerialisationMode`                           | `MessagePack`     | `Json`, `Mixed` or `MessagePack`. The server enforces it: in `Json` a MessagePack request body is refused with 415.                                        |
| `WireMessagePackCompression`                      | `Lz4`             | Responses of 1,024 bytes or more are one LZ4 block, named in `X-Wire-Compression`. Request bodies are never decompressed: a compressed request is refused. |
| `WireGeneratedDecoders`                           | `True`            | The browser downloads generated TypeScript decoders and uses them only when their hash equals the server's types hash.                                     |
| `WireEnvelopeEncryption`                          | `False`           | Per-request AES-256-GCM key wrapped with RSA-OAEP-SHA256, bound to method, URL and timestamp.                                                              |
| `WireHttpCompression`, `WireHttpCompressionLevel` | `Gzip`, `Fastest` | HTTP compression of the responses the LZ4 setting does not cover.                                                                                          |

In the `Mixed` mode only those responses that carry rows are written as MessagePack, with all else remaining JSON. A caller asking for `application/vnd.jube.columnar+json` is served such row-carrying responses as columnar JSON where MessagePack is not served.

The full text of each setting is on the [Environment Variables](../../EnvironmentVariables/index.html) page.

## The wire schema

`GET /api/WireSchema` (authenticated) returns every payload type with its positional field list, the route table that says which request bodies are written as MessagePack, a hash and, when encryption is on, the public key. The response header `X-Wire-Schema` carries the hash against which a MessagePack response was written, and `X-Wire-Shape` names its type, such that the browser can fetch the matching schema once and cache it forever by hash.

## Errors

A MessagePack response is only used for a successful (2xx) body. An error always comes back as JSON, even when MessagePack was asked for, so that the interface can read its reasons. The interface turns an error into text in the following order: field errors (`errors` as a list of `propertyName` and `errorMessage`, or as a map of field to messages), then `reasons`, then `message`, then its own translated fallback from the `errors` group (`unexpected`, `forbidden`, `conflict`, `network`, `notFound`, `server`).

| HTTP status     | Interface meaning                         | Retried                 |
|-----------------|-------------------------------------------|-------------------------|
| 0 (no response) | network                                   | reads up to three times |
| 401             | unauthorized: the user is sent to sign in | no                      |
| 403             | forbidden                                 | no                      |
| 404             | notFound                                  | no                      |
| 409, 412        | conflict (the record changed)             | no                      |
| 400, 422        | validation                                | no                      |
| other           | server                                    | reads up to three times |

Writes are never retried.

## Language

The language picked by the user in the interface is sent as `Accept-Language`. The server maps it to a resource culture from `en`, `pl`, `ar`, `ka`, `id`, `sv`, `ur`, `hi` and `fr`, and falls back to English for anything else (for example `de`), while the formatting culture stays English whatever is asked. Validation messages, form labels and tool text come from the server in that language.

## Hardening

A MessagePack body is deserialised with the untrusted-data security of MessagePack and a maximum object-graph depth of 64, converted to the equivalent JSON, and only then passes the same null-literal check, the [Web Application Firewall](../../WebApplicationFirewall/index.html) and the validators as a JSON body. It follows that the same attack in JSON and in MessagePack is given the same verdict.

Malformed MessagePack is answered 400, never 500, and a MessagePack body to an endpoint that does not take a body is answered 415.

With encryption on, a clock difference of more than five minutes is refused, a repeated request is refused by every node (the fingerprint of the wrapped key being held in the cache for ten minutes), a cache failure refuses the request, an unauthenticated caller never causes a decryption attempt, and a sealed body must declare its length (at most 64 MB) and an inner type of JSON or MessagePack.

Columnar row sets are limited to 1,000,000 rows.

No limit of its own is set upon the size of a request body in general, henceforth the default of ASP.NET, of about 28.6 MB, applies to the endpoints that do not set one for themselves.

{% comment %}TODO(verify): confirm on the supported deployment the largest request body accepted, as a proxy or load balancer in front of Kestrel may impose a smaller one than the ASP.NET default{% endcomment %}

## Regenerating the decoders

`Jube.App/Vue/src/generated/wire.generated.ts` and `wireTypes.generated.json` are generated from the DTOs when `Jube.App` is built, by the `GenerateWireDecoders` target of the project, which may be skipped with `-p:WireGenerate=false`. They are written by running `dotnet Jube.App.dll --wire-generate <directory>`, while `--wire-check <directory>` fails if they are stale. They are never to be edited by hand. A decoder is generated for every type reachable from a route, including closed generic responses such as `PayloadResult<T>`, whether they are sent singly, in a list, in a map or as nullable members, and no response type falls back to the slower decoders built from the schema at run time.
