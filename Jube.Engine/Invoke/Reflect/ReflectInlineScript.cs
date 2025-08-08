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
using Jube.Data.Extension;
using Jube.Engine.Model;
using log4net;

namespace Jube.Engine.Invoke.Reflect;

public static class ReflectInlineScript
{
    public static void Execute(EntityAnalysisModelInlineScript entityAnalysisModelInlineScript,
        ref Dictionary<string, object> dataPayload,
        ref Dictionary<string, object> responsePayload, ILog log)
    {
        object[] args = [dataPayload, log];
        entityAnalysisModelInlineScript.PreProcessingMethodInfo.Invoke(entityAnalysisModelInlineScript.ActivatedObject,
            args);

        foreach (var p in entityAnalysisModelInlineScript.InlineScriptType.GetProperties())
            try
            {
                if (entityAnalysisModelInlineScript.ActivatedObject == null) continue;

                foreach (var customAttributeData in p.CustomAttributes)
                    if (customAttributeData.AttributeType.Name.Contains("Latitude"))
                    {
                        if (!dataPayload.ContainsKey(p.Name))
                            dataPayload.Add("Latitude", dataPayload[p.Name].AsDouble());
                    }
                    else if (customAttributeData.AttributeType.Name.Contains("Longitude"))
                    {
                        if (!dataPayload.ContainsKey(p.Name))
                            dataPayload.Add("Longitude", dataPayload[p.Name].AsDouble());
                    }
                    else if (customAttributeData.AttributeType.Name.Contains("ResponsePayload"))
                    {
                        if (!responsePayload.ContainsKey(p.Name)) responsePayload.Add(p.Name, dataPayload[p.Name]);
                    }
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
    }
}