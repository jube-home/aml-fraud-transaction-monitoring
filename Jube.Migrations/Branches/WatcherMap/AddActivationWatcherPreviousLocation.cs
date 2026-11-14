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

namespace Jube.Migrations.Branches.WatcherMap
{
    using FluentMigrator;

    [Migration(20263107770000)]
    public class AddActivationWatcherPreviousLocation : Migration
    {
        public override void Up()
        {
            Alter.Table("ActivationWatcher")
                .AddColumn("PreviousLatitude").AsDouble().Nullable()
                .AddColumn("PreviousLongitude").AsDouble().Nullable()
                .AddColumn("PreviousCreatedDate").AsDateTime().Nullable();

            Execute.Sql("""
                        COMMENT ON COLUMN public."ActivationWatcher"."PreviousLatitude" IS
                        'Latitude of the previous location the engine saw for the same Key and KeyValue, taken from an in-memory record of every qualifying activation and not only those sampled into this table, so a journey can be drawn even when the earlier activation was not itself recorded. Null when the entity had no known previous location.';
                        COMMENT ON COLUMN public."ActivationWatcher"."PreviousLongitude" IS
                        'Longitude of the previous location the engine saw for the same Key and KeyValue. Null when the entity had no known previous location.';
                        COMMENT ON COLUMN public."ActivationWatcher"."PreviousCreatedDate" IS
                        'UTC time the engine saw the entity at the previous location. With the previous and current coordinates and CreatedDate it gives the implied travel speed between the two. Null when the entity had no known previous location.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
