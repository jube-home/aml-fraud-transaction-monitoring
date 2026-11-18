---
layout: default
title: Field Level Encryption
nav_order: 11
parent: Concepts
---

# Field Level Encryption

Individual String fields extracted by [Request XPath](../../Configuration/Models/RequestXPath/index.html) or computed by an [Inline Function](../../Configuration/Models/InlineFunctions/index.html) can optionally be encrypted at the point they are resolved, rather than being stored and archived as plain text. This is intended for elements that are sensitive (for example, a national identifier or account number extracted from the payload) but that still need to be persisted, be that for reporting, for Case display, or so that a value can be compared for equality on a later transaction.

## Enabling encryption on a field

Encryption is a property of the field itself, henceforth it is set where the field is defined: navigate through the menu to Models, open the model, then the Request XPath hub card or the Inline Functions hub card, and open the field of interest. Creating and changing a field otherwise follows the common manner described in [Child Objects](../../Navigation/ChildObjects/index.html).

The option is labelled Encryption and is presented as a choice of three, directly beneath the data type, in the Extraction group of a Request XPath and the Function group of an Inline Function. It is presented only where the data type of the Request XPath, or the return data type of the Inline Function, is String, keeping in mind that it is only ever a String that the engine will encrypt, and it disappears upon any other data type being selected. The three settings are as follows:

| Value         | Effect                                                                                                                                      |
|---------------|---------------------------------------------------------------------------------------------------------------------------------------------|
| None          | The default. The resolved value is stored and archived as plain text.                                                                       |
| Deterministic | The value is encrypted with a fixed, value-derived Initialisation Vector (IV), so the same plain text always produces the same cipher text. |
| Random        | The value is encrypted with a fresh random IV every time, so the same plain text produces different cipher text on every invocation.        |

Encryption happens once, before both the live payload write and the archive and report write, so that the two copies of the value can never diverge, which is to say there is no separate pass to encrypt for storage that could fall out of step with what was actually processed. It follows that rules, the cache, the Archive and the Archive Key table are all presented with the cipher text and never the plain text.

## Choosing Deterministic or Random

The choice is not a matter of strength alone, as it changes what can subsequently be done with the encrypted value:

* Deterministic is required if the field is ever used as, or compared against, a Search Key, for example in matching all prior transactions for the same encrypted national identifier. As the same input always produces the same cipher text, a comparison for equality still works on the encrypted value without it ever needing to be decrypted. For the same reason a field promoted to the Archive Key table can still be looked up by its value, the sought value being encrypted in the same manner before the comparison is made. The trade, as with any deterministic encryption scheme, is that two records having the same plain text are visibly identical in their cipher text even to someone without the key, hence it leaks repetition, although not content.
* Random should be preferred whenever the field does not need to support equality matching, since a fresh IV for each value gives no such signal of repetition at all. It follows that a field encrypted as Random cannot be matched as a Search Key nor looked up by value in the Archive Key table.

## The encryption scheme

Encryption is performed by `AesEncryption` in the `Jube.Cryptography` project, being AES with a 256-bit key derived from the `ElementSymmetricEncryptionKey` Environment Variable via PBKDF2 (`Rfc2898DeriveBytes`, 100,000 iterations, SHA-256), using that same Environment Variable value as its own salt. In the case of Deterministic, the IV is the first 16 bytes of the SHA-256 hash of the plain text. Cipher text is stored as the IV concatenated with the encrypted bytes, encoded as Base64.

Using the encryption key as its own PBKDF2 salt means that the salt is not independent of the key. This is a known trade in the design, to be revisited separately, and it is set out here so that it remains visible rather than being quietly worked around in documentation.

The `AesEncryption` instance for a model is created once, at model synchronisation, and reused for the lifetime of the process rather than being derived again for each transaction. Deriving the key via PBKDF2 is deliberately expensive (100,000 iterations) as a defence against brute force, henceforth deriving it on every invocation would have been a material, and unnecessary, performance cost in the path of real-time transaction processing.

## Action required before use

`ElementSymmetricEncryptionKey` ships with the placeholder default `SuperSecretEncryptionKeyGoesHere` (see [Environment Variables](../EnvironmentVariables/index.html)). Any deployment that enables field encryption on any Request XPath or Inline Function field must set this to a securely generated value, particular to the environment, before doing so, as the placeholder default must never be relied upon beyond initial evaluation.

Changing the key after data has already been encrypted under the old key means that the same plain text no longer produces the cipher text already stored, so Deterministic matching against the existing data is lost, and the data encrypted beforehand can only ever be recovered with the old key. It follows that the key should be set once, correctly, before the feature is first used in an environment, rather than rotated casually afterwards.

{% comment %}TODO(verify): the product encrypts but offers no decryption anywhere (AesEncryption in Jube.Cryptography has an Encrypt method only), so an encrypted value is shown as cipher text in Case display and reporting. Owner to confirm this is the intended extent of the feature, or whether a decrypting display is planned, as the page formerly gave Case display as a purpose.{% endcomment %}
