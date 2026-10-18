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
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Helpers.TextLists.Models;

namespace Jube.Service.Helpers.TextLists
{
    [FunctionToolGroup("TextLists",
        "Exact set operations on lists of text values: distinct values, union, intersection, difference, symmetric difference, joining and comparing lists, optionally ignoring case. Numeric sorting and frequency tables are analytics tools.")]
    public static class TextListsTools
    {
        [FunctionTool("DistinctValues")]
        [Description(
            "The list with duplicates removed, keeping the first occurrence of each value in its original order, optionally ignoring case and surrounding spaces.")]
        public static string[] DistinctValues(
            [Description("The values, up to 10000.")]
            string[] values,
            [Description("True to treat values differing only in case as the same.")]
            bool ignoreCase = false,
            [Description("True to trim spaces from each value first.")]
            bool trim = true)
        {
            return Prepare(values, nameof(values), trim).Distinct(Comparer(ignoreCase)).ToArray();
        }

        [FunctionTool("UnionOfLists")]
        [Description(
            "Every distinct value that appears in either list, first list order then new values from the second.")]
        public static string[] UnionOfLists(
            [Description("The first list, up to 10000.")]
            string[] first,
            [Description("The second list, up to 10000.")]
            string[] second,
            [Description("True to ignore upper and lower case.")]
            bool ignoreCase = false)
        {
            return Prepare(first, nameof(first), true)
                .Union(Prepare(second, nameof(second), true), Comparer(ignoreCase)).ToArray();
        }

        [FunctionTool("IntersectionOfLists")]
        [Description(
            "The distinct values that appear in both lists, in first-list order. Use it to find shared devices, addresses or counterparties.")]
        public static string[] IntersectionOfLists(
            [Description("The first list, up to 10000.")]
            string[] first,
            [Description("The second list, up to 10000.")]
            string[] second,
            [Description("True to ignore upper and lower case.")]
            bool ignoreCase = false)
        {
            return Prepare(first, nameof(first), true)
                .Intersect(Prepare(second, nameof(second), true), Comparer(ignoreCase)).ToArray();
        }

        [FunctionTool("DifferenceOfLists")]
        [Description(
            "The distinct values in the first list that are not in the second, in first-list order; for example new counterparties this month compared with last month.")]
        public static string[] DifferenceOfLists(
            [Description("The list to keep values from, up to 10000.")]
            string[] first,
            [Description("The list of values to remove, up to 10000.")]
            string[] second,
            [Description("True to ignore upper and lower case.")]
            bool ignoreCase = false)
        {
            return Prepare(first, nameof(first), true)
                .Except(Prepare(second, nameof(second), true), Comparer(ignoreCase)).ToArray();
        }

        [FunctionTool("CompareLists")]
        [Description(
            "Compares two lists in one call: values only in the first, only in the second, in both, and whether they hold the same set of distinct values.")]
        public static ListComparison CompareLists(
            [Description("The first list, up to 10000.")]
            string[] first,
            [Description("The second list, up to 10000.")]
            string[] second,
            [Description("True to ignore upper and lower case.")]
            bool ignoreCase = false)
        {
            var comparer = Comparer(ignoreCase);
            var a = Prepare(first, nameof(first), true);
            var b = Prepare(second, nameof(second), true);
            var onlyFirst = a.Except(b, comparer).ToArray();
            var onlySecond = b.Except(a, comparer).ToArray();

            return new ListComparison(onlyFirst, onlySecond, a.Intersect(b, comparer).ToArray(),
                onlyFirst.Length == 0 && onlySecond.Length == 0);
        }

        [FunctionTool("CountDistinctValues")]
        [Description(
            "How many different values a list holds, optionally ignoring case; the list a, A, b has 3 distinct values, or 2 ignoring case.")]
        public static int CountDistinctValues(
            [Description("The values, up to 10000.")]
            string[] values,
            [Description("True to treat values differing only in case as the same.")]
            bool ignoreCase = false)
        {
            return Prepare(values, nameof(values), true).Distinct(Comparer(ignoreCase)).Count();
        }

        [FunctionTool("JoinValues")]
        [Description(
            "Joins a list into one text with a separator, such as a comma and a space, for display or for building a rule's list literal.")]
        public static string JoinValues(
            [Description("The values, up to 10000.")]
            string[] values,
            [Description("The separator placed between values.")]
            string separator = ", ")
        {
            return string.Join(FunctionToolInput.Text(separator, nameof(separator), 100),
                FunctionToolInput.List(values, nameof(values)));
        }

        private static string[] Prepare(string[]? values, string parameterName, bool trim)
        {
            var list = FunctionToolInput.List(values, parameterName);

            return trim ? list.Select(v => v.Trim()).ToArray() : list.Select(v => v).ToArray();
        }

        private static StringComparer Comparer(bool ignoreCase)
        {
            return ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        }
    }
}