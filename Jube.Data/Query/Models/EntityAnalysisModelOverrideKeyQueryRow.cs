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

namespace Jube.Data.Query.Models
{
    using System;

    public class EntityAnalysisModelOverrideKeyQueryRow
    {
        public string OverrideKey { get; set; }

        public int EnabledOnModels { get; set; }
        public int Values { get; set; }
        public int Overrides { get; set; }
        public int Forced { get; set; }
        public int ActivationRules { get; set; }
        public int AllActivationRules { get; set; }
        public DateTimeOffset? NextExpiryDate { get; set; }
        public DateTimeOffset? LastCreatedDate { get; set; }
    }
}