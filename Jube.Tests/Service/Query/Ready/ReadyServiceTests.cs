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
using Jube.Test.Service.Query.Ready.Models;
using Xunit;

namespace Jube.Test.Service.Query.Ready
{
    [Trait("Category", "Service")]
    public class ReadyServiceTests
    {
        [Theory]
        [InlineData(true, false, false, false, false, false)]
        [InlineData(true, true, true, true, true, false)]
        [InlineData(false, true, false, true, true, false)]
        [InlineData(false, true, true, false, false, true)]
        [InlineData(false, true, true, true, false, false)]
        [InlineData(false, true, true, true, true, true)]
        [InlineData(false, false, false, false, false, true)]
        [InlineData(false, false, false, true, false, false)]
        [InlineData(false, false, false, true, true, true)]
        [InlineData(false, false, true, true, false, false)]
        [InlineData(false, false, true, true, true, true)]
        public void IsReady_MatchesLegacyDecision(bool stopping, bool engineEnabled, bool engineReady,
            bool relayEnabled, bool relayReady, bool expected)
        {
            var signals = new FakeSignals(stopping, engineReady, relayReady);

            Assert.Equal(expected, new ReadyService(signals, engineEnabled, relayEnabled).IsReady());
        }

        [Fact]
        public void IsReady_EngineDisabled_DoesNotConsultEngine()
        {
            var signals = new FakeSignals(false, false, true);

            Assert.True(new ReadyService(signals, false, true).IsReady());
            Assert.Equal(0, signals.EngineReads);
        }

        [Fact]
        public void IsReady_RelayDisabled_DoesNotConsultRelay()
        {
            var signals = new FakeSignals(false, true, false);

            Assert.True(new ReadyService(signals, true, false).IsReady());
            Assert.Equal(0, signals.RelayReads);
        }

        [Fact]
        public void IsReady_Stopping_ShortCircuitsBeforeEngineAndRelay()
        {
            var signals = new FakeSignals(true, true, true);

            Assert.False(new ReadyService(signals, true, true).IsReady());
            Assert.Equal(0, signals.EngineReads);
            Assert.Equal(0, signals.RelayReads);
        }

        [Fact]
        public void IsReady_EngineNotReady_ShortCircuitsBeforeRelay()
        {
            var signals = new FakeSignals(false, false, true);

            Assert.False(new ReadyService(signals, true, true).IsReady());
            Assert.Equal(0, signals.RelayReads);
        }
    }
}