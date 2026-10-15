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
using Jube.Service.Agent.AdvancedAnalyticsTools;

namespace Jube.Test.AdvancedAnalytics.Conventions
{
    [AdvancedAnalyticsToolGroup("FixtureBad")]
    public class FixtureBadToolGroup
    {
        [AdvancedAnalyticsTool("FixtureDuplicate")]
        public double MissingEverything(double[] values, int count)
        {
            return values.Length + count;
        }

        [AdvancedAnalyticsTool("FixtureDuplicate")]
        [AdvancedAnalyticsRequiresPermission("Some Unknown Permission")]
        [Description("Fits the Accord logistic model to the supplied rows and returns coefficients.")]
        public double LibraryNamedAndUnknownPermission(
            [Description("Rows of predictor values used for fitting")]
            double[] values)
        {
            return values.Length;
        }

        [AdvancedAnalyticsTool("FixtureShort")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description("Too short.")]
        public double ShortDescription(
            [Description("tiny")] double[] values)
        {
            return values.Length;
        }

        [AdvancedAnalyticsTool("")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description("A tool that has been given an empty name by mistake in this fixture.")]
        public double EmptyName()
        {
            return 0;
        }
    }
}