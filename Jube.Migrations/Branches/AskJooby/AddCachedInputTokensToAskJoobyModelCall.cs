/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
    [Migration(20263107660000)]
    public class AddCachedInputTokensToAskJoobyModelCall : Migration
    {
        public override void Up()
        {
            Alter.Table("AskJoobyModelCall")
                .AddColumn("CachedInputTokens").AsInt32().Nullable();

            Execute.Sql(
                "COMMENT ON COLUMN \"AskJoobyModelCall\".\"CachedInputTokens\" IS " +
                "'How many of InputTokens the provider served from its prompt cache on this call, " +
                "read from whichever cache count the provider reports. Null means the provider " +
                "reported no cache figure at all on this call, which is not the same as zero: zero " +
                "means it reported a miss. Prompt caching is best effort, so zero on a repeated " +
                "identical prefix is expected from time to time and only a sustained run of zeroes " +
                "indicates caching is not working.';");
        }

        public override void Down()
        {
        }
    }
}