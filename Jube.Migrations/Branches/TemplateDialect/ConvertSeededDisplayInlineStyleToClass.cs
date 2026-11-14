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

namespace Jube.Migrations.Branches.TemplateDialect
{
    using FluentMigrator;

    [Migration(2026111012000000)]
    public class ConvertSeededDisplayInlineStyleToClass : Migration
    {
        public const string Sql = """
                                  update "CaseWorkflowDisplay" set "Html" =
                                  replace("Html", '<div style=''font-size:30px''>', '<div class="text-3xl font-semibold">')
                                  where "Html" like '%<div style=''font-size:30px''>%'
                                  """;

        public override void Up()
        {
            ConvertInlineStyleToClass();
        }

        public override void Down()
        {
        }

        private void ConvertInlineStyleToClass()
        {
            Execute.Sql(Sql);
        }
    }
}
