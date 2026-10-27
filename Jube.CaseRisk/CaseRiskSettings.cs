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

namespace Jube.CaseRisk
{
    public sealed record CaseRiskSettings
    {
        public double Kappa { get; init; } = 0.7;
        public double Rho { get; init; } = 0.3;
        public double PriorStrength { get; init; } = 20;
        public double BaseRatePrior { get; init; } = 0.08;
        public double ProbabilityFloor { get; init; } = 0.001;
        public int DeadlineLaneDays { get; init; } = 2;
        public double[] ImpactWeights { get; init; } = [1, 3, 10, 30, 100];
        public double[] MoneyBandThresholds { get; init; } = [5_000, 100_000, 1_000_000, 10_000_000];
        public double KeyHistoryHalfLifeDays { get; init; } = 180;
        public double QaSampleRate { get; init; } = 0.05;
        public int QaMaxPriority { get; init; } = 25;
    }
}
