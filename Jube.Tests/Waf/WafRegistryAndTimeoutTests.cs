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

using System;
using System.Diagnostics;
using FluentAssertions;
using Jube.App.Code.Waf;
using Jube.App.Code.Waf.Models;
using Jube.Data.Poco;
using Xunit;

namespace Jube.Test.Waf
{
    [Trait("Category", "Unit")]
    public sealed class WafRegistryAndTimeoutTests
    {
        [Fact]
        public void ANewRegistryStartsEmpty()
        {
            var registry = new WafRegistry();

            registry.Current.Signatures.Should().BeEmpty();
            registry.Current.Exceptions.Should().BeEmpty();
        }

        [Fact]
        public void UpdateSwapsTheActiveRuleSet()
        {
            var registry = new WafRegistry();
            var ruleSet = WafRuleSetCompiler.Compile(
            [
                new WafSignature
                {
                    Id = 1, Name = "S", Category = "C", Pattern = ".", TargetScope = 7,
                    MatchTimeoutMilliseconds = 50, Drop = 1, Active = 1
                }
            ], [], 100);

            registry.Update(ruleSet);

            registry.Current.Signatures.Should().ContainSingle();
        }

        [Fact]
        public void UpdateWithNullFallsBackToEmpty()
        {
            var registry = new WafRegistry();

            registry.Update(null);

            registry.Current.Should().BeSameAs(WafRuleSet.Empty);
        }

        [Fact]
        public void ACatastrophicRegexTimesOutAndDoesNotHangOrThrow()
        {
            var registry = WafTestRuleSets.RegistryFor(
            [
                new WafSignature
                {
                    Id = 1, Name = "REDOS", Category = "test", Pattern = "^(a+)+$", TargetScope = 7,
                    MatchTimeoutMilliseconds = 50, Drop = 1, Active = 1
                }
            ], []);
            var inspector = new WafInspector(registry);
            var evilInput = new string('a', 40) + "!";

            var request = new WafInspectionRequest("Http", "/api/Test", "POST", "127.0.0.1", "tester", "trace",
                [new WafFieldValue("$.field", evilInput, WafTargetScope.Body)]);

            var stopwatch = Stopwatch.StartNew();
            var result = inspector.Inspect(request);
            stopwatch.Stop();

            result.Blocked.Should().BeFalse("a regex timeout is treated as a non-match, never a hang");
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
        }
    }
}