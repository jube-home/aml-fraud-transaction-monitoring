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

namespace Jube.Migrations.Branches.CaseAgentRuns
{
    using FluentMigrator;

    [Migration(2026111101500000)]
    public class DescribeTriageCaseEventTypes : Migration
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

                            IF existing IS NOT NULL AND position('29=Triage' IN existing) = 0 THEN
                                EXECUTE format('COMMENT ON COLUMN public."CaseEvent"."CaseEventTypeId" IS %L',
                                    replace(existing, ' Anything else renders as Unknown.',
                                        ', 29=Triage Recommended (the triage agent recommended an outcome; After holds the CaseStatusOutcome id, 0 being inconclusive), 30=Triage Accepted (an analyst accepted the recommendation; Before holds the outcome id), 31=Triage Overridden (an analyst overrode it; Before holds the outcome id and After the reason). Anything else renders as Unknown.'));
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
