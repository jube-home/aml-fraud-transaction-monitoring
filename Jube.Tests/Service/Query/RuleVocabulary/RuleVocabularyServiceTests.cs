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

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Service.Exceptions.Query.RuleVocabulary;
using Jube.Service.Query.RuleVocabulary;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.RuleVocabulary
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RuleVocabularyServiceTests(DatabaseFixture fx)
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private Task<RuleVocabularyService> ServiceAsync(Data.Context.DbContext dbContext, string? userName = null)
        {
            return RuleVocabularyService.CreateAsync(dbContext, userName ?? fx.Seed.UserWithPermission, TestLog.NoOp,
                localizers, new NullServiceChangeBus());
        }

        [Fact]
        public async Task FunctionsCanBeFoundByNameAndByTheTypeTheyAreCalledOnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var byName = await service.ListAsync("ismatch");
            var onStrings = await service.ListAsync(kind: "Function", receiverType: "String", take: 500);

            byName.Items.Should().Contain(w => w.Name == "IsMatch" && w.Kind == "Function" &&
                                               w.ReceiverTypes.Contains("String") && w.Overloads > 1);
            onStrings.Items.Should().HaveCount(200);
            onStrings.More.Should().BeTrue();
            onStrings.Items.Should().OnlyContain(w => w.ReceiverTypes.Contains("String"));
        }

        [Fact]
        public async Task PagingContinuesAfterTheLastNameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var first = await service.ListAsync(take: 10);
            var second = await service.ListAsync(take: 10, afterName: first.Items[^1].Name);

            second.Items.Select(w => w.Name).Should().NotIntersectWith(first.Items.Select(w => w.Name));
            string.Compare(second.Items[0].Name, first.Items[^1].Name, System.StringComparison.OrdinalIgnoreCase)
                .Should().BePositive();
        }

        [Fact]
        public async Task DescribeGivesEverySignatureAndIgnoresCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var word = await service.DescribeAsync("ISMATCH");
            var keyword = await service.DescribeAsync("if");

            word.Name.Should().Be("IsMatch");
            word.Overloads.Should().Contain(o => o.Signature == "<String>.IsMatch(pattern As String) As Boolean");
            keyword.Kind.Should().Be("Keyword");
            keyword.Overloads.Should().BeEmpty();
            await Assert.ThrowsAsync<NotFoundException>(() => service.DescribeAsync("NoSuchWord"));
        }

        [Fact]
        public async Task NoPermissionIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
        }
    }
}