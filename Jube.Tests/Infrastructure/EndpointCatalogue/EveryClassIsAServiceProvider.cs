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
using Microsoft.Extensions.DependencyInjection;

namespace Jube.Test.Infrastructure.EndpointCatalogue
{
    public sealed class EveryClassIsAServiceProvider(IServiceProvider inner)
        : IKeyedServiceProvider, IDisposable
    {
        private static readonly EveryClassIsAService IsService = new();

        public void Dispose()
        {
            (inner as IDisposable)?.Dispose();
        }

        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(IServiceProviderIsService) ||
                   serviceType == typeof(IServiceProviderIsKeyedService)
                ? IsService
                : inner.GetService(serviceType);
        }

        public object? GetKeyedService(Type serviceType, object? serviceKey)
        {
            return ((IKeyedServiceProvider)inner).GetKeyedService(serviceType, serviceKey);
        }

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        {
            return ((IKeyedServiceProvider)inner).GetRequiredKeyedService(serviceType, serviceKey);
        }
    }
}
