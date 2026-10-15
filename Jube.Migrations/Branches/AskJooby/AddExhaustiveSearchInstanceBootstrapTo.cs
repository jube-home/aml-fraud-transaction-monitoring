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

using FluentMigrator;

namespace Jube.Migrations.Branches.AskJooby
{
    [Migration(20263107550000)]
    public class AddExhaustiveSearchInstanceBootstrapTo : Migration
    {
        public override void Up()
        {
            Alter.Table("ExhaustiveSearchInstance")
                .AddColumn("BootstrapTo").AsInt32().Nullable().WithDefaultValue(30000)
                .AddColumn("BootstrapWeightFactor").AsDouble().Nullable().WithDefaultValue(1d);

            Execute.Sql(
                "update \"ExhaustiveSearchInstance\" set \"BootstrapTo\" = 30000 where \"BootstrapTo\" is null");

            Execute.Sql(
                "update \"ExhaustiveSearchInstance\" set \"BootstrapWeightFactor\" = 1 " +
                "where \"BootstrapWeightFactor\" is null");
        }

        public override void Down()
        {
        }
    }
}