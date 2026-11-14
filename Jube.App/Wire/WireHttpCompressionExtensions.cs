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

using System.IO.Compression;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;

namespace Jube.App.Wire
{
    public static class WireHttpCompressionExtensions
    {
        public static IServiceCollection AddWireHttpCompression(this IServiceCollection services,
            WireHttpCompressionKind kind, CompressionLevel level, bool excludeMessagePack)
        {
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.MimeTypes = excludeMessagePack
                    ? ResponseCompressionDefaults.MimeTypes
                    : ResponseCompressionDefaults.MimeTypes.Append(WireMediaType.MessagePack);

                if (kind == WireHttpCompressionKind.Brotli)
                {
                    options.Providers.Add<BrotliCompressionProvider>();
                }
                else
                {
                    options.Providers.Add<GzipCompressionProvider>();
                }
            });

            services.Configure<GzipCompressionProviderOptions>(options => options.Level = level);
            services.Configure<BrotliCompressionProviderOptions>(options => options.Level = level);

            return services;
        }
    }
}
