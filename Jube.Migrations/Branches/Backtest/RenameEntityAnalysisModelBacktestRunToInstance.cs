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

namespace Jube.Migrations.Branches.Backtest
{
    using FluentMigrator;

    [Migration(20263107420000)]
    public class RenameEntityAnalysisModelBacktestRunToInstance : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $$
                        DECLARE
                            item record;
                        BEGIN
                            IF to_regclass('public."EntityAnalysisModelBacktestRun"') IS NOT NULL
                               AND to_regclass('public."EntityAnalysisModelBacktestInstance"') IS NULL THEN
                                ALTER TABLE "EntityAnalysisModelBacktestRun" RENAME TO "EntityAnalysisModelBacktestInstance";

                                FOR item IN
                                    SELECT indexname FROM pg_indexes
                                    WHERE schemaname = 'public'
                                      AND tablename = 'EntityAnalysisModelBacktestInstance'
                                      AND indexname LIKE '%BacktestRun%'
                                LOOP
                                    EXECUTE format('ALTER INDEX %I RENAME TO %I', item.indexname,
                                        replace(item.indexname, 'BacktestRun', 'BacktestInstance'));
                                END LOOP;

                                FOR item IN
                                    SELECT sequencename FROM pg_sequences
                                    WHERE schemaname = 'public'
                                      AND sequencename LIKE 'EntityAnalysisModelBacktestRun%'
                                LOOP
                                    EXECUTE format('ALTER SEQUENCE %I RENAME TO %I', item.sequencename,
                                        replace(item.sequencename, 'BacktestRun', 'BacktestInstance'));
                                END LOOP;
                            END IF;
                        END $$;
                        """);
        }

        public override void Down()
        {
        }
    }
}