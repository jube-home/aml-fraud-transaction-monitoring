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

    [Migration(20263107780000)]
    public class AllowMapTilesInContentSecurityPolicy : Migration
    {
        public const string PreviousContentSecurityPolicy =
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; img-src 'self' data: https://*.tile.openstreetmap.org; " +
            "font-src 'self' data:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

        public const string ContentSecurityPolicy =
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: blob: https://*.tile.openstreetmap.org https://tiles.openfreemap.org; " +
            "connect-src 'self' https://tiles.openfreemap.org; worker-src 'self' blob:; child-src blob:; " +
            "font-src 'self' data:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

        public static string BuildSql()
        {
            return $"""
                    UPDATE "HttpResponseHeader"
                    SET "Value" = {Literal(ContentSecurityPolicy)}
                    WHERE "Header" = 'Content-Security-Policy'
                    AND "Value" = {Literal(PreviousContentSecurityPolicy)};
                    """;
        }

        public override void Up()
        {
            Execute.Sql(BuildSql());
        }

        public override void Down()
        {
        }

        private static string Literal(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
        }
    }
}
