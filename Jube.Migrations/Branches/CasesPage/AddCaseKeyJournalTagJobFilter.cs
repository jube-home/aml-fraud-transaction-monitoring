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

    [Migration(20263108050000)]
    public class AddCaseKeyJournalTagJobFilter : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        ALTER TABLE public."CaseKeyJournalTagJob" ADD COLUMN IF NOT EXISTS "FilterJson" text NULL;
                        ALTER TABLE public."CaseKeyJournalTagJob" ADD COLUMN IF NOT EXISTS "FilterLens" text NULL;
                        ALTER TABLE public."CaseKeyJournalTagJob" ADD COLUMN IF NOT EXISTS "FilterCaseId" integer NULL;

                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."FilterJson" IS 'The journal column filter the user had on when they asked to tag everything matching, as a JSON object holding the rule group and the kind of each column. Null when there was no filter. Only transactions that match it are tagged.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."FilterLens" IS 'The journal lens the filter was written against, because its columns are the lens columns. Null when there was no filter.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."FilterCaseId" IS 'The case the user was working on, needed only when the filter refers to pinned transactions, which belong to a case. Null otherwise.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
