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

namespace Jube.Migrations.Branches.Svirfd
{
    using FluentMigrator;

    [Migration(20263108500002)]
    public class AddIpDataTypeColumnComments : Migration
    {
        public override void Up()
        {
            foreach (var table in new[] { "EntityAnalysisModelRequestXpath", "EntityAnalysisModelRequestXpathVersion" })
            {
                Execute.Sql($"""
                             COMMENT ON COLUMN public."{table}"."DataTypeId" IS 'Datatype the value extracted from the request payload is treated as, which governs what rules may predicate on it. 1=String, 2=Integer, 3=Float, 4=Date, 5=Boolean, 6=Latitude, 7=Longitude, 8=IP. Latitude and Longitude are Float in effect; IP is String in effect and is looked up for IP intelligence when a Svirfd entry of the model names it.';
                             """);
            }
        }

        public override void Down()
        {
        }
    }
}
