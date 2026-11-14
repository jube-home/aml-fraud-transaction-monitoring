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

namespace Jube.Migrations.Branches.LegalHoldEvents
{
    using FluentMigrator;

    [Migration(2026111109000000)]
    public class DescribeLegalHoldCaseEventTypes : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $$
                        DECLARE existing text;
                        BEGIN
                            SELECT col_description('public."CaseEvent"'::regclass, a.attnum) INTO existing
                            FROM pg_attribute a
                            WHERE a.attrelid = 'public."CaseEvent"'::regclass AND a.attname = 'CaseEventTypeId';

                            IF existing IS NOT NULL AND position('32=Legal Hold Placed' IN existing) = 0 THEN
                                EXECUTE format('COMMENT ON COLUMN public."CaseEvent"."CaseEventTypeId" IS %L',
                                    replace(existing, ' Anything else renders as Unknown.',
                                        ', 32=Legal Hold Placed (a supervisor placed the case under legal hold; After holds the reason), 33=Legal Hold Lifted (a supervisor lifted it). Anything else renders as Unknown.'));
                            END IF;
                        END
                        $$;
                        """);
        }

        public override void Down()
        {
        }
    }
}
