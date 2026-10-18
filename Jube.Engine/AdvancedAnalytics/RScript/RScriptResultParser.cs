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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Engine.AdvancedAnalytics.RScript
{
    public static class RScriptResultParser
    {
        public const string StartMarker = "<<<JUBE_RESULT_START>>>";
        public const string EndMarker = "<<<JUBE_RESULT_END>>>";

        public const string InvalidJsonWarning =
            "Result markers were found but the content between them was not valid JSON and was discarded; see " +
            "ConsoleOutput for the raw script output.";

        public static RScriptParsedResult Parse(string consoleOutput)
        {
            if (string.IsNullOrEmpty(consoleOutput))
            {
                return new RScriptParsedResult(null, null);
            }

            var startIndex = consoleOutput.IndexOf(StartMarker, StringComparison.Ordinal);
            var endIndex = consoleOutput.IndexOf(EndMarker, StringComparison.Ordinal);

            if (startIndex < 0 || endIndex < 0 || endIndex <= startIndex)
            {
                return new RScriptParsedResult(null, null);
            }

            var contentStart = startIndex + StartMarker.Length;
            var candidate = consoleOutput[contentStart..endIndex].Trim();

            try
            {
                JToken.Parse(candidate);
            }
            catch (JsonException)
            {
                return new RScriptParsedResult(null, InvalidJsonWarning);
            }

            return new RScriptParsedResult(candidate, null);
        }
    }

    public sealed record RScriptParsedResult(string ResultJson, string Warning);
}