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

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics.OpenTelemetry
{
    internal sealed class Aggregator(string instrumentName, string instrumentType, string tags)
    {
        public string InstrumentName { get; } = instrumentName;
        public string InstrumentType { get; } = instrumentType;
        public string Tags { get; } = tags;
        public long Count { get; private set; }
        public double Sum { get; private set; }
        public double Min { get; private set; }
        public double Max { get; private set; }

        public void Add(double value)
        {
            Count++;
            Sum += value;
            Min = Count == 1 ? value : Math.Min(Min, value);
            Max = Count == 1 ? value : Math.Max(Max, value);
        }
    }
}