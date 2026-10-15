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
using System.Text.RegularExpressions;
using Jube.Dictionary.Extensions;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;

namespace Jube.Service.Extensions.PatternMatching
{
    [FunctionToolGroup("PatternMatching",
        "Regular-expression and wildcard matching with a hard time limit per call: test, find, capture, count, replace and escape.")]
    public static class PatternMatchingTools
    {
        [FunctionTool("RegexIsMatch")]
        [Description(
            ".NET regular expression test: whether the pattern matches anywhere in the text. Matching is time-limited, so a pattern that would backtrack catastrophically is refused rather than hanging.")]
        public static bool RegexIsMatch(
            [Description("The text to test.")] string text,
            [Description("The .NET regular expression pattern.")]
            string pattern,
            [Description("True to ignore upper and lower case.")]
            bool ignoreCase = false)
        {
            return Guard(() =>
                FunctionToolInput.Text(text, nameof(text)).IsMatch(Pattern(pattern), Options(ignoreCase)));
        }

        [FunctionTool("RegexFindAll")]
        [Description(
            "Every match of a .NET regular expression in a text, in order, as the matched texts (up to 10000). Matching is time-limited.")]
        public static string[] RegexFindAll(
            [Description("The text to search.")] string text,
            [Description("The .NET regular expression pattern.")]
            string pattern,
            [Description("True to ignore upper and lower case.")]
            bool ignoreCase = false)
        {
            return Guard(() => FunctionToolInput.Text(text, nameof(text)).Matches(Pattern(pattern), Options(ignoreCase))
                .Take(FunctionToolInput.MaximumListLength).Select(m => m.Value).ToArray());
        }

        [FunctionTool("RegexCapture")]
        [Description(
            "The text captured by a numbered group of the first match of a .NET regular expression, or empty if there is no match; group 1 of 'order-([0-9]+)' in 'order-12345-x' is '12345'. Group 0 is the whole match.")]
        public static string RegexCapture(
            [Description("The text to search.")] string text,
            [Description("The .NET regular expression pattern with capture groups.")]
            string pattern,
            [Description("The capture group number; 0 is the whole match.")]
            int group = 1)
        {
            return Guard(() => FunctionToolInput.Text(text, nameof(text)).RegexCapture(Pattern(pattern), group));
        }

        [FunctionTool("RegexCountMatches")]
        [Description(
            "Number of non-overlapping matches of a .NET regular expression in a text. Matching is time-limited.")]
        public static int RegexCountMatches(
            [Description("The text to search.")] string text,
            [Description("The .NET regular expression pattern.")]
            string pattern)
        {
            return Guard(() => FunctionToolInput.Text(text, nameof(text)).RegexMatchCount(Pattern(pattern)));
        }

        [FunctionTool("RegexReplaceAll")]
        [Description(
            "Replaces every match of a .NET regular expression; the replacement may use $1, $2 for groups. 'a1b22' with [0-9]+ replaced by # is 'a#b#'. Matching is time-limited.")]
        public static string RegexReplaceAll(
            [Description("The text to edit.")] string text,
            [Description("The .NET regular expression pattern.")]
            string pattern,
            [Description("The replacement text, which may reference groups as $1.")]
            string replacement)
        {
            return Guard(() => FunctionToolInput.Text(text, nameof(text)).RegexReplace(Pattern(pattern),
                FunctionToolInput.Text(replacement, nameof(replacement), 10_000)));
        }

        [FunctionTool("EscapeForRegex")]
        [Description(
            "Escapes a literal text so it can be placed inside a regular expression and match itself exactly; '1.5*2' becomes '1\\.5\\*2'.")]
        public static string EscapeForRegex([Description("The literal text to escape.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text), 10_000).EscapedForRegex();
        }

        [FunctionTool("WildcardIsMatch")]
        [Description(
            "Whether a whole text matches a simple wildcard pattern: * is any run of characters, ? is one character, # is one digit and [abc] or [!abc] is a character set; 'INV-2026-*' matches 'INV-2026-0042'. Case-sensitive.")]
        public static bool WildcardIsMatch(
            [Description("The text to test.")] string text,
            [Description("The wildcard pattern.")] string pattern)
        {
            return Guard(() =>
                FunctionToolInput.Text(text, nameof(text))
                    .IsLike(FunctionToolInput.Required(pattern, nameof(pattern), 1000)));
        }

        private static string Pattern(string? pattern)
        {
            return FunctionToolInput.Required(pattern, nameof(pattern), 2000);
        }

        private static RegexOptions Options(bool ignoreCase)
        {
            return ignoreCase ? RegexOptions.IgnoreCase | RegexOptions.CultureInvariant : RegexOptions.None;
        }

        private static T Guard<T>(Func<T> match)
        {
            try
            {
                return match();
            }
            catch (RegexMatchTimeoutException)
            {
                throw new ArgumentException(
                    "The pattern took too long on this text; simplify it, for example by removing nested repetition.");
            }
            catch (RegexParseException exception)
            {
                throw new ArgumentException($"The pattern is not a valid regular expression: {exception.Error}.");
            }
        }
    }
}