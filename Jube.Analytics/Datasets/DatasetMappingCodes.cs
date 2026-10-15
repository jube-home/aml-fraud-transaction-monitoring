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

namespace Jube.Analytics.Datasets
{
    public static class DatasetMappingCodes
    {
        public const string ColumnsRequired = "ColumnsRequired";
        public const string ColumnCountMismatch = "ColumnCountMismatch";
        public const string ColumnIndexInvalid = "ColumnIndexInvalid";
        public const string ColumnIndexDuplicate = "ColumnIndexDuplicate";
        public const string NoVariables = "NoVariables";
        public const string VariableNotNumeric = "VariableNotNumeric";
        public const string VariableConstant = "VariableConstant";
        public const string NameInvalid = "NameInvalid";
        public const string NameDuplicate = "NameDuplicate";
        public const string ProcessingTypeInvalid = "ProcessingTypeInvalid";
    }
}