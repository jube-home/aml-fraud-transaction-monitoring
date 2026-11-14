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

namespace Jube.Migrations.Branches.TenantHttpAdaptation
{
    using FluentMigrator;

    [Migration(20263108600001)]
    public class AddTenantHttpAdaptationSettings : Migration
    {
        public override void Up()
        {
            foreach (var table in new[] { "TenantRegistry", "TenantRegistryVersion" })
            {
                Execute.Sql($"""
                             ALTER TABLE public."{table}" ADD COLUMN "HttpAdaptationUrl" character varying(2048) NULL;
                             ALTER TABLE public."{table}" ADD COLUMN "HttpAdaptationTimeoutMilliseconds" integer NULL;
                             ALTER TABLE public."{table}" ADD COLUMN "HttpAdaptationValidateSsl" smallint NULL;
                             COMMENT ON COLUMN public."{table}"."HttpAdaptationUrl" IS 'Landlord-set base URL that this Tenant''s HTTP Adaptation endpoints are appended to. Null falls back to the HttpAdaptationUrl environment setting. Must be https, public, and carry no credentials, query or fragment.';
                             COMMENT ON COLUMN public."{table}"."HttpAdaptationTimeoutMilliseconds" IS 'Landlord-set timeout in milliseconds for this Tenant''s HTTP Adaptation calls. Null falls back to the HttpAdaptationTimeout environment setting.';
                             COMMENT ON COLUMN public."{table}"."HttpAdaptationValidateSsl" IS 'When 1 or null the TLS certificate of this Tenant''s HTTP Adaptation endpoint is validated. 0 disables validation. Only applies when HttpAdaptationUrl is set on the Tenant.';
                             """);
            }
        }

        public override void Down()
        {
        }
    }
}
