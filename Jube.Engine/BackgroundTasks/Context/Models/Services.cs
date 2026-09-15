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

using System.Collections.Concurrent;
using System.Reflection;
using Jube.Cache;
using Jube.Engine.EntityAnalysisModelInvoke.ImplicitAsync.Interfaces;
using Jube.Engine.Models;
using Jube.Engine.Observability;
using Jube.TaskCancellation.Interfaces;
using log4net;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Engine.BackgroundTasks.Context.Models
{
    public class Services
    {
        public DynamicEnvironment.DynamicEnvironment DynamicEnvironment { get; set; }
        public ILog Log { get; set; }
        public ITaskCoordinator TaskCoordinator { get; set; }
        public IConnection RabbitMqConnection { get; set; }
        public CacheService CacheService { get; set; }
        public IImplicitAsyncInvocationTracker ImplicitAsyncInvocationTracker { get; set; }
        public DefaultContractResolver ContractResolver { get; set; }
        public ConcurrentDictionary<string, Assembly> HashCacheAssembly { get; } = new();
        public ConcurrentDictionary<string, HashCacheAssemblyPayload> HashCacheAssemblyMetadata { get; } = new();
        public string ReportConnectionString { get; set; }
        public OpenTelemetryExcludeCache OpenTelemetryExcludeCache { get; set; }
        public LogCounterRuleCache LogCounterRuleCache { get; set; }
    }
}