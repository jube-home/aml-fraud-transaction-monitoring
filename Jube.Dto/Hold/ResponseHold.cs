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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

using MessagePack;

namespace Jube.Dto.Hold
{
    [MessagePackObject]
    public class ResponseHold
    {
        [Key(0)] public int StateId { get; set; }
        [Key(1)] public int RangeId { get; set; }
        [Key(2)] public string? RangeName { get; set; }
        [Key(3)] public int RangeMinimum { get; set; }
        [Key(4)] public int RangeMaximum { get; set; }
        [Key(5)] public int Elevation { get; set; }
        [Key(6)] public DateTime StampedDate { get; set; }
        [Key(7)] public ResponseHoldDecision? Decision { get; set; }
        [Key(8)] public List<ResponseHoldLeg> Legs { get; set; } = [];
    }
}