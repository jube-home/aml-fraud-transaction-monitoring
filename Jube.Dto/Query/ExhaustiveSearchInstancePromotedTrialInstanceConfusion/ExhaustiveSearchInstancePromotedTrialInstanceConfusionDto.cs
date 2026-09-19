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
namespace Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion
{
    [Description("Confusion matrix and derived ratios for the active promoted trial instance of an Exhaustive " +
                 "Search Instance. Read-only; all zeros when no active promoted trial instance exists.")]
    public class ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto
    {
        [Description("Not populated by the query; always 0.")]
        public int Id { get; set; }

        [Description("Score of the active promoted trial instance.")]
        public double Score { get; set; }

        [Description("False positive count.")] public int FalsePositive { get; set; }

        [Description("True positive count.")] public int TruePositive { get; set; }

        [Description("False negative count.")] public int FalseNegative { get; set; }

        [Description("True negative count.")] public int TrueNegative { get; set; }

        [Description("Table total (as computed by the query).")]
        public int TableTotal { get; set; }

        [Description("TruePositive plus FalseNegative.")]
        public int PositiveRowTotal { get; set; }

        [Description("TruePositive plus FalsePositive.")]
        public int PositiveColumnTotal { get; set; }

        [Description("FalsePositive plus TrueNegative.")]
        public int NegativeRowTotal { get; set; }

        [Description("FalseNegative plus TrueNegative.")]
        public int NegativeColumnTotal { get; set; }

        [Description("Ratio of PositiveRowTotal to the table total, rounded to 2 places.")]
        public double PositiveRowTableTotal { get; set; }

        [Description("Ratio of PositiveColumnTotal to the table total, rounded to 2 places.")]
        public double PositiveColumnTableTotal { get; set; }

        [Description("Ratio of NegativeRowTotal to the table total, rounded to 2 places.")]
        public double NegativeRowTableTotal { get; set; }

        [Description("Ratio of NegativeColumnTotal to the table total, rounded to 2 places.")]
        public double NegativeColumnTableTotal { get; set; }

        [Description("TruePositive ratio over the row total, rounded to 2 places.")]
        public double TruePositiveRowTotal { get; set; }

        [Description("TruePositive ratio over the column total, rounded to 2 places.")]
        public double TruePositiveColumnTotal { get; set; }

        [Description("TruePositive ratio over the table total, rounded to 2 places.")]
        public double TruePositiveTableTotal { get; set; }

        [Description("FalsePositive ratio over the row total, rounded to 2 places.")]
        public double FalsePositiveRowTotal { get; set; }

        [Description("FalsePositive ratio over the column total, rounded to 2 places.")]
        public double FalsePositiveColumnTotal { get; set; }

        [Description("FalsePositive ratio over the table total, rounded to 2 places.")]
        public double FalsePositiveTableTotal { get; set; }

        [Description("FalseNegative ratio over the row total, rounded to 2 places.")]
        public double FalseNegativeRowTotal { get; set; }

        [Description("FalseNegative ratio over the column total, rounded to 2 places.")]
        public double FalseNegativeColumnTotal { get; set; }

        [Description("FalseNegative ratio over the table total, rounded to 2 places.")]
        public double FalseNegativeTableTotal { get; set; }

        [Description("TrueNegative ratio over the row total, rounded to 2 places.")]
        public double TrueNegativeRowTotal { get; set; }

        [Description("TrueNegative ratio over the column total, rounded to 2 places.")]
        public double TrueNegativeColumnTotal { get; set; }

        [Description("TrueNegative ratio over the table total, rounded to 2 places.")]
        public double TrueNegativeTableTotal { get; set; }
    }
}