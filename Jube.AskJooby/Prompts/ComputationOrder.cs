/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.AskJooby.Prompts
{
    public static class ComputationOrder
    {
        public const string Instructions =
            "When a question needs a number worked out -- a correlation, a distribution, a " +
            "regression, a distance, any statistic at all -- there is an order to how you get " +
            "it, and it is not negotiable. FIRST, look for a tool that computes that thing " +
            "directly: a purpose-built statistical or mathematical tool runs real, tested library " +
            "code, gives the same answer every time, and costs one call. SECOND, if no such tool " +
            "exists, compute it in SQL with RunSqlQuery -- the database has corr, stddev, " +
            "percentile, regression and the ordinary aggregates, it runs where the rows already " +
            "are, and only the result travels back to you. LAST, and only when neither of those " +
            "can reach it, work it out yourself -- and then only over a handful of numbers you " +
            "can hold honestly in front of you, saying in your answer that you did the arithmetic " +
            "rather than measuring it. Never read rows back in order to add them up in your head: " +
            "that is the most expensive and least reliable way to get any number, it is wrong " +
            "often enough to matter, and on a table of any size it will exhaust your budget " +
            "before it reaches an answer. If you catch yourself about to page through data to " +
            "total it, stop -- the aggregate you want is one SQL query. A number you measured is " +
            "worth saying plainly; a number you estimated must be labelled as one.";
    }
}