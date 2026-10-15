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
    public enum DistributionKind
    {
        Normal,
        Lognormal,
        Exponential,
        Gamma,
        Beta,
        GeneralizedBeta,
        BetaPrime,
        BirnbaumSaunders,
        Cauchy,
        ChiSquare,
        F,
        FoldedNormal,
        GeneralizedNormal,
        GeneralizedPareto,
        Gompertz,
        Gumbel,
        HyperbolicSecant,
        InverseChiSquare,
        InverseGamma,
        InverseGaussian,
        Kumaraswamy,
        Laplace,
        Levy,
        Logistic,
        LogLogistic,
        Nakagami,
        NoncentralT,
        Pareto,
        PowerLognormal,
        PowerNormal,
        Rayleigh,
        ShiftedLogLogistic,
        SkewNormal,
        StudentT,
        Trapezoidal,
        Triangular,
        TukeyLambda,
        UniformContinuous,
        UQuadratic,
        VonMises,
        Weibull,
        WrappedCauchy,
        KolmogorovSmirnovStatistic,
        ShapiroWilkStatistic,
        GrubbsStatistic,
        WilcoxonSignedRankStatistic,
        MannWhitneyStatistic,
        Bernoulli,
        Binomial,
        Degenerate,
        Geometric,
        Hypergeometric,
        NegativeBinomial,
        Poisson,
        SymmetricGeometric,
        UniformDiscrete,
        GeneralDiscrete,
        Rademacher
    }
}