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

using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json;

namespace Jube.Engine.AdvancedAnalytics.Serialisation
{
    public sealed class AdvancedAnalyticsCalibratorSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public string Method { get; set; } = string.Empty;

        [JsonProperty(Required = Required.Always)]
        public double[] Parameters { get; set; } = [];

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Method))
            {
                throw new AdvancedAnalyticsSnapshotException("Calibrator method is missing.");
            }

            if (Parameters == null)
            {
                throw new AdvancedAnalyticsSnapshotException("Calibrator parameters are missing.");
            }
        }
    }
}