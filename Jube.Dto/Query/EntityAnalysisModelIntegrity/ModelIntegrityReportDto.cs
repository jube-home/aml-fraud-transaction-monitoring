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

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.Query.EntityAnalysisModelIntegrity
{
    [Description("The basic integrity of a model: every finding of the dependency, compilation, configuration and " +
                 "engine checks.")]
    public class ModelIntegrityReportDto
    {
        [Description("The model checked.")] public int EntityAnalysisModelId { get; set; }

        [Description("The model's name.")] public string? ModelName { get; set; }

        [Description("When the checks ran.")] public DateTime CheckedDate { get; set; }

        [Description("Where the engine's state came from: InProcess (the engine in this process), Snapshot (what " +
                     "engines recorded at their last synchronisation) or Unavailable.")]
        public string EngineSource { get; set; } = string.Empty;

        [Description("How many findings are errors.")]
        public int Errors { get; set; }

        [Description("How many findings are warnings.")]
        public int Warnings { get; set; }

        [Description("How many findings are information.")]
        public int Infos { get; set; }

        [Description("Every finding, errors first.")]
        public List<IntegrityCheckDto> Checks { get; set; } = [];
    }
}