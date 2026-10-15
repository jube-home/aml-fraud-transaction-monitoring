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
    [Migration(20263107760000)]
    public class AddAskJoobyMessageDetail : Migration
    {
        public override void Up()
        {
            Alter.Table("AskJoobyMessage")
                .AddColumn("Detail").AsString().Nullable();

            Execute.Sql("COMMENT ON COLUMN public.\"AskJoobyMessage\".\"Detail\" IS 'The narrated exchange (tool " +
                        "arguments/result or delegated sub-agent reply) that backs the expand/collapse pill for a " +
                        "Step, Delegation, Handoff, Validation or Revision entry, exactly as streamed live. Null for " +
                        "every other entry kind, and for entries recorded before this column existed.'");
        }

        public override void Down()
        {
        }
    }
}