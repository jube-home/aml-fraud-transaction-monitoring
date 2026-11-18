---
layout: default
title: Creating Automation
nav_order: 25
parent: Case Management
grand_parent: Configuration
---

# Creating Automation

The case management module supports basic automation and integration using Notifications and HTTP Endpoint hooks in the following cases workflow configuration items:

* Cases Workflow Status.
* Cases Workflow Form.
* Cases Workflow Action.
* Cases Workflow Macro.

The purpose of both HTTP Endpoints and Notifications is to provide a means to integrate to external systems and business processes within the context of the action being taken in the case page. The HTTP endpoint functionality is intended for the purposes of performing complex integrations and automations for which a bridge \ middleware must usually be created for that specific purpose.

## Where it lives

The fields are found in the record of each of the configuration items above, reached by navigating through the menu to Models, opening the model, then the Cases Workflows hub card, opening the Case Workflow, then the Status, Form, Action or Macro hub card. The record has a group for the HTTP endpoint and a group for the Notification, both collapsed at first, each having a switch upon which the remaining fields of the group depend, alongside a collapsed group for the SQL for the case.

## Fields

The fields are common across all cases workflow entries that process HTTP endpoints and Notifications:

| Value                    | Description                                                                                                                                                                                                                              | Example                                                               |
|--------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------|
| Enable http endpoint     | Whether an HTTP request is made upon the event.                                                                                                                                                                                          | True                                                                  |
| Http endpoint            | The URL to call, which supports tokenization in its path and query. Required when Enable http endpoint is on.                                                                                                                            | https://www.toptal.com/developers/postbin/1665212514812-5780224471818 |
| Http endpoint type       | The type of HTTP endpoint request. If POST then the values will be compiled in a JSON document and submitted in the POST body. If GET only the URL will be used in the HTTP request (although keep in mind that the URL is tokenized).    | POST                                                                  |
| Enable notification      | Whether an Email or SMS message is sent upon the event.                                                                                                                                                                                  | True                                                                  |
| Notification type        | Email or SMS.                                                                                                                                                                                                                            | Email                                                                 |
| Notification destination | The email address, or for SMS the telephone number using the + country code designation. Supports tokenization. Required when Enable notification is on.                                                                                | name@example.com                                                      |
| Notification subject     | The subject of the email, which supports tokenization. For SMS the subject is ignored.                                                                                                                                                   | Case for [@Payload.AccountId@] needs review                           |
| Notification body        | The body of the message, which supports tokenization.                                                                                                                                                                                    | Please review the case for [@Payload.AccountId@].                     |
| SQL for the case         | Optional. A single SELECT run for the case, from the rows of which the tokens are then drawn, as described under Drawing from SQL.                                                                                                       |                                                                       |

## When a dispatch takes place

| Event                | Description                                                                                                                                        |
|----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|
| Case Workflow Status | On allocation of a status in Activation Rule Case Creation, Elevation, Automatic Escalation, or updates being made by the end user.                |
| Case Workflow Form   | On submission of a Case Workflow Form.                                                                                                             |
| Case Workflow Action | Creation of a Case Note by the end user alongside a Case Workflow Action.                                                                          |
| Case Workflow Macro  | Invocation of the Case Workflow Macro by the end user.                                                                                             |

Both the email and SMS notifications, and the HTTP request, are on the basis of being straight through and synchronous. A status change is saved before its dispatch is made, and should a dispatch fail the change still stands.

## Tokenization

A token is a name flanked by the `[@` and `@]` characters, which upon the invocation of either a Notification or HTTP Endpoint is replaced by its value. In the absence of SQL for the case, the values are drawn from the payload stored alongside the case record at creation, and the name is qualified by the element of that payload in which it is found. By way of example, where the case was created by a transaction for AccountId Test1, the string "AccountId: [@Payload.AccountId@]" would be replaced to become "AccountId: Test1".

The elements available are as follows:

| Token                               | Value                                                                    |
|-------------------------------------|--------------------------------------------------------------------------|
| [@Payload.Name@]                    | A field of the request, as extracted by Request XPath, Inline Functions and Inline Scripts. |
| [@Abstraction.Name@]                | The value of an Abstraction Rule or Abstraction Calculation.             |
| [@Dictionary.Name@]                 | The value looked up in a Dictionary.                                     |
| [@TtlCounter.Name@]                 | The value of a TTL Counter.                                              |
| [@Sanction.Name@]                   | The distance returned by a Sanction.                                     |
| [@HttpAdaptation.Name@]             | The value returned by an HTTP Adaptation.                                |
| [@ExhaustiveAdaptation.Name@]       | The score of an Exhaustive Adaptation.                                   |
| [@AdvancedAnalyticsAdaptation.Name@] | The score of an Advanced Analytics model.                               |
| [@Graph.Name@]                      | A value of the graph element.                                            |
| [@Svirfd.Name@]                     | A value of the Svirfd IP intelligence element.                           |
| [@Activation.Name@]                 | true where the Activation Rule of that name matched, otherwise false.    |

A token that is not qualified in this manner, or that names something not present in the payload, is not replaced and remains as written. It follows that a token of the earlier form, such as [@AccountId@], must be written as [@Payload.AccountId@], which is the form shown by the placeholders of the Notification subject and Notification body fields.

The Case Key and Case Key Value are always embedded in the body of an HTTP POST, as `caseKey` and `caseKeyValue`, as can be seen in the POST Body Context below.

## HTTP Endpoint

HTTP Endpoint functionality brings together parameters and makes a POST or GET to an HTTP endpoint. A POST will upon invocation compile all values into a JSON document \ message and submit it to the remote HTTP endpoint. In the case of GET, the POST body would not be available, instead, data would be transposed to the query, and for this reason the querystring fully supports tokenization.

The scheme, host and port of the URL must be written out literally, and tokens may fill only the path and the query. A URL whose host, port or scheme would be decided by a token, that carries credentials, or that is of a scheme other than http or https, is refused upon saving and again upon being called. Furthermore, every call is subject to the [Outbound Host Policy](../../Administration/OutboundHostPolicy/index.html), such that loopback, private, link-local and other internal addresses are refused unless the receiver has been listed in the JUBE_HOOK_ALLOWED_HOSTS or JUBE_HOOK_ALLOWED_CIDRS Environment Variables, and a host that fails repeatedly is suspended and is no longer called.

### Example of a POST

In this example the Toptal Post Bin service is used (https://www.toptal.com/developers/postbin/), which will promiscuously accept the requests emitted from the case management functions, and the following bin has been created:

[https://www.toptal.com/developers/postbin/1665212514812-5780224471818](https://www.toptal.com/developers/postbin/1665212514812-5780224471818)

On a Cases Workflow Status, complete the HTTP endpoint group as follows:

| Value                | Example                                                               |
|----------------------|-----------------------------------------------------------------------|
| Enable http endpoint | True                                                                  |
| Http endpoint        | https://www.toptal.com/developers/postbin/1665212514812-5780224471818 |
| Http endpoint type   | POST                                                                  |

Navigate to a case and, in the Decide pane, change the status of the case to that status. On refresh of the post bin endpoint, it can be seen that the data has been posted and received.

### Example of a GET

Update the Http endpoint type to GET and alter the Http endpoint to include a query as follows:

```text
https://www.toptal.com/developers/postbin/1665212514812-5780224471818?Example=[@Payload.AccountId@]
```

Navigate to a case, changing the status back to something else if need be, then change the status to that updated. On inspection of the Post Bin, the GET can be observed with the query string values properly tokenized.

### Evidence of a dispatch

There is no need to inspect the logs to understand the interchange with the HTTP endpoint. Everything the case workflow sent for a case key (emails, texts and web calls fired by a form, a macro, an action or a status) is recorded, and presented newest first in the Messages section of the case page. Each message states what fired it, the user and the time, whether it was Sent, Failed or Refused before it left, where it went, the subject, the answer of the receiver, what was sent, and whether it was drawn from The stored payload or The record's SQL, with the rows it used in the latter case. A dispatch fired by the engine on case creation or automatic escalation is recorded in the same manner.

{% comment %}SHOT id=case-automation-messages-section
     route=/cases
     steps=open-demo-case, click:Hide the sections, click:Hide the decide panel, open-section:Messages
     frame=section:Messages
     state=the Messages section of a demonstration case listing several dispatches: an email sent by a status, a web call POST sent by a form, and a web call refused before it left, the first expanded to show To, Subject, Answer, Drawn from and What was sent
     conveys=every Notification and HTTP Endpoint dispatch is recorded against the case and can be read back exactly as sent
     alt=The Messages section of a case listing emails and web calls with their outcome of Sent, Failed or Refused, one expanded to show its destination, answer and body{% endcomment %}

![The Messages section of a case listing emails and web calls with their outcome of Sent, Failed or Refused, one expanded to show its destination, answer and body](case-automation-messages-section.png)

## Notifications

Notifications are the sending of Email or SMS messages as a consequence of an event being raised in the case page. The notification functionality fully supports Tokenization in the Notification body, Notification subject and Notification destination fields, which will replace tokenized values with the values from the triggering event.

By way of example, complete the Notification group of a Cases Workflow Status using tokenization on the AccountId (keeping in mind that all three fields support tokenization):

| Value                    | Example                                           |
|--------------------------|---------------------------------------------------|
| Enable notification      | True                                              |
| Notification type        | Email                                             |
| Notification destination | name@example.com                                  |
| Notification subject     | Case for [@Payload.AccountId@]                    |
| Notification body        | Please review the case for [@Payload.AccountId@]. |

The process to change to this status is as laid out above, however in addition to an HTTP GET (or POST) being made, an email (or SMS if that Notification type has been selected) will be dispatched having been appropriately tokenized.

The Notification type SMS expects a destination of a telephone number, using the + country code designation, and the subject will be ignored.

As both the email and SMS notifications are on the basis of being straight through and synchronous, nothing needs to be enabled in the engine. However, the following values need to be considered as Environment Variables:

```text
SMTPHost=email.emailhost.com
SMTPPort=587
SMTPUser=UsernameExample
SMTPPassword=PasswordExample
SMTPFrom=richard.churchman@jube.io
```

| Value                     | Description                                                                 |
|---------------------------|-----------------------------------------------------------------------------|
| SMTPHost                  | The SMTP host name.                                                         |
| SMTPPort                  | The SMTP port, which is 587 by default.                                     |
| SMTPUser                  | The SMTP user name for authentication.                                      |
| SMTPPassword              | The SMTP password for authentication.                                       |
| SMTPFrom                  | The email address where the email will be received from.                    |
| SMTPEnableSsl             | Whether the connection to the SMTP host is encrypted, which is True by default. |
| SMTPUseDefaultCredentials | Whether the default credentials of the process are used, which is False by default. |

Likewise the Clickatell gateway in the case of SMS, using an Environment Variable as:

```text
ClickatellAPIKey=APIKeyFromClickatell
```

| Value            | Description                         |
|------------------|-------------------------------------|
| ClickatellAPIKey | The API Key provided by Clickatell. |

## Drawing from SQL

A Status, Form, Action or Macro may carry SQL for the case, in the group of that name, which is edited with a SQL editor that can also try the SQL against the id of a case, under Case id to try it on, using Run SQL. Where SQL is present, its rows are the only data from which the Notification and HTTP Endpoint are drawn, and where it is empty the stored payload is drawn from as described above.

| Aspect      | Behaviour                                                                                                                                                                                  |
|-------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Statement   | A single SELECT, read only, limited to 8,000 characters, run against the rows of the tenant alone.                                                                                         |
| Parameters  | `@Id`, the id of the case, and `@CaseKeyValue`. Any other parameter is refused upon saving.                                                                                                |
| Limits      | 200 rows at most, and 8 seconds, after which the SQL is stopped.                                                                                                                           |
| Tokens      | The rows are available as `rows`, with tokens such as `[@ rows[0].Name @]`, alongside `columns`, `rowCount` and `truncated`, and for a form `Form` for the values entered. The qualified payload tokens are not available in this manner, and the stored payload must instead be read from the Case table in the SQL. |
| Email       | The destination must come out as email addresses, ten at most, the subject is stripped of control characters, and the body is escaped for HTML.                                            |
| SMS         | The destination must come out as a telephone number.                                                                                                                                       |
| HTTP        | Values drawn into the path and query are URL encoded. The body of a POST is JSON built from the rows, as below.                                                                            |
| Failure     | Where the SQL fails or is refused, nothing is sent, and the Notification and HTTP Endpoint are each recorded in the Messages of the case as Refused.                                       |

The body of a POST drawn from SQL has the elements `source` (status, form, action or macro), `name`, `caseId`, `caseKey`, `caseKeyValue`, `user`, `rows` and, for a form, `values`, as in the following illustration:

```json
{
  "source": "form",
  "name": "ExampleForm",
  "caseId": 1,
  "caseKey": "AccountId",
  "caseKeyValue": "Test20",
  "user": "Administrator",
  "rows": [
    {
      "Name": "Robert Mugabe"
    }
  ],
  "values": {
    "ExampleTextboxElement": "Test"
  }
}
```

Case SQL runs with the access of the person who saved it, and not that of the person who opens the case, as is explained in [Case Workflow Form](../CreatingCaseWorkflowForm/index.html).

## POST Body Context

Where there is no SQL for the case, the body of an HTTP POST is compiled from the context of the event:

| Event                | Context                                                                                                                                                                               |
|----------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Case Workflow Action | Case Note including Case Workflow Action name, context of Case including Case Workflow Status Name and context of full Archive payload on case creation.                              |
| Case Workflow Status | Case including Case Workflow Status Name and context of full Archive payload on case creation.                                                                                        |
| Case Workflow Macro  | Case Workflow Macro definition including Case Workflow Macro name, context of Case entry including Case Workflow Status Name and context of full Archive payload on case creation.    |
| Case Workflow Form   | Case Workflow Form definition including Case Workflow Form name and payload, context of Case including Case Workflow Status Name and context of full Archive payload on case creation. |

The full Archive payload on case creation is the same in each, and it is printed once, for the Case Workflow Action. In the three listings thereafter, the element `payload` of the case is abbreviated to a remark in its place.

Case Workflow Action:

```json
{
  "id": 1,
  "note": "Hello",
  "actionId": 3,
  "priorityId": 1,
  "createdDate": "0001-01-01T00:00:00",
  "caseKey": "AccountId",
  "caseKeyValue": "Test20",
  "caseId": 1,
  "caseWorkflowActionName": "Escalation",
  "case": {
    "id": 1,
    "entityAnalysisModelInstanceEntryGuid": "909c1cac-de56-4f02-ab55-f514df3c1f6d",
    "diaryDate": "2026-02-20T09:19:46.5265",
    "caseWorkflowGuid": "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
    "caseWorkflowStatusGuid": "faf5c83a-5c3e-4a62-b224-aea9e2e1efa6",
    "createdDate": "2026-02-20T09:19:46.526214",
    "locked": 0,
    "closedStatusId": 0,
    "caseKey": "AccountId",
    "diary": 0,
    "rating": 0,
    "caseKeyValue": "Test20",
    "caseWorkflowStatus": "First Line Review",
    "payload": {
      "payload": {
        "IP": "Test",
        "OS": "Lollypop",
        "MAC": "94:23:44f:2:d3",
        "OTP": "466923",
        "Is3D": "False",
        "Brand": "ZTE",
        "Email": "please@hash.me",
        "IsAPM": "True",
        "Model": "Barby",
        "TxnId": "0987654321",
        "BankId": "57",
        "System": "Android",
        "OrderId": "10607324128",
        "Storage": "True",
        "Currency": "826",
        "DeviceId": "OlaRoseGoldPhone3",
        "IsRebill": "False",
        "AccountId": "Test20",
        "AmountEUR": "100",
        "AmountGBP": "86.5866",
        "AmountUSD": "113.055",
        "ChannelId": "1",
        "Jailbreak": "True",
        "ActionDate": "2019-04-17T04:18:15.0000000+03:00",
        "BillingZip": "123456",
        "IsCascaded": "False",
        "IsCredited": "False",
        "IsModified": "False",
        "JoinedName": "Robert Mugabe",
        "Resolution": "720*1280",
        "BillingCity": "Address Line 2",
        "ServiceCode": "DID",
        "ToAccountId": "MTN",
        "TwoFATypeId": "SMS",
        "TxnDateTime": "2024-12-01T21:41:37.2480000+02:00",
        "APMAccountId": "",
        "BillingPhone": "1234567890",
        "BillingState": "",
        "ResponseCode": "0",
        "AmountEURRate": "1",
        "AmountGBPRate": "0.8658658602",
        "AmountUSDRate": "1.1305502954",
        "BusinessModel": "Sports betting",
        "AppVersionCode": "12.34",
        "BillingAddress": "Address Line 1",
        "BillingCountry": "DE",
        "CreditCardHash": "0xDA39A3EE5E6B4B0JJJ1890AFD80709",
        "CurrencyAmount": "123.45",
        "IsModification": "False",
        "OriginalAmount": "100",
        "AccountCurrency": "566",
        "AccountLatitude": "5.3536",
        "BillingLastName": "Mugabe",
        "FingerprintHash": "jhjkhjkhsjh2hjhjkhj2k",
        "TwoFAResponseId": "1",
        "AccountLongitude": "36.1408",
        "AcquirerBankName": "Caixa",
        "BillingFirstName": "Robert",
        "DebuggerAttached": "True",
        "OriginalCurrency": "EUR",
        "SettlementAmount": "100000",
        "SimulatorAttached": "True",
        "TransactionTypeId": "False",
        "IsCurrencyConverted": "False",
        "TransactionResultId": "1006",
        "ToAccountExternalRef": "ChurchmanR",
        "TransactionExternalResponseId": "0"
      },
      "responseElevation": {
        "value": 2.0,
        "redirect": "https://www.jube.io",
        "foreColor": "#fb0707",
        "backColor": "#f5f2cb",
        "content": "Declined for \"VolumeThresholdByAccountId\"",
        "createdDate": "2026-02-20T09:19:46.0658815+02:00"
      },
      "r": 0.0,
      "createdDate": "2026-02-20T09:19:46.0732995+02:00",
      "entityAnalysisModelGuid": "90c425fd-101a-420b-91d1-cb7a24a969cc",
      "entityAnalysisModelName": "Detailed_Account_Financial_Transactions",
      "entityAnalysisModelInstanceGuid": "00000000-0000-0000-0000-000000000000",
      "entityAnalysisModelInstanceEntryGuid": "909c1cac-de56-4f02-ab55-f514df3c1f6d",
      "entityInstanceEntryId": "0987654321",
      "responseElevationLimit": 10.0,
      "referenceDate": "2024-12-01T21:41:37.248",
      "archiveEnqueueDate": "2026-02-20T09:19:46.4475738+02:00",
      "matchedGatewayRule": true,
      "prevailingEntityAnalysisModelActivationRuleId": 5,
      "entityAnalysisModelActivationRuleCount": 2,
      "dictionary": {
        "VolumeThresholdByAccountId": 0.0
      },
      "ttlCounter": {
        "TtlCounterAll": 0
      },
      "sanction": {
        "FuzzyMatchDistance2JoinedName": 0.0
      },
      "abstraction": {
        "ResponseCodeEqual0Volume": 1111.0500000000002,
        "NotResponseCodeEqual0Volume": 0.0
      },
      "abstractionCalculation": {
        "ResponseCodeVolumeRatio": 0.0
      },
      "httpAdaptation": {},
      "exhaustiveAdaptation": {},
      "activation": {
        "VolumeThresholdByAccountId": {
          "visible": true
        },
        "ThresholdSanctionsDistance": {
          "visible": true
        },
        "IncrementTtlCounterAll": {
          "visible": false
        }
      },
      "createCase": {
        "caseWorkflowGuid": "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
        "caseWorkflowStatusGuid": "faf5c83a-5c3e-4a62-b224-aea9e2e1efa6",
        "caseKeyValue": "Test20",
        "caseKey": "AccountId",
        "suspendBypass": false,
        "suspendBypassDate": "2026-02-20T09:19:46.3793539+02:00"
      },
      "tag": {},
      "logs": {
        "anchorDate": "2026-02-20T09:19:46.3793539+02:00",
        "entries": [
          { "elapsedMicroseconds": 612, "sinceLastEntryMicroseconds": 612, "capturedByInfoSampling": true, "capturedByWarnThreshold": false, "threadId": 1, "message": "Entity Invoke: GUID 90c425fd-101a-420b-91d1-cb7a24a969cc and model 1 has started the invocation timer and will now update the reference date." },
          { "elapsedMicroseconds": 117393, "sinceLastEntryMicroseconds": 116781, "capturedByInfoSampling": true, "capturedByWarnThreshold": false, "threadId": 1, "message": "Entity Invoke: GUID 90c425fd-101a-420b-91d1-cb7a24a969cc and model 1 has completed Sanction Check Name with a matched score." }
        ]
      },
      "responseTimePipeline": {
        "entries": [
          { "stage": "Parse", "elapsedMicroseconds": 1200, "durationMicroseconds": 40, "allocatedBytes": 512, "threadId": 1 },
          { "stage": "CheckIntegrityAndUpsert", "elapsedMicroseconds": 1245, "durationMicroseconds": 40, "allocatedBytes": 576, "threadId": 1 },
          { "stage": "InlineFunctions", "elapsedMicroseconds": 1290, "durationMicroseconds": 40, "allocatedBytes": 640, "threadId": 1 },
          { "stage": "InlineScripts", "elapsedMicroseconds": 1335, "durationMicroseconds": 40, "allocatedBytes": 704, "threadId": 1 },
          { "stage": "Gateway", "elapsedMicroseconds": 1380, "durationMicroseconds": 40, "allocatedBytes": 768, "threadId": 1 },
          { "stage": "CacheDbStorage", "elapsedMicroseconds": 1425, "durationMicroseconds": 40, "allocatedBytes": 832, "threadId": 1 },
          { "stage": "JoinReadTasks", "elapsedMicroseconds": 1470, "durationMicroseconds": 40, "allocatedBytes": 896, "threadId": 1 },
          { "stage": "AbstractionRulesWithoutSearchKeys", "elapsedMicroseconds": 1515, "durationMicroseconds": 40, "allocatedBytes": 960, "threadId": 1 },
          { "stage": "AbstractionCalculations", "elapsedMicroseconds": 1560, "durationMicroseconds": 40, "allocatedBytes": 1024, "threadId": 1 },
          { "stage": "ExhaustiveAdaptation", "elapsedMicroseconds": 1605, "durationMicroseconds": 40, "allocatedBytes": 1088, "threadId": 1 },
          { "stage": "HttpAdaptation", "elapsedMicroseconds": 1650, "durationMicroseconds": 40, "allocatedBytes": 1152, "threadId": 1 },
          { "stage": "Activation", "elapsedMicroseconds": 1695, "durationMicroseconds": 40, "allocatedBytes": 1216, "threadId": 1 },
          { "stage": "JoinWriteTasks", "elapsedMicroseconds": 1740, "durationMicroseconds": 40, "allocatedBytes": 1280, "threadId": 1 },
          { "stage": "WriteResponse", "elapsedMicroseconds": 1785, "durationMicroseconds": 40, "allocatedBytes": 1344, "threadId": 1 },
          { "stage": "BuildArchivePayload", "elapsedMicroseconds": 1830, "durationMicroseconds": 40, "allocatedBytes": 1408, "threadId": 1 }
        ]
      },
      "invokeTaskPerformance": {
        "totalDurationMicroseconds": 1298477,
        "memory": 8860472,
        "taskWrapperStats": {
          "read": {
            "sanctionsAsync": { "computeTimeMicroseconds": 11839960, "memory": 27349 },
            "ttlCountersAsync": { "computeTimeMicroseconds": 2603136, "memory": 29327 },
            "abstractionRulesWithSearchKeysAsync": { "computeTimeMicroseconds": 83912, "memory": 96653 }
          },
          "write": {
            "cachePayloadLatestUpsertAsync": { "computeTimeMicroseconds": 20680, "memory": 34383 },
            "cachePayloadInsertAsync": { "computeTimeMicroseconds": 9480, "memory": 11739 },
            "cacheTtlCounterEntryUpsertAsync": { "computeTimeMicroseconds": 688, "memory": 2816 },
            "cacheTtlCounterEntryIncrementAsync": { "computeTimeMicroseconds": 656, "memory": 2800 },
            "upsertReferenceDateAsync": { "computeTimeMicroseconds": 412, "memory": 960 }
          }
        },
        "stages": {
          "parse": { "durationMicroseconds": 48 },
          "checkIntegrityAndUpsert": { "durationMicroseconds": 1 },
          "inlineFunctions": { "durationMicroseconds": 30, "items": { "SetRiskScore": { "computeTimeMicroseconds": 2140, "memory": 512 } } },
          "inlineScripts": { "durationMicroseconds": 7, "items": { "EnrichPayload": { "computeTimeMicroseconds": 7102, "memory": 2048 } } },
          "gateway": { "durationMicroseconds": 1, "items": { "AllTransactions": { "computeTimeMicroseconds": 1840, "memory": 64 } } },
          "cacheDbStorage": { "durationMicroseconds": 1 },
          "sanctions": { "durationMicroseconds": 117, "items": { "SanctionCheckName": { "computeTimeMicroseconds": 117200, "memory": 4096 } } },
          "ttlCounters": { "durationMicroseconds": 117, "items": { "TxnCountLast24Hours": { "computeTimeMicroseconds": 2603136, "memory": 29327 } } },
          "abstractionRulesWithSearchKeys": { "durationMicroseconds": 186, "items": { "AverageSpend": { "computeTimeMicroseconds": 83912, "memory": 96653 } } },
          "joinReadTasks": { "durationMicroseconds": 1 },
          "abstractionRulesWithoutSearchKeys": { "durationMicroseconds": 1, "items": { "IsFirstTransaction": { "computeTimeMicroseconds": 1650, "memory": 128 } } },
          "abstractionCalculations": { "durationMicroseconds": 2, "items": { "RiskWeightedSpend": { "computeTimeMicroseconds": 2800, "memory": 96 } } },
          "exhaustiveAdaptation": { "durationMicroseconds": 1, "items": { "FraudScoreModel": { "computeTimeMicroseconds": 1050, "memory": 512 } } },
          "httpAdaptation": { "durationMicroseconds": 2, "items": { "BayesianNetworkBootstrapStrength": { "computeTimeMicroseconds": 2419, "memory": 256 } } },
          "activation": { "durationMicroseconds": 129, "items": { "HighRiskTransaction": { "rule": { "computeTimeMicroseconds": 1240, "memory": 192 }, "abstractionRuleOverrideInlineScripts": {}, "notification": { "computeTimeMicroseconds": 890, "memory": 64 }, "caseCreation": { "computeTimeMicroseconds": 126800, "memory": 8192 }, "activationWatcher": { "computeTimeMicroseconds": 142, "memory": 32 }, "responseElevation": { "computeTimeMicroseconds": 55, "memory": 0 } } } },
          "joinWriteTasks": { "durationMicroseconds": 2 },
          "writeResponse": { "durationMicroseconds": 1 },
          "buildArchivePayload": { "durationMicroseconds": 1 }
        }
      },
      "isSampled": true,
      "implicitAsyncTimedOut": false
    }
  }
}
```

Case Workflow Macro:

```json
{
  "caseId": 1,
  "caseWorkflowMacroId": 1,
  "caseWorkflowMacroName": "ExampleMacro",
  "case": {
    "id": 1,
    "entityAnalysisModelInstanceEntryGuid": "909c1cac-de56-4f02-ab55-f514df3c1f6d",
    "diaryDate": "2026-02-20T09:19:46.5265",
    "caseWorkflowGuid": "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
    "caseWorkflowStatusGuid": "faf5c83a-5c3e-4a62-b224-aea9e2e1efa6",
    "createdDate": "2026-02-20T09:19:46.526214",
    "locked": 0,
    "closedStatusId": 0,
    "caseKey": "AccountId",
    "diary": 0,
    "rating": 0,
    "caseKeyValue": "Test20",
    "caseWorkflowStatus": "First Line Review",
    "payload": "The full Archive payload on case creation, as printed for the Case Workflow Action above"
  }
}
```

Case Workflow Status, where the case is not nested and the payload is an element of the document itself:

```json
{
  "id": 1,
  "entityAnalysisModelInstanceEntryGuid": "909c1cac-de56-4f02-ab55-f514df3c1f6d",
  "diaryDate": "2026-02-20T05:19:46Z",
  "caseWorkflowGuid": "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
  "caseWorkflowStatusGuid": "3ff54c93-ad25-46e7-a1aa-9efa488f928e",
  "createdDate": "2026-02-20T09:19:46.526214",
  "locked": 0,
  "lockedUser": "",
  "closedStatusId": 0,
  "caseKey": "AccountId",
  "diary": 0,
  "rating": 1,
  "caseKeyValue": "Test20",
  "caseWorkflowStatusName": "Fraudulent",
  "payload": "The full Archive payload on case creation, as printed for the Case Workflow Action above"
}
```

Case Workflow Form, where the values submitted are in the element `payload` of the document, and the Archive payload is in the element `payload` of the case:

```json
{
  "id": 11,
  "createdDate": "2026-02-20T15:57:53.703344+02:00",
  "createdUser": "Administrator",
  "caseKey": "AccountId",
  "caseId": 1,
  "caseWorkflowFormId": 1,
  "caseKeyValue": "Test20",
  "payload": {
    "ExampleTextboxElement": "Test"
  },
  "caseWorkflowFormName": "ExampleForm",
  "case": {
    "id": 1,
    "entityAnalysisModelInstanceEntryGuid": "909c1cac-de56-4f02-ab55-f514df3c1f6d",
    "diaryDate": "2026-02-20T07:19:46",
    "caseWorkflowGuid": "957c6ab0-ef2b-4d06-a7a5-e923762dd917",
    "caseWorkflowStatusGuid": "3ff54c93-ad25-46e7-a1aa-9efa488f928e",
    "createdDate": "2026-02-20T09:19:46.526214",
    "locked": 0,
    "lockedUser": "",
    "closedStatusId": 0,
    "caseKey": "AccountId",
    "diary": 0,
    "rating": 1,
    "caseKeyValue": "Test20",
    "caseWorkflowStatus": "Fraudulent",
    "payload": "The full Archive payload on case creation, as printed for the Case Workflow Action above"
  }
}
```

## Validation

Validation takes place in the server upon saving the Status, Form, Action or Macro. The record is refused where:

* Enable http endpoint is on, and the Http endpoint is empty, is not a URL having its scheme, host and port written out literally, or the Http endpoint type is neither POST nor GET.
* Enable notification is on, and the Notification destination is empty, or the Notification type is neither Email nor SMS.
* For a Cases Workflow Action, the Http endpoint is longer than 2,048 characters, the Notification destination or Notification subject is longer than 1,024 characters, or the Notification body is longer than 100,000 characters.
* The SQL for the case is longer than 8,000 characters, is not a single read only SELECT of tables the author is permitted to read, or uses a parameter other than `@Id` and `@CaseKeyValue`.

The messages returned are as follows, each against the field to which it belongs:

| Condition                                             | Message                                                                                                                                                 |
|-------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------|
| Enable http endpoint on, Http endpoint empty          | A webhook URL is required when the HTTP endpoint is enabled.                                                                                            |
| Http endpoint not well formed                         | The HTTP endpoint must be an absolute http or https URL whose scheme, host and port are written out literally, with tokens only in the path and query. |
| Http endpoint type neither POST nor GET               | A valid HTTP method is required when the HTTP endpoint is enabled.                                                                                      |
| Enable notification on, Notification destination empty | A notification destination is required when notification is enabled.                                                                                   |
| Notification type neither Email nor SMS               | A valid notification type is required when notification is enabled.                                                                                     |
| SQL for the case longer than 8,000 characters         | The SQL is longer than 8,000 characters.                                                                                                                |
| SQL for the case not acceptable                       | The SQL was refused: followed by the reason.                                                                                                            |
| SQL for the case using another parameter              | The SQL uses the parameter @Name, which is not available here. Use @Id for the case or @CaseKeyValue.                                                   |

While a switch is off, the fields beneath it are neither presented nor checked. Upon a switch being turned on for a new record, the Http endpoint type is POST and the Notification type is Email until chosen otherwise. The Notification subject and Notification body may be left empty, and the Notification destination is checked upon saving for being present only, given that it may be a token whose value is not known until the dispatch.

## Permissions and security

The fields are saved under the permission of the record that carries them: 19, Read Write Cases Workflows Status; 21, Read Write Cases Workflow Form; 22, Read Write Cases Workflow Action; and 24, Read Write Cases Workflow Macro. Writing or changing the SQL for the case requires in addition permission 67, Author Case Workflow SQL, and a user without it may still save the record so long as the SQL is left as it was.

The dispatch itself takes place under the action of the end user in the case page, which requires permission 1, Read Write Case, and a role allocated to the Case Workflow and to the Form or Macro concerned, as described in [Case Management Security Policy](../SecurityPolicy/index.html). The credentials of the SMTP host and of Clickatell are held in Environment Variables only.
