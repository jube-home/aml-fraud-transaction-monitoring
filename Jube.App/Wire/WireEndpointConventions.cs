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
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;

namespace Jube.App.Wire
{
    public static class WireEndpointConventions
    {
        private const string Json = "application/json";

        public static TBuilder AllowMessagePack<TBuilder>(this TBuilder builder, bool encryption = false)
        where TBuilder : IEndpointConventionBuilder
        {
            builder.Finally(endpoint =>
            {
                foreach (var accepts in endpoint.Metadata.OfType<IAcceptsMetadata>().ToList())
                {
                    if (!accepts.ContentTypes.Contains(Json, StringComparer.OrdinalIgnoreCase)
                        || accepts.ContentTypes.Contains(WireMediaType.MessagePack, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var contentTypes = encryption
                        ? [.. accepts.ContentTypes, WireMediaType.MessagePack, WireMediaType.Encrypted]
                        : (string[]) [.. accepts.ContentTypes, WireMediaType.MessagePack];

                    endpoint.Metadata.Remove(accepts);
                    endpoint.Metadata.Add(new AcceptsMetadata(contentTypes, accepts.RequestType,
                        accepts.IsOptional));
                }
            });

            return builder;
        }
    }
}
