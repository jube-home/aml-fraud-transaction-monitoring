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

using System.Reflection;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.Data.Context;
using Jube.Service.Agent.PageAgentToolBinder.Models;
using log4net;

namespace Jube.Service.Agent.PageAgentToolBinder
{
    internal sealed class ServiceInstanceCache(ServiceAgentBindingContext context, ILog log)
    {
        private readonly SemaphoreSlim concurrency =
            new(Math.Max(1, context.MaxConcurrentToolCalls), Math.Max(1, context.MaxConcurrentToolCalls));

        private readonly Dictionary<Type, object> inScope = [];

        internal async Task<T> InvokeIsolatedAsync<T>(Type serviceType, Func<Task<T>> work,
            CancellationToken token)
        {
            await concurrency.WaitAsync(token).ConfigureAwait(false);

            try
            {
                await using var isolated = DataConnectionDbContext
                    .GetResilientDbContextDataConnection(context.ConnectionString, log);

                var instance = await CreateAsync(serviceType, isolated, token).ConfigureAwait(false);

                lock (inScope)
                {
                    inScope[serviceType] = instance;
                }

                try
                {
                    return await work().ConfigureAwait(false);
                }
                finally
                {
                    lock (inScope)
                    {
                        inScope.Remove(serviceType);
                    }
                }
            }
            finally
            {
                concurrency.Release();
            }
        }

        internal object Get(Type serviceType)
        {
            lock (inScope)
            {
                if (!inScope.TryGetValue(serviceType, out var instance))
                {
                    throw new InvalidOperationException(
                        $"No live instance of '{serviceType.Name}' is in scope for this call.");
                }

                return instance;
            }
        }

        internal static bool CanConstruct(Type serviceType, IServiceProvider services)
        {
            var factory = Factory(serviceType);

            return factory is not null && factory.GetParameters()
                .All(parameter => CanResolve(parameter, services));
        }

        private async Task<object> CreateAsync(Type serviceType, DbContext isolated, CancellationToken token)
        {
            var factory = Factory(serviceType)
                          ?? throw new InvalidOperationException(
                              $"'{serviceType.Name}' exposes no public static CreateAsync returning itself.");

            var arguments = factory.GetParameters()
                .Select(parameter => Resolve(parameter, isolated, token))
                .ToArray();

            if (factory.Invoke(null, arguments) is not Task task)
            {
                throw new InvalidOperationException($"'{serviceType.Name}.CreateAsync' did not return a Task.");
            }

            await task.ConfigureAwait(false);

            return task.GetType().GetProperty("Result")?.GetValue(task)
                   ?? throw new InvalidOperationException($"'{serviceType.Name}.CreateAsync' returned null.");
        }

        private static MethodInfo? Factory(Type serviceType)
        {
            return serviceType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "CreateAsync")
                .Where(method => method.ReturnType.IsGenericType
                                 && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                                 && method.ReturnType.GetGenericArguments()[0] == serviceType)
                .OrderBy(method => method.GetParameters().Length)
                .FirstOrDefault();
        }

        private static bool AcceptsNull(ParameterInfo parameter)
        {
            return !parameter.ParameterType.IsValueType
                   && new NullabilityInfoContext().Create(parameter).WriteState == NullabilityState.Nullable;
        }

        private static bool IsConnectionString(string? name)
        {
            return name == "connectionString";
        }

        private static bool CanResolve(ParameterInfo parameter, IServiceProvider services)
        {
            if (parameter.ParameterType == typeof(DbContext)
                || parameter.ParameterType == typeof(CancellationToken)
                || (parameter.ParameterType == typeof(string) && parameter.Name == "userName")
                || (parameter.ParameterType == typeof(string) && IsConnectionString(parameter.Name))
                || (parameter.ParameterType == typeof(int?) && parameter.Name == "tenantRegistryId")
                || parameter.HasDefaultValue)
            {
                return true;
            }

            return services.GetService(parameter.ParameterType) is not null || AcceptsNull(parameter);
        }

        private object? Resolve(ParameterInfo parameter, DbContext isolated, CancellationToken token)
        {
            if (parameter.ParameterType == typeof(DbContext))
            {
                return isolated;
            }

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                return token;
            }

            if (parameter.ParameterType == typeof(string) && parameter.Name == "userName")
            {
                return context.UserName;
            }

            if (parameter.ParameterType == typeof(string) && IsConnectionString(parameter.Name))
            {
                return context.ConnectionString;
            }

            if (parameter.ParameterType == typeof(int?) && parameter.Name == "tenantRegistryId")
            {
                return context.TenantRegistryId;
            }

            if (parameter.ParameterType == typeof(IToolResultCache))
            {
                return context.ResultCache;
            }

            var resolved = context.Services.GetService(parameter.ParameterType);

            if (resolved is not null)
            {
                return resolved;
            }

            if (parameter.HasDefaultValue)
            {
                return parameter.DefaultValue;
            }

            if (AcceptsNull(parameter))
            {
                return null;
            }

            throw new InvalidOperationException(
                $"Cannot resolve '{parameter.ParameterType.Name} {parameter.Name}' for a bound service.");
        }
    }
}