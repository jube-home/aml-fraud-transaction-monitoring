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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.Sanctions.Models
{
    using System.Linq;
    using Jube.Dictionary.Extensions;

    public class SanctionEntry
    {
        public int SanctionEntryId { get; set; }
        private string[] elementValue;
        private volatile string[] tokens;

        public string[] SanctionElementValue
        {
            get => elementValue;
            set
            {
                elementValue = value;
                tokens = null;
                latinTokens = null;
            }
        }

        public string[] Tokens => tokens ??= elementValue
            .SelectMany(SanctionTextFolding.Tokens)
            .Distinct()
            .ToArray();

        private volatile string[] latinTokens;

        public string[] LatinTokens => latinTokens ??= elementValue
            .Select(v => v.TransliterateToLatin())
            .SelectMany(SanctionTextFolding.Tokens)
            .Distinct()
            .ToArray();

        public int SanctionEntrySourceId { get; set; }
        public string SanctionEntryReference { get; set; }
    }
}