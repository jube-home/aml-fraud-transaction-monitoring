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

using System.Collections.Generic;
using Jube.Analytics.Categorical;

namespace Jube.Test.Analytics.Categorical
{
    internal static class CategoricalTestData
    {
        internal static CategoricalCounter Count(params (string? Value, int Times)[] values)
        {
            var counter = new CategoricalCounter();

            foreach (var (value, times) in values)
            {
                for (var i = 0; i < times; i++)
                {
                    counter.Add(value);
                }
            }

            return counter;
        }

        internal static CategoricalOptions Options(int minimum = 5, int maxLevels = 25)
        {
            return new CategoricalOptions { Enabled = true, MinLevelCount = minimum, MaxLevels = maxLevels }
                .Sanitised();
        }

        internal static CategoricalFieldEncoder Country()
        {
            var counter = Count(("GB", 100), ("US", 60), ("NG", 30), ("FR", 10), ("XX", 4), ("YY", 3), (null, 12));

            return CategoricalFieldEncoder.Fit("Payload.Country", counter, Options())!;
        }

        internal static IEnumerable<string?> Sample()
        {
            yield return "GB";
            yield return "gb";
            yield return "  Gb ";
            yield return "US";
            yield return "ZZ";
            yield return null;
            yield return "";
            yield return "​GB";
        }
    }
}