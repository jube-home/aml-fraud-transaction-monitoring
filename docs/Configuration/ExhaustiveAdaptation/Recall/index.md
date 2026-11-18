---
layout: default
title: Exhaustive Adaptation Recall
nav_order: 7
parent: Exhaustive Adaptation
grand_parent: Configuration
---

# Exhaustive Adaptation Recall

Recall is the use of a trained model: the values of its variables for one transaction are presented to it, and it returns the score, a number between 0 and 1 where higher means the transaction more closely resembles the positive class. Training, described in the preceding pages, happens rarely and takes hours; recall happens for every transaction and takes a moment. This page describes how the score is obtained and how it is to be read.

For a score to be recalled as part of model invocation, three things must be true of the Exhaustive Adaptation: at least one model has been promoted, it is approved, and it is Active. In the example carried through this section, FraudScore having been trained, switched to Active and approved, which is the state in which a clean installation carries it, every transaction posted to the default model carries a value for `ExhaustiveAdaptation.FraudScore`.

Upon models having been promoted during Exhaustive training, they will be available for recall as part of model invocation, which is to say that the score is calculated for each transaction processed by the model and made available to Activation Rules.

Given the evolutionary nature of Exhaustive model training, and indeed retraining, explicit synchronisation is not required: the engine takes up the active promoted model of each Exhaustive Adaptation that is approved and Active at its next cycle.

## Recall as part of model invocation

Upon posting a transaction to the model, in the manner of the example JSON posted to `https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc`, each Exhaustive Adaptation is recalled in turn and its score is added to the exhaustiveAdaptation element of the response, under the name of the Exhaustive Adaptation, where Response payload is switched on:

```json
{
  "exhaustiveAdaptation": {
    "FraudScore": 0.10920667766782208
  }
}
```

{% comment %}TODO(verify): the response captured from a running system carried the element as ExhaustiveAdaptation, with a capital, where this page writes exhaustiveAdaptation. Owner to confirm which casing the page is to show.{% endcomment %}

The value shown is that returned upon a clean installation for the example JSON, posted once for an account without history. FraudScore was trained on the variables of the demonstration dataset, and the default model of a clean installation carries an Abstraction Rule of the same name for each of the twenty variables the active model reads, as set out under The inputs of FraudScore on a clean installation, henceforth the score moves with the transaction and with the history of its account. By way of example, six purchases by chip in the home country of an account, authenticated and spread over a day and more, are recalled between 0.09 and 0.00, whereas an account that makes one such purchase and is then seen at a cash machine in a high risk country, by swipe, without authentication and upon an unsafe terminal, eight times in forty minutes with three declines among them, is recalled between 0.89 and 1.00 from the first of those withdrawals onwards.

The score has been returned for use in Activation Rules, where it is referred to as `ExhaustiveAdaptation.Name`:

```vb
If (ExhaustiveAdaptation.FraudScore > 0.8) Then
  Return True
End If
```

The time allowed for the recall of all Exhaustive Adaptations of a model, for a single transaction, is set by the ExhaustiveRecallTimeout Environment Variable, which is 1000 milliseconds by default. An Exhaustive Adaptation not reached within that time is passed over for the transaction, and has no score.

### Matching variables

It might be observed that many of the elements used in model training do not have corresponding values in the model configuration. When matching variables with the model, in the event that the variable cannot be identified by its processing type (e.g. Abstraction) and name, it will fall back to the mean value having been created by statistics. Once a model is deployed it is advisable to lock all of the elements upon which the model depends, as falling back to the average will change model performance given how sensitive Neural Networks tend to be. A variable drawn from an Abstraction Calculation is matched against the Abstraction Calculations of the transaction. A variable drawn from an HTTP Adaptation, or from another Exhaustive Adaptation, has no value at the moment of recall, since recall takes place before those are computed, and always takes the mean; such variables are best left out of training.

Each value is normalised in the manner it was in training, and a Z score is held within the bound of the ExhaustiveZScoreClipBound Environment Variable, five standard deviations by default, such that an extreme value in a transaction cannot carry the score beyond anything the model saw in training.

### The inputs of FraudScore on a clean installation

The demonstration dataset describes card transactions, and the default model describes account transactions, henceforth each variable is computed from the field of the default model that is nearest in meaning. The Abstraction Rules are as follows, each searching upon AccountId where a window is given, and none being returned in the response, Response payload being off:

| Abstraction Rule                  | Value                                                                                                                                           |
|-----------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------|
| IsChip                            | 1 where ServiceCode is Chip, for the transaction being processed.                                                                               |
| IsManual                          | 1 where ServiceCode is Manual.                                                                                                                  |
| ATM                               | 1 where ChannelId is ATM.                                                                                                                       |
| CashTransaction                   | 1 where ChannelId is ATM or Cash.                                                                                                               |
| InPerson                          | 1 where ChannelId is Card, ATM or Cash.                                                                                                         |
| Authenticated                     | 1 where TwoFAResponseId is 1 or Is3D is true.                                                                                                   |
| HighRiskCountry                   | 1 where BillingCountry is a value of the List HighRiskCountries.                                                                                |
| TransactionAmt                    | The AmountEUR of the transaction being processed.                                                                                               |
| CountTransactionsDeclined1Day     | The count of transactions in one day whose ResponseCode is other than 0.                                                                        |
| CountTransactionsPINDecline1Day   | The count of transactions in one day whose ResponseCode is 55, being the code for an incorrect PIN.                                             |
| CountUnsafeTerminals1Day          | The count of transactions in one day for which Jailbreak, DebuggerAttached or SimulatorAttached is true.                                        |
| CountInternet1Day                 | The count of transactions in one day whose ChannelId is Internet.                                                                               |
| CountATM1Day                      | The count of transactions in one day whose ChannelId is ATM.                                                                                    |
| CountOver30SEK1Day                | The count of transactions in one day whose AmountEUR is greater than 3, being 30 Swedish kronor at ten to the euro.                              |
| SumTransactions1Day               | The sum of AmountEUR over one day, multiplied by ten, the dataset holding its sums in Swedish kronor at ten to the unit of its amounts.          |
| SumATMTransactions1Day            | The same sum, over the transactions whose ChannelId is ATM.                                                                                     |
| DifferentCountryTransactions1Week | The count of distinct BillingCountry over seven days.                                                                                           |
| DifferentCities1Week              | The count of distinct BillingCity over seven days.                                                                                              |
| CountSameMerchantUsedBefore1Week  | The count of transactions over seven days whose ToAccountId is that of the transaction being processed, which is itself counted.                |
| HasBeenAbroad                     | 1 where a transaction of the last seven days has a BillingCountry other than that of the earliest transaction of those seven days.              |

SumTransactions1Day, SumATMTransactions1Day and HasBeenAbroad are written in the Coder, as each returns a value the rule computes itself, and the remainder are written in the Builder. In order that seven days of history are available to the rules, the Cache TTL interval value of the default model, and the TTL of the AccountId Search Key, are seven days upon a clean installation. A transaction that does not carry one of the fields named, as the example JSON does not carry a ServiceCode of Chip, simply returns 0 for the rules that depend upon it.

## Direct recall via HTTP

An Exhaustive Adaptation can also be recalled on its own, without processing a transaction, by its Guid, which is shown in the Training Status group of the Exhaustive Adaptation. The names and values of the variables are posted as JSON:

```http
POST https://localhost:5001/api/Invoke/ExhaustiveSearchInstance/ac6d8875-b5fe-4e6e-8576-d4257cdc993d
Content-Type: application/json

{
  "Abstraction.CountTransactionsDeclined1Day": 6,
  "Abstraction.CountTransactions1Day": 9
}
```

The Guid in the example is that of FraudScore upon a clean installation. A variable of the model that is not posted takes its mean. The response carries the score, rounded to four decimals, alongside the explanation of it, shown here with one of its twenty relevance entries and with the content of nodeRelevance, which holds the relevance of every processing element layer by layer, left out:

```json
{
  "score": 0.032,
  "baseValue": 0.009437049651977922,
  "relevance": [
    {
      "name": "Abstraction.CountTransactionsDeclined1Day",
      "connectionWeight": 19778.401336028277,
      "lrpRelevance": 0.11829673360024213
    }
  ],
  "nodeRelevance": []
}
```

The following is returned where the recall cannot be made:

| Status | Reason                                                                                                                              |
|--------|-------------------------------------------------------------------------------------------------------------------------------------|
| 400    | The body is empty, exceeds the MaxInvokeControllerRequestBytes Environment Variable, is not well formed JSON, or has values of an invalid shape or type. |
| 403    | The caller does not belong to the tenant of the model.                                                                              |
| 404    | The Guid is not that of an Exhaustive Adaptation available to the caller, or the EnablePublicInvokeController Environment Variable is switched off. |
| 503    | The engine is not yet ready.                                                                                                        |

The same recall is used by the sliders described in [Promoted Model Testing](../TrainingPromotedModelTesting/index.html), and a file of many rows can be scored at once by a multipart POST to `/api/Invoke/ExhaustiveSearchInstance/{guid}/Batch`.

## Calibrated probabilities

A model promoted after calibration was introduced returns a calibrated probability, not the raw network output. At promotion, the scaling chosen as the Calibration method (Platt scaling being the traditional choice) is fitted on the validation split and stored with the promoted model, together with the deployment base rate (see Base rate in [Training Configuration](../TrainingConfiguration/index.html)), so a score of 0.004 means about a 0.4% chance of a positive case at that base rate. The same value is returned by the HTTP recall (rounded to four decimals), by the Exhaustive Adaptation in the invocation payload and by the simulation sliders.

It is worth dwelling upon why a calibrated score looks small. A model is trained upon data balanced to hold as many positive records as negative, as otherwise it would learn very little about the rare class, and left alone it would report scores as though half of all transactions were fraud. Calibration corrects the score back to the Base rate of the deployment. With a Base rate of 0.02, which is to say 2 transactions in 100 being positive, a transaction the model considers entirely ordinary is recalled well beneath 0.02, a transaction recalled at 0.02 is of average risk, a transaction recalled at 0.2 is ten times more likely to be positive than the average, and a score of 0.5 says the transaction is as likely to be positive as not, which at such a rate is rare and serious. A score is henceforth read against the Base rate and not against 0.5.

Rules that compare the score against a fixed value such as 0.5 will see different values for a newly promoted model: at a low base rate few cases reach 0.5. Activation Rule thresholds are to be reviewed after retraining, with the Threshold sweep chart described in [Promoted Model Performance](../TrainingPromotedModelPerformance/index.html) showing the consequence of each threshold.

{% comment %}TODO(verify): the page formerly advised the use of the minimum-risk threshold on the Threshold sweep, which supports costs. The chart endpoint accepts falseNegativeCost and falsePositiveCost, yet the Vue section How well it detects supplies neither, so the minimum-risk threshold is not drawn in the user interface. Owner to decide whether inputs for the costs are to be added.{% endcomment %}

Models promoted before calibration was introduced have no stored calibrator, keep returning raw scores and show "Not calibrated (promoted before calibration was introduced)" in the caption of the Calibration chart until they are retrained. Calibration is skipped, with the reason recorded, when the model has a hard output activation function, the validation split has a single class or fewer than 20 of either class, or the scores are constant.

## Permissions and security

Direct recall is subject to the EnablePublicInvokeController Environment Variable, is limited to the tenant of the caller, and is refused for a model that the roles of the caller may not use. An Exhaustive Adaptation whose stored model is incomplete or invalid is not scored, and the transaction is processed without its score.
