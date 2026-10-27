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

using Jube.Dto.Options;

namespace Jube.CaseRisk
{
    public static class LaneCalculator
    {
        public static LaneAssessment Decide(LaneInputs inputs, CaseRiskSettings settings)
        {
            if (inputs.LiveMoneyDeadline.HasValue)
            {
                return new LaneAssessment(CaseRiskLane.LiveMoney, "A payment hold is open and decides at its deadline.",
                    inputs.LiveMoneyDeadline, inputs.Now > inputs.LiveMoneyDeadline.Value);
            }

            if (!string.IsNullOrWhiteSpace(inputs.MandatoryReason))
            {
                return new LaneAssessment(CaseRiskLane.Mandatory, inputs.MandatoryReason, inputs.NextDeadline,
                    inputs.NextDeadline.HasValue && inputs.Now > inputs.NextDeadline.Value);
            }

            if (inputs.NextDeadline.HasValue)
            {
                var deadline = inputs.NextDeadline.Value;
                var label = string.IsNullOrWhiteSpace(inputs.NextDeadlineLabel) ? "A diary" : inputs.NextDeadlineLabel;

                if (inputs.Now > deadline)
                {
                    return new LaneAssessment(CaseRiskLane.DeadlineNear, $"{label} is overdue.", deadline, true);
                }

                if (deadline - inputs.Now <= TimeSpan.FromDays(settings.DeadlineLaneDays))
                {
                    return new LaneAssessment(CaseRiskLane.DeadlineNear,
                        $"{label} falls due within {settings.DeadlineLaneDays} days.", deadline, false);
                }

                return new LaneAssessment(CaseRiskLane.Standard,
                    $"{label} is more than {settings.DeadlineLaneDays} days away.", deadline, false);
            }

            return new LaneAssessment(CaseRiskLane.Standard, "No deadline applies.", null, false);
        }
    }
}
