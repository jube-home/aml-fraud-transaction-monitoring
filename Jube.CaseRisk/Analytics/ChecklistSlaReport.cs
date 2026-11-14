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
    public sealed record ChecklistSlaItem([property: Key(0)] Guid ItemGuid, [property: Key(1)] Guid ChecklistGuid, [property: Key(2)] string? ChecklistName, [property: Key(3)] string? ItemName,
        [property: Key(4)] int? TargetMinutes, [property: Key(5)] int Shown, [property: Key(6)] int Hidden, [property: Key(7)] int Completed, [property: Key(8)] int CompletedWithinSla, [property: Key(9)] int CompletedLate, [property: Key(10)] int Open,
        [property: Key(11)] int Breached, [property: Key(12)] double? MedianMinutes, [property: Key(13)] double? P90Minutes, [property: Key(14)] double? Attainment);

    [MessagePackObject]
    public sealed record ChecklistSlaReport([property: Key(0)] List<ChecklistSlaItem> Items, [property: Key(1)] int Shown, [property: Key(2)] int Completed,
        [property: Key(3)] int CompletedWithinSla, [property: Key(4)] int CompletedLate, [property: Key(5)] int Breached, [property: Key(6)] double? Attainment);
}
