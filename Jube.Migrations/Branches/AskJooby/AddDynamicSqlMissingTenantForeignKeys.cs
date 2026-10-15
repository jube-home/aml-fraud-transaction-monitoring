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

using System;
using System.Collections.Generic;
using FluentMigrator;

namespace Jube.Migrations.Branches.AskJooby
{
    [Migration(20260917140000)]
    public class AddDynamicSqlMissingTenantForeignKeys : Migration
    {
        private static readonly (string ChildTable, string ChildColumn, string ParentTable, string ParentColumn)[]
            relationships =
            [
                ("EntityAnalysisModelInlineScript", "EntityAnalysisInlineScriptId", "EntityAnalysisInlineScript",
                    "Id"),
                ("ExhaustiveSearchInstance", "EntityAnalysisModelId", "EntityAnalysisModel", "Id"),
                ("ExhaustiveSearchInstanceVersion", "EntityAnalysisModelId", "EntityAnalysisModel", "Id"),
                ("ExhaustiveSearchInstanceTrialInstanceSensitivity", "ExhaustiveSearchInstanceTrialInstanceId",
                    "ExhaustiveSearchInstanceTrialInstance", "Id"),
                ("ExhaustiveSearchInstanceVariableMultiCollinearity", "TestExhaustiveSearchInstanceVariableId",
                    "ExhaustiveSearchInstanceVariable", "Id"),
                ("ArchiveVersion", "EntityAnalysisModelId", "EntityAnalysisModel", "Id")
            ];

        public override void Up()
        {
            var violations = new List<string>();

            Execute.WithConnection((connection, transaction) =>
            {
                foreach (var (childTable, childColumn, parentTable, parentColumn) in relationships)
                {
                    using var command = connection.CreateCommand();
                    command.Transaction = transaction;

                    command.CommandText = $"""
                                           SELECT COUNT(*) FROM "{childTable}" c
                                           LEFT JOIN "{parentTable}" p ON p."{parentColumn}" = c."{childColumn}"
                                           WHERE c."{childColumn}" IS NOT NULL AND p."{parentColumn}" IS NULL
                                           """;

                    var orphanCount = Convert.ToInt64(command.ExecuteScalar());

                    if (orphanCount > 0)
                    {
                        violations.Add(
                            $"-- {orphanCount} orphaned row(s): \"{childTable}\".\"{childColumn}\" with no " +
                            $"matching \"{parentTable}\".\"{parentColumn}\"\n" +
                            $"DELETE FROM \"{childTable}\" WHERE \"{childColumn}\" IS NOT NULL AND " +
                            $"\"{childColumn}\" NOT IN (SELECT \"{parentColumn}\" FROM \"{parentTable}\" " +
                            $"WHERE \"{parentColumn}\" IS NOT NULL);");
                    }
                }
            });

            if (violations.Count > 0)
            {
                throw new InvalidOperationException(
                    "AddDynamicSqlMissingTenantForeignKeys: refusing to add any foreign key constraints -- " +
                    $"{violations.Count} of {relationships.Length} relationship(s) have orphaned rows that " +
                    "would violate them. Review the suggested cleanup below (or your own equivalent) before " +
                    "re-running this migration; nothing has been changed yet.\n\n" +
                    string.Join("\n\n", violations));
            }

            foreach (var (childTable, childColumn, parentTable, parentColumn) in relationships)
            {
                Create.ForeignKey()
                    .FromTable(childTable).ForeignColumn(childColumn)
                    .ToTable(parentTable).PrimaryColumn(parentColumn);
            }
        }

        public override void Down()
        {
        }
    }
}