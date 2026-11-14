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

    [Migration(20263108100000)]
    public class DropCaseKeyJournalTagJobFilterLensAndCase : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        ALTER TABLE public."CaseKeyJournalTagJob" DROP COLUMN IF EXISTS "FilterLens";
                        ALTER TABLE public."CaseKeyJournalTagJob" DROP COLUMN IF EXISTS "FilterCaseId";

                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."FilterJson" IS 'The filter on the Archive the user had on when they asked to tag everything matching, as a JSON object of an AND or OR condition and rules, each rule a field of the model, an operator and a value, or a nested group. It is compiled again against the model''s fields when the job runs. Null when there was no filter. Only transactions that match it are tagged.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."FilterJson" IS 'The filter on the Archive saved with the replay, as a JSON object of an AND or OR condition and rules, each rule a field of the model, an operator and a value, or a nested group. Null when the replay has no filter.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
