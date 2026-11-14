---
layout: default
title: Wire Format
nav_order: 14
parent: API
grand_parent: Concepts
---

# Wire Format

<!-- TODO(docs): this page was drafted from the code (Jube.App/Wire, Jube.App/Vue/src/api) and the Environment Variables page, not from the running system; the owner should read every sentence. -->

The browser user interface does not speak plain JSON to the Jube API by default. It negotiates a compact wire format,
and may add compression and a second layer of encryption. Callers that do not ask for these (scripts, the Swagger page,
the `/api/invoke` endpoints) always receive JSON.

<!-- TODO(docs): confirm whether /api/invoke and other machine integrations are always plain JSON in every WireSerialisationMode. -->

## Negotiation

The browser sends `Accept: application/x-msgpack, application/vnd.jube.columnar+json;q=0.95, application/json;q=0.9` on
every authenticated call and `Accept-Language` set to the interface language (`en`, `pl`, `ar`, `ka`, `id`, `sv`, `ur`
or `hi`). Anonymous calls such as sign-in are always JSON.

| Setting                                           | Default           | Effect                                                                                                                                                     |
|---------------------------------------------------|-------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `WireSerialisationMode`                           | `MessagePack`     | `Json`, `Mixed` or `MessagePack`. The server enforces it: in `Json` a MessagePack request body is refused with 415.                                        |
| `WireMessagePackCompression`                      | `Lz4`             | Responses of 1,024 bytes or more are one LZ4 block, named in `X-Wire-Compression`. Request bodies are never decompressed: a compressed request is refused. |
| `WireGeneratedDecoders`                           | `True`            | The browser downloads generated TypeScript decoders and uses them only when their hash equals the server's types hash.                                     |
| `WireEnvelopeEncryption`                          | `False`           | Per-request AES-256-GCM key wrapped with RSA-OAEP-SHA256, bound to method, URL and timestamp.                                                              |
| `WireHttpCompression`, `WireHttpCompressionLevel` | `Gzip`, `Fastest` | HTTP compression of the responses the LZ4 setting does not cover.                                                                                          |

The full text of each setting is on the [Environment Variables](../../EnvironmentVariables/index.html) page.

<!-- TODO(docs): link target above depends on the Jekyll URL layout; verify. -->

## The wire schema

`GET /api/WireSchema` (authenticated) returns every payload type with its positional field list, the route table that
says which request bodies are written as MessagePack, a hash, and, when encryption is on, the public key. The response
header `X-Wire-Schema` carries the hash a MessagePack response was written against, and `X-Wire-Shape` names its type,
so the browser can fetch the matching schema once and cache it forever by hash.

<!-- TODO(screenshot): file=wire-format-network-tab.png route=/administration state="Browser network tab showing a MessagePack response with X-Wire-Shape and X-Wire-Schema headers" -->

## Errors

A MessagePack response is only used for a successful (2xx) body. An error always comes back as JSON, even when
MessagePack was asked for, so the interface can read its reasons. The interface turns an error into text in this order:
field errors (`errors` as a list of `propertyName`/`errorMessage` or as a map of field to messages), then `reasons`,
then `message`, then its own translated fallback from the `errors` group (`unexpected`, `forbidden`, `conflict`,
`network`, `notFound`, `server`).

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

<!-- TODO(screenshot): file=wire-format-error-alert.png route=/administration/security state="A red error alert listing two validation reasons" -->

## Language

The language the user picks in the interface is sent as `Accept-Language`. The server maps it to a resource culture from
`en`, `pl`, `ar`, `ka`, `id`, `sv`, `ur` and `hi` and falls back to English for anything else (for example `fr` or
`de`); the formatting culture stays English whatever is asked. Validation messages, form labels and tool text come from
the server in that language.

<!-- TODO(docs): the server previously shipped French resources (*.fr.resx) that RequestLocalisation does not select; confirm whether French is intentionally not a supported language. -->

## Hardening

- A MessagePack body is deserialised with MessagePack's untrusted-data security and a maximum object-graph depth of 64,
  converted to the equivalent JSON, and only then passes the same null-literal check, the Web Application Firewall and
  the validators as a JSON body. The same attack in JSON and MessagePack gets the same verdict.
- Malformed MessagePack is a 400, never a 500. A MessagePack body to an endpoint that does not take a body is a 415.
- With encryption on: a clock difference of more than five minutes is refused, a repeated request is refused by every
  node (the wrapped-key fingerprint is held in the cache for ten minutes), a cache failure refuses the request, an
  unauthenticated caller never causes a decryption attempt, and a sealed body must declare its length (at most 64 MB)
  and an inner type of JSON or MessagePack.
- Columnar row sets are limited to 1,000,000 rows.

<!-- TODO(docs): state the largest request body Kestrel accepts in the supported deployment (the code sets no MaxRequestBodySize, so ASP.NET's default of about 28.6 MB applies). -->

## Regenerating the decoders

`Jube.App/Vue/src/generated/wire.generated.ts` and `wireTypes.generated.json` are generated from the DTOs when `Jube.App` is
built. Run `dotnet Jube.App.dll --wire-generate <directory>` to write them and `--wire-check <directory>` to fail if
they are stale. Never edit them by hand. A decoder is generated for every type reachable from a route, including closed
generic responses such as `PayloadResult<T>`, whether they are sent singly, in a list, in a map or as nullable members;
no response type falls back to the slower decoders built from the schema at run time.

<!-- TODO(docs): confirm the build target in Jube.App.csproj (GenerateWireDecoders, skipped with -p:WireGenerate=false) is the documented route. -->
