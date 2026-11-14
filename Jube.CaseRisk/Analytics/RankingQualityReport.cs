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

using MessagePack;

namespace Jube.CaseRisk.Analytics
{
    [MessagePackObject]
    public sealed record GainsPoint([property: Key(0)] double Worked, [property: Key(1)] double Found);

    [MessagePackObject]
    public sealed record CalibrationBin([property: Key(0)] double From, [property: Key(1)] double To, [property: Key(2)] int Cases, [property: Key(3)] double Predicted, [property: Key(4)] double Observed);

    [MessagePackObject]
    public sealed record RankingQualityReport(
        [property: Key(0)] int Outcomes,
        [property: Key(1)] int Confirmed,
        [property: Key(2)] double BaseRate,
        [property: Key(3)] bool Sufficient,
        [property: Key(4)] double? PriorityAuc,
        [property: Key(5)] double? LikelihoodAuc,
        [property: Key(6)] double? OldestFirstAuc,
        [property: Key(7)] double? PriorityLiftTop10,
        [property: Key(8)] double? PriorityLiftTop20,
        [property: Key(9)] double? PriorityLiftTop30,
        [property: Key(10)] List<GainsPoint> PriorityGains,
        [property: Key(11)] List<GainsPoint> LikelihoodGains,
        [property: Key(12)] List<GainsPoint> OldestFirstGains,
        [property: Key(13)] List<CalibrationBin> Calibration,
        [property: Key(14)] double? BrierScore,
        [property: Key(15)] double? ExpectedCalibrationError);
}
