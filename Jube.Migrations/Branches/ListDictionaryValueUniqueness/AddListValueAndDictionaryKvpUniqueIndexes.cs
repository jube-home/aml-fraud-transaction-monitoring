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

namespace Jube.Migrations.Branches.ListDictionaryValueUniqueness
{
    using FluentMigrator;
    using Helpers;

    [Migration(2026101512000000)]
    public class AddListValueAndDictionaryKvpUniqueIndexes : Migration
    {
        private const int EntityAnalysisModelListValueKind = 3;
        private const int EntityAnalysisModelDictionaryKvpKind = 5;

        public override void Up()
        {
            Execute.Sql(DuplicateValueUniqueIndexSql.Up(
                "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                "EntityAnalysisModelList", EntityAnalysisModelListValueKind, []));

            Execute.Sql(DuplicateValueUniqueIndexSql.Up(
                "EntityAnalysisModelDictionaryKvp", "EntityAnalysisModelDictionaryKvpVersion",
                "EntityAnalysisModelDictionaryKvpId", "EntityAnalysisModelDictionaryId", "KvpKey",
                "EntityAnalysisModelDictionary", EntityAnalysisModelDictionaryKvpKind, ["KvpValue"]));
        }

        public override void Down()
        {
        }
    }
}