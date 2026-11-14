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
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jube.App.Wire
{
    public sealed class WireCatalogueProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            return services;
        }

        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            return new GuessingProvider(containerBuilder.BuildServiceProvider());
        }

        private sealed class GuessingProvider(ServiceProvider inner) : IServiceProvider, IDisposable, IAsyncDisposable
        {
            private readonly WireCatalogueServiceGuess guess = new();

            public object GetService(Type serviceType)
            {
                return serviceType == typeof(IServiceProviderIsService)
                       || serviceType == typeof(IServiceProviderIsKeyedService)
                    ? guess
                    : inner.GetService(serviceType);
            }

            public void Dispose()
            {
                inner.Dispose();
            }

            public ValueTask DisposeAsync()
            {
                return inner.DisposeAsync();
            }
        }
    }
}
