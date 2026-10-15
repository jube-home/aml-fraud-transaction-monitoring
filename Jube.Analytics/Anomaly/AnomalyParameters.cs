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

using System.Text.Json;
using System.Text.Json.Serialization;
using Jube.Analytics.Anomaly.Models;

namespace Jube.Analytics.Anomaly
{
    public sealed record AnomalyParameters
    {
        public const int MaxJsonLength = 8000;

        private static readonly JsonSerializerOptions readOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
            MaxDepth = 8
        };

        private static readonly JsonSerializerOptions writeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        public double? Contamination { get; init; }
        public int? Trees { get; init; }
        public int? SubsampleSize { get; init; }
        public int? Bins { get; init; }
        public int? K { get; init; }
        public int? Components { get; init; }
        public bool? FullCovariance { get; init; }
        public bool? Robust { get; init; }
        public CovarianceRegularisation? Regularisation { get; init; }
        public int? Members { get; init; }
        public int? SubspaceSize { get; init; }
        public ScoreCombination? Combination { get; init; }
        public AnomalyAlgorithm? BaseAlgorithm { get; init; }
        public int? RowCap { get; init; }
        public PcaRetention? Retention { get; init; }
        public double? ExplainedVariance { get; init; }
        public ReductionOptions? Reduction { get; init; }
        public AutoOptions? Auto { get; init; }

        public static AnomalyParameters Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new AnomalyParameters();
            }

            if (json.Length > MaxJsonLength)
            {
                throw new ArgumentException($"The parameters exceed {MaxJsonLength} characters.");
            }

            try
            {
                return JsonSerializer.Deserialize<AnomalyParameters>(json, readOptions) ?? new AnomalyParameters();
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"The parameters are not valid: {ex.Message}");
            }
        }

        public static bool TryParse(string? json, out AnomalyParameters parameters, out IReadOnlyList<string> errors)
        {
            parameters = new AnomalyParameters();

            try
            {
                parameters = Parse(json);
            }
            catch (ArgumentException ex)
            {
                errors = [ex.Message];

                return false;
            }

            errors = parameters.Validate();

            return errors.Count == 0;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, writeOptions);
        }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            void Range(string name, double? value, double min, double max)
            {
                if (value.HasValue && !(value.Value >= min && value.Value <= max))
                {
                    errors.Add($"{name} must be between {min} and {max}.");
                }
            }

            Range(nameof(Contamination), Contamination, 1e-4, 0.5);
            Range(nameof(Trees), Trees, 1, 1000);
            Range(nameof(SubsampleSize), SubsampleSize, 2, 100000);
            Range(nameof(Bins), Bins, 0, 1000);
            Range(nameof(K), K, 1, 500);
            Range(nameof(Components), Components, 1, 20);
            Range(nameof(Members), Members, 1, 200);
            Range(nameof(SubspaceSize), SubspaceSize, 0, 10000);
            Range(nameof(RowCap), RowCap, 100, 20000);
            Range(nameof(ExplainedVariance), ExplainedVariance, 0.01, 1);

            if (BaseAlgorithm is AnomalyAlgorithm.OneClassSvm or AnomalyAlgorithm.Automatic
                or AnomalyAlgorithm.FeatureBagging
                or AnomalyAlgorithm.Consensus)
            {
                errors.Add("BaseAlgorithm must be a single native detector.");
            }

            if (Regularisation.HasValue && !Enum.IsDefined(Regularisation.Value))
            {
                errors.Add("Regularisation is not known.");
            }

            if (Combination.HasValue && !Enum.IsDefined(Combination.Value))
            {
                errors.Add("Combination is not known.");
            }

            if (Retention.HasValue && !Enum.IsDefined(Retention.Value))
            {
                errors.Add("Retention is not known.");
            }

            if (BaseAlgorithm.HasValue && !Enum.IsDefined(BaseAlgorithm.Value))
            {
                errors.Add("BaseAlgorithm is not known.");
            }

            if (Reduction != null)
            {
                try
                {
                    Reduction.Validate();
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    errors.Add($"Reduction.{ex.ParamName}: {ex.Message.Split('(')[0].Trim()}");
                }

                if (!Enum.IsDefined(Reduction.Mode) || !Enum.IsDefined(Reduction.Scaling) ||
                    !Enum.IsDefined(Reduction.Projection) || !Enum.IsDefined(Reduction.Grouping))
                {
                    errors.Add("Reduction contains an unknown option.");
                }
            }

            if (Auto != null)
            {
                Range("Auto.TimeBudgetSeconds", Auto.TimeBudgetSeconds, 1, 3600);
                Range("Auto.EvaluationRowCap", Auto.EvaluationRowCap, 200, 20000);
                Range("Auto.Resamples", Auto.Resamples, 2, 20);
                Range("Auto.Subspaces", Auto.Subspaces, 1, 20);

                if (Auto.Candidates != null)
                {
                    if (Auto.Candidates.Count == 0)
                    {
                        errors.Add("Auto.Candidates must not be empty when supplied.");
                    }

                    if (Auto.Candidates.Any(c => !Enum.IsDefined(c) || c == AnomalyAlgorithm.Automatic))
                    {
                        errors.Add("Auto.Candidates contains an unknown or non-selectable algorithm.");
                    }
                }
            }

            return errors;
        }
    }
}