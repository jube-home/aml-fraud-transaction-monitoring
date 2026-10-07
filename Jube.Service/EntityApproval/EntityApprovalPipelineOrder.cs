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

namespace Jube.Service.EntityApproval
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.Query.Models;
    using Data.Repository;

    public static class EntityApprovalPipelineOrder
    {
        private static readonly EntityApprovalKind[] stages =
        [
            EntityApprovalKind.EntityAnalysisModel,
            EntityApprovalKind.EntityAnalysisModelRequestXPath,
            EntityApprovalKind.EntityAnalysisModelList,
            EntityApprovalKind.EntityAnalysisModelListValue,
            EntityApprovalKind.EntityAnalysisModelDictionary,
            EntityApprovalKind.EntityAnalysisModelDictionaryKvp,
            EntityApprovalKind.EntityAnalysisModelInlineFunction,
            EntityApprovalKind.EntityAnalysisModelInlineScript,
            EntityApprovalKind.EntityAnalysisModelGatewayRule,
            EntityApprovalKind.EntityAnalysisModelSanction,
            EntityApprovalKind.EntityAnalysisModelTtlCounter,
            EntityApprovalKind.EntityAnalysisModelAbstractionRule,
            EntityApprovalKind.EntityAnalysisModelAbstractionCalculation,
            EntityApprovalKind.ExhaustiveSearchInstance,
            EntityApprovalKind.EntityAnalysisModelHttpAdaptation,
            EntityApprovalKind.EntityAnalysisModelActivationRule,
            EntityApprovalKind.EntityAnalysisModelTag
        ];

        public static int Rank(EntityApprovalKind kind)
        {
            var index = Array.IndexOf(stages, kind);
            return index < 0 ? int.MaxValue : index;
        }

        public static List<EntityApprovalSubject> Order(IEnumerable<EntityApprovalSubject> subjects)
        {
            return subjects
                .OrderBy(s => Rank(s.Kind))
                .ThenBy(s => s.Name, StringComparer.Ordinal)
                .ThenBy(s => s.EntityId)
                .ToList();
        }
    }
}