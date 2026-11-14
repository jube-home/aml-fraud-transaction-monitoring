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

namespace Jube.Analytics.Aggregation
{
    public sealed record AggregationParameterDescriptor(
        string Name,
        AggregationParameterKind Kind,
        double Default,
        double? Minimum,
        double? Maximum,
        bool MinimumIsExclusive,
        bool MaximumIsExclusive,
        string Description)
    {
        public string? Check(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return $"{Name} must be a finite number.";
            }

            var kindError = Kind switch
            {
                AggregationParameterKind.WholeNumber
                    when Math.Abs(value - Math.Round(value)) > 0 || Math.Abs(value) > int.MaxValue =>
                    $"{Name} must be a whole number.",
                AggregationParameterKind.Flag when value is not (0 or 1) => $"{Name} must be true or false.",
                _ => null
            };

            if (kindError != null)
            {
                return kindError;
            }

            if (Minimum.HasValue && (value < Minimum.Value || (MinimumIsExclusive && value <= Minimum.Value)))
            {
                return $"{Name} must be {(MinimumIsExclusive ? "greater than" : "at least")} {Minimum.Value}.";
            }

            if (Maximum.HasValue && (value > Maximum.Value || (MaximumIsExclusive && value >= Maximum.Value)))
            {
                return $"{Name} must be {(MaximumIsExclusive ? "less than" : "at most")} {Maximum.Value}.";
            }

            return null;
        }
    }
}
