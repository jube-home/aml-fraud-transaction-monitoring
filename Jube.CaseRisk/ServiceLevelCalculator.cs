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
    public static class ServiceLevelCalculator
    {
        public const double AtRiskFraction = 0.8;

        public static ServiceLevelAssessment Assess(ServiceLevelInputs inputs)
        {
            var measures = new List<ServiceLevelMeasure>();
            var stop = inputs.ClosedDate;

            Add(measures, ServiceLevelDimension.FirstView, inputs.CreatedDate, inputs.FirstViewedDate, stop,
                inputs.FirstViewTargetMinutes, inputs.Now);
            Add(measures, ServiceLevelDimension.Ownership, inputs.CreatedDate, inputs.FirstOwnedDate, stop,
                inputs.OwnershipTargetMinutes, inputs.Now);
            Add(measures, ServiceLevelDimension.Resolution, inputs.CreatedDate, inputs.ClosedDate, null,
                inputs.ResolutionTargetMinutes, inputs.Now);

            if (inputs.StayEnteredDate.HasValue && !inputs.ClosedDate.HasValue)
            {
                Add(measures, ServiceLevelDimension.StatusDwell, inputs.StayEnteredDate.Value, null, null,
                    inputs.MaxDwellMinutes, inputs.Now);
            }

            var worst = measures.OrderByDescending(m => m.State).ThenByDescending(m => m.ElapsedFraction)
                .FirstOrDefault();

            return new ServiceLevelAssessment(worst?.State ?? ServiceLevelState.NoTarget, worst, measures);
        }

        private static void Add(List<ServiceLevelMeasure> measures, ServiceLevelDimension dimension, DateTime from,
            DateTime? reached, DateTime? abandonedAt, int? targetMinutes, DateTime now)
        {
            if (targetMinutes is not > 0)
            {
                return;
            }

            var target = TimeSpan.FromMinutes(targetMinutes.Value);
            var due = from + target;

            if (reached.HasValue)
            {
                var actual = Math.Max(0, (reached.Value - from).TotalMinutes);

                measures.Add(new ServiceLevelMeasure(dimension,
                    reached.Value <= due ? ServiceLevelState.Met : ServiceLevelState.Breached, due,
                    actual / targetMinutes.Value, actual, targetMinutes.Value));

                return;
            }

            if (abandonedAt.HasValue)
            {
                return;
            }

            var elapsed = Math.Max(0, (now - from).TotalMinutes);
            var fraction = elapsed / targetMinutes.Value;

            var state = fraction >= 1 ? ServiceLevelState.Breached
                : fraction >= AtRiskFraction ? ServiceLevelState.AtRisk
                : ServiceLevelState.OnTrack;

            measures.Add(new ServiceLevelMeasure(dimension, state, due, fraction, null, targetMinutes.Value));
        }
    }
}
