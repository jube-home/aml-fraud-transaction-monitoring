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
using System.Collections.Generic;
using Jube.Graph.Motifs;

namespace Jube.Test.Graph
{
    internal static class MotifFixtures
    {
        public static readonly DateTimeOffset T0 = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

        private static long archiveId = 1;

        public static GraphEvent Event(long from, long to, TimeSpan offset, double amount)
        {
            return new GraphEvent(from, to, T0 + offset, amount, archiveId++);
        }

        public static List<GraphEvent> Benign()
        {
            return
            [
                Event(1, 2, TimeSpan.Zero, 100d),
                Event(3, 4, TimeSpan.FromDays(1), 50d),
                Event(5, 6, TimeSpan.FromDays(2), 70.5d),
                Event(2, 1, TimeSpan.FromDays(10), 20d),
                Event(4, 5, TimeSpan.FromDays(12), 15d),
                Event(6, 3, TimeSpan.FromDays(30), 42d)
            ];
        }

        public static List<GraphEvent> FanOut()
        {
            var events = new List<GraphEvent>();

            for (var i = 0; i < 5; i++)
            {
                events.Add(Event(1, 2 + i, TimeSpan.FromMinutes(10 * i), 100d));
            }

            return events;
        }

        public static List<GraphEvent> FanIn()
        {
            var events = new List<GraphEvent>();

            for (var i = 0; i < 5; i++)
            {
                events.Add(Event(2 + i, 1, TimeSpan.FromMinutes(10 * i), 100d));
            }

            return events;
        }

        public static List<GraphEvent> GatherScatter()
        {
            return
            [
                Event(51, 50, TimeSpan.Zero, 500d),
                Event(52, 50, TimeSpan.FromHours(1), 400d),
                Event(50, 53, TimeSpan.FromHours(2), 450d),
                Event(50, 54, TimeSpan.FromHours(3), 440d)
            ];
        }

        public static List<GraphEvent> ScatterGather()
        {
            return
            [
                Event(60, 61, TimeSpan.Zero, 1000d),
                Event(60, 62, TimeSpan.FromMinutes(10), 1000d),
                Event(61, 63, TimeSpan.FromHours(1), 990d),
                Event(62, 63, TimeSpan.FromHours(2), 980d)
            ];
        }

        public static List<GraphEvent> TemporalCycle()
        {
            return
            [
                Event(70, 71, TimeSpan.Zero, 1000d),
                Event(71, 72, TimeSpan.FromHours(1), 990d),
                Event(72, 70, TimeSpan.FromHours(2), 980d)
            ];
        }

        public static List<GraphEvent> PassThrough()
        {
            return
            [
                Event(81, 80, TimeSpan.Zero, 1000d),
                Event(80, 82, TimeSpan.FromHours(2), 950d),
                Event(83, 80, TimeSpan.FromDays(1), 500d),
                Event(80, 84, TimeSpan.FromDays(1) + TimeSpan.FromHours(3), 480d)
            ];
        }

        public static List<GraphEvent> SmurfTypeOne()
        {
            return
            [
                Event(91, 90, TimeSpan.Zero, 3000d),
                Event(92, 90, TimeSpan.FromHours(1), 3000d),
                Event(93, 90, TimeSpan.FromHours(2), 3000d),
                Event(90, 94, TimeSpan.FromDays(1), 4500d),
                Event(90, 95, TimeSpan.FromDays(2), 4500d)
            ];
        }

        public static List<GraphEvent> SmurfTypeTwo()
        {
            return
            [
                Event(100, 101, TimeSpan.Zero, 3000d),
                Event(100, 102, TimeSpan.FromHours(1), 3000d),
                Event(100, 103, TimeSpan.FromHours(2), 3000d),
                Event(101, 104, TimeSpan.FromDays(1), 2900d),
                Event(102, 104, TimeSpan.FromDays(1) + TimeSpan.FromHours(1), 2900d),
                Event(103, 104, TimeSpan.FromDays(2), 2900d)
            ];
        }

        public static List<GraphEvent> Structuring()
        {
            return
            [
                Event(111, 110, TimeSpan.Zero, 9500d),
                Event(112, 110, TimeSpan.FromDays(1), 9800d),
                Event(113, 110, TimeSpan.FromDays(3), 9200d),
                Event(114, 110, TimeSpan.FromDays(4), 12000d)
            ];
        }

        public static List<GraphEvent> DenseBlock()
        {
            var events = new List<GraphEvent>();
            var step = 0;

            for (long a = 120; a <= 125; a++)
            {
                for (long b = 120; b <= 125; b++)
                {
                    if (a != b)
                    {
                        events.Add(Event(a, b, TimeSpan.FromMinutes(step++), 75d));
                    }
                }
            }

            events.Add(Event(1, 120, TimeSpan.FromDays(1), 10d));
            events.Add(Event(125, 2, TimeSpan.FromDays(2), 10d));

            return events;
        }
    }
}
