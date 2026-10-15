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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jube.Cryptography;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Dto.Repository.Preservation;
using Jube.Preservation.Models;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.Preservation;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using MessagePack;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jube.Test.Preservation
{
    internal sealed record PreservationTenant(int Id, string UserName, string RoleName, Guid RoleGuid);

    internal sealed class PreservationHarness(DatabaseFixture fx)
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        public const string Salt = "ZzTestPreservationSalt";
        public const string Password = "correct horse";
        public DatabaseFixture Fixture { get; } = fx;

        public static MessagePackSerializerOptions Lz4 { get; } =
            MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        public async Task<PreservationTenant> CreateTenantAsync(string label, bool withUser = true)
        {
            await using var dbContext = Fixture.GetDbContext();
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}Pa{label}T{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            var roleGuid = Guid.NewGuid();
            var roleName = $"{DatabaseFixture.Prefix}Pa{label}R{suffix}";

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid,
                Name = roleName,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            await dbContext.InsertAsync(new RoleRegistryPermission
            {
                Guid = Guid.NewGuid(),
                PermissionSpecificationId = 38,
                RoleRegistryId = roleId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            var userName = $"{DatabaseFixture.Prefix}Pa{label}U{suffix}";

            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            if (withUser)
            {
                await dbContext.InsertAsync(new UserInTenant
                    { User = userName, TenantRegistryId = tenantId });
            }

            return new PreservationTenant(tenantId, userName, roleName, roleGuid);
        }

        public async Task<int> CreateModelAsync(PreservationTenant tenant, string tag)
        {
            await using var dbContext = Fixture.GetDbContext();

            return await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}{tag}M{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenant.Id,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = tenant.UserName
            });
        }

        public static ImportExportOptionsDto ExportOptionsFor(bool exhaustive = false, bool advancedAnalytics = true,
            bool roles = false)
        {
            return new ImportExportOptionsDto
            {
                Password = Password,
                Exhaustive = exhaustive,
                AdvancedAnalytics = advancedAnalytics,
                Roles = roles
            };
        }

        public async Task<byte[]> ExportAsync(PreservationTenant tenant, ImportExportOptionsDto options)
        {
            await using var dbContext = Fixture.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName);

            return (await service.ExportAsync(options)).EncryptedBytes;
        }

        public async Task ImportAsync(PreservationTenant tenant, byte[] bytes, CancellationToken token = default,
            IServiceChangeBus? bus = null)
        {
            await using var dbContext = Fixture.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName, bus);
            await using var stream = new MemoryStream(bytes);

            await service.ImportAsync([new PreservationImportFileDto { FileName = "x.jemp", Content = stream }],
                Password, token);
        }

        public static Task<PreservationService> BuildServiceAsync(DbContext dbContext, string userName,
            IServiceChangeBus? bus = null)
        {
            return PreservationService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                bus ?? new NullServiceChangeBus(), TestLog.NoOp, Salt, false);
        }

        public static Wrapper Decrypt(byte[] encrypted)
        {
            return MessagePackSerializer.Deserialize<Wrapper>(new JempAesEncryption(Password, Salt).Decrypt(encrypted),
                Lz4);
        }

        public static byte[] Encrypt(Wrapper wrapper)
        {
            return new JempAesEncryption(Password, Salt).Encrypt(MessagePackSerializer.Serialize(wrapper, Lz4));
        }

        public static byte[] DecryptToBytes(byte[] encrypted)
        {
            return new JempAesEncryption(Password, Salt).Decrypt(encrypted);
        }
    }
}