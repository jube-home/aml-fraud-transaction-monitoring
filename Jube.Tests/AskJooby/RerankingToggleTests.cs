/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using FluentAssertions;
using Jube.App.Code.AskJooby;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class RerankingToggleTests
    {
        [Fact]
        public void RerankingIsOnByDefaultSoAnOperatorHasToOptOut()
        {
            var environment = TestDynamicEnvironment.Create();

            AskJoobyConversationServiceFactory.FlagDefaultTrue(environment, "AskJoobyEnableReranking")
                .Should().BeTrue("existing deployments must keep reranking until an operator deliberately " +
                                 "turns it off to route around a rate-limited or unhealthy reranker provider");
        }

        [Fact]
        public void SettingTheFlagToFalseTurnsRerankingOff()
        {
            var environment = TestDynamicEnvironment.Create(
                new Dictionary<string, string> { ["AskJoobyEnableReranking"] = "False" });

            AskJoobyConversationServiceFactory.FlagDefaultTrue(environment, "AskJoobyEnableReranking")
                .Should().BeFalse();
        }

        [Fact]
        public void OnlyTheLiteralFalseTurnsRerankingOff()
        {
            var environment = TestDynamicEnvironment.Create(
                new Dictionary<string, string> { ["AskJoobyEnableReranking"] = "nonsense" });

            AskJoobyConversationServiceFactory.FlagDefaultTrue(environment, "AskJoobyEnableReranking")
                .Should().BeTrue("an unrecognised value must fail safe to the existing behaviour, not silently " +
                                 "disable reranking");
        }
    }
}