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


namespace Jube.Data.Reporting
{
    public sealed record ReportLimits(int StatementTimeoutSeconds, int MaximumCellBytes, int MaximumResultBytes)
    {
        public static ReportLimits Default { get; } = new(30, 1048576, 33554432);

        public static ReportLimits From(string statementTimeoutSeconds, string maximumCellBytes,
            string maximumResultBytes)
        {
            return new ReportLimits(
                Positive(statementTimeoutSeconds, Default.StatementTimeoutSeconds),
                Positive(maximumCellBytes, Default.MaximumCellBytes),
                Positive(maximumResultBytes, Default.MaximumResultBytes));
        }

        private static int Positive(string text, int fallback)
        {
            return int.TryParse(text, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var value) && value > 0
                ? value
                : fallback;
        }
    }
}
