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

using System;
using System.Globalization;

namespace Jube.Parser
{
    public class NumericHelper
    {
        public static bool IsNumeric(object expression)
        {
            switch (expression)
            {
                case null:
                    return false;
                case string s:
                {
                    var provider = Provider(s);

                    if (double.TryParse(s, NumberStyles.Any, provider, out _)) return true;

                    if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        if (long.TryParse(s.AsSpan("0x".Length), NumberStyles.AllowHexSpecifier,
                            provider, out _))
                            return true;
                    }
                    else if (s.StartsWith("&H", StringComparison.OrdinalIgnoreCase))
                    {
                        if (long.TryParse(s.AsSpan("&H".Length), NumberStyles.AllowHexSpecifier,
                            provider, out _))
                            return true;
                    }

                    break;
                }
                default:
                {
                    if (double.TryParse(expression.ToString(), out _))
                        return true;
                    break;
                }
            }

            return bool.TryParse(expression.ToString(), out _);
        }

        public static double Val(string expression)
        {
            if (expression == null)
                return 0;

            if (expression.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                var provider = Provider(expression);

                if (long.TryParse(expression.AsSpan("0x".Length), NumberStyles.AllowHexSpecifier, provider,
                    out var testLong))
                    return testLong;
            }
            else if (expression.StartsWith("&H", StringComparison.OrdinalIgnoreCase))
            {
                var provider = Provider(expression);

                if (long.TryParse(expression.AsSpan("&H".Length), NumberStyles.AllowHexSpecifier, provider,
                    out var testLong))
                    return testLong;
            }


            for (var size = expression.Length; size > 0; size--)
                if (double.TryParse(expression.AsSpan(0, size), out var testDouble))
                    return testDouble;

            //no value is recognized, so return 0:
            return 0;
        }

        public static double Val(object expression)
        {
            if (expression == null)
                return 0;

            if (double.TryParse(expression.ToString(), out var testDouble))
                return testDouble;

            if (bool.TryParse(expression.ToString(), out var testBool))
                return testBool ? -1 : 0;

            return DateTime.TryParse(expression.ToString(), out var testDate) ? testDate.Day : 0;
        }

        public static int Val(char expression)
        {
            return int.TryParse(expression.ToString(), out var testInt) ? testInt : 0;
        }

        private static CultureInfo Provider(string subject)
        {
            return subject.StartsWith("$") ? new CultureInfo("en-US") : CultureInfo.InvariantCulture;
        }
    }
}