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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models
{
    public enum PlottingPosition
    {
        Hazen,
        Blom,
        Weibull,
        Benard
    }

    public enum FitRankCriterion
    {
        Aic,
        Bic,
        KolmogorovSmirnov,
        AndersonDarling,
        ChiSquare,
        AverageRank
    }

    public enum FitDataType
    {
        Automatic,
        Continuous,
        Discrete
    }

    public enum HistogramScale
    {
        Count,
        RelativeFrequency,
        Percent,
        Density
    }

    public enum BinningRule
    {
        Sturges,
        Scott,
        FreedmanDiaconis,
        Rice,
        SquareRoot,
        Doane
    }

    public enum KernelKind
    {
        Gaussian,
        Epanechnikov,
        Uniform
    }

    public enum BandwidthRule
    {
        NormalReference,
        SilvermanRobust,
        Oversmoothed,
        Manual
    }

    public enum QuantileDefinition
    {
        Type1 = 1,
        Type2 = 2,
        Type3 = 3,
        Type4 = 4,
        Type5 = 5,
        Type6 = 6,
        Type7 = 7,
        Type8 = 8,
        Type9 = 9
    }

    public enum TailDirection
    {
        Upper,
        Lower
    }
}