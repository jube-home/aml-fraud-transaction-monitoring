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

using System.Globalization;
using Jube.Dictionary;
using Jube.HttpAdaptationProtocol;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    internal static class AdaptationInputExtraction
    {
        internal static string CleanName(string name)
        {
            return name.Contains('.') ? name.Split(".")[1] : name;
        }

        internal static string ProtocolSource(int processingTypeId)
        {
            return processingTypeId switch
            {
                1 => ProtocolConstants.Source.Payload,
                2 => ProtocolConstants.Source.Dictionary,
                3 => ProtocolConstants.Source.TtlCounter,
                4 => ProtocolConstants.Source.Sanction,
                6 => ProtocolConstants.Source.AbstractionCalculation,
                _ => ProtocolConstants.Source.Abstraction
            };
        }

        internal static bool TryGetValue(Context context, int processingTypeId, string cleanName, out double value)
        {
            var payload = context.EntityAnalysisModelInstanceEntryPayload;

            switch (processingTypeId)
            {
                case 1:
                    if (payload.Payload.ContainsKey(cleanName))
                    {
                        value = PayloadNumber(payload.Payload[cleanName]);

                        return true;
                    }

                    value = 0;

                    return false;
                case 2:
                    return payload.Dictionary.TryGetValue(cleanName, out value);
                case 3:
                    return payload.TtlCounter.TryGetValue(cleanName, out value);
                case 4:
                    return payload.Sanction.TryGetValue(cleanName, out value);
                case 6:
                    return payload.AbstractionCalculation.TryGetValue(cleanName, out value);
                default:
                    return payload.Abstraction.TryGetValue(cleanName, out value);
            }
        }

        private static double PayloadNumber(InternalValue value)
        {
            return value.Type == InternalValue.ValueType.Int ? value.AsInt() : value.AsDouble();
        }

        internal static bool TryGetText(Context context, int processingTypeId, string cleanName, out string value)
        {
            var payload = context.EntityAnalysisModelInstanceEntryPayload;

            if (processingTypeId == 1)
            {
                if (payload.Payload.TryGetValue(cleanName, out var internalValue))
                {
                    value = internalValue.ToString();

                    return true;
                }

                value = null;

                return false;
            }

            if (TryGetValue(context, processingTypeId, cleanName, out var numeric))
            {
                value = numeric.ToString(CultureInfo.InvariantCulture);

                return true;
            }

            value = null;

            return false;
        }
    }
}