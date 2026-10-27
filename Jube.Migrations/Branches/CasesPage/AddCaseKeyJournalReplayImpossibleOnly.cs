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

    [Migration(20263108080000)]
    public class AddCaseKeyJournalReplayImpossibleOnly : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        ALTER TABLE public."CaseKeyJournalReplay" ADD COLUMN IF NOT EXISTS "ImpossibleOnly" smallint NOT NULL DEFAULT 0;

                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."ImpossibleOnly" IS '1 when the replay shows only the legs that could not have been travelled, with the transactions at either end of them, so that the movement worth a look is not lost among ordinary journeys. 0 shows every transaction.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
