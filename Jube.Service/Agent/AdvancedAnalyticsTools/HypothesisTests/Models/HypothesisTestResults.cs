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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.Models
{
    public enum HypothesisTail
    {
        TwoSided,
        Greater,
        Less
    }

    public enum HypothesisPValueAdjustment
    {
        Bonferroni,
        Sidak,
        Holm,
        Hochberg,
        BenjaminiHochberg,
        BenjaminiYekutieli
    }

    public enum HypothesisProportionIntervalMethod
    {
        Wilson,
        ClopperPearson,
        Wald,
        AgrestiCoull
    }

    public enum HypothesisVarianceHomogeneityCentre
    {
        Mean,
        Median,
        TrimmedMean
    }

    public enum HypothesisTwoWayAnovaModel
    {
        FixedEffects,
        RandomEffects,
        FactorAFixedFactorBRandom
    }

    public enum HypothesisOutlierSide
    {
        Either,
        Maximum,
        Minimum
    }

    public enum HypothesisKappaWeighting
    {
        Linear,
        Quadratic
    }

    public enum HypothesisPowerSolveFor
    {
        Power,
        SampleSize,
        EffectSize
    }

    public enum HypothesisStatisticDistribution
    {
        StandardNormal,
        StudentT,
        ChiSquare,
        F
    }

    public enum HypothesisReferenceDistribution
    {
        Normal,
        Uniform,
        Exponential,
        LogNormal
    }

    public enum HypothesisDataKind
    {
        Continuous,
        Ordinal,
        Binary,
        Categorical
    }

    public enum HypothesisDesign
    {
        OneSampleAgainstValue,
        TwoIndependentGroups,
        TwoPairedOrMatchedGroups,
        ThreeOrMoreIndependentGroups,
        TwoWayFactorial,
        AssociationBetweenTwoCategoricalVariables,
        AgreementBetweenTwoRaters,
        ComparingVariances,
        NormalityOfOneSample,
        DistributionFitOfOneSample,
        OutlierInOneSample
    }

    public class HypothesisTwoWayAnovaResult
    {
        public string TestName { get; set; } = string.Empty;
        public HypothesisTwoWayAnovaModel Model { get; set; }
        public double FactorAfStatistic { get; set; }
        public double FactorApValue { get; set; }
        public double FactorAPartialEtaSquared { get; set; }
        public bool FactorARejectNull { get; set; }
        public double FactorBfStatistic { get; set; }
        public double FactorBpValue { get; set; }
        public double FactorBPartialEtaSquared { get; set; }
        public bool FactorBRejectNull { get; set; }
        public double InteractionFStatistic { get; set; }
        public double InteractionPValue { get; set; }
        public double InteractionPartialEtaSquared { get; set; }
        public bool InteractionRejectNull { get; set; }
        public int FactorALevels { get; set; }
        public int FactorBLevels { get; set; }
        public int Replications { get; set; }
        public double Alpha { get; set; }
        public HypothesisAnovaRow[] Table { get; set; } = Array.Empty<HypothesisAnovaRow>();
        public string[] Warnings { get; set; } = Array.Empty<string>();
        public string Interpretation { get; set; } = string.Empty;
    }
}