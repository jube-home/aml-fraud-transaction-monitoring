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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Service.AskJoobyExport;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class AskJoobyFileExportClientTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> createdConversationIds = [];
        private readonly List<int> createdMessageIds = [];
        private readonly List<int> createdTenantIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            if (createdConversationIds.Count > 0)
            {
                await dbContext.AskJoobyFile
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value)).DeleteAsync();
            }

            if (createdMessageIds.Count > 0)
            {
                await dbContext.AskJoobyMessage.Where(w => createdMessageIds.Contains(w.Id)).DeleteAsync();
            }

            if (createdConversationIds.Count > 0)
            {
                await dbContext.AskJoobyConversation.Where(w => createdConversationIds.Contains(w.Id))
                    .DeleteAsync();
            }

            if (createdTenantIds.Count > 0)
            {
                await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
            }
        }

        private async Task<(int TenantId, int ConversationId)> SeedConversationAsync(
            DbContext dbContext)
        {
            var suffix = Guid.NewGuid().ToString("N");

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}ExportClientTenant{suffix}", Active = 1, Locked = 0, Deleted = 0,
                Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var conversationId = await dbContext.InsertWithInt32IdentityAsync(new AskJoobyConversation
            {
                TenantRegistryId = tenantId, Guid = Guid.NewGuid(),
                Title = $"{DatabaseFixture.Prefix}ExportClientConversation",
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix, Version = 1
            });
            createdConversationIds.Add(conversationId);

            return (tenantId, conversationId);
        }

        [Fact]
        public async Task ExportAsyncCreatesAFileRowWithTheGivenBytesAndMetadataAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, conversationId) = await SeedConversationAsync(dbContext);
            var repository = new AskJoobyFileRepository(dbContext, tenantId, DatabaseFixture.Prefix);
            var client = new AskJoobyFileExportClient(repository, conversationId);
            var content = Encoding.UTF8.GetBytes("Name,Count\r\nWidgets,4\r\n");

            var fileId = await client.ExportAsync(content, "widgets.csv", "text/csv");

            var saved = await dbContext.AskJoobyFile.FirstOrDefaultAsync(f => f.Id == fileId);
            saved.Should().NotBeNull();
            saved!.Name.Should().Be("widgets.csv");
            saved.ContentType.Should().Be("text/csv");
            saved.Extension.Should().Be(".csv");
            saved.Object.Should().BeEquivalentTo(content);
            saved.AskJoobyConversationId.Should().Be(conversationId);
            saved.AskJoobyMessageId.Should().BeNull("nothing has attached it to a message yet");
        }

        [Fact]
        public async Task AttachToMessageAsyncLinksAnExportedFileToTheMessageThatProducedItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, conversationId) = await SeedConversationAsync(dbContext);
            var repository = new AskJoobyFileRepository(dbContext, tenantId, DatabaseFixture.Prefix);
            var client = new AskJoobyFileExportClient(repository, conversationId);
            var fileId = await client.ExportAsync([1, 2, 3], "export.csv", "text/csv");

            var messageId = await dbContext.InsertWithInt32IdentityAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.Assistant,
                Content = "Exported.",
                CreatedDate = DateTime.UtcNow, EntryTypeId = AskJoobyMessageRepository.EntryTypeMessage
            });
            createdMessageIds.Add(messageId);

            await repository.AttachToMessageAsync([fileId], messageId);

            var saved = await dbContext.AskJoobyFile.FirstOrDefaultAsync(f => f.Id == fileId);
            saved!.AskJoobyMessageId.Should().Be(messageId);

            var byMessage = await repository.ListByMessageIdsAsync([messageId]);
            byMessage.Should().ContainSingle(f => f.Id == fileId && f.Name == "export.csv");
        }
    }
}