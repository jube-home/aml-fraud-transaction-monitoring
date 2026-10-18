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

using Jube.Engine.AdvancedAnalytics.RScript;
using Jube.Engine.Helpers;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Agent.PageAgentToolBinder
{
    public sealed class ServiceAgentServiceProvider(
        ILog log,
        IStringLocalizerFactory stringLocalizerFactory,
        IServiceChangeBus serviceChangeBus,
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null,
        DynamicEnvironment.DynamicEnvironment? dynamicEnvironment = null) : IServiceProvider
    {
        private JsonSerializationHelper? jsonSerializationHelper;

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(ILog))
            {
                return log;
            }

            if (serviceType == typeof(IStringLocalizerFactory))
            {
                return stringLocalizerFactory;
            }

            if (serviceType == typeof(IServiceChangeBus))
            {
                return serviceChangeBus;
            }

            if (serviceType == typeof(IEmbeddingGenerator<string, Embedding<float>>))
            {
                return embeddingGenerator;
            }

            if (serviceType == typeof(DynamicEnvironment.DynamicEnvironment))
            {
                return dynamicEnvironment;
            }

            if (serviceType == typeof(JsonSerializationHelper))
            {
                return jsonSerializationHelper ??= new JsonSerializationHelper();
            }

            if (serviceType == typeof(RServerClient) && dynamicEnvironment is not null)
            {
                return RServerClient.FromSettings(dynamicEnvironment.AppSettings);
            }

            return null;
        }
    }
}