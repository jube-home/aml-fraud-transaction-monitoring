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

namespace Jube.Service.Extensions.UnitsAndNumerals
{
    [FunctionToolGroup("UnitsAndNumerals",
        "Converts measurements between units and whole numbers between Roman numerals and other bases, plus simple whole-number facts.")]
    public static class UnitsAndNumeralsTools
    {
        [FunctionTool("ConvertUnits")]
        [Description(
            "Converts a measurement between units of the same kind. Length: m km cm mm mi yd ft in nmi. Mass: kg g mg t lb oz st ozt. Area: m2 km2 ha acre ft2 mi2. Volume: l ml m3 gal ukgal qt pt floz. Speed: m/s km/h mph kn. Time: s ms min h d wk. Data: b kb mb gb tb kib mib gib tib. Temperature: c f k.")]
        public static double ConvertUnits(
            [Description("The measurement to convert.")]
            double value,
            [Description("The unit the value is in, from the list in the tool description.")]
            string fromUnit,
            [Description("The unit to convert to; must measure the same kind of quantity.")]
            string toUnit)
        {
            return FunctionToolInput.Defined(
                FunctionToolInput.Finite(value, nameof(value)).ConvertUnits(fromUnit, toUnit),
                "the units are unknown or measure different kinds of quantity.");
        }

        [FunctionTool("IntegerToRomanNumerals")]
        [Description("Writes a whole number from 1 to 3999 as Roman numerals; 1994 is MCMXCIV.")]
        public static string IntegerToRomanNumerals([Description("A whole number from 1 to 3999.")] int value)
        {
            return FunctionToolInput.Found(value.ToRomanNumerals(), "only 1 to 3999 can be written as Roman numerals.");
        }

        [FunctionTool("RomanNumeralsToInteger")]
        [Description(
            "Reads canonical Roman numerals as a whole number, case-insensitive; MCMXCIV is 1994. Non-canonical forms such as IIII are refused.")]
        public static int RomanNumeralsToInteger([Description("The Roman numerals to read.")] string numerals)
        {
            var value = FunctionToolInput.Required(numerals, nameof(numerals), 32).FromRomanNumerals();

            if (value == 0)
            {
                throw new ArgumentException($"'{numerals}' is not a canonical Roman numeral from 1 to 3999.",
                    nameof(numerals));
            }

            return value;
        }

        [FunctionTool("IntegerToBase")]
        [Description(
            "Writes a whole number in another base from 2 to 36 using lowercase digits; 255 in base 16 is ff and -10 in base 2 is -1010.")]
        public static string IntegerToBase(
            [Description("The whole number to write.")]
            long value,
            [Description("The base, from 2 to 36.")]
            int radix)
        {
            return FunctionToolInput.Found(value.ToRadixString(radix), "the base must be from 2 to 36.");
        }

        [FunctionTool("BaseToInteger")]
        [Description(
            "Reads a whole number written in a base from 2 to 36, case-insensitive, with an optional leading minus; ff in base 16 is 255.")]
        public static long BaseToInteger(
            [Description("The digits to read, such as ff or -1010.")]
            string digits,
            [Description("The base the digits are written in, from 2 to 36.")]
            int radix)
        {
            try
            {
                return FunctionToolInput.Required(digits, nameof(digits), 80).FromRadixString(radix);
            }
            catch (FormatException exception)
            {
                throw new ArgumentException(exception.Message, nameof(digits));
            }
            catch (OverflowException)
            {
                throw new ArgumentException("The number is too large for a 64-bit integer.", nameof(digits));
            }
        }

        [FunctionTool("IsPrimeNumber")]
        [Description(
            "Whether a whole number is prime (divisible only by 1 and itself); 1 and negative numbers are not prime.")]
        public static bool IsPrimeNumber([Description("The whole number to test.")] int value)
        {
            return value.IsPrime();
        }

        [FunctionTool("DigitSum")]
        [Description(
            "Sum of the decimal digits of a whole number, ignoring the sign; 9999 gives 36. Useful for check-digit reasoning and digit patterns.")]
        public static int DigitSum([Description("The whole number whose digits are added.")] int value)
        {
            return value.DigitSum();
        }
    }
}