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
using System.Linq;
using System.Threading;
using Jube.Service.Agent.AdvancedAnalyticsTools;

namespace Jube.Test.AdvancedAnalytics.Conventions
{
    [AdvancedAnalyticsToolGroup("FixtureGood")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class FixtureGoodToolGroup
    {
        [AdvancedAnalyticsTool("FixtureMean")]
        [Description("Returns the arithmetic mean of the supplied numbers, ignoring nothing.")]
        public double Mean(
            [Description("The numbers whose mean is wanted")]
            double[] values,
            CancellationToken token = default)
        {
            return values.Average();
        }

        [AdvancedAnalyticsTool("FixtureTrain")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description("Pretends to train a model, used only to prove permissions are unioned.")]
        public int Train([Description("Number of iterations to run")] int iterations)
        {
            return iterations;
        }
    }
}