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

namespace Jube.Migrations.Branches.AbstractionCursor
{
    using FluentMigrator;

    [Migration(2026110912000000)]
    public class AddAbstractionRuleIterationOrderAndFunctionParameters : Migration
    {
        private const string IterationOrderComment =
            "Order in which the cache records for the Search Key are presented to the rule cursor, the Offset " +
            "reduction and the aggregation function. 1=Ascending (oldest first, the default), 2=Descending (most " +
            "recent first). Null is treated as 1. The fetch itself is unchanged: it is always the most recent " +
            "records up to the Search Key fetch limit.";

        private const string FunctionParametersComment =
            "Static parameters for the aggregation function selected by SearchFunctionTypeId, as a JSON object " +
            "of parameter name to number, with 1 or 0 for a true or false parameter, for example " +
            "{\"trimFraction\": 0.1}. Null or empty uses the " +
            "default of every parameter. Only functions numbered 17 and above take parameters.";

        private const string FunctionTypeComment =
            "The aggregation computed over the matched historic transactions. 1=Count, 2=Distinct Count, 3=Sum, " +
            "4=Average, 5=Median, 6=Kurtosis, 7=Skew, 8=Standard Deviation, 11=Mode, 12=Same Count, 13=Actual " +
            "Value, 14=Max, 15=Min, 16=Since, 17=Geometric Mean, 18=Harmonic Mean, 19=Trimmed Mean, " +
            "20=Winsorised Mean, 21=Exponentially Weighted Mean, 22=Root Mean Square, 23=Variance, 24=Range, " +
            "25=Interquartile Range, 26=Percentile, 27=Median Absolute Deviation, 28=Mean Absolute Deviation, " +
            "29=Coefficient Of Variation, 30=Entropy, 31=Gini Coefficient, 32=Nth Value, 33=First To Last " +
            "Change, 34=Largest Step, 35=Count Above, 36=Count Below, 37=Share Above, 38=Sum Above, 39=Longest " +
            "Run Above, 40=Last Value Z-Score, 41=Last Value Modified Z-Score, 42=Last Value Percentile Rank, " +
            "43=Last Value To Mean Ratio, 44=Trend Slope, 45=Autocorrelation, 46=Cursor Result (the number the " +
            "rule assigned to Cursor.Result during the scan). The numbering is not contiguous: 9 and 10 are " +
            "not in use. Functions 17 to 45 read their static parameters from SearchFunctionParameters.";

        public override void Up()
        {
            foreach (var table in new[]
                         { "EntityAnalysisModelAbstractionRule", "EntityAnalysisModelAbstractionRuleVersion" })
            {
                Execute.Sql($"""
                             ALTER TABLE public."{table}" ADD COLUMN "IterationOrderId" smallint NULL DEFAULT 1;
                             ALTER TABLE public."{table}" ADD COLUMN "SearchFunctionParameters" jsonb NULL;
                             """);

                Comment(table, "IterationOrderId", IterationOrderComment);
                Comment(table, "SearchFunctionParameters", FunctionParametersComment);
                Comment(table, "SearchFunctionTypeId", FunctionTypeComment);
            }
        }

        public override void Down()
        {
        }

        private void Comment(string table, string column, string text)
        {
            var quoted = text.Replace("'", "''");

            Execute.Sql($"COMMENT ON COLUMN public.\"{table}\".\"{column}\" IS '{quoted}';");
        }
    }
}
