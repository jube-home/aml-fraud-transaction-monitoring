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
using Npgsql;

namespace Jube.Data.Reporting
{
    public static class ReportConnectionAdvice
    {
        public static ReportConnectionAssessment Assess(string reportConnectionString,
            string applicationConnectionString)
        {
            if (string.IsNullOrWhiteSpace(reportConnectionString))
            {
                return ReportConnectionAssessment.Unset;
            }

            if (string.IsNullOrWhiteSpace(applicationConnectionString))
            {
                return ReportConnectionAssessment.Separate;
            }

            try
            {
                var report = new NpgsqlConnectionStringBuilder(reportConnectionString);
                var application = new NpgsqlConnectionStringBuilder(applicationConnectionString);

                return string.Equals(report.Username, application.Username, StringComparison.OrdinalIgnoreCase)
                    ? ReportConnectionAssessment.SameLogin
                    : ReportConnectionAssessment.Separate;
            }
            catch (ArgumentException)
            {
                return ReportConnectionAssessment.Separate;
            }
        }

        public static string Warning(ReportConnectionAssessment assessment)
        {
            return assessment switch
            {
                ReportConnectionAssessment.Unset =>
                    "ReportConnectionString is not set, so user-supplied reporting SQL (visualisation datasources, " +
                    "case SQL, filters and samples) runs as the application database login, protected only by a " +
                    "read-only transaction and the SQL validator. Set ReportConnectionString to a SELECT-only " +
                    "Postgres role for production; see the Visualisations security policy documentation.",
                ReportConnectionAssessment.SameLogin =>
                    "ReportConnectionString uses the same database login as ConnectionString, so user-supplied " +
                    "reporting SQL runs with the application's privileges, protected only by a read-only " +
                    "transaction and the SQL validator. Use a separate SELECT-only Postgres role for production; " +
                    "see the Visualisations security policy documentation.",
                _ => null
            };
        }
    }
}
