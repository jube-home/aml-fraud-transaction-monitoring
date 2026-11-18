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

namespace Jube.Migrations.Branches.ExecutiveDashboards
{
    using FluentMigrator;

    [Migration(2026111793000002)]
    public class SeedExecutiveDemonstrationLocations : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        WITH centroid("Country", "Lat", "Lon") AS (VALUES
                            ('GB', 51.5074, -0.1278), ('DE', 52.5200, 13.4050), ('FR', 48.8566, 2.3522),
                            ('NL', 52.3676, 4.9041), ('ES', 40.4168, -3.7038), ('IT', 41.9028, 12.4964),
                            ('PL', 52.2297, 21.0122), ('EG', 30.0444, 31.2357), ('TR', 41.0082, 28.9784),
                            ('AE', 25.2048, 55.2708), ('IR', 35.6892, 51.3890), ('MM', 16.8409, 96.1735),
                            ('KP', 39.0392, 125.7625), ('NG', 6.5244, 3.3792)
                        ),
                        located AS (
                            SELECT a."Id",
                                   round((c."Lat" + ((abs(hashtext(a."Json"->'payload'->>'AccountId')) % 1000) - 500) / 1000.0 * 0.9
                                       + ((abs(hashtext(a."Json"->'payload'->>'TxnId')) % 100) - 50) / 100.0 * 0.04)::numeric, 4) AS "Latitude",
                                   round((c."Lon" + ((abs(hashtext(reverse(a."Json"->'payload'->>'AccountId'))) % 1000) - 500) / 1000.0 * 1.4
                                       + ((abs(hashtext(reverse(a."Json"->'payload'->>'TxnId'))) % 100) - 50) / 100.0 * 0.06)::numeric, 4) AS "Longitude"
                            FROM "Archive" a
                            JOIN centroid c ON c."Country" = a."Json"->'payload'->>'BillingCountry'
                            WHERE a."Json"->'payload'->>'TxnId' LIKE 'EXE-%'
                              AND NOT (a."Json"->'payload' ? 'AccountLatitude')
                        )
                        UPDATE "Archive" a
                        SET "Json" = jsonb_set(a."Json", '{payload}',
                            (a."Json"->'payload') || jsonb_build_object('AccountLatitude', l."Latitude", 'AccountLongitude', l."Longitude"))
                        FROM located l
                        WHERE a."Id" = l."Id";
                        """);
        }

        public override void Down()
        {
        }
    }
}
