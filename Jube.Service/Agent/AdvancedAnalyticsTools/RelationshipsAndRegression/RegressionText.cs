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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal static class RegressionText
    {
        public const string Inputs =
            "Predictor rows: one array of numbers per case, all the same length, one entry per predictor column. NaN marks a missing value.";

        public const string Outputs =
            "The outcome value for each row, in the same order as the rows. NaN marks a missing value.";

        public const string Names =
            "Optional name of each predictor column, unique and in column order. Leave empty to use x1, x2 and so on.";

        public const string Intercept =
            "True (default) includes a constant term; false forces the fitted surface through the origin, which changes how R squared and the F test are defined.";

        public const string Level = "Confidence level for the intervals, strictly between 0 and 1. Default 0.95.";

        public const string Drop =
            "True fits on rows without any NaN and reports how many were dropped; false (default) rejects missing values.";

        public const string Weights =
            "Precision weight for each row (larger means a more reliable row, for example the inverse of its variance). Every weight must be greater than zero.";

        public const string Series =
            "True also returns the fitted value and residual of every row (can be large). Default false.";

        public const string X = "The predictor variable, one number per observation. NaN marks a missing value.";

        public const string Y =
            "The outcome variable, in the same order and of the same length as x. NaN marks a missing value.";
    }
}