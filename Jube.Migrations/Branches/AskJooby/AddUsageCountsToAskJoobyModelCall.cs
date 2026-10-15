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
    [Migration(20263107680000)]
    public class AddUsageCountsToAskJoobyModelCall : Migration
    {
        public override void Up()
        {
            Alter.Table("AskJoobyModelCall")
                .AddColumn("UsageCountsJson").AsCustom("text").Nullable()
                .AddColumn("PromptDetailCachedTokens").AsInt32().Nullable();

            Execute.Sql(
                "COMMENT ON COLUMN \"AskJoobyModelCall\".\"UsageCountsJson\" IS " +
                "'Every additional usage figure the provider reported on this call, as the JSON " +
                "object it arrived as, rather than only the handful of fields mapped to columns. " +
                "It exists because a figure that is absent and a figure that is present under an " +
                "unexpected name look identical once the mapping has run: CachedInputTokens being " +
                "null says the reader found no cache key, not that the provider sent none. Reading " +
                "this column shows exactly what did arrive, which is what a provider asks for when " +
                "a reported figure is disputed. Null means the provider reported no additional " +
                "counts at all.';");

            Execute.Sql(
                "COMMENT ON COLUMN \"AskJoobyModelCall\".\"PromptDetailCachedTokens\" IS " +
                "'The provider''s own cached-token figure as reported in the nested OpenAI-compatible " +
                "field prompt_tokens_details.cached_tokens, kept apart from " +
                "CachedInputTokens deliberately: that column is filled from cache_read_input_tokens " +
                "or any other cache-and-token key the reader can find, and the two are different " +
                "fields that a provider may populate independently. Holding both means a disagreement " +
                "between them is visible rather than resolved silently by whichever the reader " +
                "happened to check first.';");
        }

        public override void Down()
        {
        }
    }
}