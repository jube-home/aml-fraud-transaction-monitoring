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

namespace Jube.Engine.EntityAnalysisModelInvoke.Simulation
{
    using System;
    using System.Collections.Generic;

    public sealed class BacktestResult
    {
        public long Scanned { get; set; }
        public long FilteredOut { get; set; }
        public long FilterErrors { get; set; }
        public long Evaluated { get; set; }
        public long Fired { get; set; }
        public long NotFired { get; set; }
        public long RuntimeErrors { get; set; }
        public bool ClassDefined { get; set; }
        public long Positives { get; set; }
        public long TruePositives { get; set; }
        public long FalsePositives { get; set; }
        public long FalseNegatives { get; set; }
        public long TrueNegatives { get; set; }
        public bool LimitReached { get; set; }
        public bool Aborted { get; set; }
        public string AbortReason { get; set; }
        public DateTime? EarliestReferenceDate { get; set; }
        public DateTime? LatestReferenceDate { get; set; }
        public long DurationMicroseconds { get; set; }
        public Dictionary<string, long> TagsInSample { get; set; } = new(StringComparer.Ordinal);
        public List<BacktestSample> TruePositiveSamples { get; set; } = [];
        public List<BacktestSample> FalsePositiveSamples { get; set; } = [];
        public List<BacktestSample> FalseNegativeSamples { get; set; } = [];
        public List<BacktestSample> ErrorSamples { get; set; } = [];
    }
}