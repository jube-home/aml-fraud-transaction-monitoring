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

using System.Collections.Generic;
using Jube.Case.Outbound;

namespace Jube.Test.Infrastructure
{
    public static class LoopbackAllowed
    {
        public const string Cidrs = "127.0.0.0/8,::1/128";

        public static IReadOnlyDictionary<string, string> Settings => new Dictionary<string, string>
        {
            [OutboundHostPolicy.AllowedCidrsKey] = Cidrs
        };

        public static Jube.DynamicEnvironment.DynamicEnvironment Environment =>
            TestDynamicEnvironment.Create(Settings);
    }
}
