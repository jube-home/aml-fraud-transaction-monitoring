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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Data.Helpers;
using Xunit;

namespace Jube.Test.Engine.Observability
{
    [Trait("Category", "Unit")]
    public sealed class LogCounterRuleSetTests
    {
        private static List<string> Run(LogCounterRuleSet set, string text, params string[] skipped)
        {
            var matched = new List<string>();
            set.Match(text, name => skipped.Contains(name), matched.Add, _ => { });

            return matched;
        }

        [Fact]
        public void EveryMatchingRuleReportsItsNameAndSkippedRulesAreNotEvaluated()
        {
            var set = new LogCounterRuleSet();
            set.Apply([("a", "boom"), ("b", "bo+m"), ("c", "quiet")], _ => { });

            Run(set, "boom").Should().Equal("a", "b");
            Run(set, "boom", "a").Should().Equal("b");
            set.Count.Should().Be(3);
        }

        [Fact]
        public void InvalidAndBlankDefinitionsAreWarnedOrSkippedAndTheRestKept()
        {
            var warnings = new List<string>();
            var set = new LogCounterRuleSet();

            set.Apply([("bad", "("), ("", "x"), ("blank", ""), ("good", "ok")], warnings.Add);

            warnings.Should().ContainSingle(w => w.Contains("'bad'"));
            Run(set, "ok").Should().Equal("good");
        }

        [Fact]
        public void NullOrEmptyTextMatchesNothing()
        {
            var set = new LogCounterRuleSet();
            set.Apply([("any", ".*")], _ => { });

            Run(set, string.Empty).Should().BeEmpty();
            Run(set, null!).Should().BeEmpty();
        }

        [Fact]
        public void AnUnchangedDefinitionIsReusedAndAChangedOneReplaced()
        {
            var set = new LogCounterRuleSet();
            set.Apply([("r", "a")], _ => { });
            set.Apply([("r", "a")], _ => { });
            Run(set, "a").Should().Equal("r");

            set.Apply([("r", "b")], _ => { });
            Run(set, "a").Should().BeEmpty();
            Run(set, "b").Should().Equal("r");
        }
    }
}
