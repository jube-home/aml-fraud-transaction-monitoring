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
    public sealed record SlaDimensionSummary([property: Key(0)] ServiceLevelDimension Dimension, [property: Key(1)] int Met, [property: Key(2)] int Breached, [property: Key(3)] int AtRisk,
        [property: Key(4)] int OnTrack, [property: Key(5)] double? Attainment);

    [MessagePackObject]
    public sealed record SlaWeek([property: Key(0)] DateTime WeekStart, [property: Key(1)] ServiceLevelDimension Dimension, [property: Key(2)] int Met, [property: Key(3)] int Breached);

    [MessagePackObject]
    public sealed record SlaWatch([property: Key(0)] int CaseId, [property: Key(1)] ServiceLevelDimension Dimension, [property: Key(2)] ServiceLevelState State,
        [property: Key(3)] double ElapsedFraction, [property: Key(4)] DateTime DueDate);

    [MessagePackObject]
    public sealed record SlaReport([property: Key(0)] List<SlaDimensionSummary> Dimensions, [property: Key(1)] List<SlaWeek> Weekly, [property: Key(2)] List<SlaWatch> Watch,
        [property: Key(3)] int OpenBreached, [property: Key(4)] int OpenAtRisk);
}
