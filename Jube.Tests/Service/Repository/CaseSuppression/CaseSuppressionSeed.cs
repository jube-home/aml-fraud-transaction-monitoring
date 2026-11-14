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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Repository.CaseNote;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace Jube.Test.Service.Repository.CaseSuppression
{
    public sealed class CaseSuppressionSeed : IAsyncDisposable
    {
        private readonly DatabaseFixture fx;
        private readonly List<int> ruleIds = [];
        private readonly List<int> xpathIds = [];
        private readonly List<Guid> archiveGuids = [];

        private CaseSuppressionSeed(DatabaseFixture fx, CaseNoteSeed seed)
        {
            this.fx = fx;
            Case = seed;
        }

        public CaseNoteSeed Case { get; }
        public Guid ModelGuid { get; private set; }
        public string RuleName { get; private set; } = string.Empty;
        public string IpKey { get; } = $"ip{Guid.NewGuid():N}"[..14];
        public string Ip { get; } = $"10.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}.1";
        public List<long> IpArchiveIds { get; } = [];
        public long OtherIpArchiveId { get; private set; }
        public string OtherAccount { get; } = $"other-a-{Guid.NewGuid():N}"[..14];
        public long ForeignArchiveId { get; private set; }

        public static async Task<CaseSuppressionSeed> CreateAsync(DatabaseFixture fx, bool ruleEnableOverride = true,
            bool ipEnabled = true, bool caseKeyEnabled = true)
        {
            var seed = new CaseSuppressionSeed(fx, await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission)
                .ConfigureAwait(false));

            await seed.BuildAsync(ruleEnableOverride, ipEnabled, caseKeyEnabled).ConfigureAwait(false);

            return seed;
        }

        public Task<string> UserAsync(params int[] specs) => Case.CreateUserAsync(fx.Seed.UserWithPermission, specs);

        public async Task SetRuleAsync(bool enableOverride, bool enableForce = true)
        {
            await using var db = fx.GetDbContext();

            await db.GetTable<Data.Poco.EntityAnalysisModelActivationRule>().Where(w => w.Name == RuleName
                    && w.EntityAnalysisModelId == Case.ModelId)
                .Set(s => s.EnableOverride, (byte)(enableOverride ? 1 : 0))
                .Set(s => s.EnableForce, (byte)(enableForce ? 1 : 0)).UpdateAsync().ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await using var db = fx.GetDbContext();

            var suppressions = db.CaseSuppression.Where(w => w.CaseId == Case.CaseId).Select(s => s.Id);

            await db.CaseSuppressionApproval.Where(w => suppressions.Contains(w.CaseSuppressionId)).DeleteAsync()
                .ConfigureAwait(false);
            await db.CaseSuppressionTransaction.Where(w => suppressions.Contains(w.CaseSuppressionId)).DeleteAsync()
                .ConfigureAwait(false);
            await db.CaseSuppressionTarget.Where(w => suppressions.Contains(w.CaseSuppressionId)).DeleteAsync()
                .ConfigureAwait(false);
            await db.CaseSuppressionVersion.Where(w => suppressions.Contains(w.CaseSuppressionId)).DeleteAsync()
                .ConfigureAwait(false);
            await db.CaseSuppression.Where(w => w.CaseId == Case.CaseId).DeleteAsync().ConfigureAwait(false);

            await db.GetTable<EntityAnalysisModelOverrideVersion>().Where(w => w.EntityAnalysisModelGuid == ModelGuid)
                .DeleteAsync().ConfigureAwait(false);
            await db.GetTable<EntityAnalysisModelActivationRuleOverrideVersion>()
                .Where(w => w.EntityAnalysisModelGuid == ModelGuid).DeleteAsync().ConfigureAwait(false);
            await db.EntityAnalysisModelOverride.Where(w => w.EntityAnalysisModelGuid == ModelGuid).DeleteAsync()
                .ConfigureAwait(false);
            await db.EntityAnalysisModelActivationRuleOverride.Where(w => w.EntityAnalysisModelGuid == ModelGuid)
                .DeleteAsync().ConfigureAwait(false);
            await db.ArchiveKey.Where(w => archiveGuids.Contains(w.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync()
                .ConfigureAwait(false);
            await db.Archive.Where(w => w.EntityAnalysisModelId == Case.ModelId).DeleteAsync().ConfigureAwait(false);
            await db.GetTable<EntityAnalysisModelRequestXpath>().Where(w => xpathIds.Contains(w.Id)).DeleteAsync()
                .ConfigureAwait(false);

            foreach (var id in ruleIds)
            {
                await db.GetTable<EntityAnalysisModelActivationRuleVersion>()
                    .Where(w => w.EntityAnalysisModelActivationRuleId == id).DeleteAsync().ConfigureAwait(false);
                await db.GetTable<Data.Poco.EntityAnalysisModelActivationRule>().Where(w => w.Id == id).DeleteAsync()
                    .ConfigureAwait(false);
            }

            await Case.DisposeAsync().ConfigureAwait(false);
        }

        private async Task BuildAsync(bool ruleEnableOverride, bool ipEnabled, bool caseKeyEnabled)
        {
            await using var db = fx.GetDbContext();

            ModelGuid = await db.EntityAnalysisModel.Where(w => w.Id == Case.ModelId).Select(s => s.Guid)
                .FirstAsync().ConfigureAwait(false);

            foreach (var (name, enabled) in new[] { (Case.CaseKey, caseKeyEnabled), (IpKey, ipEnabled) })
            {
                xpathIds.Add(await db.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = Case.ModelId, Name = name, XPath = $"$.{name}", DataTypeId = 1, Active = 1,
                    Locked = 0, Deleted = 0, EnableOverride = (byte)(enabled ? 1 : 0), ReportTable = 1, Guid = Guid.NewGuid(),
                    CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix, Version = 1
                }).ConfigureAwait(false));
            }

            RuleName = $"{DatabaseFixture.Prefix}Rule{Guid.NewGuid():N}"[..40];

            var rule = await new EntityAnalysisModelActivationRuleRepository(db, fx.Seed.UserWithPermission)
                .InsertAsync(new Data.Poco.EntityAnalysisModelActivationRule
                {
                    EntityAnalysisModelId = Case.ModelId, Name = RuleName, Active = 1, Locked = 0, Deleted = 0,
                    EnableOverride = (byte)(ruleEnableOverride ? 1 : 0), EnableForce = 1
                }).ConfigureAwait(false);

            ruleIds.Add(rule.Id);

            for (var i = 0; i < 3; i++)
            {
                IpArchiveIds.Add(await ArchiveAsync(db, Case.CaseKeyValue, Ip, i).ConfigureAwait(false));
            }

            OtherIpArchiveId = await ArchiveAsync(db, Case.CaseKeyValue, "192.0.2.99", 3).ConfigureAwait(false);
            await ArchiveAsync(db, OtherAccount, Ip, 4).ConfigureAwait(false);
            ForeignArchiveId = await ArchiveAsync(db, $"other-b-{Guid.NewGuid():N}"[..14], Ip, 5)
                .ConfigureAwait(false);
        }

        public async Task<long> ArchiveAsync(Jube.Data.Context.DbContext db, string account, string ip, int minutes)
        {
            var when = DateTime.UtcNow.AddMinutes(-minutes - 10);

            var payload = new Dictionary<string, object>
            {
                [Case.CaseKey] = account, [IpKey] = ip, ["Amount"] = 10 + minutes
            };

            var guid = Guid.NewGuid();
            archiveGuids.Add(guid);

            var id = await db.InsertWithInt64IdentityAsync(new Data.Poco.Archive
            {
                Json = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["payload"] = payload
                }),
                EntityAnalysisModelInstanceEntryGuid = guid, EntryKeyValue = account,
                ResponseElevation = 0, EntityAnalysisModelId = Case.ModelId, ActivationRuleCount = 0,
                CreatedDate = when, ReferenceDate = when, Version = 1
            }).ConfigureAwait(false);

            await db.BulkCopyAsync(new List<Data.Poco.ArchiveKey>
            {
                new() { ProcessingTypeId = 1, Key = Case.CaseKey, KeyValueString = account, EntityAnalysisModelInstanceEntryGuid = guid, Version = 1 },
                new() { ProcessingTypeId = 1, Key = IpKey, KeyValueString = ip, EntityAnalysisModelInstanceEntryGuid = guid, Version = 1 }
            }).ConfigureAwait(false);

            return id;
        }
    }
}
