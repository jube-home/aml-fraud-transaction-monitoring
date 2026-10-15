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

using System.Globalization;
using System.Text;

namespace Jube.Analytics.Categorical
{
    public static class CategoricalNormaliser
    {
        public const int Version = 1;
        public const int MaxKeyLength = 128;
        public const int MaxLabelLength = 64;

        public static string? Key(string? raw)
        {
            var cleaned = Clean(raw, MaxKeyLength, true);

            return cleaned.Length == 0 ? null : cleaned;
        }

        public static string Label(string? raw)
        {
            return Clean(raw, MaxLabelLength, false);
        }

        public static string ToRaw(bool value)
        {
            return value ? "true" : "false";
        }

        public static string ToRaw(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToRaw(double value)
        {
            return double.IsFinite(value) ? value.ToString("R", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string Clean(string? raw, int maximumLength, bool fold)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return string.Empty;
            }

            var scrubbed = StripUnpairedSurrogates(raw);
            string composed;

            try
            {
                composed = scrubbed.Normalize(NormalizationForm.FormKC);
            }
            catch (ArgumentException)
            {
                composed = scrubbed;
            }

            var builder = new StringBuilder(Math.Min(composed.Length, maximumLength + 4));
            var pendingSpace = false;

            foreach (var character in composed)
            {
                var category = char.GetUnicodeCategory(character);

                if (category is UnicodeCategory.Control or UnicodeCategory.Format)
                {
                    if (char.IsWhiteSpace(character))
                    {
                        pendingSpace = builder.Length > 0;
                    }

                    continue;
                }

                if (char.IsWhiteSpace(character) || category is UnicodeCategory.LineSeparator
                        or UnicodeCategory.ParagraphSeparator or UnicodeCategory.SpaceSeparator)
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(character);
            }

            var result = builder.ToString();

            if (fold)
            {
                result = result.ToLowerInvariant();

                try
                {
                    result = result.Normalize(NormalizationForm.FormKC);
                }
                catch (ArgumentException)
                {
                }
            }

            if (result.Length > maximumLength)
            {
                var length = maximumLength;

                if (char.IsHighSurrogate(result[length - 1]))
                {
                    length--;
                }

                result = result[..length].TrimEnd();
            }

            return result;
        }

        private static string StripUnpairedSurrogates(string value)
        {
            var clean = true;

            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    i++;
                    continue;
                }

                if (char.IsSurrogate(value[i]))
                {
                    clean = false;
                    break;
                }
            }

            if (clean)
            {
                return value;
            }

            var builder = new StringBuilder(value.Length);

            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    builder.Append(value[i]).Append(value[i + 1]);
                    i++;
                }
                else if (!char.IsSurrogate(value[i]))
                {
                    builder.Append(value[i]);
                }
            }

            return builder.ToString();
        }
    }
}