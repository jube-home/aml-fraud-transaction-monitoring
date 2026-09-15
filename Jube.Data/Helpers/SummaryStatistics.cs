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
using System.Linq;
using Accord.Statistics;
using Accord.Statistics.Visualizations;
using Jube.Dto.Payload;

namespace Jube.Data.Helpers
{
    public static class SummaryStatistics
    {
        public static ColumnStatistics Compute(double[] values)
        {
            if (values.Length == 0)
            {
                return new ColumnStatistics(0, 0, 0, 0, 0, Array.Empty<HistogramBucket>());
            }

            var histogram = new Histogram();
            histogram.Compute(values, 10);

            var standardDeviation = values.Length == 1 ? 0 : values.StandardDeviation();

            return new ColumnStatistics(
                values.Min(), values.Max(), values.Mean(), values.Median(), standardDeviation,
                histogram.Bins.Select(b => new HistogramBucket(Math.Round(b.Range.Min, 2), b.Value)).ToList());
        }

        public static PayloadStatistics Build(IDictionary<string, double[]> columnValues)
        {
            return new PayloadStatistics(columnValues.ToDictionary(kv => kv.Key, kv => Compute(kv.Value)));
        }
    }
}