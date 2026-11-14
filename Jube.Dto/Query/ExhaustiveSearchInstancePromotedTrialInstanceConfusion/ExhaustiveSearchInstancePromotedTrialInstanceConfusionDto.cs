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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion
{
    [Description("Confusion matrix and derived ratios for the active promoted trial instance of an Exhaustive " +
                 "Search Instance. Read-only; all zeros when no active promoted trial instance exists.")]
    [MessagePackObject]
    public class ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto
    {
        [Description("Not populated by the query; always 0.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Score of the active promoted trial instance.")]
        [Key(1)]
        public double Score { get; set; }

        [Description("False positive count.")] [Key(2)] public int FalsePositive { get; set; }
        [Description("True positive count.")] [Key(3)] public int TruePositive { get; set; }
        [Description("False negative count.")] [Key(4)] public int FalseNegative { get; set; }
        [Description("True negative count.")] [Key(5)] public int TrueNegative { get; set; }

        [Description("Table total (as computed by the query).")]
        [Key(6)]
        public int TableTotal { get; set; }

        [Description("TruePositive plus FalseNegative.")]
        [Key(7)]
        public int PositiveRowTotal { get; set; }

        [Description("TruePositive plus FalsePositive.")]
        [Key(8)]
        public int PositiveColumnTotal { get; set; }

        [Description("FalsePositive plus TrueNegative.")]
        [Key(9)]
        public int NegativeRowTotal { get; set; }

        [Description("FalseNegative plus TrueNegative.")]
        [Key(10)]
        public int NegativeColumnTotal { get; set; }

        [Description("Ratio of PositiveRowTotal to the table total, rounded to 2 places.")]
        [Key(11)]
        public double PositiveRowTableTotal { get; set; }

        [Description("Ratio of PositiveColumnTotal to the table total, rounded to 2 places.")]
        [Key(12)]
        public double PositiveColumnTableTotal { get; set; }

        [Description("Ratio of NegativeRowTotal to the table total, rounded to 2 places.")]
        [Key(13)]
        public double NegativeRowTableTotal { get; set; }

        [Description("Ratio of NegativeColumnTotal to the table total, rounded to 2 places.")]
        [Key(14)]
        public double NegativeColumnTableTotal { get; set; }

        [Description("TruePositive ratio over the row total, rounded to 2 places.")]
        [Key(15)]
        public double TruePositiveRowTotal { get; set; }

        [Description("TruePositive ratio over the column total, rounded to 2 places.")]
        [Key(16)]
        public double TruePositiveColumnTotal { get; set; }

        [Description("TruePositive ratio over the table total, rounded to 2 places.")]
        [Key(17)]
        public double TruePositiveTableTotal { get; set; }

        [Description("FalsePositive ratio over the row total, rounded to 2 places.")]
        [Key(18)]
        public double FalsePositiveRowTotal { get; set; }

        [Description("FalsePositive ratio over the column total, rounded to 2 places.")]
        [Key(19)]
        public double FalsePositiveColumnTotal { get; set; }

        [Description("FalsePositive ratio over the table total, rounded to 2 places.")]
        [Key(20)]
        public double FalsePositiveTableTotal { get; set; }

        [Description("FalseNegative ratio over the row total, rounded to 2 places.")]
        [Key(21)]
        public double FalseNegativeRowTotal { get; set; }

        [Description("FalseNegative ratio over the column total, rounded to 2 places.")]
        [Key(22)]
        public double FalseNegativeColumnTotal { get; set; }

        [Description("FalseNegative ratio over the table total, rounded to 2 places.")]
        [Key(23)]
        public double FalseNegativeTableTotal { get; set; }

        [Description("TrueNegative ratio over the row total, rounded to 2 places.")]
        [Key(24)]
        public double TrueNegativeRowTotal { get; set; }

        [Description("TrueNegative ratio over the column total, rounded to 2 places.")]
        [Key(25)]
        public double TrueNegativeColumnTotal { get; set; }

        [Description("TrueNegative ratio over the table total, rounded to 2 places.")]
        [Key(26)]
        public double TrueNegativeTableTotal { get; set; }
    }
}