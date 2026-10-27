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

using System.ComponentModel;
using System.Text.Json.Serialization;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Authentication
{
    [Description("Which single sign-on methods this installation has switched on, so a sign-in page can try them " +
                 "before it asks for a user name and password. Neither is on by default.")]
    public class AuthenticationMethodsDto
    {
        [Description("True when Windows integrated (Negotiate) authentication is enabled.")]
        public bool Negotiate { get; init; }

        [Description("True when sign-in is delegated to an OpenID Connect identity provider.")]
        [JsonPropertyName("oauth")]
        public bool OAuth { get; init; }
    }
}
