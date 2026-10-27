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

    [Migration(20263108110000)]
    public class AddCaseKeyJournalReplayViewSettings : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        ALTER TABLE public."CaseKeyJournalReplay" ADD COLUMN IF NOT EXISTS "ShowAll" smallint NOT NULL DEFAULT 0;
                        ALTER TABLE public."CaseKeyJournalReplay" ADD COLUMN IF NOT EXISTS "Lens" text NULL;

                        COMMENT ON TABLE public."CaseKeyJournalReplay" IS 'A saved view of the case journal on a case: the window, the extra keys, the filter on the Archive, the lens and the map options. Only these settings are stored, never the transactions, and the view is read from the Archive again whenever it is opened. Named replay because the map was the first view that was saved.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."ShowAll" IS '1 when the map draws every transaction of the view with the ones the filter matches picked out, 0 when it draws only the matching ones.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."Lens" IS 'The journal lens the view was on, payload, rules, features, models or sanctions. Null means payload.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
