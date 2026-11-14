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
using System.Reflection;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;

namespace Jube.App.Wire
{
    public sealed class WireCatalogueServiceGuess : IServiceProviderIsService, IServiceProviderIsKeyedService
    {
        public bool IsService(Type serviceType)
        {
            if (serviceType.GetCustomAttribute<MessagePackObjectAttribute>() != null)
            {
                return false;
            }

            var space = serviceType.Namespace ?? string.Empty;

            if (space.StartsWith("System.Collections", StringComparison.Ordinal)
                || space.StartsWith("System.Text.Json", StringComparison.Ordinal)
                || (space == "System" && !serviceType.IsInterface))
            {
                return false;
            }

            if (serviceType.IsInterface && space.StartsWith("Microsoft.AspNetCore.Http", StringComparison.Ordinal)
                                        && serviceType.Name.StartsWith("IFormFile", StringComparison.Ordinal))
            {
                return false;
            }

            return serviceType.IsInterface || serviceType.IsClass;
        }

        public bool IsKeyedService(Type serviceType, object serviceKey)
        {
            return false;
        }
    }
}
