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

namespace Jube.Migrations.Branches.PayloadExtraction
{
    using FluentMigrator;

    [Migration(2026110712000000)]
    public class AddEnablePayloadJObjectToEntityAnalysisModel : Migration
    {
        public override void Up()
        {
            Alter.Table("EntityAnalysisModel")
                .AddColumn("EnablePayloadJObject").AsByte().Nullable();

            Alter.Table("EntityAnalysisModelVersion")
                .AddColumn("EnablePayloadJObject").AsByte().Nullable();

            Execute.Sql("""
                        UPDATE public."EntityAnalysisModel" SET "EnablePayloadJObject" = 1;

                        UPDATE public."EntityAnalysisModelVersion" SET "EnablePayloadJObject" = 1;

                        COMMENT ON COLUMN public."EntityAnalysisModel"."EnablePayloadJObject" IS 'Switch for the request payload document: 1 parses every request into a Newtonsoft JObject that is held on the payload and available to inline scripts, 0 reads only the configured request XPaths in a single streaming pass and builds the JObject only if something asks for it. 0 is faster but only applies while every request XPath, the entry XPath and the reference date XPath are simple dotted or indexed paths; otherwise the model parses as if the switch were 1. Null is treated as 1.';
                        COMMENT ON COLUMN public."EntityAnalysisModelVersion"."EnablePayloadJObject" IS 'The EnablePayloadJObject switch of the earlier version of the model.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
