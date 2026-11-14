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

namespace Jube.Migrations.Branches.Retention
{
    using FluentMigrator;

    [Migration(2026111019000000)]
    public class AddCaseLegalHold : Migration
    {
        public override void Up()
        {
            Alter.Table("Case")
                .AddColumn("LegalHold").AsByte().Nullable()
                .AddColumn("LegalHoldReason").AsString(500).Nullable()
                .AddColumn("LegalHoldUser").AsString().Nullable()
                .AddColumn("LegalHoldDate").AsDateTime().Nullable();

            Execute.Sql("""
                        UPDATE public."Case" SET "LegalHold" = 0 WHERE "LegalHold" IS NULL;
                        COMMENT ON COLUMN public."Case"."LegalHold" IS 'Flag (1 true, 0 false). While 1 the retention purge never removes the archived transaction the case was raised from, whatever its age.';
                        COMMENT ON COLUMN public."Case"."LegalHoldReason" IS 'Why the case is under legal hold, required when the hold is set.';
                        COMMENT ON COLUMN public."Case"."LegalHoldUser" IS 'Who last set or cleared the legal hold.';
                        COMMENT ON COLUMN public."Case"."LegalHoldDate" IS 'When the legal hold was last set or cleared (UTC).';
                        """);
        }

        public override void Down()
        {
        }
    }
}
