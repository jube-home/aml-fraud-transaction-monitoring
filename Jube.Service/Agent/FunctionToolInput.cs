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

namespace Jube.Service.Agent
{
    public static class FunctionToolInput
    {
        public const int MaximumTextLength = 100_000;
        public const int MaximumNameLength = 1_000;
        public const int MaximumListLength = 10_000;

        public static string Text(string? value, string parameterName, int maximumLength = MaximumTextLength)
        {
            var text = value ?? "";

            if (text.Length > maximumLength)
            {
                throw new ArgumentException($"{parameterName} is longer than the {maximumLength} characters allowed.",
                    parameterName);
            }

            return text;
        }

        public static string Required(string? value, string parameterName, int maximumLength = MaximumTextLength)
        {
            var text = Text(value, parameterName, maximumLength);

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException($"{parameterName} is required.", parameterName);
            }

            return text;
        }

        public static string[] List(string[]? values, string parameterName, int maximumItemLength = MaximumNameLength)
        {
            var list = values ?? [];

            if (list.Length > MaximumListLength)
            {
                throw new ArgumentException($"{parameterName} has more than the {MaximumListLength} items allowed.",
                    parameterName);
            }

            foreach (var item in list)
            {
                Text(item, parameterName, maximumItemLength);
            }

            return list;
        }

        public static double Finite(double value, string parameterName)
        {
            if (!double.IsFinite(value))
            {
                throw new ArgumentException($"{parameterName} must be a finite number.", parameterName);
            }

            return value;
        }

        public static double Defined(double result, string reason)
        {
            if (!double.IsFinite(result))
            {
                throw new ArgumentException($"The result is undefined: {reason}");
            }

            return result;
        }

        public static string Found(string result, string reason)
        {
            if (string.IsNullOrEmpty(result))
            {
                throw new ArgumentException($"No result: {reason}");
            }

            return result;
        }

        public static DateTime Utc(DateTime value)
        {
            return value.Kind == DateTimeKind.Local
                ? value.ToUniversalTime()
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        public static T Choice<T>(string? value, string parameterName) where T : struct, Enum
        {
            if (Enum.TryParse<T>((value ?? "").Trim(), true, out var parsed) && Enum.IsDefined(parsed))
            {
                return parsed;
            }

            throw new ArgumentException(
                $"{parameterName} must be one of: {string.Join(", ", Enum.GetNames<T>())}.", parameterName);
        }
    }
}