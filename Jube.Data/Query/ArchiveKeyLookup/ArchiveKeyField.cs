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

namespace Jube.Data.Query.ArchiveKeyLookup
{
    using System;
    using System.Text.RegularExpressions;
    using Dto.Options;

    public sealed record ArchiveKeyField(
        ArchiveKeySourceKind Kind,
        Guid Guid,
        string Name,
        int DataTypeId,
        ArchiveKeyValueColumn Column,
        byte ProcessingTypeId,
        string Facet,
        bool IsArchiveKey,
        int? EncryptionId)
    {
        private static readonly Regex plain = new("^[A-Za-z0-9_]+$", RegexOptions.CultureInvariant);

        public bool IsEntryKey => Kind == ArchiveKeySourceKind.EntryKey;

        public bool AllowsRange => ArchiveKeyPredicate.AllowsRange(Column);

        public string JsonPath => $"{Facet}.{Name}";

        public string SelectTokenPath => plain.IsMatch(Name)
            ? $"{Facet}.{Name}"
            : $"{Facet}['{Name.Replace("\\", "\\\\").Replace("'", "\\'")}']";
    }
}
