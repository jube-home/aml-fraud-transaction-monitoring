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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.AdvancedAnalyticsNarrative
{
    [Description("A plain-language, expert-rule-generated explanation of an Advanced Analytics trained model " +
                 "version: one holistic set of paragraphs synthesising all available evidence, plus a shorter " +
                 "set of paragraphs per chart group interpreting that group's actual values for this version. " +
                 "Each entry is a list of paragraphs, rendered as separate <p> elements, never a single " +
                 "run-on block.")]
    public class AdvancedAnalyticsNarrativeDto
    {
        [Description("Paragraphs synthesising structure, data and quality for this version, in reading order.")]
        public List<string> Holistic { get; set; } = [];

        [Description("Paragraphs per chart group (ModelDiagram, Calibration, Roc), interpreting that group's " +
                     "actual values for this version. A group is omitted when there isn't enough data to say " +
                     "anything reliable about it.")]
        public Dictionary<string, List<string>> Groups { get; set; } = new();
    }
}