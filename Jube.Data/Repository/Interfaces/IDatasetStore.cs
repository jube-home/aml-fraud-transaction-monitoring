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

using System;

namespace Jube.Data.Repository.Interfaces
{
    public interface IDatasetStore
    {
        int Id { get; set; }
        Guid Guid { get; set; }
        int TenantRegistryId { get; set; }
        string Name { get; set; }
        string ColumnsJson { get; set; }
        string ReportJson { get; set; }

        [Obsolete("Superseded by the instance's Filter/FilterSql/FilterTokens (Class Definition), which now " +
                  "covers all data sources uniformly. Retained, unused, for Preservation compatibility; the " +
                  "column stays writable for old exports but current code never reads or writes it.")]
        int? DependentColumnIndex { get; set; }

        long RowCount { get; set; }
        long SizeBytes { get; set; }
        byte StatusId { get; set; }
        string CreatedUser { get; set; }
        DateTime? CreatedDate { get; set; }
        DateTime? UpdatedDate { get; set; }
        int? Version { get; set; }
        byte? Deleted { get; set; }
        string DeletedUser { get; set; }
        DateTime? DeletedDate { get; set; }
        int? ImportId { get; set; }
    }
}