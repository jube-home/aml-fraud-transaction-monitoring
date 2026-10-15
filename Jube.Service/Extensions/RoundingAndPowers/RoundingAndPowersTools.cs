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

namespace Jube.Service.Extensions.RoundingAndPowers
{
    [FunctionToolGroup("RoundingAndPowers",
        "Exact rounding, clamping, powers, roots and logarithms of single numbers, so an answer never depends on mental arithmetic.")]
    public static class RoundingAndPowersTools
    {
        [FunctionTool("RoundNumber")]
        [Description(
            "Rounds a number to a number of decimal places using an explicit rule for halves. Use it whenever a figure must be presented or compared at a fixed precision; AwayFromZero is the everyday rule (2.5 becomes 3), ToEven is banker's rounding (2.5 becomes 2).")]
        public static double RoundNumber(
            [Description("The number to round.")] double value,
            [Description("Decimal places to keep, from 0 to 15.")]
            int decimalPlaces,
            [Description(
                "Midpoint rule: AwayFromZero (default), ToEven, ToZero, ToNegativeInfinity or ToPositiveInfinity.")]
            string mode = "AwayFromZero")
        {
            if (decimalPlaces is < 0 or > 15)
            {
                throw new ArgumentException("decimalPlaces must be from 0 to 15.", nameof(decimalPlaces));
            }

            return FunctionToolInput.Finite(value, nameof(value)).Round(decimalPlaces,
                FunctionToolInput.Choice<MidpointRounding>(mode, nameof(mode)));
        }

        [FunctionTool("RoundToNearestIncrement")]
        [Description(
            "Rounds a number to the nearest multiple of an increment, halves away from zero; for example 1234 to the nearest 50 is 1250, and 0.125 to the nearest 0.05 is 0.15. Use it for price points, reporting bands and cash denominations.")]
        public static double RoundToNearestIncrement(
            [Description("The number to round.")] double value,
            [Description("The positive step to round to, such as 50, 0.05 or 1000.")]
            double increment)
        {
            return FunctionToolInput.Defined(value.RoundToNearest(increment),
                "the increment must be a positive finite number.");
        }

        [FunctionTool("RoundDownToWholeNumber")]
        [Description(
            "Largest whole number less than or equal to the value (the floor); -2.5 becomes -3. Use it for completed units such as whole days or whole bands.")]
        public static double RoundDownToWholeNumber([Description("The number to round down.")] double value)
        {
            return FunctionToolInput.Finite(value, nameof(value)).Floor();
        }

        [FunctionTool("RoundUpToWholeNumber")]
        [Description(
            "Smallest whole number greater than or equal to the value (the ceiling); 2.1 becomes 3. Use it for counts of units needed to cover an amount.")]
        public static double RoundUpToWholeNumber([Description("The number to round up.")] double value)
        {
            return FunctionToolInput.Finite(value, nameof(value)).Ceiling();
        }

        [FunctionTool("TruncateToWholeNumber")]
        [Description(
            "Drops the fractional part of a number, moving towards zero; -2.7 becomes -2 and 2.7 becomes 2. Use it when the integer part is wanted regardless of sign.")]
        public static double TruncateToWholeNumber([Description("The number to truncate.")] double value)
        {
            return FunctionToolInput.Finite(value, nameof(value)).Truncate();
        }

        [FunctionTool("ClampNumber")]
        [Description(
            "Limits a number to an inclusive range, returning the minimum when below it and the maximum when above it. Use it to cap scores, limits or percentages.")]
        public static double ClampNumber(
            [Description("The number to limit.")] double value,
            [Description("Lowest allowed result.")]
            double minimum,
            [Description("Highest allowed result; must not be below minimum.")]
            double maximum)
        {
            if (minimum > maximum)
            {
                throw new ArgumentException("minimum must not exceed maximum.", nameof(minimum));
            }

            return FunctionToolInput.Finite(value, nameof(value)).Clamp(minimum, maximum);
        }

        [FunctionTool("RaiseToPower")]
        [Description(
            "Raises a base to an exponent, including fractional and negative exponents; 2 to the 10 is 1024 and 9 to the 0.5 is 3. Use it for compounding and scale factors.")]
        public static double RaiseToPower(
            [Description("The base number.")] double baseValue,
            [Description("The exponent to raise the base to.")]
            double exponent)
        {
            return FunctionToolInput.Defined(baseValue.Pow(exponent),
                "a negative base needs a whole-number exponent, and the result must be finite.");
        }

        [FunctionTool("NthRoot")]
        [Description(
            "The real n-th root of a number; the cube root of 27 is 3 and the cube root of -8 is -2. An even root of a negative number has no real answer and is refused.")]
        public static double NthRoot(
            [Description("The number to take the root of.")]
            double value,
            [Description("The degree of the root, such as 2 for square root or 3 for cube root.")]
            double degree)
        {
            return FunctionToolInput.Defined(value.NthRoot(degree),
                "an even root of a negative number, or a zero degree, has no real value.");
        }

        [FunctionTool("Logarithm")]
        [Description(
            "Logarithm of a positive number in any base; base 10 of 1000 is 3, base 2 of 8 is 3, and base e (2.718281828) gives the natural log. Use it to compare orders of magnitude.")]
        public static double Logarithm(
            [Description("The positive number to take the logarithm of.")]
            double value,
            [Description("The positive base other than 1, such as 10, 2 or 2.718281828459045.")]
            double logarithmBase = 10)
        {
            return FunctionToolInput.Defined(value.Log(logarithmBase),
                "the value must be positive and the base positive and not 1.");
        }
    }
}