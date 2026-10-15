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

namespace Jube.Service.Extensions.ThresholdsAndLimits
{
    [FunctionToolGroup("ThresholdsAndLimits",
        "Amount-against-limit arithmetic for structuring, smurfing and limit monitoring: distance and headroom to a threshold, excess and shortfall, round amounts and position in a range.")]
    public static class ThresholdsAndLimitsTools
    {
        [FunctionTool("DistanceToThreshold")]
        [Description(
            "Absolute distance between an amount and a threshold, whichever side it is on; 9600 and 10400 are both 400 from 10000. Use it to see how close a payment came to a reporting limit.")]
        public static double DistanceToThreshold(
            [Description("The amount being assessed.")]
            double amount,
            [Description("The threshold or reporting limit.")]
            double threshold)
        {
            return FunctionToolInput.Defined(amount.DistanceToThreshold(threshold), "the inputs must be finite.");
        }

        [FunctionTool("PercentBelowThreshold")]
        [Description(
            "How far an amount is below a threshold as a percent of the threshold; 9500 against 10000 is 5. Use it to express near-threshold behaviour on a common scale.")]
        public static double PercentBelowThreshold(
            [Description("The amount being assessed.")]
            double amount,
            [Description("The threshold; must not be zero.")]
            double threshold)
        {
            return FunctionToolInput.Defined(amount.PercentBelowThreshold(threshold),
                "the threshold must not be zero.");
        }

        [FunctionTool("IsJustBelowThreshold")]
        [Description(
            "Whether an amount sits under a threshold but within a margin of it, the classic structuring pattern; 9600 is just below 10000 with a 5 percent margin.")]
        public static bool IsJustBelowThreshold(
            [Description("The amount being assessed.")]
            double amount,
            [Description("The threshold or reporting limit.")]
            double threshold,
            [Description("The margin below the threshold that counts as just below, in percent.")]
            double marginPercent)
        {
            return amount.IsJustBelowThreshold(threshold, marginPercent);
        }

        [FunctionTool("HeadroomToLimit")]
        [Description(
            "Remaining room under a limit (limit minus used), negative once the limit is breached; 700 used against 1000 leaves 300 and 1200 used leaves -200. Use it for available balance or remaining allowance.")]
        public static double HeadroomToLimit(
            [Description("The amount already used.")]
            double used,
            [Description("The limit.")] double limit)
        {
            return FunctionToolInput.Defined(used.HeadroomTo(limit), "the inputs must be finite.");
        }

        [FunctionTool("ExcessOverLimit")]
        [Description(
            "How much an amount exceeds a limit, never negative; 1200 against a 1000 limit is 200 over. Use it to size a breach.")]
        public static double ExcessOverLimit(
            [Description("The amount being assessed.")]
            double amount,
            [Description("The limit.")] double limit)
        {
            return FunctionToolInput.Defined(amount.ExcessOver(limit), "the inputs must be finite.");
        }

        [FunctionTool("ShortfallBelowMinimum")]
        [Description(
            "How much an amount falls short of a required minimum, never negative; 80 against a minimum of 100 is 20 short. Use it for minimum balances and floors.")]
        public static double ShortfallBelowMinimum(
            [Description("The amount being assessed.")]
            double amount,
            [Description("The required minimum.")] double minimum)
        {
            return FunctionToolInput.Defined(amount.ShortfallBelow(minimum), "the inputs must be finite.");
        }

        [FunctionTool("IsRoundAmount")]
        [Description(
            "Whether an amount is an exact multiple of a round unit such as 100 or 1000. Round amounts are a common money-laundering indicator when frequent.")]
        public static bool IsRoundAmount(
            [Description("The amount being assessed.")]
            double amount,
            [Description("The round unit, such as 100 or 1000; must be positive.")]
            double roundUnit)
        {
            return amount.IsRoundAmount(roundUnit);
        }

        [FunctionTool("LeadingSignificantDigit")]
        [Description(
            "The first non-zero digit of an amount (1 to 9), or 0 for zero; 4567 gives 4 and 0.0456 gives 4. Collect these across many amounts to compare with the first-digit law.")]
        public static int LeadingSignificantDigit([Description("The amount to inspect.")] double amount)
        {
            return FunctionToolInput.Finite(amount, nameof(amount)).LeadingSignificantDigit();
        }

        [FunctionTool("PositionInRange")]
        [Description(
            "Where a value sits inside a range as a percent from minimum (0) to maximum (100); 75 in 50 to 150 is 25. Values outside the range give below 0 or above 100.")]
        public static double PositionInRange(
            [Description("The value being placed.")]
            double value,
            [Description("The lower end of the range.")]
            double minimum,
            [Description("The upper end of the range; must differ from the minimum.")]
            double maximum)
        {
            return FunctionToolInput.Defined(value.PercentOfRange(minimum, maximum), "the range must not be empty.");
        }
    }
}