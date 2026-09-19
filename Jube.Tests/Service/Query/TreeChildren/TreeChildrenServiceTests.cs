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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Query.TreeChildren;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.TreeChildren;
using Jube.Service.Query.TreeChildren;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.TreeChildren.Models;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;
using Poco = Jube.Data.Poco;

namespace Jube.Test.Service.Query.TreeChildren
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class TreeChildrenServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<Func<DbContext, Task>> cleanups = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            for (var i = cleanups.Count - 1; i >= 0; i--)
            {
                await cleanups[i](dbContext);
            }
        }

        private static Child ToChild(EntityAnalysisModelTreeChildDto d) =>
            new(d.Key, d.Name, d.Color, d.EntityAnalysisModelId, d.EntityAnalysisModelGuid);

        private static Child ToChild(VisualisationRegistryTreeChildDto d) =>
            new(d.Key, d.Name, d.Color, d.VisualisationRegistryId, null);

        private static Child ToChild(RoleRegistryTreeChildDto d) =>
            new(d.Key, d.Name, d.Color, null, d.RoleRegistryGuid);

        private static Child ToChild(CasesWorkflowTreeChildDto d) =>
            new(d.Key, d.Name, d.Color, d.CasesWorkflowId, null);

        private static Task<List<Child>> NormAsync(Func<Task<List<EntityAnalysisModelTreeChildDto>>> call) =>
            NormAsync(call, ToChild);

        private static Task<List<Child>> NormAsync(Func<Task<List<VisualisationRegistryTreeChildDto>>> call) =>
            NormAsync(call, ToChild);

        private static Task<List<Child>> NormAsync(Func<Task<List<RoleRegistryTreeChildDto>>> call) =>
            NormAsync(call, ToChild);

        private static Task<List<Child>> NormAsync(Func<Task<List<CasesWorkflowTreeChildDto>>> call) =>
            NormAsync(call, ToChild);

        private static async Task<List<Child>> NormAsync<T>(Func<Task<List<T>>> call, Func<T, Child> map) =>
            [.. (await call().ConfigureAwait(false)).Select(map)];

        private static NodeCase Row<T>(TreeChildrenNode node, ParentKind kind, string dtoShape,
            Func<TreeChildrenService, Parents, CancellationToken, Task<List<Child>>> invoke,
            Func<Parents, string, byte, byte, T> build) where T : class
        {
            return new NodeCase(node, kind, dtoShape, invoke, async (self, db, _, parents, name, active, deleted) =>
            {
                var entity = build(parents, name, active, deleted);
                var id = Convert.ToInt32(await db.InsertWithIdentityAsync(entity), CultureInfo.InvariantCulture);
                typeof(T).GetProperty("Id").Required().SetValue(entity, id);
                self.cleanups.Add(async d => await d.DeleteAsync(entity));
                return new Seeded(id, name);
            });
        }

        private static readonly NodeCase[] cases =
        [
            Row(TreeChildrenNodes.RequestXPath, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetRequestXPathAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelRequestXpath
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.VisualisationRegistryDatasource, ParentKind.VisualisationRegistryId,
                "VisualisationRegistry",
                (s, p, t) => NormAsync(() => s.GetVisualisationRegistryDatasourceAsync(p.VisualisationRegistryId, t)),
                (p, n, a, d) => new Poco.VisualisationRegistryDatasource
                {
                    VisualisationRegistryId = p.VisualisationRegistryId, Name = n, Active = a, Deleted = d,
                    Guid = Guid.NewGuid()
                }),
            new NodeCase(TreeChildrenNodes.UserRegistry, ParentKind.RoleGuid, "RoleRegistry",
                (s, p, t) => NormAsync(() => s.GetUserRegistryAsync(p.RoleGuid, t)), SeedUserAsync),
            new NodeCase(TreeChildrenNodes.RoleRegistryPermission, ParentKind.RoleId, "RoleRegistry",
                (s, p, t) => NormAsync(() => s.GetRoleRegistryPermissionAsync(p.RoleId, t)), SeedRolePermissionAsync),
            Row(TreeChildrenNodes.VisualisationRegistryParameter, ParentKind.VisualisationRegistryId,
                "VisualisationRegistry",
                (s, p, t) => NormAsync(() => s.GetVisualisationRegistryParameterAsync(p.VisualisationRegistryId, t)),
                (p, n, a, d) => new Poco.VisualisationRegistryParameter
                {
                    VisualisationRegistryId = p.VisualisationRegistryId, Name = n, Active = a, Deleted = d,
                    Guid = Guid.NewGuid()
                }),
            Row(TreeChildrenNodes.InlineFunction, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetInlineFunctionAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelInlineFunction
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.Tag, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetTagAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelTag
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.GatewayRule, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetGatewayRuleAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelGatewayRule
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.Exhaustive, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetExhaustiveAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.ExhaustiveSearchInstance
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.Reprocessing, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetReprocessingAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelReprocessingRule
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d }),
            Row(TreeChildrenNodes.Adaptation, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetAdaptationAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelHttpAdaptation
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.CaseWorkflow, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetCaseWorkflowAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.CaseWorkflow
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.AbstractionCalculation, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetAbstractionCalculationAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelAbstractionCalculation
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.AbstractionRule, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetAbstractionRuleAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelAbstractionRule
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.ActivationRule, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetActivationRuleAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelActivationRule
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.TtlCounter, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetTtlCounterAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelTtlCounter
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.InlineScript, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetInlineScriptAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelInlineScript
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.Sanctions, ParentKind.ModelId, "Model",
                (s, p, t) => NormAsync(() => s.GetSanctionsAsync(p.ModelId, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelSanction
                    { EntityAnalysisModelId = p.ModelId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.List, ParentKind.ModelGuid, "ModelGuid",
                (s, p, t) => NormAsync(() => s.GetListAsync(p.ModelGuid, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelList
                {
                    EntityAnalysisModelGuid = p.ModelGuid, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid()
                }),
            Row(TreeChildrenNodes.Dictionary, ParentKind.ModelGuid, "ModelGuid",
                (s, p, t) => NormAsync(() => s.GetDictionaryAsync(p.ModelGuid, t)),
                (p, n, a, d) => new Poco.EntityAnalysisModelDictionary
                {
                    EntityAnalysisModelGuid = p.ModelGuid, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid()
                }),
            Row(TreeChildrenNodes.CaseWorkflowXPath, ParentKind.WorkflowId, "CasesWorkflow",
                (s, p, t) => NormAsync(() => s.GetCaseWorkflowXPathAsync(p.WorkflowId, t)),
                (p, n, a, d) => new Poco.CaseWorkflowXPath
                    { CaseWorkflowId = p.WorkflowId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.CaseWorkflowForm, ParentKind.WorkflowId, "CasesWorkflow",
                (s, p, t) => NormAsync(() => s.GetCaseWorkflowFormAsync(p.WorkflowId, t)),
                (p, n, a, d) => new Poco.CaseWorkflowForm
                    { CaseWorkflowId = p.WorkflowId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.CaseWorkflowAction, ParentKind.WorkflowId, "CasesWorkflow",
                (s, p, t) => NormAsync(() => s.GetCaseWorkflowActionAsync(p.WorkflowId, t)),
                (p, n, a, d) => new Poco.CaseWorkflowAction
                    { CaseWorkflowId = p.WorkflowId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.CaseWorkflowMacro, ParentKind.WorkflowId, "CasesWorkflow",
                (s, p, t) => NormAsync(() => s.GetCaseWorkflowMacroAsync(p.WorkflowId, t)),
                (p, n, a, d) => new Poco.CaseWorkflowMacro
                    { CaseWorkflowId = p.WorkflowId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.CaseWorkflowFilter, ParentKind.WorkflowId, "CasesWorkflow",
                (s, p, t) => NormAsync(() => s.GetCaseWorkflowFilterAsync(p.WorkflowId, t)),
                (p, n, a, d) => new Poco.CaseWorkflowFilter
                    { CaseWorkflowId = p.WorkflowId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.CaseWorkflowDisplay, ParentKind.WorkflowId, "CasesWorkflow",
                (s, p, t) => NormAsync(() => s.GetCaseWorkflowDisplayAsync(p.WorkflowId, t)),
                (p, n, a, d) => new Poco.CaseWorkflowDisplay
                    { CaseWorkflowId = p.WorkflowId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() }),
            Row(TreeChildrenNodes.CaseWorkflowStatus, ParentKind.WorkflowId, "CasesWorkflow",
                (s, p, t) => NormAsync(() => s.GetCaseWorkflowStatusAsync(p.WorkflowId, t)),
                (p, n, a, d) => new Poco.CaseWorkflowStatus
                    { CaseWorkflowId = p.WorkflowId, Name = n, Active = a, Deleted = d, Guid = Guid.NewGuid() })
        ];

        public static TheoryData<string> Routes()
        {
            var data = new TheoryData<string>();
            foreach (var c in cases)
            {
                data.Add(c.Node.Route);
            }

            return data;
        }

        private static NodeCase CaseFor(string route) => cases.Single(c => c.Node.Route == route);

        private static Task<Seeded> SeedUserAsync(TreeChildrenServiceTests self, DbContext db, string _,
            Parents parents, string name, byte active, byte deleted)
        {
            return self.InsertUserRowAsync(db, parents, name, active, deleted);
        }

        private async Task<Seeded> InsertUserRowAsync(DbContext db, Parents parents, string name, byte active,
            byte deleted)
        {
            var entity = new Poco.UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = parents.RoleGuid,
                Name = name,
                Email = $"{name}@example.invalid",
                Password = "not-used",
                Active = active,
                PasswordLocked = 0,
                Deleted = deleted,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };
            var id = await db.InsertWithInt32IdentityAsync(entity);
            entity.Id = id;
            cleanups.Add(async d => await d.DeleteAsync(entity));
            return new Seeded(id, name);
        }

        private static readonly int[] seedablePermissionSpecs = [40, 41, 42, 43];
        private int nextSpec;

        private static Task<Seeded> SeedRolePermissionAsync(TreeChildrenServiceTests self, DbContext db, string _,
            Parents parents, string name, byte active, byte deleted)
        {
            return self.InsertRolePermissionRowAsync(db, parents, active, deleted);
        }

        private async Task<Seeded> InsertRolePermissionRowAsync(DbContext db, Parents parents, byte active,
            byte deleted)
        {
            var spec = seedablePermissionSpecs[nextSpec++ % seedablePermissionSpecs.Length];
            var entity = new Poco.RoleRegistryPermission
            {
                Guid = Guid.NewGuid(),
                PermissionSpecificationId = spec,
                RoleRegistryId = parents.RoleId,
                Active = active,
                Locked = 0,
                Deleted = deleted,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };
            var id = await db.InsertWithInt32IdentityAsync(entity);
            entity.Id = id;
            cleanups.Add(async d => await d.DeleteAsync(entity));
            var specName = await db.PermissionSpecification.Where(w => w.Id == spec).Select(w => w.Name).FirstAsync();
            return new Seeded(id, specName);
        }

        private static string NewName(string tag) =>
            $"{DatabaseFixture.Prefix}{tag}{Guid.NewGuid():N}"[
                ..Math.Min(40, DatabaseFixture.Prefix.Length + tag.Length + 32)];

        private static Task<TreeChildrenService> BuildServiceAsync(DbContext dbContext, string? userName,
            ILog? log = null, ILog? auditLog = null, IServiceChangeBus? serviceChangeBus = null)
        {
            return TreeChildrenService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private static Task<int> TenantOfAsync(DbContext db, string user) =>
            db.UserInTenant.Where(w => w.User == user).Select(w => w.TenantRegistryId).FirstAsync();

        private async Task<Parents> CreateParentsAsync(DbContext db, string tenantUser)
        {
            var tenantId = await TenantOfAsync(db, tenantUser);

            var model = await new EntityAnalysisModelRepository(db, tenantUser).InsertAsync(
                new Poco.EntityAnalysisModel
                {
                    Name = NewName("Model"), Guid = Guid.NewGuid(), Active = 1, Locked = 0, Deleted = 0
                });
            cleanups.Add(async d =>
            {
                await d.GetTable<Poco.EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == model.Id)
                    .DeleteAsync();
                await d.EntityAnalysisModel.Where(w => w.Id == model.Id).DeleteAsync();
            });

            var workflow = new Poco.CaseWorkflow
            {
                Name = NewName("Workflow"), Guid = Guid.NewGuid(), EntityAnalysisModelId = model.Id, Active = 1,
                Locked = 0, Deleted = 0, EnableVisualisation = 0, Version = 1, CreatedUser = tenantUser,
                CreatedDate = DateTime.UtcNow
            };
            workflow.Id = await db.InsertWithInt32IdentityAsync(workflow);
            cleanups.Add(async d => await d.DeleteAsync(workflow));

            var visualisation = new Poco.VisualisationRegistry
            {
                Name = NewName("Vis"), Guid = Guid.NewGuid(), Active = 1, Deleted = 0, TenantRegistryId = tenantId
            };
            visualisation.Id = await db.InsertWithInt32IdentityAsync(visualisation);
            cleanups.Add(async d => await d.DeleteAsync(visualisation));

            var role = new Poco.RoleRegistry
            {
                Guid = Guid.NewGuid(), Name = NewName("Role"), Active = 1, Locked = 0, Deleted = 0,
                TenantRegistryId = tenantId, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };
            role.Id = await db.InsertWithInt32IdentityAsync(role);
            cleanups.Add(async d => await d.DeleteAsync(role));

            return new Parents(model.Id, model.Guid, visualisation.Id, workflow.Id, role.Id, role.Guid);
        }

        private async Task<string> CreatePartialUserAsync(DbContext db, string tenantUser, params int[] specs)
        {
            var tenantId = await TenantOfAsync(db, tenantUser);
            var role = new Poco.RoleRegistry
            {
                Guid = Guid.NewGuid(), Name = NewName("PartialRole"), Active = 1, Locked = 0, Deleted = 0,
                TenantRegistryId = tenantId, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };
            role.Id = await db.InsertWithInt32IdentityAsync(role);
            cleanups.Add(async d => await d.DeleteAsync(role));

            foreach (var spec in specs)
            {
                var permission = new Poco.RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(), PermissionSpecificationId = spec, RoleRegistryId = role.Id, Active = 1,
                    Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                };
                permission.Id = await db.InsertWithInt32IdentityAsync(permission);
                cleanups.Add(async d => await d.DeleteAsync(permission));
            }

            var userName = NewName("PartialUser");
            var user = new Poco.UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = role.Guid, Name = userName,
                Email = $"{userName}@example.invalid", Password = "not-used", Active = 1, PasswordLocked = 0,
                Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            };
            user.Id = await db.InsertWithInt32IdentityAsync(user);
            cleanups.Add(async d => await d.DeleteAsync(user));

            await db.InsertAsync(new Poco.UserInTenant { User = userName, TenantRegistryId = tenantId });
            cleanups.Add(async d => await d.UserInTenant.Where(w => w.User == userName).DeleteAsync());
            return userName;
        }

        private static int[] PreExistingKeys(NodeCase c, Parents p) =>
            c.Node.Route == "CaseWorkflow" ? [p.WorkflowId] : [];

        private static int? ExpectedIntParent(NodeCase c, Parents p) => c.Kind switch
        {
            ParentKind.ModelId => p.ModelId,
            ParentKind.VisualisationRegistryId => p.VisualisationRegistryId,
            ParentKind.WorkflowId => p.WorkflowId,
            _ => null
        };

        private static Guid? ExpectedGuidParent(NodeCase c, Parents p) => c.Kind switch
        {
            ParentKind.ModelGuid => p.ModelGuid,
            ParentKind.RoleGuid or ParentKind.RoleId => p.RoleGuid,
            _ => null
        };

        private static readonly (string Route, string Key, int[] Permissions)[] legacyTable =
        [
            ("RequestXPath", "id", [7]), ("VisualisationRegistryDatasource", "id", [33]),
            ("UserRegistry", "guid", [35]), ("RoleRegistryPermission", "id", [36]),
            ("VisualisationRegistryParameter", "id", [32]), ("InlineFunction", "id", [8]), ("Tag", "id", [37]),
            ("GatewayRule", "id", [10]), ("Exhaustive", "id", [16]), ("Reprocessing", "id", [26]),
            ("Adaptation", "id", [15]), ("CaseWorkflow", "id", [18, 19, 20, 21, 22, 23, 24, 25]),
            ("AbstractionCalculation", "id", [14]), ("AbstractionRule", "id", [13, 14]),
            ("ActivationRule", "id", [17]), ("TTLCounter", "id", [12]), ("InlineScript", "id", [9]),
            ("Sanctions", "id", [11]), ("List", "guid", [3]), ("Dictionary", "guid", [4]),
            ("CaseWorkflowXPath", "key", [20]), ("CaseWorkflowForm", "key", [21]),
            ("CaseWorkflowAction", "key", [22]), ("CaseWorkflowMacro", "key", [24]),
            ("CaseWorkflowFilter", "key", [25]), ("CaseWorkflowDisplay", "key", [23]),
            ("CaseWorkflowStatus", "key", [19])
        ];

        [Fact]
        public void NodeTableMatchesTheLegacyRoutesKeysAndPermissionSets()
        {
            legacyTable.Should().HaveCount(27);
            TreeChildrenNodes.All.Should().HaveCount(27);

            TreeChildrenNodes.All.Select(n => (n.Route, n.KeyName, n.Permissions)).Should()
                .BeEquivalentTo(legacyTable, options => options.WithStrictOrdering());
            TreeChildrenNodes.All.Select(n => n.Route).Should().OnlyHaveUniqueItems();
            cases.Select(c => c.Node.Route).Should().BeEquivalentTo(legacyTable.Select(l => l.Route));
        }

        [Fact]
        public void EveryOperationHasAUniqueToolNameRegisteredInTheCatalogue()
        {
            var tools = new List<ServiceToolDescriptor>();
            typeof(ServiceToolCatalogue).GetMethod("AddTreeChildren",
                BindingFlags.NonPublic | BindingFlags.Static).Required().Invoke(null, [tools]);

            var expected = legacyTable.Select(l => $"TreeChildren{l.Route}Get").ToList();
            tools.Select(t => t.Name).Should().BeEquivalentTo(expected);
            tools.Select(t => t.Name).Should().OnlyHaveUniqueItems();
            tools.Should().OnlyContain(t => t.Kind == OperationKind.Read && t.Idempotent && !t.Destructive);
            tools.Should().OnlyContain(t => !t.Name.Contains('_'));
            ServiceToolCatalogue.All.Select(t => t.Name).Should().OnlyHaveUniqueItems();

            var attributes = typeof(TreeChildrenService).GetMethods()
                .Select(m => m.GetCustomAttribute<ServiceOperationAttribute>()).Where(a => a != null).ToList();
            attributes.Select(a => a.Required().Name).Should().BeEquivalentTo(expected);
            attributes.Should().OnlyContain(a =>
                a.Required().Kind == OperationKind.Read && a.Required().Idempotent && !a.Required().Destructive);
            TreeChildrenNodes.All.Select(n => n.ToolName).Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void EndpointsExposeExactlyTheLegacyRoutesAsAuthorisedGetsWithTheLegacyParameterNames()
        {
            var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
            builder.Services.AddSingleton<ILog>(TestLog.NoOp);
            builder.Services.AddSingleton(
                (global::Jube.DynamicEnvironment.DynamicEnvironment)System.Runtime.CompilerServices.RuntimeHelpers
                    .GetUninitializedObject(typeof(global::Jube.DynamicEnvironment.DynamicEnvironment)));
            builder.Services.AddSingleton(localizers);
            builder.Services.AddSingleton<IServiceChangeBus>(new NullServiceChangeBus());
            using var app = builder.Build();

            Jube.App.Endpoints.Query.TreeChildrenEndpoints.MapTreeChildrenEndpoints(app);

            var endpoints = ((Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)app).DataSources
                .SelectMany(d => d.Endpoints).OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>().ToList();

            endpoints.Select(e => e.RoutePattern.RawText).Should().BeEquivalentTo(
                legacyTable.Select(l => $"/api/TreeChildren/{l.Route}"));
            foreach (var (route, key, _) in legacyTable)
            {
                var endpoint = endpoints.Single(e => e.RoutePattern.RawText == $"/api/TreeChildren/{route}");
                endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>().Required().HttpMethods
                    .Should().BeEquivalentTo(["GET"]);
                endpoint.Metadata.GetOrderedMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>()
                    .Should().NotBeEmpty($"{route} must require authorisation");
                endpoint.Metadata.GetMetadata<MethodInfo>().Required().GetParameters().Select(p => p.Name).Should()
                    .Contain(key, $"{route} binds its parent from '{key}'");
                endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.EndpointNameMetadata>().Required()
                    .EndpointName
                    .Should().Be($"TreeChildren{route}Get");
            }
        }

        [Fact]
        public void DtoJsonShapesAreUnchanged()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            Property(new EntityAnalysisModelTreeChildDto(), options).Should()
                .BeEquivalentTo("entityAnalysisModelId", "entityAnalysisModelGuid", "key", "name", "color");
            Property(new VisualisationRegistryTreeChildDto(), options).Should()
                .BeEquivalentTo("visualisationRegistryId", "key", "name", "color");
            Property(new RoleRegistryTreeChildDto(), options).Should()
                .BeEquivalentTo("roleRegistryGuid", "key", "name", "color");
            Property(new CasesWorkflowTreeChildDto(), options).Should()
                .BeEquivalentTo("casesWorkflowId", "key", "name", "color");
        }

        private static List<string> Property<T>(T dto, JsonSerializerOptions options) =>
        [
            .. JsonDocument.Parse(JsonSerializer.Serialize(dto, options)).RootElement.EnumerateObject()
                .Select(p => p.Name)
        ];

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                TreeChildrenService.CreateAsync(dbContext, userName, log, localizers, new NullServiceChangeBus(),
                    TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Theory]
        [MemberData(nameof(Routes))]
        public async Task EveryNodeThrowsForbiddenWithItsOwnRequiredSpecificationsWhenPermissionMissingAsync(
            string route)
        {
            var c = CaseFor(route);
            await using var dbContext = fx.GetDbContext();
            var parents = await CreateParentsAsync(dbContext, fx.Seed.UserWithPermission);
            await c.Seed(this, dbContext, route, parents, NewName("Hidden"), 1, 0);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => c.Invoke(service, parents, default));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo(c.Node.Permissions);
        }

        [Theory]
        [MemberData(nameof(Routes))]
        public async Task EveryNodeIsForbiddenForARoleHoldingOnlyAnUnrelatedPermissionAsync(string route)
        {
            var c = CaseFor(route);
            await using var dbContext = fx.GetDbContext();
            var parents = await CreateParentsAsync(dbContext, fx.Seed.UserWithPermission);
            var unrelated = await CreatePartialUserAsync(dbContext, fx.Seed.UserWithPermission, 40);
            var service = await BuildServiceAsync(dbContext, unrelated);

            await Assert.ThrowsAsync<ForbiddenException>(() => c.Invoke(service, parents, default));
        }

        [Theory]
        [MemberData(nameof(Routes))]
        public async Task EveryNodeIsAllowedWhenTheCallerHoldsAnyOneOfItsPermissionsAsync(string route)
        {
            var c = CaseFor(route);
            await using var dbContext = fx.GetDbContext();
            var parents = await CreateParentsAsync(dbContext, fx.Seed.UserWithPermission);
            foreach (var spec in c.Node.Permissions)
            {
                var single = await CreatePartialUserAsync(dbContext, fx.Seed.UserWithPermission, spec);
                var service = await BuildServiceAsync(dbContext, single);

                var result = await c.Invoke(service, parents, default);

                result.Should().NotBeNull($"holding only spec {spec} is enough for {route}");
            }
        }

        [Theory]
        [MemberData(nameof(Routes))]
        public async Task EveryNodeReturnsSeededChildrenMappedFieldByFieldAsync(string route)
        {
            var c = CaseFor(route);
            await using var dbContext = fx.GetDbContext();
            var parents = await CreateParentsAsync(dbContext, fx.Seed.UserWithPermission);
            var activeRow = await c.Seed(this, dbContext, route, parents, NewName("Act"), 1, 0);
            var inactiveRow = await c.Seed(this, dbContext, route, parents, NewName("Inact"), 0, 0);
            await c.Seed(this, dbContext, route, parents, NewName("Del"), 1, 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await c.Invoke(service, parents, default);

            result.Select(r => r.Key).Should().BeEquivalentTo(
                [activeRow.Id, inactiveRow.Id, .. PreExistingKeys(c, parents)],
                $"{route} returns every non-deleted child of the parent and nothing else");
            var active = result.Single(r => r.Key == activeRow.Id);
            active.Name.Should().Be(activeRow.Name);
            active.Color.Should().Be("green");
            var inactive = result.Single(r => r.Key == inactiveRow.Id);
            inactive.Name.Should().Be(inactiveRow.Name);
            inactive.Color.Should().Be("red");
            foreach (var child in result)
            {
                child.IntParent.Should().Be(ExpectedIntParent(c, parents));
                child.GuidParent.Should().Be(ExpectedGuidParent(c, parents));
            }
        }

        [Fact]
        public void EveryDtoShapeIsCoveredByAtLeastOneNode()
        {
            cases.Select(c => c.DtoShape).Distinct().Should()
                .BeEquivalentTo("Model", "ModelGuid", "VisualisationRegistry", "RoleRegistry", "CasesWorkflow");
        }

        [Fact]
        public async Task ModelNodesFillTheModelIdAndListNodesFillTheModelGuidAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var parents = await CreateParentsAsync(dbContext, fx.Seed.UserWithPermission);
            await CaseFor("Tag").Seed(this, dbContext, "Tag", parents, NewName("T"), 1, 0);
            await CaseFor("List").Seed(this, dbContext, "List", parents, NewName("L"), 1, 0);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var tag = (await service.GetTagAsync(parents.ModelId)).Single();
            var list = (await service.GetListAsync(parents.ModelGuid)).Single();

            tag.EntityAnalysisModelId.Should().Be(parents.ModelId);
            tag.EntityAnalysisModelGuid.Should().BeNull();
            list.EntityAnalysisModelGuid.Should().Be(parents.ModelGuid);
            list.EntityAnalysisModelId.Should().BeNull();
        }

        [Fact]
        public async Task UserRegistryChildrenAreTheUsersOfTheRoleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var parents = await CreateParentsAsync(dbContext, fx.Seed.UserWithPermission);
            var seeded = await CaseFor("UserRegistry")
                .Seed(this, dbContext, "UserRegistry", parents, NewName("U"), 1, 0);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetUserRegistryAsync(parents.RoleGuid);

            var child = result.Should().ContainSingle().Subject;
            child.Key.Should().Be(seeded.Id);
            child.RoleRegistryGuid.Should().Be(parents.RoleGuid);
        }

        [Theory]
        [MemberData(nameof(Routes))]
        public async Task UnknownOrMissingParentReturnsAnEmptyListAsync(string route)
        {
            var c = CaseFor(route);
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await c.Invoke(service, Parents.None, default);

            result.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(Routes))]
        public async Task EveryNodeIsTenantIsolatedInBothDirectionsAsync(string route)
        {
            var c = CaseFor(route);
            await using var dbContext = fx.GetDbContext();
            var parentsA = await CreateParentsAsync(dbContext, fx.Seed.UserWithPermission);
            var parentsB = await CreateParentsAsync(dbContext, fx.Seed.UserTenantB);
            var rowA = await c.Seed(this, dbContext, route, parentsA, NewName("TenA"), 1, 0);
            var rowB = await c.Seed(this, dbContext, route, parentsB, NewName("TenB"), 1, 0);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await c.Invoke(serviceA, parentsA, default)).Select(r => r.Key).Should()
                .BeEquivalentTo([rowA.Id, .. PreExistingKeys(c, parentsA)]);
            (await c.Invoke(serviceB, parentsB, default)).Select(r => r.Key).Should()
                .BeEquivalentTo([rowB.Id, .. PreExistingKeys(c, parentsB)]);
            (await c.Invoke(serviceA, parentsB, default)).Should().BeEmpty("tenant A must not see tenant B's parent");
            (await c.Invoke(serviceB, parentsA, default)).Should().BeEmpty("tenant B must not see tenant A's parent");
        }

        [Fact]
        public async Task IdOrderedNodesReturnAscendingIdsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var parents = await CreateParentsAsync(dbContext, fx.Seed.UserWithPermission);
            var seeded = new List<int>();
            for (var i = 0; i < 3; i++)
            {
                seeded.Add((await CaseFor("RequestXPath").Seed(this, dbContext, "RequestXPath", parents,
                    NewName("Ord"), 1, 0)).Id);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetRequestXPathAsync(parents.ModelId)).Select(r => r.Key).Should()
                .Equal(seeded.OrderBy(i => i));
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetTagAsync(1, cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetTagAsync(1));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLinePerCallAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetTagAsync(1);

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Tag");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, serviceChangeBus: bus);

            await service.GetTagAsync(1);
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetTagAsync(1));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task PermissionDeniedMessageResolvesForFrenchCultureAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");
                await using var dbContext = fx.GetDbContext();
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

                var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetTagAsync(1));

                localizers.Create(typeof(TreeChildrenResources))[TreeChildrenResources.PermissionDenied].Value
                    .Should().Be(ex.Message);
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }
    }
}