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

namespace Jube.Migrations.Branches
{
    using FluentMigrator;

    /// <summary>
    ///     Seeds a sanctions source for the US restricted-party lists that are not in OFAC's SDN file: DoD Section
    ///     1260H Chinese military companies (with their named subsidiaries), Section 889, the FCC Covered List, the
    ///     UFLPA Entity List and the interim BIOSECURE list. The file is one row per name (the primary name and each
    ///     alias) with the record id in column 0 and the name in column 1, under one header row, so it loads through
    ///     the existing delimited importer unchanged. The source is seeded switched off, as an option an
    ///     administrator turns on in the Sanctions Loader.
    /// </summary>
    [Migration(2026111812000000)]
    public class UsRestrictedPartiesSanctionEntrySource : Migration
    {
        public const string Name = "US Restricted Parties";

        public const string HttpLocation =
            "https://github.com/Mattbusel/us-restricted-parties/releases/latest/download/all.csv";

        public override void Up()
        {
            Insert.IntoTable("SanctionEntrySource").Row(new
            {
                Name,
                Severity = 1,
                DirectoryLocation = "",
                Delimiter = ",",
                MultiPartStringIndex = "1",
                ReferenceIndex = 0,
                EnableDirectoryLocation = 0,
                EnableHttpLocation = 0,
                HttpLocation,
                Skip = 1
            });
        }

        public override void Down()
        {
        }
    }
}
