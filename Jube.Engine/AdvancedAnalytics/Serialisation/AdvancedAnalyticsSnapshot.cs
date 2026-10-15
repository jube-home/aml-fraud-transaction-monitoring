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

namespace Jube.Engine.AdvancedAnalytics.Serialisation
{
    public abstract class AdvancedAnalyticsSnapshot
    {
        public abstract void Validate(int inputCount);

        protected static void RequireLength(string name, double[] values, int expected)
        {
            if (values == null)
            {
                throw new AdvancedAnalyticsSnapshotException($"Snapshot array '{name}' is missing.");
            }

            if (values.Length != expected)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Snapshot array '{name}' has {values.Length} elements but {expected} were expected.");
            }
        }
    }
}