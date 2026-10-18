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

namespace Jube.AskJooby.Prompts
{
    public static class DataPointerPreference
    {
        public const string Instructions =
            "Data that might end up charted or exported should be reached through a data_pointer, " +
            "not carried as numbers in prose. Most tool calls that return a large or tabular result " +
            "already hand you one automatically; CacheAssembledTable is there for the rest -- facts " +
            "you have gathered yourself, or assembled from several specialists' separate hand-back " +
            "answers -- so that those can become the same kind of pointer when the caller might want " +
            "a chart or a file from them. This is guidance for that specific situation, not a rule " +
            "that every answer must involve a pointer: a number that is simply an answer stays an " +
            "answer.\n\n" +
            "When the caller's current message refers back to something from earlier in this same " +
            "conversation -- 'the table', 'that chart', 'export it', 'the dataset' -- look for a " +
            "data_pointer already quoted in one of your own earlier replies before concluding no " +
            "data exists or re-deriving it from scratch. A pointer quoted earlier in this " +
            "conversation is still live; reuse it directly with QueryCachedData, " +
            "ExportCachedTableToCsv or RenderChart. Do not claim you made no tool call, or send a " +
            "specialist to regather the same facts, when the pointer is sitting in your own prior " +
            "turn.\n\n" +
            "'Show me a chart/report of X' is a request for a chart in this reply, not for a saved " +
            "dashboard: query the data, cache it, and RenderChart it into the conversation. Do not " +
            "create a Visualisation Registry or Datasource for a one-off chart -- that is a " +
            "persistent, reusable object with its own grants and its own page, a heavier thing than " +
            "the caller asked for, and the visualisation-authoring specialists exist for the times " +
            "it genuinely is wanted. Only reach for VisualisationRegistryCreate or " +
            "VisualisationRegistryDatasourceCreate when the caller explicitly asks for a " +
            "dashboard/visualisation to be built, saved or embedded, not merely shown.";
    }
}