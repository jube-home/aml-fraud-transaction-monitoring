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

namespace Jube.CaseRisk.Journal
{
    [MessagePackObject]
    public sealed record PeerComparison(
        [property: Key(0)] string Field,
        [property: Key(1)] double KeyMedian,
        [property: Key(2)] int KeyRows,
        [property: Key(3)] double PeerMedian,
        [property: Key(4)] double PeerScale,
        [property: Key(5)] double PeerQ1,
        [property: Key(6)] double PeerQ3,
        [property: Key(7)] int PeerKeys,
        [property: Key(8)] double? Z,
        [property: Key(9)] double Percentile,
        [property: Key(10)] string Direction,
        [property: Key(11)] bool LowConfidence);
}
