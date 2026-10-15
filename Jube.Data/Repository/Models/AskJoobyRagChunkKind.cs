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

namespace Jube.Data.Repository.Models
{
    public enum AskJoobyRagChunkKind : byte
    {
        Unknown = 0,
        TableRow = 1,
        ConfigBlock = 2,
        Page = 3,
        FrontMatter = 4,
        SectionGroup = 5,
        Section = 6,
        SectionPart = 7,
        File = 8,
        Type = 9,
        Method = 10,
        Constructor = 11,
        Destructor = 12,
        Operator = 13,
        ConversionOperator = 14,
        Property = 15,
        PropertyAccessor = 16,
        Indexer = 17,
        IndexerAccessor = 18,
        Event = 19,
        EventAccessor = 20,
        MemberBlock = 21,
        Table = 22,
        View = 23,
        Infrastructure = 24,
        InfrastructureFile = 25
    }
}