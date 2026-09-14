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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Jube.Engine.Attributes.Events;
using Jube.Engine.Attributes.Properties;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript.
    EntityAnalysisModelInlineScriptPropertyAttribute;
using Jube.Engine.Interfaces;
using Jube.Parser.Compiler;
using log4net;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    public static class InlineScriptCompilationExtensions
    {
        public static Compile CompileAndConfigure(this EntityAnalysisModelInlineScript inlineScript,
            string[] dependencyArray, ILog log)
        {
            var compile = new Compile();
            compile.CompileCode(inlineScript.InlineScriptCode, log, dependencyArray,
                inlineScript.LanguageId == 2 ? Compile.Language.CSharp : Compile.Language.Vb);

            if (compile.Errors != null)
            {
                return compile;
            }

            inlineScript.InlineScriptCompile = compile.CompiledAssembly;

            var interfaceType = typeof(IInlineScript);
            var implementations = inlineScript.InlineScriptCompile.GetExportedTypes()
                .Where(t => interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            foreach (var type in implementations)
            {
                inlineScript.ClassName = type.FullName;
                break;
            }

            if (inlineScript.ClassName != null)
            {
                inlineScript.InlineScriptType = inlineScript.InlineScriptCompile.GetType(inlineScript.ClassName);
            }

            if (inlineScript.InlineScriptType == null)
            {
                return compile;
            }

            SetupInlineScriptDelegates(inlineScript);
            SetupEvents(inlineScript);
            SetupPropertyAttributes(inlineScript);

            return compile;
        }

        private static void SetupPropertyAttributes(EntityAnalysisModelInlineScript inlineScript)
        {
            foreach (var p in inlineScript.InlineScriptType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var entityAnalysisModelInlineScriptProperty = new EntityAnalysisModelInlineScriptPropertyAttribute
                {
                    Name = p.Name,
                    ReportTable = p.GetCustomAttribute<ReportTable>() != null,
                    Latitude = p.GetCustomAttribute<Latitude>() != null,
                    Longitude = p.GetCustomAttribute<Longitude>() != null,
                    ResponsePayload = p.GetCustomAttribute<ResponsePayload>() != null,
                    CacheIndexId = p.GetCustomAttribute<CacheIndex>()?.Id,
                    GetValueDelegate = CompileGetValueDelegate(p),
                    PropertyType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType
                };

                var searchKeyAttribute = p.GetCustomAttribute<SearchKey>();

                if (searchKeyAttribute != null)
                {
                    var distinctSearchKey = new DistinctSearchKey
                    {
                        SearchKey = p.Name,
                        SearchKeyTtlInterval = searchKeyAttribute.SearchKeyTtlInterval,
                        SearchKeyTtlIntervalValue = searchKeyAttribute.SearchKeyTtlIntervalValue,
                        SearchKeyFetchLimit = searchKeyAttribute.SearchKeyFetchLimit,
                        SearchKeyCache = searchKeyAttribute.SearchKeyCache,
                        SearchKeyCacheInterval = searchKeyAttribute.SearchKeyCacheInterval,
                        SearchKeyCacheValue = searchKeyAttribute.SearchKeyCacheValue,
                        SearchKeyCacheSample = searchKeyAttribute.SearchKeyCacheSample,
                        SearchKeyCacheFetchLimit = searchKeyAttribute.SearchKeyFetchLimit,
                        SearchKeyCacheTtlInterval = searchKeyAttribute.SearchKeyCacheTtlInterval,
                        SearchKeyCacheTtlValue = searchKeyAttribute.SearchKeyCacheTtlValue
                    };

                    inlineScript.GroupingKeys.Add(distinctSearchKey);
                    entityAnalysisModelInlineScriptProperty.SearchKey = distinctSearchKey;
                }

                inlineScript.EntityAnalysisModelInlineScriptPropertyAttributes.Add(p.Name,
                    entityAnalysisModelInlineScriptProperty);
            }
        }

        internal static void SetupEvents(EntityAnalysisModelInlineScript inlineScript)
        {
            var shadowEntityAnalysisModelInlineScriptEventsToBeSorted =
                new List<EntityAnalysisModelInlineScriptEvent>();
            foreach (var attribute in inlineScript.PreProcessingMethodInfo.GetCustomAttributes())
            {
                var entityAnalysisModelInlineScriptEvent = new EntityAnalysisModelInlineScriptEvent();

                switch (attribute)
                {
                    case ActivationRuleOverrideEvent activationRuleOverrideEvent:
                        entityAnalysisModelInlineScriptEvent.EntityAnalysisModelInlineScriptEventType =
                            EntityAnalysisModelInlineScriptEventTypeEnum.AbstractionRuleOverride;

                        if (activationRuleOverrideEvent.Guid != null)
                        {
                            entityAnalysisModelInlineScriptEvent.Guid = Guid.Parse(activationRuleOverrideEvent.Guid);
                        }

                        entityAnalysisModelInlineScriptEvent.Priority = activationRuleOverrideEvent.Priority;
                        entityAnalysisModelInlineScriptEvent.Name = activationRuleOverrideEvent.Name;

                        shadowEntityAnalysisModelInlineScriptEventsToBeSorted.Add(entityAnalysisModelInlineScriptEvent);
                        break;

                    case PayloadEvent payloadEvent:
                        entityAnalysisModelInlineScriptEvent.EntityAnalysisModelInlineScriptEventType =
                            EntityAnalysisModelInlineScriptEventTypeEnum.Payload;

                        if (payloadEvent.Guid != null)
                        {
                            entityAnalysisModelInlineScriptEvent.Guid = Guid.Parse(payloadEvent.Guid);
                        }

                        entityAnalysisModelInlineScriptEvent.Priority = payloadEvent.Priority;
                        entityAnalysisModelInlineScriptEvent.Name = payloadEvent.Name;

                        shadowEntityAnalysisModelInlineScriptEventsToBeSorted.Add(entityAnalysisModelInlineScriptEvent);
                        break;
                }
            }

            inlineScript.EntityAnalysisModelInlineScriptEvents =
                shadowEntityAnalysisModelInlineScriptEventsToBeSorted.OrderBy(o => o.Priority).ToList();
        }

        private static Func<object> CompileActivatorDelegate(EntityAnalysisModelInlineScript inlineScript)
        {
            return Expression.Lambda<Func<object>>(
                Expression.Convert(
                    Expression.New(inlineScript.InlineScriptType),
                    typeof(object)
                )
            ).Compile();
        }

        private static Func<object, EntityAnalysisModelInvoke.Context.Context, Task<bool>>
            CompileMethodDelegate(Type type, MethodInfo methodInfo)
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var contextParam = Expression.Parameter(typeof(EntityAnalysisModelInvoke.Context.Context), "context");
            var castInstance = Expression.Convert(instanceParam, type);
            var methodCall = Expression.Call(castInstance, methodInfo, contextParam);

            return Expression
                .Lambda<Func<object, EntityAnalysisModelInvoke.Context.Context, Task<bool>>>(
                    methodCall,
                    instanceParam,
                    contextParam
                ).Compile();
        }

        private static Func<object, object> CompileGetValueDelegate(PropertyInfo p)
        {
            if (p.DeclaringType == null)
            {
                throw new ArgumentException($"Property '{p.Name}' has no DeclaringType.");
            }

            var instParam = Expression.Parameter(typeof(object), "instance");
            var castInstance = Expression.Convert(instParam, p.DeclaringType);
            var propertyAccess = Expression.Property(castInstance, p);
            var castResult = Expression.Convert(propertyAccess, typeof(object));

            return Expression.Lambda<Func<object, object>>(castResult, instParam).Compile();
        }

        internal static void SetupInlineScriptDelegates(EntityAnalysisModelInlineScript inlineScript)
        {
            inlineScript.InlineScriptType =
                inlineScript.InlineScriptCompile.GetType(inlineScript.ClassName);

            if (inlineScript.InlineScriptType == null)
            {
                return;
            }

            inlineScript.PreProcessingMethodInfo =
                inlineScript.InlineScriptType.GetMethod("ExecuteAsync",
                    [typeof(EntityAnalysisModelInvoke.Context.Context)]);

            inlineScript.ActivatorDelegate = CompileActivatorDelegate(inlineScript);
            inlineScript.ExecuteAsyncDelegate = CompileMethodDelegate(
                inlineScript.InlineScriptType,
                inlineScript.PreProcessingMethodInfo);
        }
    }
}