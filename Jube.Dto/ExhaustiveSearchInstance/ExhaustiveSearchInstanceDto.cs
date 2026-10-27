/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using Jube.Dto.Forms;
using Jube.Dto.Options;
using Jube.Dto.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.ExhaustiveSearchInstance
{
    [FormEndpoint("ExhaustiveSearchInstance")]
    [FormApproval]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [ExhaustiveDatasetEditor(Csv = nameof(ExhaustiveInstanceCsvId), Json = nameof(ExhaustiveInstanceJsonId),
        Source = nameof(DataSourceId), Status = nameof(StatusId))]
    [ExhaustiveFilterEditor(FilterJson = nameof(FilterJson), FilterSql = nameof(FilterSql),
        FilterTokens = nameof(FilterTokens), Source = nameof(DataSourceId), Csv = nameof(ExhaustiveInstanceCsvId),
        Json = nameof(ExhaustiveInstanceJsonId))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Training Data", Order = 15)]
    [FormGroup("Anomaly Detection", Order = 20)]
    [FormGroup("Training Data Filter", Order = 30)]
    [FormGroup("Categorical Variables", Order = 35)]
    [FormGroup("Output", Order = 40)]
    [FormGroup("Training Status", Order = 50, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class ExhaustiveSearchInstanceDto : IUpdated, IActivatable, ILockable, ITreeChild, IGuidIdentified
    {
        [Description("Identifier of the Model this Exhaustive Adaptation is registered against. Set from the " +
                     "parent Model context; not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this Exhaustive Adaptation. Unique within the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Fraud classifier v1")]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("Where training reads its data from: 0 Archive (the default, real processed transactions), " +
                     "1 DemoDataset (the built-in demonstration dataset, loaded once per Model), 2 " +
                     "UploadedDataset (a confirmed dataset uploaded by the user, chosen by " +
                     "ExhaustiveInstanceCsvId or ExhaustiveInstanceJsonId). Null is treated as 0.")]
        [FormField(Group = "Training Data", Order = 10, Widget = "select")]
        [NewDefault(0)]
        [Options(typeof(ExhaustiveDataSource))]
        public int? DataSourceId { get; set; }

        [Description("Identifier of the uploaded CSV dataset to train on. When DataSourceId is 2 exactly one of " +
                     "ExhaustiveInstanceCsvId and ExhaustiveInstanceJsonId must be set to a confirmed dataset of " +
                     "the caller's tenant; both must be null otherwise.")]
        [FormField(Group = "Training Data", Order = 20, Widget = "select")]
        [VisibleWhen(nameof(DataSourceId), 2)]
        [Lookup("/api/ExhaustiveInstanceCsv", TextField = "name", ValueField = "id")]
        public int? ExhaustiveInstanceCsvId { get; set; }

        [Description("Identifier of the uploaded JSON dataset to train on. When DataSourceId is 2 exactly one of " +
                     "ExhaustiveInstanceCsvId and ExhaustiveInstanceJsonId must be set to a confirmed dataset of " +
                     "the caller's tenant; both must be null otherwise.")]
        [FormField(Group = "Training Data", Order = 25, Widget = "select")]
        [VisibleWhen(nameof(DataSourceId), 2)]
        [Lookup("/api/ExhaustiveInstanceJson", TextField = "name", ValueField = "id")]
        public int? ExhaustiveInstanceJsonId { get; set; }

        [Description("Optional deployment base rate: the fraction of real cases that are positive, strictly " +
                     "between 0 and 1 (exclusive). Recalled probabilities are calibrated to this rate. When null " +
                     "the rate is measured from the population (archive or dataset row counts, or the flagged " +
                     "fraction for anomaly labelled training) and recorded with its source on the promoted model.")]
        [FormField(Group = "Training Data", Order = 30, Widget = "number", Placeholder = "e.g. 0.01")]
        public double? BaseRate { get; set; }

        [Description("When true, a vine copula is fitted to the minority class after shuffling/normalising/" +
                     "balancing and used to synthesise additional training rows -- placed only in the training " +
                     "partition, never validation or test -- so the balanced training set reaches Bootstrap To " +
                     "rows. Runs after Anomaly Detection's class labelling. Skipped if the minority class " +
                     "already reaches half of Bootstrap To.")]
        [FormField(Group = "Training Data", Order = 37, Widget = "switch")]
        [NewDefault(true)]
        public bool Bootstrap { get; set; }

        [Description("Target total row count (real plus synthesised) for the balanced training partition once " +
                     "Bootstrap runs. Required when Bootstrap is true.")]
        [FormField(Group = "Training Data", Order = 38, Widget = "number")]
        [VisibleWhen(nameof(Bootstrap), true)]
        [RequiredWhen(nameof(Bootstrap), true)]
        [NewDefault(30000)]
        public int? BootstrapTo { get; set; }

        [Description("Down-weights synthesised (bootstrapped) rows relative to real rows, from 0 to 1, applied " +
                     "as a per-sample training weight to challenger models only -- the Neural Network topology " +
                     "evolver has no per-sample weighting support and always trains synthesised rows at full " +
                     "weight. 1 applies full weight (no down-weighting); lower values reduce the synthetic " +
                     "rows' influence on the fit.")]
        [FormField(Group = "Training Data", Order = 39, Widget = "slider")]
        [VisibleWhen(nameof(Bootstrap), true)]
        [NewDefault(1)]
        public double? BootstrapWeightFactor { get; set; }

        [Description("Base probability (0-1) that a given trial trains one of the challenger classifier " +
                     "families instead of evolving a Neural Network topology. Ramps up toward 1 as the best " +
                     "topology's complexity approaches Exhaustive Topology Complexity Limit, so this is the " +
                     "floor rate, not a fixed rate.")]
        [FormField(Group = "Training Data", Order = 40, Widget = "slider")]
        [NewDefault(0.1)]
        public double? ChallengerModelBaseProbability { get; set; }

        [Description("How the model's raw score is turned into a probability at promotion time: 'automatic' " +
                     "(the default) fits Platt, Isotonic and Beta scaling on the validation split and keeps " +
                     "whichever scores best on the held-out test split; 'platt' fits a two-parameter sigmoid, " +
                     "the traditional choice and best suited to small validation samples; 'isotonic' fits a " +
                     "non-parametric monotone step function, more flexible but needs a larger validation sample " +
                     "to avoid overfitting; 'beta' fits an asymmetric two-parameter curve, often a better fit " +
                     "than Platt for score families such as Naive Bayes. Only read at training submission time, " +
                     "like the training data source, and fixed once training has started.")]
        [FormField(Group = "Training Data", Order = 35, Widget = "select")]
        [NewDefault("automatic")]
        [Options(typeof(CalibrationMethod))]
        public string? CalibrationMethod { get; set; }

        [Description("When true, a One Class Support Vector Machine Anomaly Detection model is trained and " +
                     "used to derive the positive/negative class for supervised learning, in place of (or " +
                     "alongside) a Training Data Filter.")]
        [FormField(Group = "Anomaly Detection", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool Anomaly { get; set; }

        [Description("The probability threshold below which the Anomaly Detection model classifies an example " +
                     "as positive class. Required when Anomaly is true.")]
        [FormField(Group = "Anomaly Detection", Order = 20, Widget = "number")]
        [VisibleWhen(nameof(Anomaly), true)]
        [RequiredWhen(nameof(Anomaly), true)]
        [NewDefault(0.02)]
        public double AnomalyProbability { get; set; }

        [Description("The Anomaly Detection algorithm that labels the positive class. Null or 0 is the One Class " +
                     "Support Vector Machine with Gaussian kernel (the original behaviour, AnomalyProbability is a " +
                     "probability threshold). 1 Isolation Forest, 2 HBOS, 3 Mahalanobis, 4 Robust Mahalanobis, " +
                     "5 PCA, 6 Gaussian Mixture, 7 kNN Distance, 8 Local Outlier Factor, 9 Marginal Likelihood, " +
                     "10 Feature Bagging, 11 Consensus, 100 Automatic (evaluated and selected at training time). For " +
                     "every algorithm other than 0, AnomalyProbability is the contamination fraction, above 0 and at " +
                     "most 0.5, of the rows flagged as positive class.")]
        [FormField(Group = "Anomaly Detection", Order = 15, Widget = "select")]
        [VisibleWhen(nameof(Anomaly), true)]
        [NewDefault(0)]
        [Options(typeof(AnomalyAlgorithm))]
        public int? AnomalyAlgorithmId { get; set; }

        [Description("Optional JSON of algorithm parameters, variable reduction options and automatic evaluation " +
                     "options. Unknown properties are rejected. Reduction defaults to none for algorithm 0 and to " +
                     "the recommended reduction for the others.")]
        [FormField(Group = "Anomaly Detection", Order = 30, Widget = "textarea")]
        [VisibleWhen(nameof(Anomaly), true)]
        public string? AnomalyParametersJson { get; set; }

        [Description("One line summary of the anomaly detection run recorded at training: algorithm, variables " +
                     "kept, rows flagged and threshold. Server-assigned. Read-only.")]
        [FormField(Group = "Training Status", Order = 25, ReadOnly = true)]
        public string? AnomalySummary { get; set; }

        [Description("The full machine-readable anomaly report of the training run: algorithm, parameters, " +
                     "variables kept and dropped with reasons, components, score summary, threshold applied and " +
                     "every automatic candidate's evaluation. Server-written; any client supplied value is " +
                     "ignored. Read-only.")]
        [FormField(Group = "Training Status", Order = 26, ReadOnly = true, Widget = "textarea")]
        [ListColumn(Hidden = true)]
        public string? AnomalyReportJson { get; set; }

        [Description("When true, positive class for supervised learning is instead defined by filtering the " +
                     "Archive table with FilterSql -- a match on the filter is positive class, and the filter " +
                     "is also used to negate the sample of negative class.")]
        [FormField(Group = "Training Data Filter", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool Filter { get; set; }

        [Description("The Query Builder JSON definition backing FilterSql. Required when Filter is true.")]
        [FormField(Group = "Training Data Filter", Order = 20)]
        [VisibleWhen(nameof(Filter), true)]
        [RequiredWhen(nameof(Filter), true)]
        public string? FilterJson { get; set; }

        [Description("The compiled SQL predicate, derived from FilterJson, applied to the Archive table to " +
                     "define positive class. Required when Filter is true. Not directly user-edited -- " +
                     "generated by the Training Data Filter builder.")]
        [FormField(Group = "Training Data Filter", Order = 30, ReadOnly = true)]
        [VisibleWhen(nameof(Filter), true)]
        [RequiredWhen(nameof(Filter), true)]
        public string? FilterSql { get; set; }

        [Description("The SQL parameter tokens for FilterSql, derived from FilterJson. Required when Filter is " +
                     "true. Not directly user-edited -- generated by the Training Data Filter builder.")]
        [FormField(Group = "Training Data Filter", Order = 40, ReadOnly = true)]
        [VisibleWhen(nameof(Filter), true)]
        [RequiredWhen(nameof(Filter), true)]
        public string? FilterTokens { get; set; }

        [Description("Optional JSON options that explode categorical string payload fields into indicator " +
                     "variables for training, for example {\"enabled\":true,\"maxLevels\":25," +
                     "\"minLevelCount\":20,\"maxTotalIndicators\":200,\"exclude\":[\"Payload.Reference\"]," +
                     "\"include\":[]}. When absent or not enabled, string fields are ignored as before.")]
        [FormField(Group = "Categorical Variables", Order = 10, Placeholder = "{\"enabled\":true,\"maxLevels\":25}")]
        public string? CategoricalOptionsJson { get; set; }

        [Description("When true, every recall of this Exhaustive Adaptation is persisted to the reporting " +
                     "table for later analysis.")]
        [FormField(Group = "Output", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool ReportTable { get; set; }

        [Description("Whether the response payload is returned for a recall of this Exhaustive Adaptation.")]
        [FormField(Group = "Output", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("Server-managed training progress status. 0 Awaiting Server, 1 Query for base case data, " +
                     "2 Calculating Statistics for base case, 3 Normalising Data for unsupervised learning, " +
                     "4 Unsupervised training, 5 Unsupervised training recall, 6 Unsupervised training " +
                     "statistics creation, 7 Query for class filter data, 8 Statistics for class filter data, " +
                     "9 Shuffling data, 10 Normalising data, 11 Over/Symmetric Sampling, 12 Storing final data, " +
                     "13 Correlation analysis, 14 Multicollinearity analysis, 15 Supervised Learning, " +
                     "16 Finished, 17 Finished no class available, 18 Stopping, 19 Stopped. An Exhaustive " +
                     "Adaptation can only be edited while this is 0. Read-only.")]
        [FormField(Group = "Training Status", Order = 20, ReadOnly = true)]
        public byte StatusId { get; set; }

        [Description("Server-managed count of Neural Network topologies attempted so far during evolution. " +
                     "Read-only.")]
        [FormField(Group = "Training Status", Order = 30, ReadOnly = true)]
        public int Models { get; set; }

        [Description("Server-managed count of topologies tried since the last promoted (best-scoring) " +
                     "topology. Training terminates once this exceeds the configured threshold. Read-only.")]
        [FormField(Group = "Training Status", Order = 40, ReadOnly = true)]
        public int ModelsSinceBest { get; set; }

        [Description("Server-managed score of the best topology promoted so far during evolution. Read-only.")]
        [FormField(Group = "Training Status", Order = 50, ReadOnly = true)]
        public double Score { get; set; }

        [Description("Server-managed topology complexity of the best topology promoted so far. Read-only.")]
        [FormField(Group = "Training Status", Order = 60, ReadOnly = true)]
        public double TopologyComplexity { get; set; }

        [Description("Timestamp (UTC) training completed, if applicable. Server-assigned. Read-only.")]
        [FormField(Group = "Training Status", Order = 70, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CompletedDate { get; set; }

        [Description("When true, this Exhaustive Adaptation participates in evaluation on transaction " +
                     "invocation once trained.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("Server-assigned globally-unique identifier for this Exhaustive Adaptation, usable for " +
                     "direct HTTP recall of the trained model once training has completed. Read-only.")]
        [FormField(Group = "Training Status", Order = 10, ReadOnly = true)]
        public Guid Guid { get; set; }

        [Description("When true, this Exhaustive Adaptation is locked and cannot be edited or deleted. Also " +
                     "set implicitly once training has started -- an Exhaustive Adaptation can only be edited " +
                     "while its training StatusId is still 'Awaiting Server' (0).")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this Exhaustive Adaptation. Server-assigned. Read-only. Note: also " +
                     "re-assigned to the acting user on every update -- see the migration report.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this Exhaustive Adaptation was created. Server-assigned. Read-only. " +
                     "Note: also reset to the update time on every update -- see the migration report.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Not populated -- the underlying table has no corresponding column. Always null. See the " +
                     "migration report.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Exhaustive Adaptation was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this Exhaustive Adaptation, if soft-deleted. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Exhaustive Adaptation was soft-deleted, if applicable. " +
                     "Server-assigned. Read-only. Note: the underlying column is non-nullable, so a row that " +
                     "has never been deleted reports 0001-01-01 rather than null -- see the migration report.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}