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

namespace Jube.CaseRisk.Analytics
{
    public sealed record GainsPoint(double Worked, double Found);

    public sealed record CalibrationBin(double From, double To, int Cases, double Predicted, double Observed);

    public sealed record RankingQualityReport(
        int Outcomes,
        int Confirmed,
        double BaseRate,
        bool Sufficient,
        double? PriorityAuc,
        double? LikelihoodAuc,
        double? OldestFirstAuc,
        double? PriorityLiftTop10,
        double? PriorityLiftTop20,
        double? PriorityLiftTop30,
        List<GainsPoint> PriorityGains,
        List<GainsPoint> LikelihoodGains,
        List<GainsPoint> OldestFirstGains,
        List<CalibrationBin> Calibration,
        double? BrierScore,
        double? ExpectedCalibrationError);
}
