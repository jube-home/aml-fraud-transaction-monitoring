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
using System.Collections.Generic;
using System.Linq;

namespace Jube.App.Wire
{
    public static class WireGeneration
    {
        public const string GenerateFlag = "--wire-generate";
        public const string CheckFlag = "--wire-check";

        public static bool Handles(string[] args)
        {
            return args.Contains(GenerateFlag) || args.Contains(CheckFlag);
        }

        public static int Run(string[] args, Action<string> output, Action<string> error)
        {
            var flag = args.Contains(CheckFlag) ? CheckFlag : GenerateFlag;
            var position = Array.IndexOf(args, flag);
            var directory = position + 1 < args.Length && !args[position + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[position + 1]
                : "src/generated";

            var failures = new List<string>();

            try
            {
                WireEndpointCatalogue.PayloadTypesOf(WireEndpointCatalogue.Map(failures));
            }
            catch (Exception exception)
            {
                failures.Add(exception.GetBaseException().Message);
            }

            if (failures.Count > 0)
            {
                error("The endpoint catalogue could not be built, so the wire types are unknown: " +
                      string.Join("; ", failures));

                return 1;
            }

            var registry = new WireTypeRegistry();
            registry.Attach([]);
            var schema = registry.GetSchema();

            if (flag == GenerateFlag)
            {
                var written = WireTypeScriptGenerator.Write(directory, schema, registry.GeneratedTypeNames);

                output($"Wire decoders for types hash {schema.TypesHash} in {directory}: " +
                       $"{(written.Count == 0 ? "already current" : "wrote " + string.Join(", ", written))}.");

                return 0;
            }

            var stale = WireTypeScriptGenerator.Stale(directory, schema, registry.GeneratedTypeNames);

            if (stale.Count == 0)
            {
                output($"The wire decoders in {directory} are current ({schema.TypesHash}).");

                return 0;
            }

            error($"The wire decoders in {directory} are out of date: {string.Join(", ", stale)}. " +
                  "Build Jube.App, or run it with --wire-generate, to regenerate them.");

            return 1;
        }
    }
}
