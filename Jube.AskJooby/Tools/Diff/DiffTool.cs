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
using System.Text.Json;
using System.Text.Json.Serialization;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.AskJooby.Tools.Diff
{
    public sealed record DiffHunk(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("lineOld")]
        int? LineOld,
        [property: JsonPropertyName("lineNew")]
        int? LineNew,
        [property: JsonPropertyName("textOld")]
        string? TextOld,
        [property: JsonPropertyName("textNew")]
        string? TextNew,
        [property: JsonPropertyName("message")]
        string? Message = null);

    public static class DiffTool
    {
        private const int MaxInputCharacters = 200_000;
        private const int MaxHunks = 500;

        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        [Description(
            "Diffs two blocks of text line by line and returns a JSON array of only the lines that " +
            "changed -- ALWAYS use this instead of pasting both versions into your own reasoning and " +
            "working out what changed by eye; that costs real inference and this costs none. Each " +
            "array entry is one changed line: type \"replaced\" (an old line paired with the new line " +
            "it became -- textOld and textNew both set), \"deleted\" (textOld only, the line is gone), " +
            "or \"inserted\" (textNew only, the line is new); a final entry of type \"truncated\" " +
            "appears only if there were too many changed lines to list. lineOld/lineNew are 1-based " +
            "line numbers in whichever document still has that line, null on the side that doesn't. " +
            "Comparison is exact -- whitespace and case both count -- and a single trailing newline on " +
            "either input is ignored so it never shows up as a phantom blank-line change. Identical " +
            "inputs return an empty array []. Pairing within a changed block is positional, not by " +
            "matching similar content, same as any line-based diff (git included): if 2 lines become " +
            "5, the first 2 pair as \"replaced\" and the remaining 3 are \"inserted\".")]
        public static string DiffText(
            [Description("The first / \"before\" text.")]
            string? oldText,
            [Description("The second / \"after\" text.")]
            string? newText)
        {
            oldText ??= string.Empty;
            newText ??= string.Empty;

            if (oldText.Length > MaxInputCharacters || newText.Length > MaxInputCharacters)
            {
                return $"One of those texts is too large to diff in one call (limit {MaxInputCharacters:N0} " +
                       "characters each). Narrow it down to the section that actually matters first.";
            }

            var normalizedOld = StripSingleTrailingNewline(oldText);
            var normalizedNew = StripSingleTrailingNewline(newText);

            var sideBySide = SideBySideDiffBuilder.Diff(normalizedOld, normalizedNew,
                ignoreWhiteSpace: false, ignoreCase: false);

            var oldLines = sideBySide.OldText.Lines;
            var newLines = sideBySide.NewText.Lines;

            var hunks = new List<DiffHunk>();

            for (var i = 0; i < oldLines.Count; i++)
            {
                var o = oldLines[i];
                var n = newLines[i];

                if (o.Type == ChangeType.Unchanged && n.Type == ChangeType.Unchanged)
                {
                    continue;
                }

                if (hunks.Count >= MaxHunks)
                {
                    hunks.Add(new DiffHunk("truncated", null, null, null, null,
                        $"Stopped after {MaxHunks} changed lines -- these texts differ too much to list " +
                        "in full. Diff a smaller section, or treat this as evidence the two inputs are " +
                        "largely unrelated rather than close revisions of each other."));

                    break;
                }

                hunks.Add(Classify(o, n));
            }

            return JsonSerializer.Serialize(hunks, serializerOptions);
        }

        private static DiffHunk Classify(DiffPiece oldLine, DiffPiece newLine)
        {
            if (oldLine.Type == ChangeType.Imaginary)
            {
                return new DiffHunk("inserted", null, newLine.Position, null, newLine.Text);
            }

            if (newLine.Type == ChangeType.Imaginary)
            {
                return new DiffHunk("deleted", oldLine.Position, null, oldLine.Text, null);
            }

            return new DiffHunk("replaced", oldLine.Position, newLine.Position, oldLine.Text, newLine.Text);
        }

        private static string StripSingleTrailingNewline(string text)
        {
            if (text.EndsWith("\r\n", StringComparison.Ordinal))
            {
                return text[..^2];
            }

            return text.EndsWith('\n') ? text[..^1] : text;
        }
    }
}