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

using Jube.Service.Query.Ready;

namespace Jube.Test.Service.Query.Ready.Models
{
    internal sealed class FakeSignals(bool stopping, bool engineReady, bool relayReady) : IReadinessSignals
    {
        public int EngineReads;
        public int RelayReads;

        public bool StoppingRequested => stopping;

        public bool EngineReady
        {
            get
            {
                EngineReads++;
                return engineReady;
            }
        }

        public bool RelayReady
        {
            get
            {
                RelayReads++;
                return relayReady;
            }
        }
    }
}