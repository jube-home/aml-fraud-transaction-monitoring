/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Text;
using System.Text.Json.Serialization;
using Jube.Service.Agent.RuleExtensionTools.Models;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Service.Agent.RuleExtensionTools
{
    public sealed record RuleExtensionSignature(
        string Name,
        string Instance,
        [property: JsonIgnore] IReadOnlyList<RuleExtensionParameter> Parameters,
        string Returns,
        RuleExtensionOrigin Origin)
    {
        public string Declaration
        {
            get
            {
                var builder = new StringBuilder();

                builder.Append('<').Append(Instance).Append(">.").Append(Name).Append('(');

                for (var index = 0; index < Parameters.Count; index++)
                {
                    var parameter = Parameters[index];

                    if (index > 0)
                    {
                        builder.Append(", ");
                    }

                    if (parameter.ParamArray)
                    {
                        builder.Append("ParamArray ");
                    }
                    else if (parameter.Optional)
                    {
                        builder.Append("Optional ");
                    }

                    builder.Append(parameter.Name).Append(" As ").Append(parameter.Type);

                    if (parameter.Optional && parameter.Default is not null)
                    {
                        builder.Append(" = ").Append(parameter.Default);
                    }
                }

                builder.Append(')');

                if (!string.Equals(Returns, "Void", StringComparison.Ordinal))
                {
                    builder.Append(" As ").Append(Returns);
                }

                return builder.ToString();
            }
        }
    }
}