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
using Jube.Dictionary.Extensions;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;

namespace Jube.Service.Extensions.PercentagesAndChanges
{
    [FunctionToolGroup("PercentagesAndChanges",
        "Percentages, ratios and growth between two figures, such as the change from last month or the share of a total, computed exactly.")]
    public static class PercentagesAndChangesTools
    {
        [FunctionTool("PercentOfTotal")]
        [Description(
            "What percent a part is of a total; 25 of 200 is 12.5. Use it for shares such as declined spend as a percent of total spend.")]
        public static double PercentOfTotal(
            [Description("The part being measured.")]
            double part,
            [Description("The whole the part belongs to; must not be zero.")]
            double total)
        {
            return FunctionToolInput.Defined(part.PercentOf(total), "the total must not be zero.");
        }

        [FunctionTool("PercentageChange")]
        [Description(
            "Percentage change from a previous value to a current value; 80 to 100 is +25 and 100 to 80 is -20. Use it for period-on-period movement.")]
        public static double PercentageChange(
            [Description("The current or later value.")]
            double current,
            [Description("The previous or baseline value; must not be zero.")]
            double previous)
        {
            return FunctionToolInput.Defined(current.PercentageChangeFrom(previous),
                "the previous value must not be zero.");
        }

        [FunctionTool("PercentDifference")]
        [Description(
            "Symmetric percent difference between two values relative to their average; 90 and 110 differ by 20 percent. Use it when neither value is the baseline.")]
        public static double PercentDifference(
            [Description("The first value.")] double first,
            [Description("The second value.")] double second)
        {
            return FunctionToolInput.Defined(first.PercentDifferenceFrom(second),
                "the two values must not average to zero.");
        }

        [FunctionTool("RatioOfValues")]
        [Description(
            "One value divided by another as a plain ratio; 30 over 120 is 0.25. Use it for velocity ratios, coverage and other quotients.")]
        public static double RatioOfValues(
            [Description("The value on top of the fraction.")]
            double numerator,
            [Description("The value underneath; must not be zero.")]
            double denominator)
        {
            return FunctionToolInput.Defined(numerator.RatioOf(denominator), "the denominator must not be zero.");
        }

        [FunctionTool("LogRatio")]
        [Description(
            "Natural log of numerator over denominator, a symmetric measure of relative size: doubling is +0.693 and halving is -0.693. Both values must be positive.")]
        public static double LogRatio(
            [Description("The value on top of the fraction; must be positive.")]
            double numerator,
            [Description("The value underneath; must be positive.")]
            double denominator)
        {
            return FunctionToolInput.Defined(numerator.LogRatioOf(denominator),
                "the ratio of the two values must be positive.");
        }

        [FunctionTool("CompoundGrowthRate")]
        [Description(
            "Average compound growth rate per period between a starting and an ending value, as a fraction; 100 growing to 121 over 2 periods is 0.1 (10 percent per period).")]
        public static double CompoundGrowthRate(
            [Description("The ending value; must be positive.")]
            double endingValue,
            [Description("The starting value; must be positive.")]
            double startingValue,
            [Description("Number of periods between them; must be positive.")]
            double periods)
        {
            return FunctionToolInput.Defined(endingValue.CompoundGrowthRate(startingValue, periods),
                "values and periods must be positive.");
        }

        [FunctionTool("AddPercentage")]
        [Description(
            "Increases an amount by a percentage; 100 plus 20 percent is 120. Use it for tax, fees, uplifts and mark-ups.")]
        public static double AddPercentage(
            [Description("The starting amount.")] double amount,
            [Description("The percentage to add, such as 20 for 20 percent.")]
            double percent)
        {
            return FunctionToolInput.Defined(amount.AddPercent(percent), "the inputs must be finite.");
        }

        [FunctionTool("SubtractPercentage")]
        [Description(
            "Decreases an amount by a percentage; 100 less 20 percent is 80. Use it for discounts, haircuts and deductions.")]
        public static double SubtractPercentage(
            [Description("The starting amount.")] double amount,
            [Description("The percentage to take off, such as 20 for 20 percent.")]
            double percent)
        {
            return FunctionToolInput.Defined(amount.SubtractPercent(percent), "the inputs must be finite.");
        }

        [FunctionTool("AmountBeforePercentageAdded")]
        [Description(
            "Recovers the original amount before a percentage was added; 120 including 20 percent was 100 before it. Use it to back tax or fees out of a gross amount.")]
        public static double AmountBeforePercentageAdded(
            [Description("The gross amount that already includes the percentage.")]
            double grossAmount,
            [Description("The percentage that was added, such as 20.")]
            double percent)
        {
            return FunctionToolInput.Defined(grossAmount.BaseBeforePercentAdded(percent),
                "the percentage must not be -100.");
        }

        [FunctionTool("IsWithinPercentOf")]
        [Description(
            "Whether a value lies within a percentage tolerance of a target, inclusive; 104 is within 5 percent of 100. Use it for near-match checks on amounts.")]
        public static bool IsWithinPercentOf(
            [Description("The value being checked.")]
            double value,
            [Description("The target value.")] double target,
            [Description("The tolerance as a percentage of the target, such as 5.")]
            double percent)
        {
            return value.IsWithinPercentOf(target, percent);
        }
    }
}