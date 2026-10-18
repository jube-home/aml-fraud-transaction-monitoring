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

using System;
using System.Text.Json;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Analytics.Calibration;

namespace Jube.Engine.Exhaustive.Calibration
{
    public sealed class ExhaustiveCalibration
    {
        public const string MethodNone = "none";
        public const string MethodPlatt = "platt";
        public const string MethodIsotonic = "isotonic";
        public const string MethodBeta = "beta";
        public const string InputRaw = "raw";
        public const string InputLogit = "logit";
        public const string StatusCalibrated = "Calibrated";
        public const string StatusCalibratedLowSample = "CalibratedLowSample";
        public const string StatusSkipped = "Skipped";
        public const string StatusNotCalibrated = "NotCalibrated";
        public const string StatusInvalid = "Invalid";
        private readonly BetaCalibrator? betaCalibrator;
        private readonly IsotonicCalibrator isotonicCalibrator;
        private readonly Calibrator? plattCalibrator;

        private ExhaustiveCalibration(string method, Calibrator? plattCalibrator,
            IsotonicCalibrator isotonicCalibrator, BetaCalibrator? betaCalibrator, double calibrationPrior,
            double? baseRate, string baseRateSource, string status, string invalidReason)
        {
            Method = method;
            this.plattCalibrator = plattCalibrator;
            this.isotonicCalibrator = isotonicCalibrator;
            this.betaCalibrator = betaCalibrator;
            CalibrationPrior = calibrationPrior;
            BaseRate = baseRate;
            BaseRateSource = baseRateSource;
            Status = status;
            InvalidReason = invalidReason;
        }

        public static ExhaustiveCalibration Identity { get; } =
            new(MethodNone, null, null, null, 0.5, null, null, StatusNotCalibrated, null);

        public string Method { get; }

        public bool IsCalibrated => plattCalibrator.HasValue || isotonicCalibrator is not null ||
                                    betaCalibrator.HasValue;

        public bool IsInvalid => InvalidReason != null;
        public string Status { get; }
        public string InvalidReason { get; }
        public double CalibrationPrior { get; }
        public double? BaseRate { get; }
        public string BaseRateSource { get; }
        public double Slope => plattCalibrator?.Slope ?? double.NaN;
        public double Intercept => plattCalibrator?.Intercept ?? double.NaN;

        public static ExhaustiveCalibration Invalid(string reason)
        {
            return new ExhaustiveCalibration(MethodNone, null, null, null, 0.5, null, null, StatusInvalid, reason);
        }

        public static ExhaustiveCalibration FromColumns(string method, string input, double? slope,
            double? intercept, double? epsilon, double? calibrationPrior, double? baseRate, string baseRateSource,
            string parametersJson = null)
        {
            if (string.IsNullOrEmpty(method) || method == MethodNone)
            {
                return Identity;
            }

            try
            {
                if (baseRate.HasValue)
                {
                    if (!(baseRate.Value is > 0 and < 1))
                    {
                        return Invalid("The stored base rate is not strictly between 0 and 1.");
                    }

                    if (!calibrationPrior.HasValue || !(calibrationPrior.Value is > 0 and < 1))
                    {
                        return Invalid("The stored calibration prior is not strictly between 0 and 1.");
                    }
                }

                return method switch
                {
                    MethodPlatt => FromPlattColumns(input, slope, intercept, epsilon, calibrationPrior, baseRate,
                        baseRateSource),
                    MethodIsotonic => FromIsotonicColumns(parametersJson, calibrationPrior, baseRate,
                        baseRateSource),
                    MethodBeta => FromBetaColumns(parametersJson, calibrationPrior, baseRate, baseRateSource),
                    _ => Invalid($"The calibration method '{method}' is not known.")
                };
            }
            catch (ArgumentException ex)
            {
                return Invalid(ex.Message);
            }
            catch (FormatException ex)
            {
                return Invalid(ex.Message);
            }
        }

        private static ExhaustiveCalibration FromPlattColumns(string input, double? slope, double? intercept,
            double? epsilon, double? calibrationPrior, double? baseRate, string baseRateSource)
        {
            var mode = input switch
            {
                InputRaw => CalibrationInput.RawScore,
                InputLogit => CalibrationInput.LogitOfBoundedScore,
                _ => throw new ArgumentException($"The calibration input '{input}' is not known.")
            };

            if (!slope.HasValue || !intercept.HasValue || !epsilon.HasValue)
            {
                return Invalid("The calibrator slope, intercept or epsilon is missing.");
            }

            var fitted = Calibrator.FromPrimitives([slope.Value, intercept.Value, (double)mode, epsilon.Value]);

            if (!fitted.IsIncreasing)
            {
                return Invalid("The calibrator slope is not positive.");
            }

            return new ExhaustiveCalibration(MethodPlatt, fitted, null, null, calibrationPrior ?? 0.5, baseRate,
                baseRateSource, StatusCalibrated, null);
        }

        private static ExhaustiveCalibration FromIsotonicColumns(string parametersJson, double? calibrationPrior,
            double? baseRate, string baseRateSource)
        {
            if (string.IsNullOrWhiteSpace(parametersJson))
            {
                return Invalid("The isotonic calibrator parameters are missing.");
            }

            var fitted = IsotonicCalibrator.FromJson(parametersJson);

            return new ExhaustiveCalibration(MethodIsotonic, null, fitted, null, calibrationPrior ?? 0.5, baseRate,
                baseRateSource, StatusCalibrated, null);
        }

        private static ExhaustiveCalibration FromBetaColumns(string parametersJson, double? calibrationPrior,
            double? baseRate, string baseRateSource)
        {
            if (string.IsNullOrWhiteSpace(parametersJson))
            {
                return Invalid("The beta calibrator parameters are missing.");
            }

            var primitives = JsonSerializer.Deserialize<double[]>(parametersJson) ??
                             throw new FormatException("The beta calibrator parameters are empty.");

            var fitted = BetaCalibrator.FromPrimitives(primitives);

            return new ExhaustiveCalibration(MethodBeta, null, null, fitted, calibrationPrior ?? 0.5, baseRate,
                baseRateSource, StatusCalibrated, null);
        }

        private double ApplyRaw(double raw)
        {
            if (plattCalibrator is { } platt)
            {
                return platt.Apply(raw);
            }

            if (isotonicCalibrator is { } isotonic)
            {
                return isotonic.Apply(raw);
            }

            if (betaCalibrator is { } beta)
            {
                return beta.Apply(raw);
            }

            return raw;
        }

        public double Apply(double raw)
        {
            if (IsInvalid)
            {
                throw new InvalidOperationException(
                    $"The stored calibrator is invalid and the model will not score: {InvalidReason}");
            }

            if (!IsCalibrated || !double.IsFinite(raw))
            {
                return raw;
            }

            var calibrated = ApplyRaw(raw);

            return BaseRate.HasValue ? PriorShift.Apply(calibrated, CalibrationPrior, BaseRate.Value) : calibrated;
        }

        public double ApplyAtSamplePrior(double raw)
        {
            if (IsInvalid)
            {
                throw new InvalidOperationException(
                    $"The stored calibrator is invalid and the model will not score: {InvalidReason}");
            }

            return !IsCalibrated || !double.IsFinite(raw) ? raw : ApplyRaw(raw);
        }
    }
}