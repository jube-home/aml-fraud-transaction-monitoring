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
using System.Reflection;
using FluentAssertions;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class ThinkingEffortLadderTests
    {
        [Fact]
        public void TheCheckSitsAtTheCheapestBandRatherThanInheritingAProviderDefault()
        {
            Default("AskJoobyValidationThinkingEffort").Should().Be(0,
                "the check is a bounded judgement against retrieved text, and it runs on top of a " +
                "turn that has already been paid for");
        }

        [Fact]
        public void TheShortUtilityCallsDoNotReasonAtAllByDefault()
        {
            Setting("AskJoobyUtilityEnableThinking")
                .Should().Be("False",
                    "a four-word chat title has a known output shape and nothing to reason about");
        }

        [Fact]
        public void EveryModelRoleCarriesItsOwnSwitchRatherThanInheritingAnother()
        {
            foreach (var key in new[]
                     {
                         "AskJoobyValidationEnableThinking", "AskJoobyValidationThinkingEffort",
                         "AskJoobyUtilityEnableThinking", "AskJoobyUtilityThinkingEffort"
                     })
            {
                Setting(key).Should().NotBeNullOrWhiteSpace(
                    $"{key} must be a registered setting with a default");
            }
        }

        private static int Default(string key)
        {
            return int.Parse(Setting(key));
        }

        private static string Setting(string key)
        {
            var environment = TestDynamicEnvironment.Create();

            var field = typeof(Jube.DynamicEnvironment.DynamicEnvironment)
                .GetField("appSettings", BindingFlags.Instance | BindingFlags.NonPublic);

            var settings = (Dictionary<string, string>)field!.GetValue(environment)!;

            return settings[key];
        }
    }
}