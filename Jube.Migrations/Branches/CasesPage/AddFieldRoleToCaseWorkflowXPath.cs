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

namespace Jube.Migrations.Branches.CasesPage
{
    using FluentMigrator;

    [Migration(20263107990000)]
    public class AddFieldRoleToCaseWorkflowXPath : Migration
    {
        public override void Up()
        {
            foreach (var table in new[] { "CaseWorkflowXPath", "CaseWorkflowXPathVersion" })
            {
                Alter.Table(table).AddColumn("RoleId").AsByte().NotNullable().WithDefaultValue(0);
                Alter.Table(table).AddColumn("Segment").AsByte().NotNullable().WithDefaultValue(0);
            }

            Execute.Sql("""
                        COMMENT ON COLUMN public."CaseWorkflowXPath"."RoleId" IS
                        'What kind of field this is to the Case Key Journal analytics and to network mapping. 0 Inferred from sampled values, 1 Measure (a number that is compared with its own history, such as an amount), 2 Category (a small set of values such as a channel), 3 Counterparty (the other party to a transaction), 4 Identifier (a key that links transactions, such as a device), 5 Time, 6 Geography. Anything other than 0 overrides the inference.';
                        COMMENT ON COLUMN public."CaseWorkflowXPath"."Segment" IS
                        'When 1 the field splits the tenant peer distributions of the Case Key Journal, so that a value is compared with peers in the same segment, such as the customer tier. Null or 0 compares with all peers of the workflow.';
                        COMMENT ON COLUMN public."CaseWorkflowXPathVersion"."RoleId" IS
                        'Version history copy of CaseWorkflowXPath.RoleId.';
                        COMMENT ON COLUMN public."CaseWorkflowXPathVersion"."Segment" IS
                        'Version history copy of CaseWorkflowXPath.Segment.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
