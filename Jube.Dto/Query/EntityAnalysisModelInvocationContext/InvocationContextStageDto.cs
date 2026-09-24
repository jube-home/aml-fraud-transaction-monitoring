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

using System.ComponentModel;

namespace Jube.Dto.Query.EntityAnalysisModelInvocationContext
{
    [Description("One invocation pipeline stage and what the context knows about it.")]
    public class InvocationContextStageDto
    {
        [Description("Stage name, in pipeline order, e.g. Extraction, TtlCounters, ActivationRules, CaseCreation.")]
        public string Stage { get; set; } = string.Empty;

        [Description("Computed (worked out for this context), FromArchive (as archived), NotComputed (set its " +
                     "values with an overlay) or Excluded (stores or notifies, so never performed).")]
        public string Status { get; set; } = string.Empty;

        [Description("What the status means for this stage.")]
        public string Note { get; set; } = string.Empty;
    }
}