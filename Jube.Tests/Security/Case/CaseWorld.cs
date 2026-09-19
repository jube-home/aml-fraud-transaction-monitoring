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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jube.Test.Infrastructure;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using Xunit.Abstractions;
using CasePoco = Jube.Data.Poco.Case;

namespace Jube.Test.Security.Case;

public sealed class CaseWorld
{
    private readonly DatabaseFixture fx;
    private readonly List<int> modelIds = [];
    private readonly List<Guid> archiveGuids = [];

    private CaseWorld(DatabaseFixture fx)
    {
        this.fx = fx;
    }

    public string Tag { get; } = Guid.NewGuid().ToString("N")[..8];

    public CaseGraph A
    {
        get => field.Required();
        private set;
    }

    public CaseGraph Gated
    {
        get => field.Required();
        private set;
    }

    public CaseGraph HiddenStatus
    {
        get => field.Required();
        private set;
    }

    public CaseGraph B
    {
        get => field.Required();
        private set;
    }

    public Guid ArchiveA { get; private set; }
    public Guid ArchiveB { get; private set; }
    public int WatcherA { get; private set; }
    public int WatcherB { get; private set; }
    public Guid VisualisationA { get; private set; }
    public Guid VisualisationB { get; private set; }
    public Guid RoleA { get; private set; }
    public Guid RoleNoPermission { get; private set; }
    public Guid RoleB { get; private set; }
    public int TenantAId { get; private set; }
    public int TenantBId { get; private set; }
    public IReadOnlyList<int> ModelIds => modelIds;

    public static async Task<CaseWorld> CreateAsync(DatabaseFixture fx)
    {
        var world = new CaseWorld(fx);
        await world.BuildAsync();
        return world;
    }

    private async Task BuildAsync()
    {
        await using var db = fx.GetDbContext();
        var userA = fx.Seed.UserWithPermission;
        var userB = fx.Seed.UserTenantB;

        RoleA = await RoleOfAsync(db, userA);
        RoleNoPermission = await RoleOfAsync(db, fx.Seed.UserWithoutPermission);
        RoleB = await RoleOfAsync(db, userB);
        TenantAId = (await db.UserInTenant.Where(u => u.User == userA).Select(u => u.TenantRegistryId).FirstAsync());
        TenantBId = (await db.UserInTenant.Where(u => u.User == userB).Select(u => u.TenantRegistryId).FirstAsync());

        A = await GraphAsync(db, userA, "A", RoleA, RoleA, RoleA);
        Gated = await GraphAsync(db, userA, "G", RoleNoPermission, RoleNoPermission, RoleNoPermission);
        HiddenStatus = await GraphAsync(db, userA, "H", RoleA, RoleNoPermission, RoleA);
        B = await GraphAsync(db, userB, "B", RoleB, RoleB, RoleB);

        ArchiveA = await ArchiveAsync(db, A.ModelId, "A");
        ArchiveB = await ArchiveAsync(db, B.ModelId, "B");
        WatcherA = await WatcherAsync(db, TenantAId, "A");
        WatcherB = await WatcherAsync(db, TenantBId, "B");
        VisualisationA = await VisualisationAsync(db, TenantAId, userA, "A");
        VisualisationB = await VisualisationAsync(db, TenantBId, userB, "B");
    }

    private static Task<Guid> RoleOfAsync(DbContext db, string user)
    {
        return db.UserRegistry.Where(u => u.Name == user).Select(u => u.RoleRegistryGuid).FirstAsync();
    }

    private async Task<Guid> VisualisationAsync(DbContext db, int tenantId, string user, string label)
    {
        var guid = Guid.NewGuid();
        await db.InsertWithInt32IdentityAsync(new Data.Poco.VisualisationRegistry
        {
            Name = $"ZzTestPenVis{label}{Tag}",
            Guid = guid,
            TenantRegistryId = tenantId,
            Active = 1,
            Locked = 0,
            Deleted = 0,
            Version = 1,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow
        });
        return guid;
    }

    private Task<int> WatcherAsync(DbContext db, int tenantId, string label)
    {
        return db.InsertWithInt32IdentityAsync(new Data.Poco.ActivationWatcher
        {
            TenantRegistryId = tenantId,
            Key = $"ZzTestWatcherKey{label}{Tag}",
            KeyValue = $"ZzTestWatcherValue{label}{Tag}",
            Longitude = 1.5,
            Latitude = 2.5,
            ActivationRuleSummary = $"ZzTestSummary{label}{Tag}",
            ResponseElevationContent = "content",
            ResponseElevation = 1,
            BackColor = "#ff0000",
            ForeColor = "#000000",
            CreatedDate = DateTime.UtcNow
        });
    }

    private async Task<Guid> ArchiveAsync(DbContext db, int modelId, string label)
    {
        var guid = Guid.NewGuid();
        archiveGuids.Add(guid);
        await db.InsertWithInt64IdentityAsync(new Data.Poco.Archive
        {
            Json = "{\"payload\":{\"AccountId\":\"ZzTestArchive" + label + Tag + "\"}}",
            EntityAnalysisModelInstanceEntryGuid = guid,
            EntityAnalysisModelId = modelId,
            CreatedDate = DateTime.UtcNow
        });
        return guid;
    }

    private async Task<CaseGraph> GraphAsync(DbContext db, string user, string label, Guid workflowRole,
        Guid statusRole, Guid childRole)
    {
        var repository = new EntityAnalysisModelRepository(db, user);
        var model = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
        {
            Name = $"ZzTestPenModel{label}{Tag}",
            Guid = Guid.NewGuid(),
            Active = 1,
            Locked = 0,
            Deleted = 0
        });
        modelIds.Add(model.Id);

        var workflowGuid = Guid.NewGuid();
        var workflowId = await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
        {
            Name = $"ZzTestPenWorkflow{label}{Tag}",
            Guid = workflowGuid,
            EntityAnalysisModelId = model.Id,
            Active = 1,
            Locked = 0,
            Deleted = 0,
            Version = 1,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow
        });
        await db.InsertAsync(new Data.Poco.CaseWorkflowRole
        {
            CaseWorkflowGuid = workflowGuid,
            Guid = Guid.NewGuid(),
            RoleRegistryGuid = workflowRole,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow,
            Deleted = 0,
            Version = 1
        });

        var (statusId, statusGuid) = await StatusAsync(db, user, label + "1", workflowId, statusRole, 1);
        var (status2Id, status2Guid) = await StatusAsync(db, user, label + "2", workflowId, statusRole, 2);

        var actionGuid = Guid.NewGuid();
        var actionId = await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowAction
        {
            Name = $"ZzTestPenAction{label}{Tag}",
            Guid = actionGuid,
            CaseWorkflowId = workflowId,
            Active = 1,
            Locked = 0,
            Deleted = 0,
            Version = 1,
            EnableHttpEndpoint = 0,
            EnableNotification = 0,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow
        });
        var actionRoleId = await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowActionRole
        {
            CaseWorkflowActionGuid = actionGuid,
            Guid = Guid.NewGuid(),
            RoleRegistryGuid = childRole,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow,
            Deleted = 0,
            Version = 1
        });

        var displayGuid = Guid.NewGuid();
        var displayId = await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowDisplay
        {
            Name = $"ZzTestPenDisplay{label}{Tag}",
            Guid = displayGuid,
            CaseWorkflowId = workflowId,
            Html = "<b>ZzTest</b>",
            Active = 1,
            Locked = 0,
            Deleted = 0,
            Version = 1,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow
        });
        var displayRoleId = await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowDisplayRole
        {
            CaseWorkflowDisplayGuid = displayGuid,
            Guid = Guid.NewGuid(),
            RoleRegistryGuid = childRole,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow,
            Deleted = 0,
            Version = 1
        });

        var filterGuid = Guid.NewGuid();
        var filterId = await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowFilter
        {
            Name = $"ZzTestPenFilter{label}{Tag}",
            Guid = filterGuid,
            CaseWorkflowId = workflowId,
            SelectJson = "{}",
            FilterJson = "{}",
            FilterSql = "1=1",
            FilterTokens = "[]",
            Active = 1,
            Locked = 0,
            Deleted = 0,
            Version = 1,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow
        });
        var filterRoleId = await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowFilterRole
        {
            CaseWorkflowFilterGuid = filterGuid,
            Guid = Guid.NewGuid(),
            RoleRegistryGuid = childRole,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow,
            Deleted = 0,
            Version = 1
        });

        var caseKey = "AccountId";
        var caseKeyValue = $"ZzTestCaseValue{label}{Tag}";
        var entryGuid = Guid.NewGuid();
        var caseId = await db.InsertWithInt32IdentityAsync(new CasePoco
        {
            EntityAnalysisModelInstanceEntryGuid = entryGuid,
            CaseWorkflowGuid = workflowGuid,
            CaseWorkflowStatusGuid = statusGuid,
            CreatedDate = DateTime.UtcNow,
            Locked = 0,
            ClosedStatusId = 0,
            LastClosedStatus = 0,
            CaseKey = caseKey,
            CaseKeyValue = caseKeyValue,
            Diary = 0,
            Rating = 1,
            Json = "{}"
        });

        var fileId = await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseFile
        {
            Object = Encoding.UTF8.GetBytes($"ZzTestFileContent{label}{Tag}"),
            CaseId = caseId,
            CaseKey = caseKey,
            CaseKeyValue = caseKeyValue,
            Name = $"ZzTestFile{label}{Tag}.txt",
            Extension = ".txt",
            ContentType = "text/plain",
            Size = 20,
            CreatedDate = DateTime.UtcNow,
            CreatedUser = user
        });

        var noteId = await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseNote
        {
            Note = $"ZzTestNote{label}{Tag}",
            ActionId = actionId,
            PriorityId = 1,
            CaseId = caseId,
            CaseKey = caseKey,
            CaseKeyValue = caseKeyValue,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow
        });

        return new CaseGraph
        {
            ModelId = model.Id,
            WorkflowId = workflowId,
            WorkflowGuid = workflowGuid,
            StatusId = statusId,
            StatusGuid = statusGuid,
            Status2Id = status2Id,
            Status2Guid = status2Guid,
            ActionId = actionId,
            ActionGuid = actionGuid,
            DisplayId = displayId,
            DisplayGuid = displayGuid,
            FilterId = filterId,
            FilterGuid = filterGuid,
            CaseId = caseId,
            CaseKey = caseKey,
            CaseKeyValue = caseKeyValue,
            EntryGuid = entryGuid,
            FileId = fileId,
            NoteId = noteId,
            ActionRoleId = actionRoleId,
            DisplayRoleId = displayRoleId,
            FilterRoleId = filterRoleId,
            RoleGuid = childRole
        };
    }

    private async Task<(int Id, Guid Guid)> StatusAsync(DbContext db, string user, string label, int workflowId,
        Guid role, byte priority)
    {
        var guid = Guid.NewGuid();
        var id = await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
        {
            Name = $"ZzTestPenStatus{label}{Tag}",
            Guid = guid,
            CaseWorkflowId = workflowId,
            Active = 1,
            Locked = 0,
            Deleted = 0,
            Version = 1,
            Priority = priority,
            EnableNotification = 0,
            EnableHttpEndpoint = 0,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow
        });
        await db.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
        {
            CaseWorkflowStatusGuid = guid,
            Guid = Guid.NewGuid(),
            RoleRegistryGuid = role,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow,
            Deleted = 0,
            Version = 1
        });
        return (id, guid);
    }

    public async Task<CaseGraph> ExtraGraphAsync(string label, bool tenantB = false)
    {
        await using var db = fx.GetDbContext();
        return tenantB
            ? await GraphAsync(db, fx.Seed.UserTenantB, label, RoleB, RoleB, RoleB)
            : await GraphAsync(db, fx.Seed.UserWithPermission, label, RoleA, RoleA, RoleA);
    }

    public async Task<int> ExtraCaseAsync(CaseGraph graph, string keyValue, Action<CasePoco>? shape = null)
    {
        await using var db = fx.GetDbContext();
        var row = new CasePoco
        {
            EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
            CaseWorkflowGuid = graph.WorkflowGuid,
            CaseWorkflowStatusGuid = graph.StatusGuid,
            CreatedDate = DateTime.UtcNow,
            Locked = 0,
            ClosedStatusId = 0,
            LastClosedStatus = 0,
            CaseKey = graph.CaseKey,
            CaseKeyValue = keyValue,
            Diary = 0,
            Rating = 1,
            Json = "{}"
        };
        shape?.Invoke(row);
        return await db.InsertWithInt32IdentityAsync(row);
    }

    public async Task<CasePoco?> LoadCaseAsync(int id)
    {
        await using var db = fx.GetDbContext();
        return await db.Case.Where(c => c.Id == id).FirstOrDefaultAsync();
    }

    public async Task<long> CountAsync(string table, string where, params DataParameter[] parameters)
    {
        await using var db = fx.GetDbContext();
        return await db.ExecuteAsync<long>($"SELECT COUNT(*) FROM \"{table}\" WHERE {where}", parameters);
    }

    public async Task<T> ScalarAsync<T>(string sql, params DataParameter[] parameters)
    {
        await using var db = fx.GetDbContext();
        return await db.ExecuteAsync<T>(sql, parameters);
    }

    public async Task<string> FingerprintAsync()
    {
        await using var db = fx.GetDbContext();
        var models = modelIds.ToArray();
        var parts = new List<string>();
        foreach (var (table, filter) in new[]
                 {
                     ("Case",
                         "\"CaseWorkflowGuid\" IN (SELECT \"Guid\" FROM \"CaseWorkflow\" WHERE \"EntityAnalysisModelId\" = ANY(@m))"),
                     ("CaseWorkflow", "\"EntityAnalysisModelId\" = ANY(@m)"),
                     ("CaseWorkflowAction",
                         "\"CaseWorkflowId\" IN (SELECT \"Id\" FROM \"CaseWorkflow\" WHERE \"EntityAnalysisModelId\" = ANY(@m))"),
                     ("CaseWorkflowDisplay",
                         "\"CaseWorkflowId\" IN (SELECT \"Id\" FROM \"CaseWorkflow\" WHERE \"EntityAnalysisModelId\" = ANY(@m))"),
                     ("CaseWorkflowFilter",
                         "\"CaseWorkflowId\" IN (SELECT \"Id\" FROM \"CaseWorkflow\" WHERE \"EntityAnalysisModelId\" = ANY(@m))"),
                     ("CaseNote",
                         "\"CaseId\" IN (SELECT \"Id\" FROM \"Case\" WHERE \"CaseWorkflowGuid\" IN (SELECT \"Guid\" FROM \"CaseWorkflow\" WHERE \"EntityAnalysisModelId\" = ANY(@m)))"),
                     ("CaseFile",
                         "\"CaseId\" IN (SELECT \"Id\" FROM \"Case\" WHERE \"CaseWorkflowGuid\" IN (SELECT \"Guid\" FROM \"CaseWorkflow\" WHERE \"EntityAnalysisModelId\" = ANY(@m)))"),
                     ("CaseEvent",
                         "\"CaseId\" IN (SELECT \"Id\" FROM \"Case\" WHERE \"CaseWorkflowGuid\" IN (SELECT \"Guid\" FROM \"CaseWorkflow\" WHERE \"EntityAnalysisModelId\" = ANY(@m)))")
                 })
        {
            var count = await db.ExecuteAsync<long>($"SELECT COUNT(*) FROM \"{table}\" WHERE {filter}",
                new DataParameter("m", models));
            parts.Add($"{table}={count}");
        }

        var casesState = await db.Case.Where(c => c.Id == A.CaseId || c.Id == B.CaseId || c.Id == Gated.CaseId ||
                                                  c.Id == HiddenStatus.CaseId)
            .Select(c => new
            {
                c.Id, c.Locked, c.LockedUser, c.ClosedStatusId, c.Diary, c.DiaryDate, c.Rating,
                c.CaseWorkflowStatusGuid,
                c.Json, c.CaseKey, c.CaseKeyValue
            }).ToListAsync();
        parts.AddRange(casesState.OrderBy(c => c.Id).Select(c => JsonSerializer.Serialize(c)));
        return string.Join("|", parts);
    }

    public async Task DisposeAsync(ITestOutputHelper? output = null)
    {
        await using var db = fx.GetDbContext();
        var m = new DataParameter("m", modelIds.ToArray());
        var g = new DataParameter("g", archiveGuids.ToArray());
        const string wf = "(SELECT \"Id\" FROM \"CaseWorkflow\" WHERE \"EntityAnalysisModelId\" = ANY(@m))";
        const string wfg = "(SELECT \"Guid\" FROM \"CaseWorkflow\" WHERE \"EntityAnalysisModelId\" = ANY(@m))";
        const string cases = "(SELECT \"Id\" FROM \"Case\" WHERE \"CaseWorkflowGuid\" IN " + wfg + ")";
        string[] statements =
        [
            "DELETE FROM \"CaseWorkflowFormEntryValue\" WHERE \"CaseWorkflowFormEntryId\" IN (SELECT \"Id\" FROM \"CaseWorkflowFormEntry\" WHERE \"CaseId\" IN " +
            cases + ")",
            "DELETE FROM \"CaseWorkflowFormEntry\" WHERE \"CaseId\" IN " + cases,
            "DELETE FROM \"CaseEvent\" WHERE \"CaseId\" IN " + cases,
            "DELETE FROM \"CaseNote\" WHERE \"CaseId\" IN " + cases,
            "DELETE FROM \"CaseFile\" WHERE \"CaseId\" IN " + cases,
            "DELETE FROM \"Case\" WHERE \"CaseWorkflowGuid\" IN " + wfg,
            "DELETE FROM \"CaseWorkflowActionRole\" WHERE \"CaseWorkflowActionGuid\" IN (SELECT \"Guid\" FROM \"CaseWorkflowAction\" WHERE \"CaseWorkflowId\" IN " +
            wf + ")",
            "DELETE FROM \"CaseWorkflowDisplayRole\" WHERE \"CaseWorkflowDisplayGuid\" IN (SELECT \"Guid\" FROM \"CaseWorkflowDisplay\" WHERE \"CaseWorkflowId\" IN " +
            wf + ")",
            "DELETE FROM \"CaseWorkflowFilterRole\" WHERE \"CaseWorkflowFilterGuid\" IN (SELECT \"Guid\" FROM \"CaseWorkflowFilter\" WHERE \"CaseWorkflowId\" IN " +
            wf + ")",
            "DELETE FROM \"CaseWorkflowStatusRole\" WHERE \"CaseWorkflowStatusGuid\" IN (SELECT \"Guid\" FROM \"CaseWorkflowStatus\" WHERE \"CaseWorkflowId\" IN " +
            wf + ")",
            "DELETE FROM \"CaseWorkflowActionVersion\" WHERE \"CaseWorkflowActionId\" IN (SELECT \"Id\" FROM \"CaseWorkflowAction\" WHERE \"CaseWorkflowId\" IN " +
            wf + ")",
            "DELETE FROM \"CaseWorkflowDisplayVersion\" WHERE \"CaseWorkflowDisplayId\" IN (SELECT \"Id\" FROM \"CaseWorkflowDisplay\" WHERE \"CaseWorkflowId\" IN " +
            wf + ")",
            "DELETE FROM \"CaseWorkflowFilterVersion\" WHERE \"CaseWorkflowFilterId\" IN (SELECT \"Id\" FROM \"CaseWorkflowFilter\" WHERE \"CaseWorkflowId\" IN " +
            wf + ")",
            "DELETE FROM \"CaseWorkflowStatusVersion\" WHERE \"CaseWorkflowStatusId\" IN (SELECT \"Id\" FROM \"CaseWorkflowStatus\" WHERE \"CaseWorkflowId\" IN " +
            wf + ")",
            "DELETE FROM \"CaseWorkflowAction\" WHERE \"CaseWorkflowId\" IN " + wf,
            "DELETE FROM \"CaseWorkflowDisplay\" WHERE \"CaseWorkflowId\" IN " + wf,
            "DELETE FROM \"CaseWorkflowFilter\" WHERE \"CaseWorkflowId\" IN " + wf,
            "DELETE FROM \"CaseWorkflowStatus\" WHERE \"CaseWorkflowId\" IN " + wf,
            "DELETE FROM \"CaseWorkflowRole\" WHERE \"CaseWorkflowGuid\" IN " + wfg,
            "DELETE FROM \"CaseWorkflowVersion\" WHERE \"CaseWorkflowId\" IN " + wf,
            "DELETE FROM \"CaseWorkflow\" WHERE \"EntityAnalysisModelId\" = ANY(@m)",
            "DELETE FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE 'ZzTestPenVis%' AND \"Name\" LIKE '%" + Tag +
            "'",
            "DELETE FROM \"ArchiveTagVersion\" WHERE \"ArchiveTagId\" IN (SELECT \"Id\" FROM \"ArchiveTag\" WHERE \"EntityAnalysisModelInstanceEntryGuid\" = ANY(@g))",
            "DELETE FROM \"ArchiveTag\" WHERE \"EntityAnalysisModelInstanceEntryGuid\" = ANY(@g)",
            "DELETE FROM \"Archive\" WHERE \"EntityAnalysisModelId\" = ANY(@m)",
            "DELETE FROM \"ActivationWatcher\" WHERE \"Key\" LIKE 'ZzTestWatcherKey%' AND \"KeyValue\" LIKE '%" + Tag +
            "'",
            "DELETE FROM \"EntityAnalysisModelVersion\" WHERE \"EntityAnalysisModelId\" = ANY(@m)",
            "DELETE FROM \"EntityAnalysisModelInstance\" WHERE \"EntityAnalysisModelId\" = ANY(@m)",
            "DELETE FROM \"MockArchive\" WHERE \"EntityAnalysisModelId\" = ANY(@m)",
            "DELETE FROM \"EntityAnalysisModel\" WHERE \"Id\" = ANY(@m)"
        ];
        foreach (var statement in statements)
        {
            try
            {
                await db.ExecuteAsync(statement, m, g);
            }
            catch (Exception ex)
            {
                output?.WriteLine(
                    $"CaseWorld cleanup failed: {statement[..Math.Min(60, statement.Length)]} => {ex.Message}");
            }
        }
    }
}