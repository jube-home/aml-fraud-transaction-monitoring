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

using Jube.AskJooby.Agents;
using Jube.AskJooby.Agents.Models;

namespace Jube.Service.Agent
{
    internal static class FunctionToolGroupIcons
    {
        internal static AgentToolIcon For(string groupName)
        {
            return groupName switch
            {
                "CalendarFacts" => AgentToolIcon.CalendarDays,
                "ClockAndIdentifiers" => AgentToolIcon.Clock,
                "CurrencyAndCountry" => AgentToolIcon.Coins,
                "DateArithmetic" => AgentToolIcon.CalendarPlus,
                "EncodingAndHashing" => AgentToolIcon.Fingerprint,
                "FinancialIdentifiers" => AgentToolIcon.Landmark,
                "Geospatial" => AgentToolIcon.MapPin,
                "ListScreening" => AgentToolIcon.ListFilter,
                "NameSimilarity" => AgentToolIcon.SpellCheck,
                "OnlineIdentifiers" => AgentToolIcon.Globe,
                "PatternMatching" => AgentToolIcon.Regex,
                "PercentagesAndChanges" => AgentToolIcon.TrendingDown,
                "RoundingAndPowers" => AgentToolIcon.Superscript,
                "StructuredData" => AgentToolIcon.Braces,
                "TextCleaning" => AgentToolIcon.Eraser,
                "TextEditing" => AgentToolIcon.Pencil,
                "TextExtraction" => AgentToolIcon.Scissors,
                "TextInspection" => AgentToolIcon.TextSearch,
                "TextLists" => AgentToolIcon.List,
                "ThresholdsAndLimits" => AgentToolIcon.GaugeCircle,
                "TimeZonesAndFormats" => AgentToolIcon.Globe2,
                "UnitsAndNumerals" => AgentToolIcon.Binary,
                _ => AgentToolIcons.Default
            };
        }
    }
}