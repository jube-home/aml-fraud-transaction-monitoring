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
using Jube.Data.Poco;
using Jube.Data.Repository.Models;

namespace Jube.Engine.Exhaustive.Calibration
{
    internal sealed class Outcome
    {
        internal string Method { get; init; }
        internal string Input { get; init; }
        internal double? Slope { get; init; }
        internal double? Intercept { get; init; }
        internal string ParametersJson { get; init; }
        internal double? CalibratorEpsilon { get; init; }
        internal double? TrainingPrior { get; init; }
        internal double? CalibrationPrior { get; init; }
        internal double? BaseRate { get; init; }
        internal string BaseRateSource { get; init; }
        internal int SampleSize { get; init; }
        internal string Status { get; init; }
        internal string Reason { get; init; }
        internal CalibrationQuality Quality { get; init; }

        internal void WriteTo(ExhaustiveSearchInstancePromotedTrialInstance row)
        {
            row.CalibrationMethodId = ParseEnum<ExhaustiveCalibrationMethod>(Method);
            row.CalibrationInputId = ParseEnum<ExhaustiveCalibrationInput>(Input);
            row.CalibrationSlope = Slope;
            row.CalibrationIntercept = Intercept;
            row.CalibrationParametersJson = ParametersJson;
            row.CalibrationEpsilon = CalibratorEpsilon;
            row.TrainingPrior = TrainingPrior;
            row.CalibrationPrior = CalibrationPrior;
            row.BaseRate = BaseRate;
            row.BaseRateSourceId = ParseEnum<ExhaustiveBaseRateSource>(BaseRateSource);
            row.CalibrationSampleSize = SampleSize;
            row.CalibrationStatusId = ParseEnum<ExhaustiveCalibrationStatus>(Status);
            row.CalibrationReason = Reason;
            row.CalibrationQualityJson = Quality?.ToJson();
        }

        private static TEnum? ParseEnum<TEnum>(string value) where TEnum : struct, Enum
        {
            return !string.IsNullOrEmpty(value) && Enum.TryParse<TEnum>(value, true, out var parsed)
                ? parsed
                : null;
        }
    }
}