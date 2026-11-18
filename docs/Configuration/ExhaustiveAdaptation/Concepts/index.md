---
layout: default
title: Exhaustive Adaptation Concepts
nav_order: 1
parent: Exhaustive Adaptation
grand_parent: Configuration
---

# Exhaustive Adaptation Concepts

Exhaustive Adaptation exists to answer a single question of every transaction: how likely is it that this transaction is one of the few that matter, be that fraud, money laundering or some other event of interest. It answers with one number between 0 and 1, henceforth the score, which an Activation Rule compares with a threshold in the manner of any other value. Everything else in this section of the documentation, which is to say the training, the charts and the testing, exists to produce that number, to show how far it can be trusted, and to explain how it was arrived at.

The name describes the manner in which the number is produced. A model is not designed by hand; instead a great many candidate models are trained automatically, one after another, each a little different from the last, and only a candidate that beats the best so far is kept. The search is exhaustive in the sense that it continues until a limit of candidates has been reached, and adaptation in the sense that the result is recalled alongside the rules of a model and can be retrained as the data moves on.

The pages of this section use one example throughout: the Exhaustive Adaptation named FraudScore, which a clean installation carries on the default model Detailed Account Financial Transactions. It has been trained on the Demonstration dataset over 50 trials, of which two were promoted, the better of the two being the active model with a score of 0.80, and it is approved and Active, such that every section described in these pages has something to show before any transaction has been processed. A further Exhaustive Adaptation of the same kind can be created with the Data source set to Demonstration dataset, and trained on a clean installation in the same manner.

## Terms

The following terms are used throughout, and each is explained at more length where it first matters:

| Term                | Meaning                                                                                                                                                                                                                          |
|---------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Variable            | One numeric input to the model, for example the count of declined transactions in a day. Variables are the values already created by a model, such as Abstraction Rules, and not the raw payload.                                  |
| Class               | What a record is, for the purposes of training: positive (the event of interest) or negative (everything else). The class is the answer the model is trained to predict.                                                          |
| Score               | The number between 0 and 1 that the model returns for a record. The nearer to 1, the more the record resembles the positive class.                                                                                                |
| Weight              | A multiplier inside the model. Training is the process of finding the weights that make the score agree with the class.                                                                                                           |
| Neural Network      | A model in which the variables pass through one or more hidden layers of processing elements on the way to the score, with a weight on every join.                                                                                |
| Topology            | The shape of a Neural Network: how many hidden layers, how wide each is, and which activation function (the curve applied by each processing element) is used. Topology complexity is the number of weights in that shape.         |
| Normalisation       | Putting every variable on the same scale before training, by expressing each value as a Z score, being the number of standard deviations it lies from the mean of the variable.                                                   |
| Trial               | One candidate model, trained from start to finish on a random selection of the variables.                                                                                                                                         |
| Promoted            | A trial whose score beat the best so far, and which was henceforth kept as a model that can be recalled. One promoted model is active at a time, and it is the active one that produces the score.                                |
| Challenger          | A trial that trains a classifier of another family (for example a Random Forest) in place of a Neural Network, and is promoted on the same basis.                                                                                 |
| Training, Cross Validation and Testing | The three parts into which the data is split, by default 60%, 20% and 20%, as explained under The 60/20/20 split.                                                                                              |
| Sensitivity         | The extent to which the score moves as one variable moves, the others held still.                                                                                                                                                 |
| Monte Carlo simulation | Presenting a large number of randomly generated records to a model in order to observe which of them it scores as positive.                                                                                                    |
| Prescription        | The description, variable by variable, of the simulated records a promoted model scored as positive, which is the model's own account of what a positive case looks like.                                                         |
| Calibration         | The adjustment of the raw output of a model such that the score can be read as a probability.                                                                                                                                     |
| Base rate           | The share of real cases that are positive in the deployment, for example 0.02 for 2%.                                                                                                                                             |

## Why not rules alone

Traditional risk management or anomaly detection is typically rule based. Examples of rules might be:

* More than 4 transactions in a one day period.
* More than 2 declined transactions in a one day period.
* More than 2 different transactions in a one day period.
* More than 5 transactions at the same merchant in a one day period.
* Transaction more than twice (x2) customer average transaction.

Jube of course has a highly capable rules engine and crafting such rules without programming is trivial.

While rules are effective, the manner in which they are derived is often quite anecdotal. In the above examples, deciding where to set the threshold is generally haphazard. Furthermore, over time, the number of rules tends to burgeon, and the thresholds are rarely adapted. It could be said that with rules the system's adaptation is neglected, and the rules' efficacy almost certainly wanes (putting aside the haphazard manner in which they were implemented in the first place).

Doubtless over time rules become impossible to manage and fail to adapt to new risk typology.

## From rules to a score

For the above example, consider the continuous values, or components, of the rules:

* 4 transactions.
* 2 declined transactions.
* 2 different transactions.
* 5 transactions at the same merchant.
* Customer average transaction.

Disregarding the threshold, it could be expressed as a model of a single layer, in which each of the five continuous values is an input, and all of the inputs are joined to a single output:

```text
Count of transactions in one day ──────────────┐
Count of declined transactions in one day ─────┤
Count of different transactions in one day ────┼──► Score
Count of transactions at the same merchant ────┤
Ratio of transaction to customer average ──────┘
```

In the above representation, all of the continuous values converge. The convergence would be expressed as a score. The score might be derived by a series of weights, which is to say that each input is multiplied by a weight of its own before the products are summed to the score:

```text
Score = (w1 × Count of transactions in one day)
      + (w2 × Count of declined transactions in one day)
      + (w3 × Count of different transactions in one day)
      + (w4 × Count of transactions at the same merchant)
      + (w5 × Ratio of transaction to customer average)
```

The establishment of weights in the above example would not be a significant step forward from rules if it were not for the manner in which these weights were established, and weights are established by machine learning algorithms (in Exhaustive the Levenberg Marquardt back propagation algorithm), on a wholly quantitative basis.

The final rule set out within Jube, might resemble:

* Transaction Score greater 0.8.

It follows that risk appetite can be set in the varying of just a single threshold value.

The internal weights are updated over time as new data emerges, representing the system's ability to adapt. The retraining of models, or the tweaking of weights is invoked on a batch basis and is most commonly approached in a regular regime of perpetual champion and challenger model creation and evaluation. The online adaptation of the weights is not all that achievable without some batch intervention, and even then, online learning tends to be an illusion crafted by the perpetual execution of the end to end training process.

## Supervised learning, by way of unsupervised learning

One of the challenges in the selection of Machine Learning algorithms is the extent to which the weights are created based upon feedback to the system (this is to say supervised) as opposed to identifying anomaly or clusters without feedback (this is to say unsupervised). A significant challenge in risk management systems is establishing and obtaining enough feedback data to be able to create a supervised model (which is to say fraud does not happen often, and when it does the typology moves on rapidly).

Jube takes a novel approach to machine learning, which is ultimately Supervised Learning, yet relying on Unsupervised Learning to create datasets with sufficient amounts of class data. The approach to Machine Learning taken by Jube is automated, and as introduced follows:

* Data will be extracted from Jube for the Abstraction Rules, which would be the continuous values set out in the above example.
* This data will be trained on the basis of anomaly, where highly anomalous events would be classified as being fraudulent.
* This data will be blended with any specific feedback data tagged, tending to be lower in volume, but salient.
* Many Neural Network topologies (this is the number of hidden layers, the width of the hidden layers and the activation function type) are trialled, evolved and trained based upon the data.

The outcome will be a complex neural network model created on a wholly quantitative basis, in which the inputs no longer converge directly upon the score, rather they pass through one or more hidden layers of processing elements, each of which is joined to every element of the layer before and the layer after, with a weight on every join:

```text
Inputs            Hidden layer 1      Hidden layer 2      Output

Variable 1 ──┐    ┌── Element ──┐     ┌── Element ──┐
Variable 2 ──┼────┼── Element ──┼─────┼── Element ──┼──►  Score
Variable 3 ──┤    ├── Element ──┤     └── Element ──┘
Variable n ──┘    └── Element ──┘
```

More hidden layers, and wider hidden layers, allow for more and more complex and comprehensive scenarios to be identified. Jube has its own training algorithm that is especially adept at identifying optimal model topology, although the actual weights are established by the Levenberg Marquardt Back Propagation algorithm. Identification of optimal topology for the anomalous class is more limited, and relies on a Single Class Support Vector Machine evaluation on all available variables.

The topology arrived at for a trained model is drawn in the user interface, as described in [Promoted Model Testing](../TrainingPromotedModelTesting/index.html).

{% comment %}SHOT id=exhaustive-concepts-evolved-topology
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, open-first-row, open-section:What if…, scroll-to:Network with the relevance of this input
     frame=section:What if…
     state=the network diagram of the live promoted model of a trained demonstration adaptation, showing the input variables, at least two hidden layers and the single output, with the joins shaded by relevance
     conveys=the model arrived at is a network of several layers, far beyond what could be drawn or weighted by hand
     alt=A diagram of a trained neural network with a column of input variables on one side, hidden layers of processing elements in the middle and a single score output, each element joined to those of the next layer{% endcomment %}

## The training algorithm

The exhaustive training algorithm takes the following steps in training a model:

* For the model, a list of variables that are eligible for Exhaustive Machine Learning are collated. Eligible variables are the values created by Abstraction Rules, Abstraction Calculations and TTL Counters, alongside the values returned by HTTP Adaptations, other Exhaustive Adaptations, Graph Recall and Svirfd. The raw payload is not processed as a continuous variable, whatever its data type, as the assumption is that at least some abstraction of that raw payload data be required (abstraction is seen as a way to introduce domain experience into the model). The single exception is that string fields of the payload can, upon request, be exploded into indicator variables, as described under Categorical options JSON in [Training Configuration](../TrainingConfiguration/index.html).
* For the eligible variables, a sample of data is extracted from the archive for the given filter specification. If a class variable is specified by filter, this will be excluded, as the class variable will be merged in a separate processing step. The sampling is subject to limits specified in the Environment Variables.
* Statistics are performed on the sample, noting again that the class data is not included. These statistics provide valuable insight, however are principally available for Z score variable normalisation which is an important training step.
* Using the statistics, data will be normalised if continuous variables, but not binary variables. A Z score is held within a bound, five standard deviations either side of the mean by default as set in the ExhaustiveZScoreClipBound Environment Variable, such that a single extreme value cannot overwhelm the model in training or in recall.
* If Anomaly has been selected then an anomaly detection model, a One Class Support Vector Machine by default, will be trained for all variables. Given that the data is sampled randomly across the whole population of data, this will create a reliable anomaly detection model that will return the probability that an example fits the general pattern expected of the sample. The One Class Support Vector Machine could on its own provide for adequate detection of anomaly, however, the Exhaustive model seeks to blend any such classifications with data fed back by the end users, henceforth the recall of the anomaly detection model serves as the class variable for a Neural Network model training.
* Given a filter being provided, records matching the filter are sampled for blending to the dataset that had otherwise been used on a one class basis. Data matching the filter is merged into the dataset. The dataset with merged class data is shuffled, however at this stage it is unlikely that the dataset will be symmetric around the class (in that there will be more records in one class than another).
* The dataset will be made symmetric randomly to ensure there is an even number of class records. Where Bootstrap is switched on, and the class in the minority is short of the number of records sought, further records are synthesised from that class and placed in the training data only, never in the data kept back for validation and testing.
* Correlations will be calculated using Spearman Rank Correlation for each variable versus the class variable to identify variables with the strongest linear association. The process of correlation is repeated for each variable, for each variable, to allow for detection of multi-collinearity. Given that Neural Networks are considered non linear and that the Exhaustive algorithm will select variables randomly to evolve topology on an exhaustive basis, the correlation analysis is largely for manual analytical insight.
* The Exhaustive algorithm will train a neural network on an exhaustive basis seeking the most powerful model (based upon the mean of correlation and classification accuracy between predicted vs actual) with the simplest topology, via the following steps, each pass of which is henceforth referred to as a trial:
    * The number of variables and the variables themselves will be selected randomly as per settings in the Environment Variables.
    * A search for the most performant Activation Function will be performed to be carried forward as follows.
    * A search for variables for which the model appears insensitive is performed, with those variables being removed given no reduction in performance.
    * Data is split between Training, Cross Validation and Testing as per settings in the Environment Variables.
    * Training data will be used to train a model on an evolving basis whereby processing elements then hidden layers will continue to be added until such time as there is no further improvement in performance.
    * Upon topology being selected, the model will be finalised and trained more intensively. Testing data will be used to validate the model and the performance of the model compared with current best, promoting if the model is more performant (although in absolute deadlock the least complex topology would win).
    * Final sensitivity analysis is performed and;
    * Using variable statistics and a normalised variable (i.e. Min, Max and Mode) Triangle Distribution, Monte Carlo simulation is performed through the model for the amount of times specified in the Environment Variables, with the descriptive statistics of the positive class values being stored.
* For a proportion of the trials, a classifier of another family (for example a Logistic Regression, a Random Forest or a Gradient Boosting classifier) is trained as a challenger in place of the evolution of a Neural Network topology, and is promoted on the same basis should it be the more performant. The proportion is set by Challenger model base probability, and rises as the best topology approaches the limit of complexity allowed.
* Upon promotion, the score of the model is calibrated, such that the value recalled is a probability at the base rate of the deployment, as described in [Recall](../Recall/index.html).

The trials continue until the number set in the ExhaustiveTrialsLimit Environment Variable has been reached, unless a model arrives at a total fit beforehand (which is probably an over fit), or the training is [stopped](../Stopping/index.html).

## The 60/20/20 split

A model that is examined on the same records it was trained upon will always flatter itself, as it may have memorised those records rather than learned anything that holds for records it has not seen. It follows that the data of each trial is divided into three parts, by default 60%, 20% and 20%, each with a purpose of its own:

| Part             | Share | Purpose                                                                                                                                                                                             |
|------------------|-------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Training         | 60%   | The records from which the weights are established. Synthesised records, where Bootstrap is on, are placed only here.                                                                               |
| Cross Validation | 20%   | Records the weights never learn from, used during the trial to decide whether a wider or deeper topology is in fact an improvement, and upon promotion to fit the calibration of the score.            |
| Testing          | 20%   | Records kept back until the trial is finished, upon which the score of the trial, and every performance chart of a promoted model, is computed.                                                      |

The shares are set by Environment Variables, as described in [Training Configuration](../TrainingConfiguration/index.html). Keeping in mind that the Testing part has had no influence upon the model, the performance reported for a promoted model is a fair estimate of how the model would behave upon new transactions, which is the reason every figure in [Promoted Model Performance](../TrainingPromotedModelPerformance/index.html) is drawn from it.

## The score of a trial, and what promoted means

Each trial ends with a score of its own, being the mean of two measures taken upon the Testing part: the share of records classified correctly, and the Spearman Rank Correlation between the predicted and the actual class. Both lie between 0 and 1 for a model of any use, henceforth so does the score: a score near 0.5 is little better than guessing, a score above 0.9 is a strong model, and a score of exactly 1 is a total fit that is more likely to be an over fit than a perfect model. This score of a trial is not to be confused with the score recalled for a transaction; the former says how good a model is, the latter is what the model says about one record.

A trial is promoted where its score beats the best score so far. In the example, the first trial of FraudScore is promoted by default, as there is nothing to beat; a later trial scoring higher is promoted in its turn and becomes the active promoted model, the earlier one being kept yet no longer active. It follows that the promoted models form a short history of ever better models, and that at any moment exactly one of them, ordinarily the best, is the model that is recalled. Trials that did not beat the best are recorded, and are otherwise discarded.

## Where to go next

Having trained FraudScore as described in [Training Configuration](../TrainingConfiguration/index.html), the results are read in the following order: the [Variable Statistics](../TrainingVariableStatistics/index.html) show what the data looks like and which variables are associated with the class, [Promoted Model Performance](../TrainingPromotedModelPerformance/index.html) shows how well the active model detects and at what cost, [Promoted Model Testing](../TrainingPromotedModelTesting/index.html) allows a scenario to be composed by hand and the score observed, and [Recall](../Recall/index.html) describes how the score reaches an Activation Rule.

Every step of the training is inserted into tables in the database, and these can grow very large. Keeping in mind, putting aside Deep Learning, topology design is one of the hardest parts of the machine learning discipline; the intention of storing such large amounts of data about the exploration of the model is to be able to use this data in the future to make estimations about the most appropriate model topology based on the statistics calculated at the start of the process, thus improving generalisation while dramatically reducing training time.

Keeping in mind that the model is set to identify anomaly, it should allow for anomalous – the unknown unknown - events to be reliably identified, which is something that cannot reliably be done with rules. As the score can be recalled in real-time, it naturally follows that the transaction can be declined on the basis of overly anomalous nature.
