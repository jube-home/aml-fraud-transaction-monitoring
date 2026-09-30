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

namespace Jube.Migrations.Branches.ZapWarnNewHardening
{
    using FluentMigrator;

    [Migration(20260929200000)]
    public class HardenSiteIsolationAndContentSecurityPolicy : Migration
    {
        private const string PreviousContentSecurityPolicy =
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; img-src 'self' data: https://*.tile.openstreetmap.org; " +
            "font-src 'self' data:; frame-ancestors 'none'";

        private const string ContentSecurityPolicy =
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; img-src 'self' data: https://*.tile.openstreetmap.org; " +
            "font-src 'self' data:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

        public override void Up()
        {
            Update.Table("HttpResponseHeader")
                .Set(new { Value = ContentSecurityPolicy })
                .Where(new { Header = "Content-Security-Policy" });

            Insert.IntoTable("HttpResponseHeader").Row(new
            {
                Header = "Cross-Origin-Opener-Policy",
                Value = "same-origin"
            });

            Insert.IntoTable("HttpResponseHeader").Row(new
            {
                Header = "Cross-Origin-Embedder-Policy",
                Value = "credentialless"
            });
        }

        public override void Down()
        {
            Delete.FromTable("HttpResponseHeader").Row(new { Header = "Cross-Origin-Embedder-Policy" });
            Delete.FromTable("HttpResponseHeader").Row(new { Header = "Cross-Origin-Opener-Policy" });

            Update.Table("HttpResponseHeader")
                .Set(new { Value = PreviousContentSecurityPolicy })
                .Where(new { Header = "Content-Security-Policy" });
        }
    }
}