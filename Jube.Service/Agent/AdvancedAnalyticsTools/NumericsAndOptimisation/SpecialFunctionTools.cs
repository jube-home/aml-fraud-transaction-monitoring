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
using Accord.Math;
using Accord.Statistics.Links;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation
{
    public enum NumericsErrorFunctionKind
    {
        Erf,
        Erfc,
        InverseErf,
        InverseErfc
    }

    public enum NumericsNormalFunctionKind
    {
        Cdf,
        LogCdf,
        Survival,
        Quantile,
        Density,
        LogDensity,
        HighAccuracyCdf,
        HighAccuracySurvival
    }

    public enum NumericsBesselKind
    {
        FirstKindJ,
        SecondKindY,
        ModifiedFirstKindI
    }

    public enum NumericsStableFunctionKind
    {
        Log1P,
        Expm1,
        Log1M,
        Softplus
    }

    public enum NumericsHyperbolicKind
    {
        Secant,
        Cosecant,
        Cotangent,
        InverseSecant,
        InverseCosecant,
        InverseCotangent,
        HyperbolicSecant,
        HyperbolicCosecant,
        HyperbolicCotangent,
        InverseHyperbolicSine,
        InverseHyperbolicCosine,
        InverseHyperbolicTangent,
        InverseHyperbolicSecant,
        InverseHyperbolicCosecant,
        InverseHyperbolicCotangent
    }

    public enum NumericsTaylorFunction
    {
        Sine,
        Cosine,
        HyperbolicSine,
        HyperbolicCosine,
        Exponential
    }

    public enum NumericsLogPairOperation
    {
        LogSum,
        LogDifference
    }

    public enum NumericsLogisticDirection
    {
        Sigmoid,
        Logit
    }

    [AdvancedAnalyticsToolGroup("NumericsAndOptimisation")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class SpecialFunctionTools
    {
        private static NumericsVectorResult Map(double[] x, string name, Func<double, double> function)
        {
            NumericsGuard.RequireVector(x, name);
            var result = new double[x.Length];

            for (var i = 0; i < x.Length; i++)
            {
                result[i] = function(x[i]);
            }

            return NumericsGuard.ToVectorResult(result);
        }

        private static NumericsVectorResult MapPairs(double[] first, string firstName, double[] second,
            string secondName,
            Func<double, double, double> function)
        {
            NumericsGuard.RequireVector(first, firstName);
            NumericsGuard.RequireVector(second, secondName, first.Length, first.Length);
            var result = new double[first.Length];

            for (var i = 0; i < first.Length; i++)
            {
                result[i] = function(first[i], second[i]);
            }

            return NumericsGuard.ToVectorResult(result);
        }

        [AdvancedAnalyticsTool("GammaFunction")]
        [Description(
            "Evaluates the gamma function, the continuous extension of the factorial with gamma(n) = (n-1)! for positive integers, at each supplied value. " +
            "It is defined for all reals except zero and the negative integers (NaN there) and overflows to positive infinity above about 171; use LogGammaFunction for large arguments. Maximum 200000 values.")]
        public NumericsVectorResult GammaFunction(
            [Description("Values at which to evaluate the gamma function.")]
            double[] x)
        {
            return Map(x, nameof(x), v =>
            {
                try
                {
                    return Gamma.Function(v);
                }
                catch (ArithmeticException)
                {
                    return v > 0 ? double.PositiveInfinity : double.NaN;
                }
            });
        }

        [AdvancedAnalyticsTool("LogGammaFunction")]
        [Description(
            "Evaluates the natural logarithm of the gamma function without overflow, so it works for very large arguments where the gamma function itself is infinite. " +
            "It underlies log-factorials, log-binomials and many likelihoods. The argument must be positive; non-positive values give NaN or infinity. Maximum 200000 values.")]
        public NumericsVectorResult LogGammaFunction(
            [Description("Positive values at which to evaluate log gamma.")]
            double[] x)
        {
            return Map(x, nameof(x), Gamma.Log);
        }

        [AdvancedAnalyticsTool("DigammaFunction")]
        [Description(
            "Evaluates the digamma function, the derivative of the log-gamma function (psi). It appears in maximum likelihood for gamma and beta distributions; digamma(1) is minus the Euler-Mascheroni constant, about -0.5772. " +
            "Non-positive integers give NaN or infinity. Maximum 200000 values.")]
        public NumericsVectorResult DigammaFunction(
            [Description("Values at which to evaluate digamma.")]
            double[] x)
        {
            return Map(x, nameof(x), Gamma.Digamma);
        }

        [AdvancedAnalyticsTool("TrigammaFunction")]
        [Description(
            "Evaluates the trigamma function, the second derivative of log gamma (the derivative of digamma). It gives the curvature of gamma and beta likelihoods and trigamma(1) equals pi squared over 6, about 1.645. " +
            "Maximum 200000 values.")]
        public NumericsVectorResult TrigammaFunction(
            [Description("Values at which to evaluate trigamma.")]
            double[] x)
        {
            return Map(x, nameof(x), Gamma.Trigamma);
        }

        [AdvancedAnalyticsTool("MultivariateGammaFunction")]
        [Description(
            "Evaluates the multivariate gamma function of dimension p, which normalises Wishart and inverse-Wishart distributions; p = 1 is the ordinary gamma function. " +
            "Set logScale to get its logarithm, which avoids overflow. The argument x must exceed (p - 1) / 2. Maximum 200000 values.")]
        public NumericsVectorResult MultivariateGammaFunction(
            [Description("Values at which to evaluate, each greater than (p - 1) / 2.")]
            double[] x,
            [Description("Dimension p of the multivariate gamma function, from 1 to 1000.")]
            int dimension,
            [Description("True returns the natural log of the function instead of the function.")]
            bool logScale = false)
        {
            NumericsGuard.RequireInRange(dimension, nameof(dimension), 1, 1000);

            return Map(x, nameof(x), v => logScale ? Gamma.Log(v, dimension) : Gamma.Multivariate(v, dimension));
        }

        [AdvancedAnalyticsTool("RegularisedLowerIncompleteGamma")]
        [Description(
            "Evaluates the regularised lower incomplete gamma function P(a, x), the probability that a gamma random variable with shape a and unit scale is at most x; it is the chi-square and Poisson tail building block. " +
            "Results run from 0 to 1. The shape a must be positive and x non-negative, otherwise the library returns 1. Maximum 200000 values.")]
        public NumericsVectorResult RegularisedLowerIncompleteGamma(
            [Description("Shape parameter a, greater than 0.")]
            double a,
            [Description("Upper limits x, each 0 or more.")]
            double[] x)
        {
            NumericsGuard.RequirePositive(a, nameof(a));

            return Map(x, nameof(x), v => Gamma.LowerIncomplete(a, v));
        }

        [AdvancedAnalyticsTool("RegularisedUpperIncompleteGamma")]
        [Description(
            "Evaluates the regularised upper incomplete gamma function Q(a, x) = 1 - P(a, x), the survival probability of a gamma variable with shape a; with a = k/2 and x = statistic/2 it is the chi-square p-value. " +
            "Results run from 0 to 1. The shape a must be positive. Maximum 200000 values.")]
        public NumericsVectorResult RegularisedUpperIncompleteGamma(
            [Description("Shape parameter a, greater than 0.")]
            double a,
            [Description("Lower limits x, each 0 or more.")]
            double[] x)
        {
            NumericsGuard.RequirePositive(a, nameof(a));

            return Map(x, nameof(x), v => Gamma.UpperIncomplete(a, v));
        }

        [AdvancedAnalyticsTool("InverseRegularisedIncompleteGamma")]
        [Description(
            "Finds x such that the regularised incomplete gamma function equals the given probability: with upper false it inverts P(a, x), the gamma quantile function; with upper true it inverts Q(a, x). " +
            "Use it for chi-square and gamma critical values. Probabilities must lie in (0, 1) and a must be positive. Maximum 200000 values.")]
        public NumericsVectorResult InverseRegularisedIncompleteGamma(
            [Description("Shape parameter a, greater than 0.")]
            double a,
            [Description("Probabilities between 0 and 1.")]
            double[] probabilities,
            [Description("True inverts the upper (survival) function Q; false the lower function P.")]
            bool upper = false)
        {
            NumericsGuard.RequirePositive(a, nameof(a));

            return Map(probabilities, nameof(probabilities),
                v => upper ? Gamma.InverseUpperIncomplete(a, v) : Gamma.InverseLowerIncomplete(a, v));
        }

        [AdvancedAnalyticsTool("BetaFunction")]
        [Description(
            "Evaluates the beta function B(a, b) = gamma(a) gamma(b) / gamma(a + b) for pairs of parameters given as two equal-length arrays; it normalises the beta distribution. " +
            "Set logScale to return its natural log, which is safer for large parameters. Both parameters must be positive. Maximum 200000 pairs.")]
        public NumericsVectorResult BetaFunction(
            [Description("First parameters a, each greater than 0.")]
            double[] a,
            [Description("Second parameters b, same length as a.")]
            double[] b,
            [Description("True returns the natural log of the beta function.")]
            bool logScale = false)
        {
            return MapPairs(a, nameof(a), b, nameof(b), (x, y) => logScale ? Beta.Log(x, y) : Beta.Function(x, y));
        }

        [AdvancedAnalyticsTool("MultinomialBetaFunction")]
        [Description(
            "Evaluates the multinomial (Dirichlet) beta function of a parameter vector: the product of gamma(alpha_i) divided by gamma of their sum, the normalising constant of the Dirichlet distribution. With two entries it equals the ordinary beta function. " +
            "Each entry must be positive; very long vectors underflow to zero. Maximum 1000 entries.")]
        public double MultinomialBetaFunction(
            [Description("Positive Dirichlet concentration parameters.")]
            double[] alpha)
        {
            NumericsGuard.RequireVector(alpha, nameof(alpha), 1, 1000);

            return Beta.Multinomial(alpha);
        }

        [AdvancedAnalyticsTool("RegularisedIncompleteBeta")]
        [Description(
            "Evaluates the regularised incomplete beta function I_x(a, b), which is the cumulative distribution function of the beta distribution and underlies the Student t, F and binomial tail probabilities. " +
            "Results run from 0 to 1; x must lie between 0 and 1 and a, b must be positive. Maximum 200000 values.")]
        public NumericsVectorResult RegularisedIncompleteBeta(
            [Description("Shape parameter a, greater than 0.")]
            double a,
            [Description("Shape parameter b, greater than 0.")]
            double b,
            [Description("Values x between 0 and 1.")]
            double[] x)
        {
            NumericsGuard.RequirePositive(a, nameof(a));
            NumericsGuard.RequirePositive(b, nameof(b));

            return Map(x, nameof(x), v => Beta.Incomplete(a, b, v));
        }

        [AdvancedAnalyticsTool("InverseRegularisedIncompleteBeta")]
        [Description(
            "Finds x in [0, 1] such that the regularised incomplete beta function I_x(a, b) equals the given probability, that is the beta distribution quantile function. " +
            "Use it for critical values of the beta, t and F distributions. Probabilities must be between 0 and 1. Maximum 200000 values.")]
        public NumericsVectorResult InverseRegularisedIncompleteBeta(
            [Description("Shape parameter a, greater than 0.")]
            double a,
            [Description("Shape parameter b, greater than 0.")]
            double b,
            [Description("Probabilities between 0 and 1.")]
            double[] probabilities)
        {
            NumericsGuard.RequirePositive(a, nameof(a));
            NumericsGuard.RequirePositive(b, nameof(b));

            return Map(probabilities, nameof(probabilities), v => Beta.IncompleteInverse(a, b, v));
        }

        [AdvancedAnalyticsTool("ErrorFunction")]
        [Description(
            "Evaluates the error function family: Erf, its complement Erfc (accurate in the upper tail), and their inverses. erf(x) = 2 Phi(x sqrt 2) - 1 relates it to the normal distribution. " +
            "InverseErf needs inputs in (-1, 1) and InverseErfc inputs in (0, 2); values outside give NaN or infinity. Maximum 200000 values.")]
        public NumericsVectorResult ErrorFunction(
            [Description("Values to evaluate.")] double[] x,
            [Description("Which member of the family to evaluate.")]
            NumericsErrorFunctionKind kind = NumericsErrorFunctionKind.Erf)
        {
            return Map(x, nameof(x), v => kind switch
            {
                NumericsErrorFunctionKind.Erf => Special.Erf(v),
                NumericsErrorFunctionKind.Erfc => Special.Erfc(v),
                NumericsErrorFunctionKind.InverseErf => Special.Ierf(v),
                _ => Special.Ierfc(v)
            });
        }

        [AdvancedAnalyticsTool("StandardNormalFunction")]
        [Description(
            "Evaluates the standard normal distribution (mean 0, standard deviation 1): Cdf, LogCdf, Survival (1 minus Cdf, accurate in the tail), Quantile (the inverse Cdf, for probabilities in (0, 1)), Density, LogDensity, or the high accuracy Cdf and Survival variants. " +
            "Use it for z-scores and p-values; a two-sided p-value is twice the Survival of the absolute z. Quantile of 0 or 1 gives minus or plus infinity. Maximum 200000 values.")]
        public NumericsVectorResult StandardNormalFunction(
            [Description("Values to evaluate: z-scores, or probabilities when kind is Quantile.")]
            double[] x,
            [Description("Which function of the standard normal to evaluate.")]
            NumericsNormalFunctionKind kind = NumericsNormalFunctionKind.Cdf)
        {
            return Map(x, nameof(x), v => kind switch
            {
                NumericsNormalFunctionKind.Cdf => Normal.Function(v),
                NumericsNormalFunctionKind.LogCdf => Normal.Log(v),
                NumericsNormalFunctionKind.Survival => Normal.Complemented(v),
                NumericsNormalFunctionKind.Quantile => Normal.Inverse(v),
                NumericsNormalFunctionKind.Density => Normal.Derivative(v),
                NumericsNormalFunctionKind.LogDensity => Normal.LogDerivative(v),
                NumericsNormalFunctionKind.HighAccuracyCdf => Normal.HighAccuracyFunction(v),
                _ => Normal.HighAccuracyComplemented(v)
            });
        }

        [AdvancedAnalyticsTool("BivariateNormalCdf")]
        [Description(
            "Evaluates the bivariate standard normal cumulative probability P(X <= x, Y <= y) for two standard normal variables with correlation rho, for pairs given as two equal-length arrays. " +
            "With rho = 0 it equals the product of the two marginal Cdfs. Rho must be strictly between -1 and 1. Maximum 200000 pairs.")]
        public NumericsVectorResult BivariateNormalCdf(
            [Description("Upper limits for the first variable.")]
            double[] x,
            [Description("Upper limits for the second variable, same length as x.")]
            double[] y,
            [Description("Correlation between the two variables, between -1 and 1 exclusive.")]
            double rho)
        {
            NumericsGuard.RequireFinite(rho, nameof(rho));

            if (rho <= -1 || rho >= 1)
            {
                throw new ArgumentException(
                    "'rho' must be strictly between -1 and 1; a perfect correlation is a one-dimensional case.",
                    nameof(rho));
            }

            return MapPairs(x, nameof(x), y, nameof(y), (a, b) => Normal.Bivariate(a, b, rho));
        }

        [AdvancedAnalyticsTool("OwensTFunction")]
        [Description(
            "Evaluates Owen's T function T(h, a), the integral of exp(-h squared (1 + x squared) / 2) / (1 + x squared) from 0 to a, divided by 2 pi. It is needed for the skew-normal distribution and for bivariate normal probabilities. " +
            "Pass h and a as two equal-length arrays; T(h, 0) is 0 and T(0, 1) is 1/8. Maximum 200000 pairs.")]
        public NumericsVectorResult OwensTFunction(
            [Description("First argument h for each pair.")]
            double[] h,
            [Description("Second argument a for each pair, same length as h.")]
            double[] a)
        {
            return MapPairs(h, nameof(h), a, nameof(a), OwensT.Function);
        }

        [AdvancedAnalyticsTool("BesselFunction")]
        [Description(
            "Evaluates Bessel functions of integer order n: FirstKindJ (oscillating, solutions of the wave equation in cylinders), SecondKindY (singular at 0, needs x greater than 0) or ModifiedFirstKindI (growing, used in the von Mises and Rice distributions). " +
            "Order 0 and 1 are the common cases; order is limited to 0 to 100. The values are accurate to roughly seven significant digits. Maximum 200000 values.")]
        public NumericsVectorResult BesselFunction(
            [Description("Arguments x at which to evaluate.")]
            double[] x,
            [Description("Which kind of Bessel function.")]
            NumericsBesselKind kind = NumericsBesselKind.FirstKindJ,
            [Description("Integer order n, from 0 to 100.")]
            int order = 0)
        {
            NumericsGuard.RequireInRange(order, nameof(order), 0, 100);

            return Map(x, nameof(x), v => kind switch
            {
                NumericsBesselKind.FirstKindJ => Bessel.J(order, v),
                NumericsBesselKind.SecondKindY => Bessel.Y(order, v),
                _ => Bessel.I(order, v)
            });
        }

        [AdvancedAnalyticsTool("FactorialFunction")]
        [Description(
            "Returns n factorial for each non-negative integer n, or its natural log when logScale is true. Plain factorials overflow above 170 (returning infinity), so use logScale for larger n. " +
            "Inputs must be whole numbers from 0 up to 170 (or up to 1000000000 with logScale). Maximum 200000 values.")]
        public NumericsVectorResult FactorialFunction(
            [Description("Non-negative whole numbers n.")]
            double[] n,
            [Description("True returns ln(n!) instead of n!.")]
            bool logScale = false)
        {
            NumericsGuard.RequireVector(n, nameof(n));
            var upper = logScale ? 1000000000.0 : 170.0;

            foreach (var value in n)
            {
                if (value < 0 || value > upper || value != Math.Floor(value))
                {
                    throw new ArgumentException($"Every n must be a whole number between 0 and {upper}; got {value}.",
                        nameof(n));
                }
            }

            return Map(n, nameof(n), v => logScale ? Special.LogFactorial((int)v) : Special.Factorial((int)v));
        }

        [AdvancedAnalyticsTool("BinomialCoefficient")]
        [Description(
            "Returns the binomial coefficient 'n choose k' (the number of ways to pick k items from n) for each pair of whole numbers, or its natural log when logScale is true, which avoids overflow for large n. " +
            "Pairs are given as two equal-length arrays; k greater than n gives 0. n and k are limited to 1000000 and 200000 pairs.")]
        public NumericsVectorResult BinomialCoefficient(
            [Description("Whole numbers n, from 0 to 1000000.")]
            int[] n,
            [Description("Whole numbers k, same length as n, from 0 to 1000000.")]
            int[] k,
            [Description("True returns ln of the coefficient.")]
            bool logScale = false)
        {
            if (n == null || n.Length == 0 || n.Length > NumericsGuard.MaxVectorLength)
            {
                throw new ArgumentException("'n' must contain between 1 and 200000 whole numbers.", nameof(n));
            }

            if (k == null || k.Length != n.Length)
            {
                throw new ArgumentException("'k' must have the same length as 'n'.", nameof(k));
            }

            var result = new double[n.Length];

            for (var i = 0; i < n.Length; i++)
            {
                NumericsGuard.RequireInRange(n[i], nameof(n), 0, 1000000);
                NumericsGuard.RequireInRange(k[i], nameof(k), 0, 1000000);

                result[i] = k[i] > n[i]
                    ? logScale ? double.NegativeInfinity : 0.0
                    : logScale
                        ? Special.LogBinomial(n[i], k[i])
                        : Special.Binomial(n[i], k[i]);
            }

            return NumericsGuard.ToVectorResult(result);
        }

        [AdvancedAnalyticsTool("LogSumExp")]
        [Description(
            "Computes ln(exp(v1) + exp(v2) + ...) stably, without overflow or underflow even for values such as 1000 or -1000. Use it to add probabilities held as logs, to normalise log-likelihoods and to build softmax. " +
            "An empty array is rejected; a single value returns itself; minus infinity entries contribute nothing. Maximum 200000 values.")]
        public double LogSumExp(
            [Description("Values on the log scale.")]
            double[] values)
        {
            NumericsGuard.RequireVector(values, nameof(values), finite: false);

            return values.LogSumExp();
        }

        [AdvancedAnalyticsTool("SoftmaxProbabilities")]
        [Description(
            "Converts scores into probabilities that are positive and sum to 1 using the softmax exp(v_i) / sum of exp(v_j), computed stably. Use it to turn model scores or log-odds into a categorical distribution. " +
            "Adding the same constant to every score does not change the result. Maximum 200000 values.")]
        public NumericsVectorResult SoftmaxProbabilities(
            [Description("Scores to convert.")] double[] scores)
        {
            NumericsGuard.RequireVector(scores, nameof(scores));

            return NumericsGuard.ToVectorResult(Special.Softmax(scores));
        }

        [AdvancedAnalyticsTool("SigmoidAndLogit")]
        [Description(
            "Applies the logistic function or its inverse: Sigmoid maps any real number to a probability between 0 and 1 (sigmoid(0) = 0.5), and Logit maps a probability in (0, 1) back to log-odds. " +
            "Logit of exactly 0 or 1 gives minus or plus infinity. Maximum 200000 values.")]
        public NumericsVectorResult SigmoidAndLogit(
            [Description("Values to transform: real numbers for Sigmoid, probabilities for Logit.")]
            double[] x,
            [Description("Sigmoid maps log-odds to probability; Logit maps probability to log-odds.")]
            NumericsLogisticDirection direction = NumericsLogisticDirection.Sigmoid)
        {
            var link = new LogitLinkFunction();

            return Map(x, nameof(x),
                v => direction == NumericsLogisticDirection.Sigmoid ? link.Inverse(v) : link.Function(v));
        }

        [AdvancedAnalyticsTool("StableLogExpFunctions")]
        [Description(
            "Evaluates accurate forms of common expressions that lose precision when computed naively: Log1p is ln(1 + x) and Expm1 is exp(x) - 1 (both exact for tiny x), Log1m is ln(1 - x), and Softplus is ln(1 + exp(x)) without overflow. " +
            "Use these when x is near zero or very large. Arguments outside each function's domain give NaN. Maximum 200000 values.")]
        public NumericsVectorResult StableLogExpFunctions(
            [Description("Values to evaluate.")] double[] x,
            [Description("Which stable function to evaluate.")]
            NumericsStableFunctionKind kind = NumericsStableFunctionKind.Log1P)
        {
            return Map(x, nameof(x), v => kind switch
            {
                NumericsStableFunctionKind.Log1P => Special.Log1p(v),
                NumericsStableFunctionKind.Expm1 => Special.Expm1(v),
                NumericsStableFunctionKind.Log1M => Special.Log1m(v),
                _ => Special.Log1pexp(v)
            });
        }

        [AdvancedAnalyticsTool("LogAddAndSubtractPairs")]
        [Description(
            "Adds or subtracts pairs of numbers that are stored as natural logs, returning the log of the result: LogSum gives ln(exp(a) + exp(b)) and LogDifference gives ln(exp(a) - exp(b)) which needs a greater than b. " +
            "Use it to combine tiny probabilities without underflow. The two arrays must have equal length. Maximum 200000 pairs.")]
        public NumericsVectorResult LogAddAndSubtractPairs(
            [Description("First log-scale values.")]
            double[] logA,
            [Description("Second log-scale values, same length as logA.")]
            double[] logB,
            [Description("LogSum adds, LogDifference subtracts.")]
            NumericsLogPairOperation operation = NumericsLogPairOperation.LogSum)
        {
            return MapPairs(logA, nameof(logA), logB, nameof(logB),
                (a, b) => operation == NumericsLogPairOperation.LogSum ? Special.LogSum(a, b) : Special.LogDiff(a, b));
        }

        [AdvancedAnalyticsTool("ReciprocalAndInverseHyperbolicFunctions")]
        [Description(
            "Evaluates trigonometric and hyperbolic functions that a basic calculator lacks: secant, cosecant, cotangent, their inverses, hyperbolic secant, cosecant and cotangent, and inverse hyperbolic sine, cosine, tangent, secant, cosecant and cotangent. " +
            "Angles are in radians. Arguments outside a function's domain give NaN or infinity. Maximum 200000 values.")]
        public NumericsVectorResult ReciprocalAndInverseHyperbolicFunctions(
            [Description("Values to evaluate.")] double[] x,
            [Description("Which function to evaluate.")]
            NumericsHyperbolicKind kind)
        {
            return Map(x, nameof(x), v => kind switch
            {
                NumericsHyperbolicKind.Secant => Special.Sec(v),
                NumericsHyperbolicKind.Cosecant => Special.Cosec(v),
                NumericsHyperbolicKind.Cotangent => Special.Cotan(v),
                NumericsHyperbolicKind.InverseSecant => Special.Asec(v),
                NumericsHyperbolicKind.InverseCosecant => Special.Acosec(v),
                NumericsHyperbolicKind.InverseCotangent => Special.Acotan(v),
                NumericsHyperbolicKind.HyperbolicSecant => Special.Sech(v),
                NumericsHyperbolicKind.HyperbolicCosecant => Special.Cosech(v),
                NumericsHyperbolicKind.HyperbolicCotangent => Special.Cotanh(v),
                NumericsHyperbolicKind.InverseHyperbolicSine => Special.Asinh(v),
                NumericsHyperbolicKind.InverseHyperbolicCosine => Special.Acosh(v),
                NumericsHyperbolicKind.InverseHyperbolicTangent => Special.Atanh(v),
                NumericsHyperbolicKind.InverseHyperbolicSecant => Special.Asech(v),
                NumericsHyperbolicKind.InverseHyperbolicCosecant => Special.Acosech(v),
                _ => Special.Acotanh(v)
            });
        }

        [AdvancedAnalyticsTool("StirlingGammaApproximation")]
        [Description(
            "Approximates the gamma function with Stirling's formula, accurate to a fraction of a percent for arguments of about 10 and better for larger ones; it is useful as a cheap check of gamma or factorial values. " +
            "Do not use it for small arguments below about 5. Maximum 200000 values.")]
        public NumericsVectorResult StirlingGammaApproximation(
            [Description("Positive values, ideally 10 or more.")]
            double[] x)
        {
            return Map(x, nameof(x), Gamma.Stirling);
        }

        [AdvancedAnalyticsTool("GaussianBellFunction")]
        [Description(
            "Evaluates the zero-mean Gaussian bell curve exp(-x squared / (2 variance)) / sqrt(2 pi variance) at each x, which is the normal density with a chosen variance, or the two-dimensional bell when a second coordinate array y is given, exp(-(x squared + y squared) / (2 variance)) / (2 pi variance). " +
            "Use it to build smoothing weights or spatial kernels. The variance must be positive. Maximum 200000 values.")]
        public NumericsVectorResult GaussianBellFunction(
            [Description("Positive variance sigma squared of the bell.")]
            double variance,
            [Description("First coordinate values.")]
            double[] x,
            [Description(
                "Optional second coordinate values, same length as x, for the two-dimensional bell; leave empty for one dimension.")]
            double[]? y = null)
        {
            NumericsGuard.RequirePositive(variance, nameof(variance));

            if (y == null || y.Length == 0)
            {
                return Map(x, nameof(x), v => Normal.Gaussian(variance, v));
            }

            return MapPairs(x, nameof(x), y, nameof(y), (a, b) => Normal.Gaussian2D(variance, a, b));
        }

        [AdvancedAnalyticsTool("GaussianKernelWeights")]
        [Description(
            "Returns the weights of a one-dimensional Gaussian smoothing kernel: the bell curve sampled at the integer offsets -r to r where r is half the size, so the middle weight is the largest. Use it with a convolution to smooth a series. " +
            "Size must be odd and between 3 and 101. The weights are not rescaled to sum to 1, so divide by their sum if a weighted average is wanted.")]
        public NumericsVectorResult GaussianKernelWeights(
            [Description("Positive variance sigma squared of the bell.")]
            double variance,
            [Description("Odd kernel length from 3 to 101.")]
            int size)
        {
            NumericsGuard.RequirePositive(variance, nameof(variance));
            NumericsGuard.RequireInRange(size, nameof(size), 3, 101);

            if (size % 2 == 0)
            {
                throw new ArgumentException("'size' must be odd so the kernel has a centre; use 3, 5, 7 and so on.",
                    nameof(size));
            }

            return NumericsGuard.ToVectorResult(Normal.Kernel(variance, size));
        }

        [AdvancedAnalyticsTool("TaylorSeriesApproximation")]
        [Description(
            "Approximates sine, cosine, hyperbolic sine, hyperbolic cosine or the exponential by summing a fixed number of Taylor series terms, so you can see how quickly the series converges. " +
            "More terms give higher accuracy; convergence is slower for large arguments. The number of terms is limited to 1 through 200 and up to 200000 values.")]
        public NumericsVectorResult TaylorSeriesApproximation(
            [Description("Arguments (radians for the trigonometric functions).")]
            double[] x,
            [Description("Which function to approximate.")]
            NumericsTaylorFunction function,
            [Description("Number of series terms to sum, from 1 to 200.")]
            int terms = 10)
        {
            NumericsGuard.RequireInRange(terms, nameof(terms), 1, 200);

            return Map(x, nameof(x), v => function switch
            {
                NumericsTaylorFunction.Sine => Taylor.Sin(v, terms),
                NumericsTaylorFunction.Cosine => Taylor.Cos(v, terms),
                NumericsTaylorFunction.HyperbolicSine => Taylor.Sinh(v, terms),
                NumericsTaylorFunction.HyperbolicCosine => Taylor.Cosh(v, terms),
                _ => Taylor.Exp(v, terms)
            });
        }

        [AdvancedAnalyticsTool("BSplineBasisFunction")]
        [Description(
            "Evaluates the centred cardinal B-spline basis function of a given order at each x: order 0 is a box, order 1 a triangle, order 3 the cubic B-spline; it is zero outside (-(order+1)/2, (order+1)/2). " +
            "Use it to build smooth basis expansions. Order is limited to 0 through 20. Maximum 200000 values.")]
        public NumericsVectorResult BSplineBasisFunction(
            [Description("Positions at which to evaluate the basis function.")]
            double[] x,
            [Description("Spline order n from 0 to 20.")]
            int order)
        {
            NumericsGuard.RequireInRange(order, nameof(order), 0, 20);

            return Map(x, nameof(x), v => Special.BSpline(order, v));
        }

        [AdvancedAnalyticsTool("StableHypotenuse")]
        [Description(
            "Computes sqrt(a squared + b squared) for pairs of numbers without overflow or underflow, even for values around 1e200. Use it for vector lengths and complex magnitudes. " +
            "The two arrays must have equal length. Maximum 200000 pairs.")]
        public NumericsVectorResult StableHypotenuse(
            [Description("First legs a.")] double[] a,
            [Description("Second legs b, same length as a.")]
            double[] b)
        {
            return MapPairs(a, nameof(a), b, nameof(b), Tools.Hypotenuse);
        }

        [AdvancedAnalyticsTool("PlanarAngle")]
        [Description(
            "Returns the angle in radians, between 0 and 2 pi, of the point (x, y) measured anticlockwise from the positive x axis, for each pair of coordinates; it is a full-circle arctangent. " +
            "The two arrays must have equal length. The origin (0, 0) gives 0. Maximum 200000 pairs.")]
        public NumericsVectorResult PlanarAngle(
            [Description("X coordinates.")] double[] x,
            [Description("Y coordinates, same length as x.")]
            double[] y)
        {
            return MapPairs(x, nameof(x), y, nameof(y), Tools.Angle);
        }
    }
}