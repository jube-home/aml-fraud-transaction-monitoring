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

namespace Jube.Migrations.Branches.Totp
{
    using FluentMigrator;

    [Migration(2026111017000000)]
    public class AddUserRegistryTotp : Migration
    {
        public override void Up()
        {
            Alter.Table("UserRegistry")
                .AddColumn("TotpSecret").AsString().Nullable()
                .AddColumn("TotpEnrolledDate").AsDateTime().Nullable()
                .AddColumn("TotpLastAcceptedStep").AsInt64().Nullable();

            Execute.Sql("""
                        COMMENT ON COLUMN public."UserRegistry"."TotpSecret" IS 'The authenticator app secret for time based one time passwords (TOTP, RFC 6238), encrypted with AES-GCM under a key derived from MfaSecretEncryptionKey. Never returned by any API. Null when no authenticator has been set up. Present with a null TotpEnrolledDate while set up is pending the first valid code.';
                        COMMENT ON COLUMN public."UserRegistry"."TotpEnrolledDate" IS 'When the user proved their authenticator by entering a valid code (UTC). Null while set up is pending or no authenticator exists.';
                        COMMENT ON COLUMN public."UserRegistry"."TotpLastAcceptedStep" IS 'The 30 second time step of the last accepted code, so that a code can never be used twice.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
