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

using Jube.Data.Repository.Interfaces;

namespace Jube.Data.Poco
{
    public enum DatasetKind : byte
    {
        Csv = 0,
        Json = 1
    }

    public readonly record struct DatasetReference(DatasetKind Kind, int Id)
    {
        public string Table => Kind == DatasetKind.Csv ? "ExhaustiveInstanceCsv" : "ExhaustiveInstanceJson";
        public string RowTable => Table + "Row";
        public string KeyColumn => Table + "Id";
    }

    public static class DatasetStores
    {
        public static DatasetKind KindOf(IDatasetStore store)
        {
            return store is ExhaustiveInstanceJson ? DatasetKind.Json : DatasetKind.Csv;
        }

        public static DatasetReference ReferenceOf(IDatasetStore store)
        {
            return new DatasetReference(KindOf(store), store.Id);
        }
    }
}