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

using System.ComponentModel;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram
{
    public class ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramDto
    {
        [Description("Lower bound of the histogram bin, rounded to two decimal places. Each bin covers the " +
                     "prediction error (actual minus predicted) of the active promoted trial instance of the " +
                     "exhaustive search instance, across ten equal-width bins.")]
        public double Bin { get; set; }

        [Description("Number of prediction errors (actual minus predicted) that fall within this bin.")]
        public int Frequency { get; set; }
    }
}