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

    [Migration(20263108070000)]
    public class AddCaseKeyJournalReplayTravelLimits : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        ALTER TABLE public."CaseKeyJournalReplay" ADD COLUMN IF NOT EXISTS "LimitKmh" double precision NULL;
                        ALTER TABLE public."CaseKeyJournalReplay" ADD COLUMN IF NOT EXISTS "MinimumKilometres" double precision NULL;

                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."LimitKmh" IS 'The speed in kilometres an hour above which a leg between two consecutive transactions is counted as impossible. Null means the default of 900.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."MinimumKilometres" IS 'The shortest leg in kilometres that can be counted as impossible, so that a short hop is never flagged however fast. Null means the default of 50.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
