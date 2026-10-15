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

using System.ComponentModel;
using System.Reflection;
using System.Text;
using Jube.AskJooby.Prompts.Models;

namespace Jube.AskJooby.Tools.SystemContext
{
    public sealed class SystemContextTool(PageContext? pageContext)
    {
        [Description(
            "Lists the structured context the platform itself attached to this run -- the page the " +
            "caller is on, its purpose, and the live record behind it, when present. None of this came " +
            "from the Pasture and none of it ever could: it is handed to every agent on this run " +
            "straight from the request, the same way a tool result is, before any agent ever saw it. " +
            "Call this before treating a claim about the current page, its purpose, or its record as " +
            "something that needs a Pasture search -- if the claim traces back to a field listed here, " +
            "it is already as confirmed as it can be, and an empty Pasture search for it is not a gap, " +
            "it is searching somewhere that was never going to have the answer.")]
        public string DescribeSystemContext()
        {
            return Describe(pageContext);
        }

        public static string Describe(PageContext? context)
        {
            if (context is null)
            {
                return "No structured page context on this run -- the caller did not arrive from a " +
                       "specific page of the platform, or none was supplied, so there is nothing here " +
                       "to exempt from a Pasture check.";
            }

            var builder = new StringBuilder();

            builder.AppendLine(
                "Structured context the platform attached to this run, straight from the request -- " +
                "not from the Pasture, and never checkable against it. Treat every field below as " +
                "already confirmed:");

            foreach (var property in typeof(PageContext).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetValue(context) is string { Length: > 0 } value)
                {
                    builder.AppendLine($"- {property.Name}: {value}");
                }
            }

            return builder.ToString();
        }
    }
}