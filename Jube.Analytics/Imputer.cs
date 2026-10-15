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

using Jube.Analytics.Models;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Analytics
{
    public readonly record struct Imputer(ImputationStrategy Strategy, double FillValue, bool AddMissingIndicator)
    {
        public static Imputer Fit(double[] values, ImputationStrategy strategy, double constant = 0d,
            bool addMissingIndicator = false)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (strategy == ImputationStrategy.Constant)
            {
                if (!Numerics.IsFinite(constant))
                {
                    throw new ArgumentException("The constant must be finite.");
                }

                return new Imputer(strategy, constant, addMissingIndicator);
            }

            var finite = Preprocessing.FiniteOrThrow(values);

            var fill = strategy switch
            {
                ImputationStrategy.Mean => finite.Average(),
                ImputationStrategy.Median => Numerics.Median(finite.OrderBy(v => v).ToArray()),
                ImputationStrategy.Mode => finite.GroupBy(v => v).OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key).First().Key,
                _ => throw new ArgumentOutOfRangeException(nameof(strategy))
            };

            return new Imputer(strategy, fill, addMissingIndicator);
        }

        public static bool IsMissing(double value)
        {
            return !Numerics.IsFinite(value);
        }

        public double Transform(double value)
        {
            return IsMissing(value) ? FillValue : value;
        }

        public double[] Transform(double[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            return values.Select(Transform).ToArray();
        }

        public double[] MissingIndicator(double[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            return values.Select(v => IsMissing(v) ? 1d : 0d).ToArray();
        }
    }
}