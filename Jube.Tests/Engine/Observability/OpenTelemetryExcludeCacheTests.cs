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

using FluentAssertions;
using Jube.Engine.Observability;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Engine.Observability
{
    [Trait("Category", "Unit")]
    public sealed class OpenTelemetryExcludeCacheTests
    {
        private static OpenTelemetryExcludeCache NewCache()
        {
            return new OpenTelemetryExcludeCache(string.Empty, TestLog.NoOp);
        }

        [Fact]
        public void NothingIsExcludedBeforeTheFirstRefresh()
        {
            NewCache().IsExcluded("jube.engine.logs.warn.count").Should().BeFalse();
        }

        [Fact]
        public void AnAppliedNameIsExcludedIgnoringCase()
        {
            var cache = NewCache();
            cache.Apply(["jube.engine.logs.warn.count"]);

            cache.IsExcluded("jube.engine.logs.warn.count").Should().BeTrue();
            cache.IsExcluded("Jube.Engine.Logs.Warn.Count").Should().BeTrue();
        }

        [Theory]
        [InlineData("jube.engine.logs.warn")]
        [InlineData("jube.engine.logs.warn.count.extra")]
        [InlineData("*")]
        [InlineData("jube.*")]
        [InlineData("")]
        [InlineData(null)]
        public void OnlyTheFullNameMatchesAndWildcardsAreLiteral(string? candidate)
        {
            var cache = NewCache();
            cache.Apply(["jube.engine.logs.warn.count"]);

            cache.IsExcluded(candidate!).Should().BeFalse();
        }

        [Fact]
        public void AWildcardRowExcludesOnlyAnInstrumentLiterallyNamedThat()
        {
            var cache = NewCache();
            cache.Apply(["*", "jube.*"]);

            cache.IsExcluded("jube.engine.stage.duration").Should().BeFalse();
            cache.IsExcluded("*").Should().BeTrue();
        }

        [Fact]
        public void ApplyingANewListReplacesTheOldOneSoDeletedExclusionsStopApplying()
        {
            var cache = NewCache();
            cache.Apply(["a.one", "a.two"]);
            cache.Apply(["a.two"]);

            cache.IsExcluded("a.one").Should().BeFalse();
            cache.IsExcluded("a.two").Should().BeTrue();
        }

        [Fact]
        public void NullAndEmptyRowsAreDropped()
        {
            var cache = NewCache();

            cache.Apply([null!, "", "a.one"]);

            cache.IsExcluded("a.one").Should().BeTrue();
            cache.IsExcluded(string.Empty).Should().BeFalse();
        }
    }
}
