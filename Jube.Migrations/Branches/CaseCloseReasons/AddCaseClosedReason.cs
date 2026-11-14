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

namespace Jube.Migrations.Branches.CaseCloseReasons
{
    using FluentMigrator;

    [Migration(2026111015000000)]
    public class AddCaseClosedReason : Migration
    {
        public override void Up()
        {
            Alter.Table("Case")
                .AddColumn("ClosedReasonId").AsByte().Nullable()
                .AddColumn("ClosedReasonNote").AsString(1000).Nullable();

            Execute.Sql("""
                        COMMENT ON COLUMN public."Case"."ClosedReasonId" IS 'Enumeration (CaseCloseReason) of why the case was closed, chosen by the analyst at close: 1 False positive known customer activity, 2 False positive rule too broad, 3 Duplicate of another case, 4 Insufficient evidence, 5 Suspicion confirmed and reported, 6 Fraud confirmed loss prevented, 7 Customer contacted and explained, 8 Other. Null when the case is open or was closed without a reason. Cleared when the case is reopened.';
                        COMMENT ON COLUMN public."Case"."ClosedReasonNote" IS 'Optional free text the analyst entered with the close reason. Null when the case is open. Cleared when the case is reopened.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
