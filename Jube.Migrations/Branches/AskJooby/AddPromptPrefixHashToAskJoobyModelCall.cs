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
    [Migration(20263107670000)]
    public class AddPromptPrefixHashToAskJoobyModelCall : Migration
    {
        public override void Up()
        {
            Alter.Table("AskJoobyModelCall")
                .AddColumn("PromptPrefixHash").AsString(32).Nullable();

            Execute.Sql(
                "COMMENT ON COLUMN \"AskJoobyModelCall\".\"PromptPrefixHash\" IS " +
                "'A hash of the very start of what was sent to the provider on this call -- the " +
                "system message, which is what a prompt cache matches from token zero. It exists " +
                "to answer one question that cannot be answered from the outside: when a call " +
                "reports no cached tokens, was the prefix the provider saw actually identical to " +
                "the last one. Two calls sharing this hash and still reporting zero in " +
                "CachedInputTokens point at the provider routing them to different workers; two " +
                "calls differing in it point at something rewriting the prompt on our side.';");
        }

        public override void Down()
        {
        }
    }
}