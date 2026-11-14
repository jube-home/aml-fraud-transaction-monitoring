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

namespace Jube.Migrations.Branches.SanctionMatchAlgorithm
{
    using FluentMigrator;

    [Migration(2026111106000000)]
    public class AddSanctionCheckMatchAlgorithm : Migration
    {
        public override void Up()
        {
            foreach (var table in new[] { "EntityAnalysisModelSanction", "EntityAnalysisModelSanctionVersion" })
            {
                Alter.Table(table).AddColumn("MatchAlgorithmId").AsInt16().Nullable();
                Alter.Table(table).AddColumn("MinimumSimilarity").AsDouble().Nullable();
                Alter.Table(table).AddColumn("Transliterate").AsInt16().Nullable();

                Execute.Sql($"""
                            COMMENT ON COLUMN public."{table}"."MatchAlgorithmId" IS 'Enumeration (SanctionMatchAlgorithm): how a screened name is compared with a list entry. 1 Levenshtein edit distance per name part (the default when empty), 2 Jaro-Winkler similarity per name part, 3 Token set: the whole name compared ignoring the order of the parts, each part paired with its closest part and the score reduced for every part left unmatched.';
                            COMMENT ON COLUMN public."{table}"."MinimumSimilarity" IS 'For Jaro-Winkler and Token set, the least similarity from 0 to 1 that counts as a match (0.9 when empty). Not used by Levenshtein.';
                            COMMENT ON COLUMN public."{table}"."Transliterate" IS '1 means Cyrillic and Greek script is transliterated to Latin on both the screened name and the list entries before they are compared, so a name written in Cyrillic can match its Latin spelling; 0 or empty compares as written.';
                            """);
            }
        }

        public override void Down()
        {
        }
    }
}
