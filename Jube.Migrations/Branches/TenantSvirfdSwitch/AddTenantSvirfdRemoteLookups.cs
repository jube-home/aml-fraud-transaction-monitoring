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

namespace Jube.Migrations.Branches.TenantSvirfdSwitch
{
    using FluentMigrator;

    [Migration(20263108620001)]
    public class AddTenantSvirfdRemoteLookups : Migration
    {
        public override void Up()
        {
            foreach (var table in new[] { "TenantRegistry", "TenantRegistryVersion" })
            {
                Execute.Sql($"""
                             ALTER TABLE public."{table}" ADD COLUMN "SvirfdRemoteLookupsEnabled" smallint NULL DEFAULT 1;
                             COMMENT ON COLUMN public."{table}"."SvirfdRemoteLookupsEnabled" IS 'Landlord-set data-protection switch. When 1 or null the engine may send IP addresses from this Tenant''s transactions to the remote Svirfd service. 0 means no remote Svirfd lookup is made for this Tenant (local and directory lookups are unaffected).';
                             """);
            }
        }

        public override void Down()
        {
        }
    }
}
