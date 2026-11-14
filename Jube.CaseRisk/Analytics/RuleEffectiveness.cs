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
    public sealed record RuleEffectiveness(
        [property: Key(0)] Guid? RuleGuid,
        [property: Key(1)] string Name,
        [property: Key(2)] int Cases,
        [property: Key(3)] int Open,
        [property: Key(4)] int Confirmed,
        [property: Key(5)] int NotConfirmed,
        [property: Key(6)] int NoOutcome,
        [property: Key(7)] double? Precision,
        [property: Key(8)] double MeanHandlingMinutes,
        [property: Key(9)] double WastedMinutes,
        [property: Key(10)] double ShareOfWastedEffort);
}
