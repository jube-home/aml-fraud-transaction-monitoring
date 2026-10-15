/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.AskJooby.Workflows.Models;

namespace Jube.AskJooby.Workflows
{
    public static class AdvancedAnalyticsTrainingWorkflows
    {
        private const string InstanceInspector = "advanced-analytics-instance-inspector";
        private const string InstanceAuthor = "advanced-analytics-instance-author";
        private const string TrainingData = "advanced-analytics-training-data";
        private const string ClassifierProbabilistic = "advanced-analytics-training-classifier-probabilistic";
        private const string ClassifierMargin = "advanced-analytics-training-classifier-margin";
        private const string ClassifierTrees = "advanced-analytics-training-classifier-trees";
        private const string ModelInspector = "advanced-analytics-model-inspector";
        private const string ModelEvaluation = "advanced-analytics-model-evaluation";
        private const string ModelAuthor = "advanced-analytics-model-author";
        private const string EntityAnalysisModelInspector = "entity-analysis-model-inspector";
        private const string ExhaustiveInstanceAuthor = "exhaustive-search-instance-author";
        private const string ExhaustiveEvidence = "exhaustive-promotion-curves-and-history";

        public static readonly IReadOnlyList<GraphWorkflow> All =
        [
            new(
                1,
                "train-advanced-analytics-trial-model",
                "Train a Trial Advanced Analytics Model",
                "Fitting a single model on a cached table to see what it finds, rather than inspecting, "
                + "comparing or promoting a model version that already exists.",
                "An Advanced Analytics instance, a cache pointer with a resolved label and predictor set, "
                + "and one submitted training run under that instance, read back by id.",
                "Use when the ask is to actually train something and see evidence back, and the caller "
                + "has named a single family (or named none, which defaults to logistic regression as "
                + "the cheapest first trial). If the ask is open-ended -- no family named, explore and "
                + "find the best one -- that is "
                + "search-and-promote-best-advanced-analytics-classifier, not this: that is the only one "
                + "of the three training graphs that also races an Exhaustive Adaptation, and it does so "
                + "because the caller asked for exactly that, not because every training ask should. If "
                + "a model version already exists and the ask is to inspect, compare or promote it, that "
                + "is the page slice under model-risk.advanced-analytics, not this.",
                [
                    "train a logistic regression model to find fraud",
                    "build me a model on the demo dataset",
                    "try a trial model on this data",
                    "train a classifier on the cached table",
                    "fit a model and tell me how it did",
                    "train an advanced analytics model"
                ],
                [
                    new GraphWorkflowNode(
                        "reuse-check",
                        "Check for an existing Advanced Analytics instance",
                        "List the instances already in this tenant before creating one. Every submission is "
                        + "filed under a numeric instance id with no default, so this is the step that resolves "
                        + "it rather than leaving it to be discovered by a refused submission three steps later.",
                        InstanceInspector,
                        ["AdvancedAnalyticsInstanceList"],
                        ["InstanceId"],
                        [],
                        "Skipping straight to submission fails outright on the missing field. Skipping to "
                        + "creation instead risks a duplicate-name refusal if one close enough already exists, "
                        + "and splits one tenant's training history across two instances for no reason."),
                    new GraphWorkflowNode(
                        "create-instance",
                        "Create a trial instance if the reuse check found none",
                        "Only when the previous step found zero instances. A write, so it needs the caller's "
                        + "explicit confirmation before it runs. It starts unallocated, which is correct for a "
                        + "trial: allocating it to a live Model is a separate, later decision under a separate "
                        + "permission.",
                        InstanceAuthor,
                        ["AdvancedAnalyticsInstanceCreate"],
                        ["InstanceId"],
                        [],
                        "Nothing downstream can be submitted without a resolved instance id, and inventing one "
                        + "files the run under an instance that does not exist."),
                    new GraphWorkflowNode(
                        "prepare-training-data",
                        "Turn the cached table into training data",
                        "Point this at the cache pointer already held, from GetDemoDataset or a RunSqlQuery "
                        + "result, and name the label column and the predictor columns by their real names, "
                        + "never invented ones. Which columns are actually predictive is your own judgement to "
                        + "make from InspectDataSchema and PreviewDataRows, not a fixed list to take on faith "
                        + "from the caller or from an example -- include or drop a column because you looked at "
                        + "it and decided, not because it was the first list handed to you. Returns a new cache "
                        + "pointer and a resolved label index, both to be carried unchanged into the submit "
                        + "step.",
                        TrainingData,
                        ["AdvancedAnalyticsTrainingDataPut"],
                        ["TrainingDataPointer", "LabelColumn", "PredictorNames"],
                        [],
                        "A column renamed or guessed here trains a model on data that is not what the caller "
                        + "described, and the mistake is invisible until someone checks the coefficients "
                        + "against real column names."),
                    new GraphWorkflowNode(
                        "submit-trial-model",
                        "Submit one trial model against the prepared data",
                        "Logistic regression is the default first trial: data asked to be trained for fraud, or "
                        + "any other 0/1 outcome, reads straight into it, and it is the cheapest classifier to "
                        + "read evidence from. Carry the instance id and the pointer, label column and predictor "
                        + "names from the earlier steps unchanged.",
                        ClassifierProbabilistic,
                        ["SubmitLogisticRegressionTraining"],
                        ["TrainingRunId"],
                        ["InstanceId", "TrainingDataPointer", "LabelColumn", "PredictorNames"],
                        "This call is refused outright without a resolved instance id, and that refusal is the "
                        + "platform working correctly, not a fault: the instance is a precondition of the run, "
                        + "not an output of it."),
                    new GraphWorkflowNode(
                        "observe-run",
                        "Poll the run and read its evidence",
                        "Poll GetAdvancedAnalyticsTrainingRunStatus by the run id until it reports Succeeded, "
                        + "Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never "
                        + "promotes a version to live; that is a separate decision behind a separate permission.",
                        ClassifierProbabilistic,
                        ["GetAdvancedAnalyticsTrainingRunStatus", "GetAdvancedAnalyticsTrainingRunResult"],
                        ["TrainingRunEvidence"],
                        ["TrainingRunId"],
                        "Reporting success from the submit call alone, before the run has actually finished, "
                        + "reports an opinion as a result.")
                ],
                "An Advanced Analytics instance must exist before any submission: it is a required, "
                + "caller-supplied field with no default, so the order above is the only one that does not end "
                + "in a dead refusal."),
            new(
                2,
                "trial-several-advanced-analytics-classifiers",
                "Trial Several Advanced Analytics Classifiers and Pick the Best",
                "Fitting several classifier families on the same cached table against the same label and "
                + "predictor set, then reading each one's held-out evidence back to name the strongest, rather "
                + "than committing to one algorithm before any of them have actually been tried.",
                "One Advanced Analytics instance and one resolved label/predictor set, carried unchanged into "
                + "three independent classifier submissions -- one per classifier sub-kind (probabilistic, "
                + "margin, tree-ensemble) -- each submitted and polled on its own, then weighed against the "
                + "other two on held-out metrics by a read-only comparison step.",
                "Use when the ask is to trial several or many named models and pick the best, with no single "
                + "algorithm named -- 'train a logistic regression model' or any other single-algorithm ask is "
                + "the one-model graph (`train-advanced-analytics-trial-model`), not this. If the ask is "
                + "open-ended instead -- no family named, explore and find the best one -- that is "
                + "search-and-promote-best-advanced-analytics-classifier, not this: that is the only one of "
                + "the three training graphs that also races an Exhaustive Adaptation, because the caller "
                + "asked for exactly that. This graph never promotes a version to live: naming the winner "
                + "here is advisory, the same separate decision behind the same separate Promote Model "
                + "permission as the one-model graph.",
                [
                    "trial several models and pick the best one",
                    "try many models, not just logistic regression, and find the best",
                    "train a few classifiers and tell me which one wins",
                    "compare a few different models on the demo dataset",
                    "build more than one model and pick the strongest",
                    "find the best classifier for fraud, try a few kinds"
                ],
                [
                    new GraphWorkflowNode(
                        "reuse-check",
                        "Check for an existing Advanced Analytics instance",
                        "List the instances already in this tenant before creating one, exactly as the "
                        + "one-model graph does -- every family below files under the same single instance.",
                        InstanceInspector,
                        ["AdvancedAnalyticsInstanceList"],
                        ["InstanceId"],
                        [],
                        "Skipping straight to submission fails outright on the missing field, for every family "
                        + "below, not just the first one tried."),
                    new GraphWorkflowNode(
                        "create-instance",
                        "Create a trial instance if the reuse check found none",
                        "Only when the previous step found zero instances. A write, so it needs the caller's "
                        + "explicit confirmation before it runs, and it starts unallocated.",
                        InstanceAuthor,
                        ["AdvancedAnalyticsInstanceCreate"],
                        ["InstanceId"],
                        [],
                        "Nothing downstream can be submitted without a resolved instance id, and inventing one "
                        + "files every one of the three runs under an instance that does not exist."),
                    new GraphWorkflowNode(
                        "prepare-training-data",
                        "Resolve the label and predictor columns, once",
                        "Point this at the cache pointer already held, from GetDemoDataset or a RunSqlQuery "
                        + "result, and name the label column and the predictor columns by their real names, "
                        + "never invented ones. Which columns are actually predictive is your own judgement to "
                        + "make from InspectDataSchema and PreviewDataRows, not a fixed list to take on faith. "
                        + "The cache pointer and label column this returns carry directly into every one of "
                        + "the three submissions below -- all three take the same cache pointer, so the rows "
                        + "are read once from the cache rather than three times.",
                        TrainingData,
                        ["AdvancedAnalyticsTrainingDataPut"],
                        ["TrainingDataPointer", "LabelColumn", "PredictorNames"],
                        [],
                        "A column renamed or guessed here trains every one of the three families on data that "
                        + "is not what the caller described, and the mistake is invisible until someone checks "
                        + "a coefficient or a split against real column names."),
                    new GraphWorkflowNode(
                        "submit-logistic-regression",
                        "Submit the probabilistic-family candidate",
                        "Logistic regression fits the log-odds directly and is the cheapest of the three to "
                        + "read evidence from. Carry the instance id and the pointer, label column and "
                        + "predictor names from the earlier steps unchanged -- this reads training rows by "
                        + "cache pointer, the same pointer every other family below reuses.",
                        ClassifierProbabilistic,
                        ["SubmitLogisticRegressionTraining"],
                        ["LogisticRegressionRunId"],
                        ["InstanceId", "TrainingDataPointer", "LabelColumn", "PredictorNames"],
                        "This call is refused outright without a resolved instance id, and that refusal is the "
                        + "platform working correctly, not a fault."),
                    new GraphWorkflowNode(
                        "submit-neural-network-classifier",
                        "Submit the margin-family candidate",
                        "A small feedforward network, Platt-calibrated to a probability, lets a boundary curve "
                        + "rather than sit on a single straight line. Carry the instance id and the same cache "
                        + "pointer and label column `prepare-training-data` resolved, unchanged -- this family's "
                        + "submission takes a cache pointer exactly like the probabilistic family above, so the "
                        + "rows are never read back out of the cache a second time.",
                        ClassifierMargin,
                        ["SubmitNeuralNetworkClassifierTraining"],
                        ["NeuralNetworkClassifierRunId"],
                        ["InstanceId", "TrainingDataPointer", "LabelColumn", "PredictorNames"],
                        "Reading the rows back out of the cache and passing them inline here is pure waste: "
                        + "this family's submission takes the same cache pointer as every other family in this "
                        + "workflow, so there is never a reason to fetch the rows a second time."),
                    new GraphWorkflowNode(
                        "submit-random-forest-classifier",
                        "Submit the tree-ensemble candidate",
                        "An ensemble of bootstrap-sampled decision trees, Platt-calibrated the same way as the "
                        + "other two. Carry the instance id and the same cache pointer and label column "
                        + "unchanged -- this family's submission takes a cache pointer exactly like the other "
                        + "two, so all three submissions below train on the one set of cached rows.",
                        ClassifierTrees,
                        ["SubmitRandomForestClassifierTraining"],
                        ["RandomForestClassifierRunId"],
                        ["InstanceId", "TrainingDataPointer", "LabelColumn", "PredictorNames"],
                        "Treating the three submissions as unable to share one cache pointer is the exact "
                        + "mistake that used to make 'trial several models' look unsupported: every classifier "
                        + "family's submission tool takes the same cache pointer, so a caller who falls back to "
                        + "reading rows inline for anything but logistic regression is doing needless, wasteful "
                        + "work."),
                    new GraphWorkflowNode(
                        "observe-logistic-regression",
                        "Poll the probabilistic-family run and read its evidence",
                        "Poll by the run id until it reports Succeeded, Failed or Cancelled, then read the "
                        + "result. Training never promotes a version to live.",
                        ClassifierProbabilistic,
                        ["GetAdvancedAnalyticsTrainingRunStatus", "GetAdvancedAnalyticsTrainingRunResult"],
                        ["LogisticRegressionEvidence"],
                        ["LogisticRegressionRunId"],
                        "Reporting success from the submit call alone, before the run has actually finished, "
                        + "reports an opinion as a result."),
                    new GraphWorkflowNode(
                        "observe-neural-network-classifier",
                        "Poll the margin-family run and read its evidence",
                        "Same polling discipline as the probabilistic family, against this family's own run id.",
                        ClassifierMargin,
                        ["GetAdvancedAnalyticsTrainingRunStatus", "GetAdvancedAnalyticsTrainingRunResult"],
                        ["NeuralNetworkClassifierEvidence"],
                        ["NeuralNetworkClassifierRunId"],
                        "A run observed by the wrong family's agent, or not polled to a terminal status, "
                        + "leaves the final comparison short one real candidate."),
                    new GraphWorkflowNode(
                        "observe-random-forest-classifier",
                        "Poll the tree-ensemble run and read its evidence",
                        "Same polling discipline again, against the tree-ensemble family's own run id.",
                        ClassifierTrees,
                        ["GetAdvancedAnalyticsTrainingRunStatus", "GetAdvancedAnalyticsTrainingRunResult"],
                        ["RandomForestClassifierEvidence"],
                        ["RandomForestClassifierRunId"],
                        "Comparing only two of the three candidates because the third was never polled to a "
                        + "terminal status quietly narrows 'pick the best of several' to 'pick the best of "
                        + "whichever finished first'."),
                    new GraphWorkflowNode(
                        "select-best-model",
                        "Compare the three candidates and name the strongest",
                        "List the instance's models to find the three just trained, read each one's "
                        + "promotion evidence, and rank them on held-out figures -- AUC, average precision, log "
                        + "loss, Brier score, expected calibration error -- never on training-split numbers, "
                        + "which reward whichever family memorised its own rows hardest. This is a read: naming "
                        + "a winner here promotes nothing. Making that version live is the separate decision "
                        + "behind the Promote Model permission, same as the one-model graph.",
                        ModelInspector,
                        ["AdvancedAnalyticsModelList", "AdvancedAnalyticsModelGetPromotionEvidence"],
                        ["BestTrainedModel"],
                        [
                            "LogisticRegressionEvidence", "NeuralNetworkClassifierEvidence",
                            "RandomForestClassifierEvidence"
                        ],
                        "Naming a winner before all three runs have actually finished, or comparing them on "
                        + "whichever metric happens to favour the family already expected to win, reports a "
                        + "foregone conclusion as a finding.")
                ],
                "The instance and the resolved cache pointer, label column and predictor names must exist "
                + "before any family is submitted, because every submission is filed under the instance and "
                + "reads the same cached rows. After that the three submissions are independent of one "
                + "another and may be filed in any order -- they read the same cache pointer concurrently "
                + "without competing for it -- but the comparison step needs every one of them polled to a "
                + "terminal status first, because comparing on a run still in progress reports a guess as a "
                + "result."),
            new(
                3,
                "search-and-promote-best-advanced-analytics-classifier",
                "Search Every Advanced Analytics Classifier Family, Pit the Winner Against Exhaustive, and "
                + "Promote Whichever Actually Wins",
                "Running an exhaustive, cross-validated comparison across every classifier family at once, "
                + "training one confirmatory version of whichever family wins, racing an Exhaustive Adaptation "
                + "against it on the same data as an independent second contender rather than a formality, and "
                + "promoting whichever of the two actually scores better -- rather than trialling a fixed few "
                + "families, treating the Advanced Analytics search as the only source of truth, or leaving the "
                + "strongest candidate as an unpromoted recommendation nobody acts on.",
                "One Exhaustive Adaptation created cold against the same underlying data and left to evolve "
                + "its own topology in the background, started first and before anything else once the Model "
                + "id is resolved, because it is usually the eventual champion and its evolution is the "
                + "slowest part of this graph. Alongside it: one Advanced Analytics instance and one resolved "
                + "label/predictor set, one stateless cross-validated comparison across up to twelve "
                + "classifier families on identical folds naming a winner, one confirmatory training run "
                + "submitted under whichever family it named, and one promotion decided by comparing the two "
                + "side's held-out evidence -- each step a precondition of the next, with nothing here "
                + "promoted silently and no side favoured by default except the tie-break toward simplicity.",
                "Use when no algorithm is named and the ask is genuinely open-ended -- 'I don't know which, "
                + "just explore and do it for me', 'try everything and pick the best one' -- never when a "
                + "short, named list is being trialled instead (`trial-several-advanced-analytics-classifiers`) "
                + "or a single algorithm is already chosen (`train-advanced-analytics-trial-model`). This is "
                + "also the only one of the three advanced analytics training graphs that ends in an actual "
                + "promotion rather than an advisory recommendation, and that promotion still needs the "
                + "caller's explicit confirmation and the Promote Model permission, exactly as it would from "
                + "the model slice directly -- naming a winner earlier in this graph authorises nothing by "
                + "itself, and neither does training it for real: only the bout against Exhaustive decides "
                + "which side's permission gets exercised.",
                [
                    "I don't know which model to use, just explore and find the best one",
                    "try everything and pick the best model for fraud",
                    "search all the classifier families and promote whichever wins",
                    "I don't care which algorithm, find me the strongest one and make it live",
                    "run an exhaustive search over every classifier and promote the winner",
                    "build me a model, explore the options yourself, and go with whatever is best"
                ],
                [
                    new GraphWorkflowNode(
                        "resolve-comparison-model",
                        "Resolve the Model the Exhaustive side of the bout registers under, first",
                        "Do this before the Advanced Analytics instance or data steps below: an Exhaustive "
                        + "Adaptation is always registered under a Model, Advanced Analytics needs no Model id "
                        + "at all, and Exhaustive is usually the eventual champion of the two, so the side that "
                        + "takes longest to evolve is the side that starts first. Find the Model the caller is "
                        + "already working against, or the one the demo or queried data came from, rather than "
                        + "inventing or skipping this.",
                        EntityAnalysisModelInspector,
                        ["EntityAnalysisModelList", "EntityAnalysisModelGet"],
                        ["EntityAnalysisModelId"],
                        [],
                        "Guessing a Model id, or carrying one left over from an earlier task, registers the "
                        + "Exhaustive side of the bout against the wrong population, and the two contenders "
                        + "end up scored on different data without either side's evidence saying so."),
                    new GraphWorkflowNode(
                        "exhaustive-comparison-instance",
                        "Create the Exhaustive contender cold, on the same underlying data, before anything "
                        + "on the Advanced Analytics side",
                        "Register one Exhaustive Adaptation under the resolved Model and leave it to evolve in "
                        + "the background while every step below runs -- always start this one first, not in "
                        + "whatever order is convenient, because its topology evolution is the slowest part of "
                        + "this graph and Exhaustive is usually the eventual champion of the bout; starting it "
                        + "last or skipping it, and only running the Advanced Analytics side's own ad hoc "
                        + "search or tuning tools, is exactly the shortfall this node exists to prevent. Match "
                        + "the data source to the Advanced Analytics side -- "
                        + "Archive when training is against real processed transactions, the Model's own "
                        + "demo dataset (DataSourceId 1) when this bout runs on demonstration data. Exhaustive "
                        + "has no cache-pointer ingestion of its own, so the two sides read the same "
                        + "underlying source rather than literally the same cached rows; say so rather than "
                        + "implying the rows are identical. Leave Anomaly off: the bout is against the same "
                        + "labelled outcome the classifier comparison already used. Which payload fields the "
                        + "categorical options explode into indicator variables is this side's own judgement "
                        + "too, within the one fixed rule neither side may loosen: Payload fields are excluded "
                        + "from training deliberately, by the engine itself, not by an oversight this step "
                        + "could correct. This contender starts "
                        + "cold, exactly like the Advanced Analytics side -- no warm start from a reigning "
                        + "champion or a carried-over topology -- and its distinguishing move is topology "
                        + "evolution: it keeps growing and reshaping its own network in the background, "
                        + "swapping in challenger classifier families along the way, until more complexity "
                        + "stops scoring better, rather than picking one family once. A write, so it needs "
                        + "the caller's explicit confirmation unless already given. Poll "
                        + "ExhaustiveSearchInstanceGet until StatusId reaches a terminal value (16 Finished, "
                        + "17 Finished no class available, or 19 Stopped) before treating it as done.",
                        ExhaustiveInstanceAuthor,
                        ["ExhaustiveSearchInstanceCreate", "ExhaustiveSearchInstanceGet"],
                        ["ExhaustiveSearchInstanceId"],
                        ["EntityAnalysisModelId"],
                        "Reading evidence before StatusId is terminal reports a half-evolved network as the "
                        + "contender's final answer, which is exactly the same mistake as reading an Advanced "
                        + "Analytics training run before it reports Succeeded, Failed or Cancelled -- just "
                        + "against a different status column with different terminal values."),
                    new GraphWorkflowNode(
                        "reuse-check",
                        "Check for an existing Advanced Analytics instance",
                        "List the instances already in this tenant before creating one, exactly as the "
                        + "other two training graphs do -- the comparison and the eventual confirmatory run "
                        + "both file under the same single instance. Runs alongside the Exhaustive side above, "
                        + "which does not depend on it and should already be under way by now.",
                        InstanceInspector,
                        ["AdvancedAnalyticsInstanceList"],
                        ["InstanceId"],
                        [],
                        "Skipping straight to the confirmatory submission fails outright on the missing "
                        + "field, after the comparison step has already spent its search for nothing."),
                    new GraphWorkflowNode(
                        "create-instance",
                        "Create a trial instance if the reuse check found none",
                        "Only when the previous step found zero instances. A write, so it needs the caller's "
                        + "explicit confirmation before it runs, and it starts unallocated.",
                        InstanceAuthor,
                        ["AdvancedAnalyticsInstanceCreate"],
                        ["InstanceId"],
                        [],
                        "Nothing downstream can be submitted without a resolved instance id, and inventing "
                        + "one files the eventual confirmatory run under an instance that does not exist."),
                    new GraphWorkflowNode(
                        "prepare-training-data",
                        "Resolve the label and predictor columns, once",
                        "Point this at the cache pointer already held, from GetDemoDataset or a RunSqlQuery "
                        + "result, and name the label column and the predictor columns by their real names, "
                        + "never invented ones. Which columns are actually predictive is your own judgement to "
                        + "make from InspectDataSchema and PreviewDataRows, not a fixed list to take on faith. "
                        + "The cache pointer and label column this returns carry unchanged into both the "
                        + "comparison step and whichever confirmatory submission follows it, so the rows are "
                        + "read from the cache once rather than twice.",
                        TrainingData,
                        ["AdvancedAnalyticsTrainingDataPut"],
                        ["TrainingDataPointer", "LabelColumn", "PredictorNames"],
                        [],
                        "A column renamed or guessed here taints both the comparison and the confirmatory "
                        + "run with data that is not what the caller described, and the mistake is invisible "
                        + "until someone checks a coefficient or a split against real column names."),
                    new GraphWorkflowNode(
                        "compare-classifier-families",
                        "Compare every classifier family on the same folds",
                        "CompareModels reads the cache pointer directly -- it needs no instance id, since "
                        + "nothing here is saved -- and fits up to all twelve classifier families on "
                        + "identical stratified folds with calibration inside each fold, ranking them on "
                        + "held-out score rather than on training-split numbers. Leave the families argument "
                        + "empty to compare all twelve rather than naming a subset, which is the entire point "
                        + "of this graph over the other two. The result names a winner and says when two "
                        + "families are statistically indistinguishable, in which case the simpler of the two "
                        + "is the one to carry forward.",
                        ModelEvaluation,
                        ["CompareModels"],
                        ["WinningClassifierFamily", "ClassifierComparisonEvidence"],
                        ["TrainingDataPointer", "LabelColumn", "PredictorNames"],
                        "Naming a family from a handful tried rather than from all twelve defeats the one "
                        + "reason to use this graph instead of the short-list one, and reports a partial "
                        + "search as an exhaustive one."),
                    new GraphWorkflowNode(
                        "finalise-trees-family",
                        "Train the winner for real, if it is a tree or ensemble family",
                        "Only follow this step if the named winner is a decision tree, random forest, bagged "
                        + "ensemble, AdaBoost or gradient boosting classifier. Submit one confirmatory run "
                        + "under the resolved instance id with the matching Submit tool, carrying the same "
                        + "cache pointer, label column and predictor names the comparison step used, then "
                        + "poll it to a terminal status before reading its evidence -- CompareModels saves "
                        + "nothing, so this is the first time the winning family is actually trained for real "
                        + "on this data.",
                        ClassifierTrees,
                        [
                            "SubmitDecisionTreeClassifierTraining", "SubmitRandomForestClassifierTraining",
                            "SubmitBaggingClassifierTraining", "SubmitAdaBoostClassifierTraining",
                            "SubmitGradientBoostingClassifierTraining", "GetAdvancedAnalyticsTrainingRunStatus",
                            "GetAdvancedAnalyticsTrainingRunResult"
                        ],
                        ["FinalisedModelVersion"],
                        [
                            "InstanceId", "TrainingDataPointer", "LabelColumn", "PredictorNames",
                            "WinningClassifierFamily"
                        ],
                        "Promoting on the comparison's cross-validated figures alone, without ever actually "
                        + "training the winning family on this instance, leaves the live model backed by a "
                        + "number nobody can trace to a real trained version."),
                    new GraphWorkflowNode(
                        "finalise-probabilistic-family",
                        "Train the winner for real, if it is a probabilistic family",
                        "Only follow this step if the named winner is logistic regression, naive Bayes or "
                        + "tree-augmented naive Bayes. Same discipline as the tree and ensemble alternative: "
                        + "submit one confirmatory run under the resolved instance id with the matching "
                        + "Submit tool, carrying the same cache pointer, label column and predictor names, "
                        + "then poll it to a terminal status before reading its evidence.",
                        ClassifierProbabilistic,
                        [
                            "SubmitLogisticRegressionTraining", "SubmitNaiveBayesClassifierTraining",
                            "SubmitTreeAugmentedNaiveBayesClassifierTraining",
                            "GetAdvancedAnalyticsTrainingRunStatus", "GetAdvancedAnalyticsTrainingRunResult"
                        ],
                        ["FinalisedModelVersion"],
                        [
                            "InstanceId", "TrainingDataPointer", "LabelColumn", "PredictorNames",
                            "WinningClassifierFamily"
                        ],
                        "Promoting on the comparison's cross-validated figures alone, without ever actually "
                        + "training the winning family on this instance, leaves the live model backed by a "
                        + "number nobody can trace to a real trained version."),
                    new GraphWorkflowNode(
                        "finalise-margin-family",
                        "Train the winner for real, if it is a margin, instance or connectionist family",
                        "Only follow this step if the named winner is a linear or kernel support vector "
                        + "machine, k nearest neighbours or a neural network. Same discipline again: submit "
                        + "one confirmatory run under the resolved instance id with the matching Submit tool, "
                        + "carrying the same cache pointer, label column and predictor names, then poll it to "
                        + "a terminal status before reading its evidence.",
                        ClassifierMargin,
                        [
                            "SubmitLinearSupportVectorClassifierTraining",
                            "SubmitKernelSupportVectorClassifierTraining",
                            "SubmitKNearestNeighboursClassifierTraining",
                            "SubmitNeuralNetworkClassifierTraining", "GetAdvancedAnalyticsTrainingRunStatus",
                            "GetAdvancedAnalyticsTrainingRunResult"
                        ],
                        ["FinalisedModelVersion"],
                        [
                            "InstanceId", "TrainingDataPointer", "LabelColumn", "PredictorNames",
                            "WinningClassifierFamily"
                        ],
                        "Promoting on the comparison's cross-validated figures alone, without ever actually "
                        + "training the winning family on this instance, leaves the live model backed by a "
                        + "number nobody can trace to a real trained version."),
                    new GraphWorkflowNode(
                        "exhaustive-comparison-evidence",
                        "Read the Exhaustive contender's held-out evidence",
                        "Once the Exhaustive Adaptation's StatusId is terminal, read its promotion history -- "
                        + "the score and topology complexity of every trial instance it judged better than the "
                        + "last, already persisted and durably queryable, the same way a later question of "
                        + "why a model is there always has an answer -- and the ROC curve of whichever trial "
                        + "instance it currently has promoted. This is what GetAdvancedAnalyticsTrainingRunResult "
                        + "is to the other side of the bout: evidence to weigh a verdict against, not a "
                        + "verdict itself.",
                        ExhaustiveEvidence,
                        [
                            "ExhaustiveSearchInstancePromotionHistoryGet",
                            "ExhaustiveSearchInstancePromotedTrialInstanceQueryGet",
                            "ExhaustiveSearchInstancePromotedTrialInstanceRocGet"
                        ],
                        ["ExhaustiveComparisonEvidence"],
                        ["ExhaustiveSearchInstanceId"],
                        "Comparing against the in-progress Models, ModelsSinceBest or Score counters read "
                        + "mid-evolution, instead of this evidence read once training is terminal, compares a "
                        + "moving target to a finished one and the moving target always looks worse than it "
                        + "will end up being."),
                    new GraphWorkflowNode(
                        "promote-advanced-analytics-winner",
                        "Promote the Advanced Analytics side, if it won the bout",
                        "Weigh the confirmatory run's held-out evidence against the Exhaustive side's on "
                        + "whichever metric is appropriate to the training run -- decide this yourself, never "
                        + "ask the caller which of the two to prefer. The stronger scorer wins; on a genuine "
                        + "statistical tie the simpler model wins, and the Advanced Analytics side -- one "
                        + "picked classifier family, trained once -- is always the simpler of the two against "
                        + "an Exhaustive network that may have evolved an arbitrarily deep topology, so a tie "
                        + "goes to this side, never to Exhaustive, on simplicity alone. Only follow this node "
                        + "if that comparison genuinely favours Advanced Analytics: read the promotion evidence "
                        + "for the version the confirmatory run produced, then promote it. A write, behind the "
                        + "Promote Model permission, and needs the caller's explicit confirmation first unless "
                        + "already given as a standing instruction. Report which side won the bout and why, "
                        + "with both sides' figures, rather than only naming the winner.",
                        ModelAuthor,
                        ["AdvancedAnalyticsModelGetPromotionEvidence", "AdvancedAnalyticsModelPromoteVersion"],
                        ["PromotedModelVersion"],
                        ["FinalisedModelVersion", "ExhaustiveComparisonEvidence"],
                        "Promoting the Advanced Analytics version without having genuinely weighed Exhaustive's "
                        + "evidence first treats the Advanced Analytics search as the only source of truth, "
                        + "which is exactly the narrowed, one-sided verdict this bout exists to prevent."),
                    new GraphWorkflowNode(
                        "promote-exhaustive-winner",
                        "Activate the Exhaustive contender instead, if it won the bout",
                        "Only follow this node if the comparison in the previous node genuinely favours "
                        + "Exhaustive -- its held-out score beats the Advanced Analytics confirmatory run's by "
                        + "more than a statistical tie, since any tie resolves to the simpler Advanced "
                        + "Analytics side regardless of which raw number is marginally higher. Activate the "
                        + "Exhaustive instance so it participates in live scoring, and leave the Advanced "
                        + "Analytics confirmatory version unpromoted. A write, so it needs the caller's "
                        + "explicit confirmation first unless already given. The activation call is itself the "
                        + "durable record of the bout's outcome, exactly as AdvancedAnalyticsModelPromoteVersion's "
                        + "own audit entry is for the other branch -- a later question of why this model is "
                        + "live, and why the Advanced Analytics candidate is not, resolves to this write and "
                        + "the evidence read immediately before it. Report which side won the bout and why, "
                        + "with both sides' figures, rather than only naming the winner.",
                        ExhaustiveInstanceAuthor,
                        ["ExhaustiveSearchInstanceSetActive"],
                        ["PromotedExhaustiveInstance"],
                        ["ExhaustiveSearchInstanceId", "ExhaustiveComparisonEvidence", "FinalisedModelVersion"],
                        "Running both promotion nodes, or neither, makes two production candidates answer the "
                        + "same question or leaves a genuinely tested winner never actually live -- exactly "
                        + "one of the two promotion nodes runs, chosen by which side the bout evidence names "
                        + "as stronger.")
                ],
                "The Exhaustive side of the bout needs only a resolved Model id, not the instance or the "
                + "cache pointer, so it has no reason to wait for either: resolve the Model and create the "
                + "Exhaustive contender first, before the Advanced Analytics instance or data steps, and "
                + "leave it evolving in the background for the rest of this graph. Treat this as the default "
                + "order, not an optimisation to apply only when convenient -- Exhaustive is usually the "
                + "eventual champion of the two, its evolution is the slowest part of the whole graph, and a "
                + "run of this graph that never starts it, or starts it last, has quietly narrowed 'pick the "
                + "best model and promote it' down to 'pick the best Advanced Analytics model', which is "
                + "exactly the one-sided verdict the bout exists to prevent. The instance and the resolved "
                + "cache pointer, label column and predictor names must still exist before the comparison "
                + "runs, and the comparison must name a winner before any confirmatory run is submitted, "
                + "because the confirmatory step cannot choose a family for itself. GridSearchHyperParameters "
                + "and RandomSearchHyperParameters, held by the model evaluation specialist, tune a named "
                + "family's settings -- they sense-check a choice or narrow a field quickly, and are never a "
                + "substitute for the comparison and confirmatory steps below: carry a search's BestParameters "
                + "forward by their real names and values if a later step uses them, never a remembered score "
                + "with the settings that produced it left behind. Exactly one of the "
                + "three finalise nodes actually runs, chosen by which family the comparison named -- they "
                + "are alternatives, not a sequence, and the other two are skipped outright rather than "
                + "attempted and refused. Both sides' evidence must be read, each once its own training is "
                + "genuinely terminal, before the bout is decided, and exactly one of the two promotion nodes "
                + "runs, chosen by that comparison and never by which side happened to finish first or which "
                + "one the caller might prefer -- promoting a run still in progress, on either side, makes "
                + "live something that does not yet exist.")
        ];

        public static IReadOnlyDictionary<string, GraphWorkflow> ById { get; } =
            All.ToDictionary(workflow => workflow.Id, StringComparer.Ordinal);
    }
}