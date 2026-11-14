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
using System.Collections.Generic;
using Jube.Data.Poco;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;

namespace Jube.Engine.EntityAnalysisModelInvoke.Extraction.Extensions
{
    namespace YourNamespace.Extensions
    {
        public static class ArchiveKeyExtensions
        {
            public const byte RequestXPathProcessingTypeId = 1;

            public static void AddArchiveKey(
                this List<ArchiveKey> reportDatabaseValues,
                EntityAnalysisModelRequestXPath xPath,
                EntityAnalysisModelInstanceEntryPayload payload,
                string valueString = null,
                int? valueInt = null,
                double? valueFloat = null,
                DateTime? valueDate = null,
                bool? valueBool = null)
            {
                if (!xPath.ReportTable)
                {
                    return;
                }

                var key = new ArchiveKey
                {
                    ProcessingTypeId = RequestXPathProcessingTypeId,
                    Key = xPath.Name,
                    EntityAnalysisModelInstanceEntryGuid = payload.EntityAnalysisModelInstanceEntryGuid,
                    Version = 1
                };

                if (valueString != null)
                {
                    key.KeyValueString = valueString;
                }
                else if (valueInt.HasValue)
                {
                    key.KeyValueInteger = valueInt.Value;
                }
                else if (valueFloat.HasValue)
                {
                    key.KeyValueFloat = valueFloat.Value;
                }
                else if (valueDate.HasValue)
                {
                    key.KeyValueDate = valueDate.Value;
                }
                else if (valueBool.HasValue)
                {
                    key.KeyValueBoolean = (byte)(valueBool.Value ? 1 : 0);
                }

                reportDatabaseValues.Add(key);
            }

            public static void AddArchiveKey(
                this List<ArchiveKey> reportDatabaseValues,
                EntityAnalysisModelRequestXPath xPath,
                EntityAnalysisModelInstanceEntryPayload payload,
                InternalValue value)
            {
                switch (value.Type)
                {
                    case InternalValue.ValueType.String:
                        reportDatabaseValues.AddArchiveKey(xPath, payload, value.AsString());
                        break;
                    case InternalValue.ValueType.Int:
                        reportDatabaseValues.AddArchiveKey(xPath, payload, valueInt: value.AsInt());
                        break;
                    case InternalValue.ValueType.Double:
                        reportDatabaseValues.AddArchiveKey(xPath, payload, valueFloat: value.AsDouble());
                        break;
                    case InternalValue.ValueType.Bool:
                        reportDatabaseValues.AddArchiveKey(xPath, payload, valueBool: value.AsBool());
                        break;
                    case InternalValue.ValueType.DateTime:
                        reportDatabaseValues.AddArchiveKey(xPath, payload, valueDate: value.AsDateTime());
                        break;
                    case InternalValue.ValueType.Guid:
                        reportDatabaseValues.AddArchiveKey(xPath, payload, value.AsGuid().ToString());
                        break;
                }
            }
        }
    }
}
