---
layout: default
title: Activation Rule Notifications
nav_order: 17
parent: Models
grand_parent: Configuration
---

# Activation Rule Notifications

It is possible to send Email and SMS notifications as a consequence of an Activation Rule having matched, with Notification configuration strings being available for tokenization (i.e. swapping values with that available in the processing for the purposes of providing context to the recipient).

> **Breaking change.** Email and SMS notifications are no longer configured on an Activation Rule. The five fields (Enable Notification, Notification Type, Destination, Subject and Body) have been removed from the Activation Rule, and the Engine no longer reads them or sends notifications for a rule match directly. The stored values are left in the database so they can be copied by hand, and they are not changed when a rule is updated. The equivalent is configured on a [Response Elevation Range](../ResponseElevationRanges/index.html) instead, which carries a web hook and notification of its own.

It follows that the notification is now a consequence of the Response Elevation that an Activation Rule raises, rather than of the Activation Rule itself: the Activation Rule matches and raises a Response Elevation, the Response Elevation prevailing for the transaction falls inside an active Response Elevation Range, and the Notification of that range is dispatched asynchronously. An Activation Rule that is to notify must therefore have Response Elevation enabled, as described in [Basic Activation Rules](../BasicActivationRules/index.html), with a value inside the range carrying the Notification.

## Where it lives

Navigate through the menu to Models, open the model, then the Response Elevation Ranges hub card, and open the range. The fields are in the Notification group of the form, which is collapsed until opened. Creating, changing, approving and deleting a Response Elevation Range follows the common manner described in [Child Objects](../../../Navigation/ChildObjects/index.html), and the remaining fields of the range are described in [Response Elevation Ranges](../ResponseElevationRanges/index.html).

## Engine configuration

It is important to note that the Engine must be configured to use an SMTP gateway in the case of email by updating Environment Variables:

```text
SMTPHost=email.emailhost.com
SMTPPort=587
SMTPUser=UsernameExample
SMTPPassword=PasswordExample
SMTPFrom=richard.churchman@jube.io
```

| Value | Description |
|-------|-------------|
| SMTPHost | The SMTP host name. |
| SMTPPort | The SMTP port name. |
| SMTPUser | The SMTP user name for authentication. |
| SMTPPassword | The SMTP password for authentication. |
| SMTPFrom | The email address where the email will be received from. |

Likewise the Clickatell gateway in the case of SMS, using the value in the Environment Variable:

```text
ClickatellAPIKey=APIKeyFromClickatell
```

| Value | Description |
|-------|-------------|
| ClickatellAPIKey | The API Key provided by Clickatell. |

In all instances if the engine instance is to dispatch notification it must be enabled via an Environment Variable, which is True by default:

```text
EnableNotification=True
```

| Value | Description |
|-------|-------------|
| EnableNotification | Enables Notification dispatch from the engine either via AMQP or internal concurrent queue. |

## Fields

| Value | Description | Example |
|-------|-------------|---------|
| Enable notification | Whether a notification is sent asynchronously when the range is reached. Switching it on presents the fields that follow. | True |
| Notification type | The channel of the notification, Email or SMS. | Email |
| Notification destination | The destination of the Email or SMS notification, being an email address or a telephone number. Required where Enable notification is on. | richard.churchman@jube.io |
| Notification subject | The tokenized subject (which is the replacement of tokens with values from the processing). The subject is ignored in the case of SMS. | This is a fraud alert for account [@Payload.AccountId@] |
| Notification body | The tokenized body (which is the replacement of tokens with values from the processing). | This is a fraud alert for account [@Payload.AccountId@] |

Enable notification is off for a new Response Elevation Range, and while it is off the remaining fields are not presented. Upon it being switched on, Notification type starts as Email. The validation of the group is as follows, each failure being written beneath its field and counted in the summary at the head of the form:

| Field                    | Message                                                                                                       |
|--------------------------|---------------------------------------------------------------------------------------------------------------|
| Notification type        | The notification channel must be Email or SMS.                                                                |
| Notification destination | A destination is required when the notification is enabled. The destination must be 512 characters or fewer.  |
| Notification subject     | The subject must be 512 characters or fewer.                                                                  |
| Notification body        | The body must be 65536 characters or fewer.                                                                   |

Neither the form of the destination, as an email address or a telephone number, nor the presence of a subject or a body, is checked upon saving, such that a destination that cannot be delivered to is discovered only upon dispatch, where it is recorded as a failed message against the transaction.

## Tokenization

The destination, subject and body of a Notification support tokenization, which will swap values enclosed in [@ @] brackets with a corresponding value. A token is written as the name of a collection, a full stop, and the name of the value within it. For example, the AccountId is present in the model definition, hence will be available in the payload, and to instruct the AccountId to be swapped in during processing, the value would be tokenized as `[@Payload.AccountId@]`.

The collections available to a token are Payload, Abstraction, Dictionary, TtlCounter, Sanction, HttpAdaptation, ExhaustiveAdaptation, AdvancedAnalyticsAdaptation, Graph, Svirfd and Activation, the latter being swapped for true or false according to whether the Activation Rule named matched. A token naming a collection or a value that is not available is left as it was written.

## Behaviour

The notification is dispatched asynchronously, such that there will be no noticeable difference to the response payload. The outcome of the message is recorded against the transaction, as described in [Response Elevation Ranges](../ResponseElevationRanges/index.html).

Only plain text emails are supported.

Exactly the same tokenization process would take place given an SMS selected, although the destination would need be the telephone number as per the Clickatell API specifications. To send SMS, it is a simple matter of changing the Notification type from Email to SMS and changing the destination from an email address to a mobile telephone number (noting in the Clickatell API this should be + and not 00 for the country code at the time of writing).

A notification is not dispatched where:

* The Response Elevation for the transaction is zero, or does not fall inside an active Response Elevation Range.
* The transaction is being processed by a reprocessing job.
* The EnableNotification Environment Variable is not True.
* The ActivationRuleIdempotency Environment Variable is True and the same transaction has already been notified for the same range, in which case a duplicate invocation of the transaction is not notified twice.

## Example

In this example a notification will be sent to richard.churchman@jube.io upon the Activation Rule created beforehand, titled Volume1DayUSDForIPOver100, having matched, showing some elements of tokenization. The Activation Rule has Response Elevation enabled, and the Response Elevation Range inside which its value falls is completed in the Notification group as follows:

| Value | Example |
|-------|---------|
| Enable notification | True |
| Notification type | Email |
| Notification destination | richard.churchman@jube.io |
| Notification subject | This is a fraud alert for account [@Payload.AccountId@] |
| Notification body | This is a fraud alert for account [@Payload.AccountId@] from IP [@Payload.IP@] |

Upon the change being approved, and taken up by the engine at its next cycle, the HTTP POST to the endpoint [https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc](https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc) is repeated. An email will have been dispatched asynchronously, and receipt of the email can be observed on inspection of the Inbox of the email address, where the message has been received and correctly tokenized:

```text
Subject: This is a fraud alert for account Test5

This is a fraud alert for account Test5 from IP 123.456.789.123
```

## Permissions and security

The Notification is configured on the Response Elevation Range, henceforth it requires permission 17, Read Write Activation Rule, which is the permission for reading and writing Response Elevation Ranges.

The SMTP password and the Clickatell API Key are held in Environment Variables only, and are never stored in the configuration of a model.
