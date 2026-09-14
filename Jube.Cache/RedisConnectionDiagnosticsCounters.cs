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

namespace Jube.Cache
{
    public sealed class RedisConnectionDiagnosticsCounters
    {
        private long configurationChangedBroadcastCount;
        private long configurationChangedCount;
        private long connectionFailedCount;
        private long connectionRestoredCount;
        private long errorMessageCount;
        private long internalErrorCount;

        public void IncrementConnectionFailed()
        {
            Interlocked.Increment(ref connectionFailedCount);
        }

        public void IncrementConnectionRestored()
        {
            Interlocked.Increment(ref connectionRestoredCount);
        }

        public void IncrementErrorMessage()
        {
            Interlocked.Increment(ref errorMessageCount);
        }

        public void IncrementInternalError()
        {
            Interlocked.Increment(ref internalErrorCount);
        }

        public void IncrementConfigurationChanged()
        {
            Interlocked.Increment(ref configurationChangedCount);
        }

        public void IncrementConfigurationChangedBroadcast()
        {
            Interlocked.Increment(ref configurationChangedBroadcastCount);
        }

        public RedisConnectionDiagnosticsSnapshot TakeSnapshot()
        {
            return new RedisConnectionDiagnosticsSnapshot(
                Interlocked.Exchange(ref connectionFailedCount, 0),
                Interlocked.Exchange(ref connectionRestoredCount, 0),
                Interlocked.Exchange(ref errorMessageCount, 0),
                Interlocked.Exchange(ref internalErrorCount, 0),
                Interlocked.Exchange(ref configurationChangedCount, 0),
                Interlocked.Exchange(ref configurationChangedBroadcastCount, 0));
        }
    }
}