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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ReflectionHelpers
{
    using Dictionary;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using log4net;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;

    public static class ReflectRuleHelper
    {
        public static bool Execute(EntityAnalysisModelAbstractionRule abstractionRule,
            Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.ModelSnapshot snapshot, DictionaryNoBoxing<string> fields,
            PooledDictionary<string, double> entityInstanceEntryDictionaryKvPs, ILog log)
        {
            var matched = abstractionRule.AbstractionRuleCompileDelegate(fields,
                snapshot.EntityAnalysisModelLists, entityInstanceEntryDictionaryKvPs, log);
            return matched;
        }

        public static bool Execute(EntityAnalysisModelActivationRule activationRule,
            Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.ModelSnapshot snapshot,
            EntityAnalysisModelInstanceEntryPayload payload,
            PooledDictionary<string, double> entityInstanceEntryDictionaryKvPs,
            ILog log)
        {
            var matched = activationRule.ActivationRuleCompileDelegate(payload.Payload,
                payload.TtlCounter, payload.Abstraction,
                payload.HttpAdaptation, payload.ExhaustiveAdaptation,
                snapshot.EntityAnalysisModelLists,
                payload.AbstractionCalculation,
                payload.Sanction, entityInstanceEntryDictionaryKvPs,
                payload.Activation.Keys,
                log);
            return matched;
        }

        public static double Execute(EntityAnalysisModelAbstractionCalculation functionRule,
            Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.ModelSnapshot snapshot,
            EntityAnalysisModelInstanceEntryPayload payload,
            PooledDictionary<string, double> entityInstanceEntryDictionaryKvPs,
            ILog log)
        {
            var matched = functionRule.FunctionCalculationCompileDelegate(payload.Payload,
                payload.TtlCounter, payload.Abstraction,
                snapshot.EntityAnalysisModelLists,
                payload.AbstractionCalculation, payload.Sanction,
                entityInstanceEntryDictionaryKvPs, log);

            return matched;
        }

        public static object Execute(EntityAnalysisModelInlineFunction entityAnalysisModelInlineFunction,
            Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.ModelSnapshot snapshot,
            EntityAnalysisModelInstanceEntryPayload payload,
            PooledDictionary<string, double> entityInstanceEntryDictionaryKvPs, ILog log)
        {
            var matched = entityAnalysisModelInlineFunction.FunctionCalculationCompileDelegate(payload.Payload,
                snapshot.EntityAnalysisModelLists, entityInstanceEntryDictionaryKvPs, log);

            return matched;
        }
    }
}