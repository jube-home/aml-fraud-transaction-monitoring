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

    [Migration(20263108120000)]
    public class AddVisualisationRegistryContextKind : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        ALTER TABLE public."VisualisationRegistry" ADD COLUMN IF NOT EXISTS "ContextKindId" smallint NULL;
                        ALTER TABLE public."VisualisationRegistryVersion" ADD COLUMN IF NOT EXISTS "ContextKindId" smallint NULL;

                        DO $fold$
                        BEGIN
                            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public'
                                       AND table_name = 'VisualisationRegistry' AND column_name = 'ShowInDirectory') THEN
                                UPDATE public."VisualisationRegistry" SET "ContextKindId" = CASE WHEN "ShowInDirectory" = 1 THEN 1 ELSE 2 END WHERE "ContextKindId" IS NULL;
                            END IF;

                            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public'
                                       AND table_name = 'VisualisationRegistryVersion' AND column_name = 'ShowInDirectory') THEN
                                UPDATE public."VisualisationRegistryVersion" SET "ContextKindId" = CASE WHEN "ShowInDirectory" = 1 THEN 1 ELSE 2 END WHERE "ContextKindId" IS NULL;
                            END IF;
                        END
                        $fold$;

                        UPDATE public."VisualisationRegistry" SET "ContextKindId" = 1 WHERE "ContextKindId" IS NULL;
                        ALTER TABLE public."VisualisationRegistry" ALTER COLUMN "ContextKindId" SET DEFAULT 1;

                        ALTER TABLE public."VisualisationRegistry" DROP COLUMN IF EXISTS "ShowInDirectory";
                        ALTER TABLE public."VisualisationRegistryVersion" DROP COLUMN IF EXISTS "ShowInDirectory";

                        COMMENT ON COLUMN public."VisualisationRegistry"."ContextKindId" IS 'Where the visualisation is shown: 1 Directory, listed and recalled by the roles it is granted to, 2 Case, embedded as the dashboard of a Case. It replaces the former Show In Directory flag: a registry that was shown in the Directory is 1 and one that was not is 2. A registry shown in a Case receives the built-in SQL parameter @Id, the id of the Case, from which anything else is traced in the SQL.';
                        COMMENT ON COLUMN public."VisualisationRegistryVersion"."ContextKindId" IS 'The value of ContextKindId at the time the version was recorded.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
