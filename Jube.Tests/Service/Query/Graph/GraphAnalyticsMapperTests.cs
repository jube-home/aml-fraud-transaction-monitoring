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
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Resources;
using Jube.Service.Query.Graph;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.Graph
{
    [Trait("Category", "Unit")]
    public sealed class GraphAnalyticsMapperTests
    {
        private static readonly IStringLocalizer strings =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance).Create(typeof(GraphResources));

        private static GraphCommunity Community(double? ratio)
        {
            return new GraphCommunity
            {
                Id = 7, Size = 5, ConfirmedCount = 2, ClosedCount = 4, TenantRateRatio = ratio,
                ComputedDate = DateTime.UtcNow
            };
        }

        [Fact]
        public void ACommunityWithARatioStatesItAgainstTheTenantRate()
        {
            GraphAnalyticsMapper.ToCommunity(Community(3), strings).Sentence.Should()
                .Be("Community 7 of 5 nodes: 2 confirmed of 4 closed cases, 3.0x the tenant rate.");
        }

        [Fact]
        public void ACommunityWithoutARatioOmitsItRatherThanPrintingZero()
        {
            GraphAnalyticsMapper.ToCommunity(Community(null), strings).Sentence.Should()
                .Be("Community 7 of 5 nodes: 2 confirmed of 4 closed cases.");
        }
    }
}
