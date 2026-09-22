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

#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using FluentMigrator;

namespace Jube.Migrations.Branches.AbstractionCalculationCoderOnly
{
    [Migration(20260921130000)]
    public class ConvertArithmeticAbstractionCalculationsToCoder : Migration
    {
        private const string CalculationTable = "EntityAnalysisModelAbstractionCalculation";
        private const string VersionTable = "EntityAnalysisModelAbstractionCalculationVersion";

        private static readonly string[] removedColumns =
        [
            "EntityAnalysisModelAbstractionNameLeft",
            "EntityAnalysisModelAbstractionNameRight",
            "AbstractionCalculationTypeId"
        ];

        public sealed record Row(int Id, int? TypeId, string? Left, string? Right);

        public sealed record Conversion(int Id, string Script, bool Converted);

        public static List<Conversion> Plan(IEnumerable<Row> rows)
        {
            var plan = new List<Conversion>();

            foreach (var row in rows)
            {
                if (row.TypeId is not (>= 1 and <= 4))
                {
                    continue;
                }

                plan.Add(AbstractionCalculationScriptBuilder.TryBuild(row.TypeId, row.Left, row.Right, out var script)
                    ? new Conversion(row.Id, script, true)
                    : new Conversion(row.Id,
                        AbstractionCalculationScriptBuilder.BuildPlaceholder(row.TypeId, row.Left, row.Right), false));
            }

            return plan;
        }

        public override void Up()
        {
            Execute.WithConnection((connection, transaction) =>
            {
                ConvertTable(connection, transaction, CalculationTable, deactivateUnconverted: true);
                ConvertTable(connection, transaction, VersionTable, deactivateUnconverted: false);
            });

            foreach (var column in removedColumns)
            {
                Delete.Column(column).FromTable(CalculationTable);
                Delete.Column(column).FromTable(VersionTable);
            }
        }

        private static void ConvertTable(IDbConnection connection, IDbTransaction transaction, string table,
            bool deactivateUnconverted)
        {
            var rows = new List<Row>();

            using (var select = connection.CreateCommand())
            {
                select.Transaction = transaction;
                select.CommandText = $"select \"Id\", \"AbstractionCalculationTypeId\", " +
                                     "\"EntityAnalysisModelAbstractionNameLeft\", " +
                                     "\"EntityAnalysisModelAbstractionNameRight\" " +
                                     $"from \"{table}\" where \"AbstractionCalculationTypeId\" in (1, 2, 3, 4)";

                using var reader = select.ExecuteReader();
                while (reader.Read())
                {
                    rows.Add(new Row(
                        reader.GetInt32(0),
                        reader.IsDBNull(1) ? null : Convert.ToInt32(reader.GetValue(1)),
                        reader.IsDBNull(2) ? null : reader.GetString(2),
                        reader.IsDBNull(3) ? null : reader.GetString(3)));
                }
            }

            foreach (var conversion in Plan(rows))
            {
                using var update = connection.CreateCommand();
                update.Transaction = transaction;

                var deactivate = deactivateUnconverted && !conversion.Converted;
                update.CommandText = $"update \"{table}\" set \"FunctionScript\" = @script" +
                                     (deactivate ? ", \"Active\" = 0" : string.Empty) +
                                     " where \"Id\" = @id";

                var script = update.CreateParameter();
                script.ParameterName = "script";
                script.DbType = DbType.String;
                script.Value = conversion.Script;
                update.Parameters.Add(script);

                var id = update.CreateParameter();
                id.ParameterName = "id";
                id.DbType = DbType.Int32;
                id.Value = conversion.Id;
                update.Parameters.Add(id);

                update.ExecuteNonQuery();
            }
        }

        public override void Down()
        {
            foreach (var table in new[] { CalculationTable, VersionTable })
            {
                Create.Column("EntityAnalysisModelAbstractionNameLeft").OnTable(table).AsString().Nullable();
                Create.Column("EntityAnalysisModelAbstractionNameRight").OnTable(table).AsString().Nullable();
                Create.Column("AbstractionCalculationTypeId").OnTable(table).AsByte().Nullable();
            }
        }
    }
}
