---
layout: default
title: Exhaustive Adaptation Training Promoted Models Performance
nav_order: 5
parent: Exhaustive Adaptation
grand_parent: Configuration
---

# Exhaustive Adaptation Training Promoted Model Testing

Testing answers the question a person naturally asks of a model before trusting it: if this transaction looked a little different, what would the model say. The charts of [Promoted Model Performance](../TrainingPromotedModelPerformance/index.html) describe a model across thousands of records at once, whereas here one imagined record is composed by hand, a variable at a time, and the score is observed to respond.

Promoted models can be manually tested, by presenting values of the variables to the model and observing the score returned. The intention of the manual simulation is to process several intuitive scenarios and observe the score to change equally intuitively, although keeping in mind that the model is largely biased to anomaly.

## Where it lives

Navigate through the menu to Models, open the model, then the Exhaustive Adaptation hub card, and open the Exhaustive Adaptation. Testing is the results section What if…, described there as Move a variable and watch the score, which is collapsed at first. The model tested is always the promoted model that is active, as described in [Promoted Model Performance](../TrainingPromotedModelPerformance/index.html).

## Moving a variable

The section opens with a narrative, written by the server, naming the variables to which the model is most sensitive, which is a sound place to begin. Beneath it the Score is shown in large figures, to four decimal places, above a control for each variable of the model:

* A continuous variable is presented as a slider, ranging from the minimum to the maximum of the variable in the sample, with the present value written beside its name.
* A string field of the payload that was exploded into indicator variables is presented as a single list of its values, rather than as a slider for each indicator.
* A variable having no range in the sample is not presented.

The sliders are set at first to the mean of each variable in the sample, such that the score first shown is that of an entirely typical case.

{% comment %}TODO(verify): the page formerly stated that the sliders are set at first to their optimal prescription values for the purpose of classifying positive. On this branch the starting value is the mean of the variable in the sample (controlsOf in Jube.App/Vue/src/exhaustive/model.ts, fed by GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery). Owner to decide whether starting from the prescription is to be restored.{% endcomment %}

Moving a slider, or choosing a value, presents the collated values of all controls to the model to return a score, a moment after the movement comes to rest. The higher the score, the more likely the simulation is a positive case (e.g. fraud). The score is the same value that is returned upon recall, which is to say a calibrated probability where the model has been calibrated, as described in [Recall](../Recall/index.html).

By way of example, upon What if… being opened for FraudScore every slider rests at the mean of its variable and the Score reads 0.1842, which is to say that an entirely ordinary transaction is recalled well beneath the Base rate of 0.49 measured from the demonstration dataset, close to half of whose records are positive. The narrative names Abstraction.Authenticated and Abstraction.CountUnsafeTerminals1Day as the variables to which the model is most sensitive. Upon the slider of Abstraction.CountUnsafeTerminals1Day being drawn from its mean towards its maximum, the others being left alone, the Score climbs, to 0.6089 at a value of 5 and to 0.7275 at the maximum, and upon the slider being returned the Score returns to 0.1842. A model behaving in this manner agrees with intuition; a Score that fell as the count of unsafe terminals rose, or that did not move at all, would be the counter intuitive behaviour that warrants a closer look at the [prescriptions](../TrainingPromotedModelPerformance/index.html) or the deactivation of the model.

A slider moves in steps of roughly one two-hundredth of the range of its variable, rounded to a 1, a 2 or a 5 in the leading digit, and the score is requested a quarter of a second after the last movement, such that drawing a slider across its range makes one request and not hundreds. The score shown remains that of the previous position until the new one arrives, and until a first score has arrived a dash is shown in its place. The positions of the controls are not stored: upon the section being closed and opened again, or the page being reloaded, every control returns to its starting value.

The section otherwise states There are no variables to vary for the live model. where there is no active promoted model or it has no variable with a range, The variables could not be loaded. where the controls cannot be read, and The score could not be calculated. beside the Score where a recall fails.

Given a score behaving counter intuitively or not flexibly enough, the promoted model can of course be deactivated, and the model with the next best score will be considered promoted in its place.

## Network with the relevance of this input

Beneath the controls the topology of the model is drawn: the variables as the input layer, each hidden layer of processing elements, and the output, which is the score. The diagram is redrawn with each score, and it is shaded by relevance, which is to say the extent to which each input and each processing element contributed to the score for the values presently set. It follows that the diagram is read alongside the sliders: upon moving a variable, it can be seen not only that the score changed, yet through which part of the network the change was carried. The diagram is drawn for a Neural Network, and not for a challenger of another family.

{% comment %}SHOT id=exhaustive-testing-what-if
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, open-first-row, open-section:What if…
     frame=section:What if…
     state=the What if… section of a trained demonstration adaptation, showing the narrative, the Score in large figures, and the sliders for each variable at their starting values
     conveys=a scenario is composed on the sliders and the score answers at once
     alt=A large score figure above a set of sliders, one for each variable of the model with its present value{% endcomment %}

{% comment %}SHOT id=exhaustive-testing-topology-relevance
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, open-first-row, open-section:What if…, scroll-to:Network with the relevance of this input
     frame=section:What if…
     state=the network diagram for the values set on the sliders, its elements shaded by how much each contributed to the score
     conveys=the network shows where in the model the present score came from
     alt=A diagram of a neural network with input variables, hidden layers and a single output, the nodes and joins shaded by how much each contributed to the present score{% endcomment %}

## Batch recall from a file

Where many scenarios are to be tested, Batch recall from a file scores every row of a file against the live promoted model at once, instead of one row at a time with the controls above. The file is not saved as training data.

A CSV, text or JSON file is chosen and uploaded, whereupon a summary gives the number of rows and the number scored, any variables of the model for which the file had no column are named, and the result is presented 25 rows to a page:

| Column     | Description                                                                                     |
|------------|-------------------------------------------------------------------------------------------------|
| Row        | The number of the row in the file.                                                              |
| Score      | The score recalled for the row.                                                                 |
| Base value | The score the model returns before the contribution of the values of the row is taken into account. |
| Error      | The reason the row could not be scored, where that is the case.                                 |

The file is chosen under the label CSV, text or JSON file and sent with Upload, only files ending `.csv`, `.txt` or `.json` being accepted, and an empty file or the absence of a file being refused by the page before anything is sent (Only .csv, .txt, .json files can be uploaded., The chosen file is empty., Choose a file first.). The first line of a CSV or text file is its headings, and a column is matched to a variable of the model where its heading is exactly the name of the variable (for example Abstraction.CountTransactionsDecline), or the name of the payload field for a string field exploded into indicator variables. A variable having no column takes the fall back described under Matching variables in [Recall](../Recall/index.html).

The summary reads, for example, 250 rows, 248 scored. A row is not scored, and carries a reason under Error, where one of its values could not be interpreted as the type of the variable (One or more mapped values could not be interpreted as the trained variable's type.) or where the recall was not a finite number. The Score and the Base value are shown to four decimal places. Where the result holds no rows the grid states No rows were scored.

The number of rows scored from one file is limited by the ExhaustiveBatchRecallMaxRows Environment Variable, which is 2000 by default. A file having more rows than the limit is refused as a whole, nothing being scored, with the reason given above the grid as The file has more than the permitted 2000 data rows. A file that cannot be read as CSV or JSON is refused in the same manner with the reason returned by the server, or otherwise The file could not be processed. The result is presented 25 rows to a page, with a pager beneath the grid. The result is held in the page only, and is gone upon the page being left.

{% comment %}TODO(verify): confirm on a running system that Base value is the expectation of the model against which relevance is measured, then correct the description of the Base value column if it differs.{% endcomment %}

## Permissions and security

Testing a promoted model, by the controls or by a file, is a recall of the Exhaustive Adaptation by its Guid, and is limited to the tenant of the user and to the models their roles may use. The file of a batch recall is read as data and is not stored.
