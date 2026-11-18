---
layout: default
title: Exhaustive Adaptation Training Configuration
nav_order: 2
parent: Exhaustive Adaptation
grand_parent: Configuration
---

# Exhaustive Adaptation Training Configuration

An Exhaustive Adaptation is the record that asks for a model to be trained, shows the training as it proceeds, and thereafter holds everything learned. Configuring one is a matter of answering three questions: where the training data is to come from, how a record is to be recognised as belonging to the positive class (the event of interest), and whether the score is to be stored and returned. The terms used on this page are introduced in [Concepts](../Concepts/index.html), and the example carried through it is the Exhaustive Adaptation named FraudScore, trained on the demonstration dataset.

There are two ways, which may be combined, of telling the training which records are positive. Anomaly asks for an anomaly detection model to find the records that least resemble the rest, and to treat those as positive, which requires no knowledge of past fraud whatsoever. Filter names the records already known to be positive, for example those carrying a Tag of Fraud, and is the stronger evidence where such feedback exists. Where both are on, the two are blended into a single positive class.

Exhaustive training is controlled by several Environment Variables, however the defaults are adequate in most use cases. Exhaustive models are otherwise given some basic training parameters which specify if Anomaly detection is to be used and the extent to which other class data is to be enriched into the anomalous classifications.

The output of Exhaustive training will, in the main, be a Neural Network having been trained by the Supervised Learning Levenberg Marquardt back propagation algorithm, notwithstanding that a classifier of another family, trained as a challenger, is promoted in its place where it is the more performant. As introduced beforehand, while there does exist a One Class Support Vector Machine for the purpose of anomaly detection, this is not directly recalled, rather it is used to derive class for supervised learning.

Keep in mind that processing of data in Jube will have created large amounts of data in the Archive database table and this data will be targeted for machine learning training.

## Where it lives

Navigate through the menu to Models, open the model, then the Exhaustive Adaptation hub card. Creating, changing, approving and deleting an Exhaustive Adaptation follows the common manner described in [Child Objects](../../../Navigation/ChildObjects/index.html), with the particulars that follow.

Before a model can be recalled it must be trained, and creating the Exhaustive Adaptation is also the instruction to train it. It is created in the status Awaiting server, and the training is picked up by the background thread of the engine, which looks for work each minute, irrespective of approval. Approval instead governs recall: only an Exhaustive Adaptation that is approved and Active is recalled as part of model invocation.

An Exhaustive Adaptation can only be changed while it is still in the status Awaiting server. Once the background thread has picked it up for training the configuration is fixed for the remainder of its lifecycle, and an update is refused as not found.

{% comment %}TODO(verify): Active is the exception in the server, being changed after training by PUT /api/ExhaustiveSearchInstance/{id}/active, yet no control in the Vue user interface calls that endpoint for an Exhaustive Adaptation. Confirm on a running system how an Exhaustive Adaptation is switched on or off once it has been picked up for training, and state the control here.{% endcomment %}

## Training data

Training data comes from one of three sources, chosen per Exhaustive Adaptation as the Data source:

* Archive (the default): the real processed transactions in the Archive database table.
* Demonstration dataset: a built-in sample dataset for testing and demonstration. Its rows are loaded once per model into the MockArchive database table using the same JSON structure as the Archive, storing the first column as a Tag by the same name and the rest of the columns as Abstraction Rules. Choosing it requires no configuration beyond the choice itself.
* Uploaded dataset: a CSV or JSON file uploaded by the user and confirmed with a column mapping, as described under Uploaded datasets.

The former UseMockDataExhaustive Environment Variable has been removed. A deployment that set it to True to obtain the demonstration dataset now selects Demonstration dataset on each Exhaustive Adaptation instead; existing Exhaustive Adaptations are migrated to Archive, so until one selects Demonstration dataset it no longer trains on the demo rows. The documentation as follows assumes that the demonstration dataset is being used.

## Fields

The form is arranged in the groups Identity, Training Data, Anomaly Detection, Training Data Filter, Categorical Variables, Output, Training Status (collapsed) and Audit (collapsed). The page accepts the following parameters:

| Value                              | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     | Example                                    |
|------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------|
| Name                               | The name of the Exhaustive Adaptation, by which rules refer to its score. Required, limited to 256 characters, and unique within the Model without regard to case.                                                                                                                                                                                                                                                                                                                                                                                                                                                             | FraudScore                                 |
| Active                             | Whether the Exhaustive Adaptation is recalled upon model invocation once trained. Active is off for a new Exhaustive Adaptation and must be switched on.                                                                                                                                                                                                                                                                                                                                                                                                                                                                        | True                                       |
| Data source                        | Archive, Demonstration dataset or Uploaded dataset, as above.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   | Demonstration dataset                      |
| Base rate                          | Optional. The fraction of real cases that are positive in deployment, greater than 0 and less than 1 (for example 0.02 for 2% fraud). Recalled scores are calibrated probabilities at this rate. When left empty the rate is measured from the population (the count of Archive or dataset records matching the class definition, or the flagged fraction when the class comes from anomaly detection) and recorded with its source on the promoted model. It is to be set where the deployment rate is known and the training sample is not representative of it.                                                             | Empty                                      |
| Calibration method                 | The manner in which the raw score of the model is turned into a probability upon promotion. Automatic, the default, fits Platt, Isotonic and Beta scaling on the validation data and keeps whichever scores best on the testing data. Platt fits a sigmoid of two parameters, and is best suited to small validation samples. Isotonic fits a monotone step function, being more flexible yet needing a larger validation sample. Beta fits an asymmetric curve of two parameters.                                                                                                                                              | Automatic                                  |
| Bootstrap                          | A switch, on by default, to indicate that further records are to be synthesised from the class in the minority, after class labelling, shuffling, normalising and balancing, and placed in the training data only. It is passed over where the minority class already reaches half of Bootstrap to.                                                                                                                                                                                                                                                                                                                              | True                                       |
| Bootstrap to                       | The number of records, real and synthesised together, that the balanced training data is to reach. Required where Bootstrap is on.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | 30000                                      |
| Bootstrap weight factor            | The weight, from 0 to 1, given to synthesised records relative to real records in the training of challenger models, where 1 is full weight. The evolution of Neural Network topology has no weighting by record, and always trains synthesised records at full weight.                                                                                                                                                                                                                                                                                                                                                          | 1                                          |
| Challenger model base probability  | The probability, from 0 to 1, that a given trial trains a challenger classifier of another family instead of evolving a Neural Network topology. It is the floor and not a fixed rate, as it rises towards 1 as the complexity of the best topology approaches the ExhaustiveTopologyComplexityLimit Environment Variable.                                                                                                                                                                                                                                                                                                       | 0.1                                        |
| Anomaly                            | A switch to indicate that an Anomaly Detection Model should be trained for the purpose of deriving a class for supervised Learning.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             | True                                       |
| Anomaly algorithm                  | The algorithm that labels the positive class. The default, One class support vector machine with Gaussian kernel, is the original behaviour. The alternatives are Isolation forest, Histogram based outlier score, Mahalanobis distance, Robust Mahalanobis distance, Principal component analysis, Gaussian mixture, k nearest neighbour distance, Local outlier factor, Marginal likelihood, Feature bagging ensemble, Consensus ensemble and Automatic, which evaluates a fixed set of candidates and records the choice. See the Exhaustive Anomaly Study for evidence on which algorithm suits which number of variables. | One class support vector machine (default) |
| Anomaly probability                | The One Class Support Vector Machine will be recalled to return a probability that the example fits the overall pattern of the data sample. The threshold is the amount whereby examples are classified as positive in the event that they are less than a certain probability. Required where Anomaly is on.                                                                                                                                                                                                                                                                                                                   | 0.02                                       |
| Anomaly parameters JSON            | Presented as a Variable reduction choice above a box of JSON. Variable reduction decides what is removed before the anomaly algorithm fits: None, Standard, which drops constant, near constant and mostly missing variables, exact duplicates and highly correlated groups (one representative is kept) and applies robust scaling, or Standard with principal components. Default for the algorithm is None for the One Class Support Vector Machine and Standard for every other algorithm. Reduction applies only to the anomaly step: the supervised search always sees every variable. The JSON otherwise holds optional algorithm parameters, reduction options and automatic evaluation options, and unknown properties are rejected. | Default for the algorithm                  |
| Filter                             | A switch to indicate that the class should be defined by a filter of records in the archive table whereby a match on the filter will return positive class.                                                                                                                                                                                                                                                                                                                                                                                                                                                                     | True                                       |
| Filter (the builder)               | The filter to apply to the Archive table to define positive class, built in the filter builder that is presented where Filter is on. The filter will also be used to negate the sample of negative class.                                                                                                                                                                                                                                                                                                                                                                                                                       | Tag Fraud = 1                              |
| Categorical options JSON           | Optional JSON that explodes the string fields of the payload into indicator variables for training. When absent, or not enabled, string fields are ignored.                                                                                                                                                                                                                                                                                                                                                                                                                                                                     | `{"enabled":true,"maxLevels":25}`          |
| Archive Key                        | Whether every recall of the Exhaustive Adaptation is stored to the reporting table for later analysis.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | False                                      |
| Response payload                   | Whether the score is returned in the response payload upon recall.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | True                                       |

A new Exhaustive Adaptation is offered with the following defaults: Data source of Archive, Calibration method of Automatic, Bootstrap on, Bootstrap to of 30000, Bootstrap weight factor of 1, Challenger model base probability of 0.1, Anomaly off, Anomaly algorithm of One class support vector machine, Anomaly probability of 0.02, and Filter, Archive Key and Response payload all off. It follows that an Exhaustive Adaptation created with nothing changed has no means of recognising a positive class, and at least one of Anomaly and Filter is to be switched on for the training to have anything to supervise; where neither yields a positive record, training concludes as Finished, no class available.

The Training Status group is read only and is written by the server: the Guid, the Status, the Anomaly summary, the Anomaly report JSON, Models, Models since best, Score, Topology complexity and Completed date. Notice the availability of a Guid, which can be used for direct recall of the model via HTTP as described in [Recall](../Recall/index.html).

The Categorical options JSON takes the following form, where exclude and include name fields of the payload:

```json
{
  "enabled": true,
  "maxLevels": 25,
  "minLevelCount": 20,
  "maxTotalIndicators": 200,
  "exclude": ["Payload.Reference"],
  "include": []
}
```

## Validation

An Exhaustive Adaptation is validated in the server, and is rejected where:

* The Name is empty, longer than 256 characters, or already in use within the Model.
* The Data source is Uploaded dataset and no confirmed dataset has been chosen, or a dataset is chosen for any other Data source.
* The Base rate is not greater than 0 and less than 1.
* The Calibration method is not one of Automatic, Platt, Isotonic or Beta.
* The Bootstrap weight factor, or the Challenger model base probability, is not between 0 and 1.
* The Anomaly probability is out of range. For the One Class Support Vector Machine it must be between 0 and 1 (a percentage such as 2% is entered as 0.02). For every other algorithm it is the contamination fraction, above 0 and at most 0.5: the highest scoring fraction of the sample is labelled positive class, and the number of rows flagged is recorded.
* The Anomaly parameters JSON is not valid, or carries a property that is not known.
* Filter is on and no filter has been built, which is to say an Exhaustive Adaptation cannot be saved with the class definition enabled and no filter defined.
* The filter is refused, as described under The class definition filter.
* The Categorical options JSON is not acceptable JSON.

## Training status

Upon creation the status is Awaiting server, with the background thread updating the status as it makes progress through the following stages:

| Stage | Status                                      |
|-------|---------------------------------------------|
| 0     | Awaiting server                             |
| 1     | Querying base case data                     |
| 2     | Calculating statistics for the base case    |
| 3     | Normalising data for unsupervised learning  |
| 4     | Unsupervised training                       |
| 5     | Unsupervised training recall                |
| 6     | Unsupervised training statistics            |
| 7     | Querying class filter data                  |
| 8     | Statistics for class filter data            |
| 9     | Shuffling data                              |
| 10    | Normalising data                            |
| 11    | Over sampling and symmetric sampling        |
| 12    | Storing final data                          |
| 13    | Correlation analysis                        |
| 14    | Multicollinearity analysis                  |
| 15    | Supervised learning                         |
| 16    | Finished                                    |

Training may otherwise conclude as Finished, no class available, where no record was classified as positive and there is henceforth nothing to supervise, as No data found for the configured data source, as Stopping then Stopped where it has been [stopped](../Stopping/index.html), or as Stopped for an unexpected reason.

In the example, FraudScore, as carried by a clean installation, is created with the Data source set to Demonstration dataset, Bootstrap switched off (the demonstration dataset holding nearly as many positive records as negative), Challenger model base probability set to 0, Anomaly left off, Filter switched on with the filter built as Tag.Fraud equal to Yes, and Response payload switched on. Within a minute the status moves from Awaiting server to Querying base case data, passes over the unsupervised stages quickly as Anomaly is off, and comes to rest for the greater part of the run at Supervised learning, stage 15, which is where the trials take place and where Models trained, Since best and Best score begin to move.

The status is presented in a band at the head of the record, which remains in view as the page is scrolled. The status is preceded by a coloured dot, which is read as follows:

| Colour | State                                                                                                   |
|--------|---------------------------------------------------------------------------------------------------------|
| Blue   | Training is awaiting the server or running, at any of stages 0 to 15.                                   |
| Green  | Finished, or Finished, no class available.                                                              |
| Amber  | Stopping.                                                                                               |
| Red    | Stopped, No data found for the configured data source, or Stopped for an unexpected reason.             |

While training is running or stopping the band shows a progress bar reading Stage n of 16, and it refreshes itself every five seconds, such that the page need not be reloaded to observe the most recent training status. The refreshing ceases once the status is Finished, Finished, no class available, Stopped or No data found for the configured data source, there being nothing further to observe. Should the Exhaustive Adaptation fail to load at all, the page states The adaptation could not be loaded. in place of the band. The band also carries:

| Value          | Description                                                                                              |
|----------------|----------------------------------------------------------------------------------------------------------|
| Models trained | The number of trials attempted so far.                                                                   |
| Since best     | The number of trials since the best, being the last to have been promoted. A small number means the search is still finding improvements, and a number running into the hundreds means that it has, in all likelihood, found what there is to find. |
| Best score     | The score of the best model promoted so far, to four decimal places. It is read on the scale described in [Concepts](../Concepts/index.html): the nearer to 1 the better, with exactly 1 to be treated with suspicion. |
| Updated        | The date and time the status was last written by the background thread, or a dash where it has yet to be written. |
| Completed      | The date and time training completed, shown once it has.                                                 |
| Refresh        | Reloads the status and everything beneath it, the results being extensive and not otherwise refetched. It is disabled for the moment that a reload is under way. |
| Stop           | Offered while training is awaiting the server or running, as described in [Stopping Training](../Stopping/index.html). |

Every anomaly run records a report (algorithm, parameters, variables kept and dropped with the reason, score summary, threshold applied and, for Automatic, every candidate's evaluation). A one line summary is shown as Anomaly detection in the Verdict section of the results, and in the Training Status group, and the full report is returned by the instance endpoint as AnomalyReportJson. The time allowed for the Automatic evaluation is set by the ExhaustiveAnomalyAutoBudgetSeconds Environment Variable.

## Training results

Until correlation analysis has finished (stage 14), the page states, beneath the band: Results appear once correlation analysis has finished (stage 14). The status above updates by itself. Thereafter the form itself folds away to a single summary line (for FraudScore: Demonstration dataset, class definition on, bootstrap off, anomaly detection off. Open to change.), which can be opened to read the configuration, and the results are presented in its place as six sections, with an index beside them to move between sections. The first four sections are open at first, and What if… and How it got here are collapsed at first, being the more demanding to draw. It follows from the stage at which the results appear that the sections are presented while Supervised learning is still under way, and they fill as models are promoted: the results are redrawn whenever the status or the Best score changes, and upon Refresh.

| Section             | Shows                                                                                                                                                                             | Described in                                                                       |
|---------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------|
| Verdict             | What happened, in words: a narrative of the training written by the server from the results, and the summary of anomaly detection.                                                | This page                                                                          |
| What it learned     | The variables and how the search improved: the Variable statistics and the Learning curve.                                                                                        | [Variable Statistics](../TrainingVariableStatistics/index.html) and this page      |
| Which model is live | The Promoted models, of which only one is active at a time.                                                                                                                       | [Promoted Model Performance](../TrainingPromotedModelPerformance/index.html)       |
| How well it detects | Whether the live model finds what it should, and at what cost, as a series of performance charts.                                                                                 | [Promoted Model Performance](../TrainingPromotedModelPerformance/index.html)       |
| What if…            | A control for each variable, to move a variable and watch the score, and Batch recall from a file. Collapsed at first.                                                            | [Promoted Model Testing](../TrainingPromotedModelTesting/index.html)               |
| How it got here     | Every candidate that was tried, as the Trials grid. Collapsed at first.                                                                                                           | This page                                                                          |

{% comment %}SHOT id=exhaustive-training-results-overview
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, open-first-row
     frame=record
     state=a trained Exhaustive Adaptation on the demonstration dataset, status Finished, showing the status band with Models trained, Since best and Best score, the folded configuration summary line, the index of sections and the Verdict section with its narrative
     conveys=the record of an Exhaustive Adaptation is its status band above a set of results sections, the configuration having folded away
     alt=An Exhaustive Adaptation record showing a status band reading Finished with the number of models trained and the best score, an index of six results sections and the Verdict section open with a written narrative{% endcomment %}

### Learning curve

Training can take several hours or days depending on dataset size. Throughout, trials continue to evolve better scores for simpler topology, and each time a trial beats the best score it is promoted. The Learning curve, in the section What it learned, plots that progress as two charts beneath a narrative:

* **Promotion history** plots the score of each promoted model against the date and time of its promotion, such that it can be seen how quickly the search arrived at its better models and for how long it has since gone without improvement.
* **Learning curve** plots the score against the sequence of promotions, the first promotion being at the left. As a promotion only takes place where the best score is beaten, the curve can only rise, and it is read for its shape: a curve that is still climbing steeply suggests that training has more to give, whereas a curve that has flattened suggests that further trials are unlikely to be rewarded.

By way of example, FraudScore was promoted at the first of its 50 trials, with a score of 0.78, and at the second, with a score of 0.80, and at none of the 48 that followed. The Learning curve henceforth shows a short climb between its two points, Promotion history shows both within the first minutes of the training, and Since best in the band reads 48. Read together, the three say the same thing: training could have been [stopped](../Stopping/index.html) at little cost. A longer training upon more data would ordinarily show more promotions, crowded towards the start, with the last of them far to the right.

{% comment %}SHOT id=exhaustive-training-learning-curve
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, open-first-row, open-section:What it learned, scroll-to:Learning curve
     frame=section:What it learned
     state=the Learning curve of a trained demonstration adaptation with several promotions, showing the narrative, the Promotion history chart and the Learning curve chart side by side
     conveys=the score rises with each promotion and then flattens, which is how the progress of training is judged
     alt=Two line charts side by side, the first plotting the score of each promoted model over time and the second plotting the score against the number of the promotion, both rising steeply and then flattening{% endcomment %}

### Trials

The section How it got here lists every trial, 25 to a page with a pager beneath the grid for moving between the pages, and in the order in which the trials were made, with the Model type (Neural Network (evolved topology), or the family of a challenger), the date and time Created, the Score to four decimal places, and whether it was Promoted, as a green Yes or a grey No. Before any trial has been recorded the grid states No trials have been recorded yet.

Beneath the grid the page states Choose Show evidence on a trial to see its model diagram and, for a challenger family, its evidence. Show evidence on a trial replaces that sentence with a heading naming the type and score of the trial (for example Neural Network (evolved topology) trial, score 0.9312), its Model diagram and, for a challenger family, the evidence recorded for it. Choosing Show evidence on another trial replaces the one shown. Where the evidence cannot be read the page states The evidence for this trial could not be loaded., and where the trials themselves cannot be read, The trials could not be loaded.

{% comment %}SHOT id=exhaustive-training-trial-evidence
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, open-first-row, open-section:How it got here, scroll-to:How it got here, click:Show evidence
     frame=section:How it got here
     state=the Trials grid with promoted and unpromoted trials of differing model types, and Show evidence chosen on a promoted Neural Network trial so that its Model diagram is drawn beneath the grid
     conveys=every candidate tried is kept, and the model of any one of them can be inspected
     alt=A grid of training trials listing model type, date, score and whether promoted, with the diagram of the network of the selected trial drawn beneath it{% endcomment %}

## The class definition filter

The filter is built in the browser, in a filter builder offering the fields of the chosen Data source, and three things are stored on the Exhaustive Adaptation: the builder definition (Filter JSON), the generated SQL condition (Filter SQL) and the SQL parameter values (Filter Tokens, a JSON array). Filter SQL and Filter Tokens are generated by the browser and stored as supplied, so the server never trusts them. They are checked twice.

When the Exhaustive Adaptation is saved the Filter SQL is refused unless it is a plain boolean expression of at most 65,536 characters. It is refused when it contains a semicolon, a backslash, a dollar sign, a NUL or other control character, a comment (`--`, `/*`, `*/`), an `E'...'` escape string, unbalanced parentheses or quotes, or any of these words outside a quoted value: select, union, intersect, except, from, into, copy, insert, update, delete, drop, alter, create, truncate, grant, revoke, call, do, execute, set, with, values, returning, window, offset, limit, order, group, having, for, any `pg_` or `lo_` function, `current_setting`, `version()` or a cast to `regclass`. Filter Tokens must be a JSON array of at most 1 MB.

When training runs, the condition is placed inside a fixed statement that reads only the Archive (or demonstration, or uploaded dataset) rows of the Exhaustive Adaptation's own model or dataset, and the whole statement is parsed again and refused unless it is a single read-only SELECT that calls no denied function (names beginning `pg_`, `lo_`, `set_config`, `dblink`, `repeat` and others). It runs in a read-only transaction with a 30 second statement timeout and a 5 second lock timeout, so a stored filter cannot modify data, read another table, or run unbounded.

Should a field named in a stored filter no longer exist, the builder states which fields were dropped upon the record being opened, as: These fields are no longer available and were removed from the filter: (the fields). Review this filter. Where a stored filter cannot be restored at all it states The saved filter could not be restored. Review this filter. The builder otherwise states No filterable columns are available for this data source yet. where the Data source offers no fields, and The filter builder could not be loaded. Refresh the page to try again. where it cannot be drawn. A condition left incomplete is marked within the builder, by Choose a field., A value is needed., A number is needed. or The group is empty. as the case may be.

## Uploaded datasets

When the Data source is Uploaded dataset a panel titled Training dataset is presented in place of the plain choice, to pick a confirmed dataset or to upload a CSV or JSON file, check how its columns are read and confirm the mapping. Upon a file being uploaded its columns are profiled, and a summary of the kind of file, the number of rows, columns and bytes, and the delimiter detected is given above a grid with a row for each column:

| Column          | Description                                                                                                           |
|-----------------|-----------------------------------------------------------------------------------------------------------------------|
| Include         | Whether the column is to be available to training.                                                                    |
| Column          | The heading of the column in the file.                                                                                |
| Type            | The type the column was read as.                                                                                      |
| Categorical     | Whether the column was read as categorical. A row can be expanded to show the distinct values and the count of each.  |
| Missing         | The number of missing values.                                                                                         |
| Min             | The smallest value.                                                                                                   |
| Max             | The largest value.                                                                                                    |
| Variable name   | The name the variable takes, which must start with a letter and use only letters, digits and underscores, and must not be shared with another included column. |
| Processing type | Payload, Dictionary, TTL counter, Sanction, Abstraction or Abstraction calculation.                                   |

The panel offers a Dataset list, reading Select a dataset until one is chosen, and Upload a dataset, which accepts a CSV or JSON file. Upon upload the panel asks: Review how each column is read, then confirm the mapping. Confirm mapping stores the mapping, whereupon the dataset can be selected, Cancel abandons the review, and Delete removes the dataset, having first asked Delete this dataset? with the explanation The dataset is removed. It cannot be deleted while an Exhaustive adaptation that uses it is still training.

The mapping is refused, with the reason given in the panel, as follows:

| Condition                                                               | Message                                                                                                   |
|-------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|
| No column is included.                                                  | Include at least one column.                                                                              |
| A variable name is not a letter followed by letters, digits and underscores. | (column): the variable name must start with a letter and use only letters, digits and underscores.   |
| Two included columns share a variable name.                             | (column): another included column already has this variable name.                                         |
| The file exceeds the permitted size.                                    | The file is larger than the permitted size.                                                               |
| The dataset is used by an Exhaustive Adaptation still in training.      | The dataset is still in use by an Exhaustive adaptation that has not finished training.                   |
| The user lacks the permission.                                          | You do not have permission to manage datasets.                                                            |

A failure of any other kind is reported as The dataset could not be uploaded., The mapping could not be saved. or The dataset could not be deleted. A file having no columns states This dataset has no columns., and a column without a breakdown of values states No distinct value breakdown for this column. upon being expanded. The positive class of an uploaded dataset is defined by the filter, built over the columns of the dataset, in the same manner as for the Archive.

{% comment %}SHOT id=exhaustive-training-dataset-mapping
     route=/models
     steps=open-default-model, open-hub:Exhaustive Adaptation, new, select:Data source=Uploaded dataset, select:Dataset=Demonstration, scroll-to:Training dataset
     frame=record
     state=a new Exhaustive Adaptation with Data source set to Uploaded dataset and the uploaded Demonstration dataset chosen, showing the summary of rows and columns and the column grid with Include, Type, Missing, Min, Max, Variable name and Processing type
     conveys=an uploaded file is profiled column by column, and the mapping is reviewed before the dataset can be trained upon
     alt=The Training dataset panel showing a summary of an uploaded CSV file above a grid listing each column with its type, missing values, minimum, maximum, variable name and processing type{% endcomment %}

The upload is bounded by the following Environment Variables (defaults shown): ExhaustiveDatasetUploadMaxBytes (50 MB), ExhaustiveDatasetUploadMaxRows (1,000,000) and ExhaustiveDatasetUploadMaxColumns (500). JSON is read with a nesting limit of 8 levels. A dataset can only be selected by an Exhaustive Adaptation of the same tenant, only one of a CSV and a JSON dataset may be selected, and a dataset used by an Exhaustive Adaptation that has not finished training cannot be changed or deleted.

## Environment Variables

The Environment Variables particular to Exhaustive training, all of which are prefixed Exhaustive, are set out with their defaults in [Environment Variables](../../../Concepts/EnvironmentVariables/index.html). Those most often of interest are ExhaustiveTrialsLimit (1000), being the number of trials after which training finishes, ExhaustiveMinVariableCount (5) and ExhaustiveMaxVariableCount (30), being the bounds of the number of variables selected at random for a trial, the shares of the data used for Training, Cross Validation and Testing (0.6, 0.2 and 0.2), ExhaustiveSimulationsCount (100), being the number of Monte Carlo simulations performed through a promoted model, and ExhaustiveZScoreClipBound (5), being the bound in standard deviations within which a Z score is held in training and in recall. EnableExhaustiveTraining (True) decides whether the background thread for Exhaustive training is started at all.

## Permissions and security

The Exhaustive Adaptation, its results, its charts and the datasets all require permission 16, Read Write Exhaustive. Making a promoted model active or inactive is held apart, such that a user who can only view training results cannot promote a model, and requires permission 14.

{% comment %}TODO(verify): permission 14 is named Read Write Abstraction Calculation in the permission catalogue, so the changing of the active promoted model is at present governed by the Abstraction Calculation permission (ExhaustiveSearchInstancePromotedTrialInstanceService.writePermissions). Owner to decide whether 14 is intended, or whether promotion is to have a permission of its own.{% endcomment %}

Every request is scoped to the caller's tenant. The trained network is stored as JSON, and when the engine loads it only types from the neural network library itself (plus numbers, text and arrays) may be named in the stored type information; anything else is ignored and the model does not score.
