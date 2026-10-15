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
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Repository;
using Jube.Service.Agent.PageAgentToolBinder;
using Jube.Service.Agent.PageAgentToolBinder.Models;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Agent
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PageAgentToolBinderBindTests(DatabaseFixture fx)
    {
        private static IServiceProvider BuildServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILog>(TestLog.NoOp);

            services.AddSingleton<IStringLocalizerFactory>(
                new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                    NullLoggerFactory.Instance));

            services.AddSingleton<IServiceChangeBus>(new NullServiceChangeBus());

            return services.BuildServiceProvider();
        }

        [Fact]
        public void ABoundToolTakesItsNameAndPermissionsFromTheCatalogue()
        {
            using var dbContext = fx.GetDbContext();

            var context = new ServiceAgentBindingContext(dbContext, fx.Seed.UserWithPermission, BuildServices(),
                CancellationToken.None, false,
                Environment.GetEnvironmentVariable("JubeTestConnectionString"));

            var tools = PageAgentToolBinder.Bind(context, ["EntityAnalysisModelTagList"], TestLog.NoOp);

            var tool = tools.Should().ContainSingle().Subject;
            tool.Function.Name.Should().Be("EntityAnalysisModelTagList");

            tool.RequiredPermissions.Should().BeEquivalentTo(
                ServiceToolCatalogue.All.Single(t => t.Name == "EntityAnalysisModelTagList").RequiredPermissions);

            tool.RequiresSnapshot.Should().BeFalse();
        }

        [Fact]
        public void ABoundWriteToolRequiresASnapshotAndIsSkippedEntirelyWhenReadOnly()
        {
            using var dbContext = fx.GetDbContext();
            var services = BuildServices();

            var writable = PageAgentToolBinder.Bind(
                new ServiceAgentBindingContext(dbContext, fx.Seed.UserWithPermission, services,
                    CancellationToken.None, false,
                    Environment.GetEnvironmentVariable("JubeTestConnectionString")), ["EntityAnalysisModelTagCreate"],
                TestLog.NoOp);

            writable.Should().ContainSingle().Which.RequiresSnapshot.Should().BeTrue();

            var readOnly = PageAgentToolBinder.Bind(
                new ServiceAgentBindingContext(dbContext, fx.Seed.UserWithPermission, services,
                    CancellationToken.None, true), ["EntityAnalysisModelTagCreate"], TestLog.NoOp);

            readOnly.Should().BeEmpty();
        }

        [Fact]
        public void TheGeneratedSchemaCarriesTheParameterDescriptionsTheServiceDeclares()
        {
            using var dbContext = fx.GetDbContext();

            var context = new ServiceAgentBindingContext(dbContext, fx.Seed.UserWithPermission, BuildServices(),
                CancellationToken.None, false,
                Environment.GetEnvironmentVariable("JubeTestConnectionString"));

            var tool = PageAgentToolBinder.Bind(context, ["EntityAnalysisModelTagList"], TestLog.NoOp).Single();
            var schema = tool.Function.JsonSchema.GetRawText();

            schema.Should().Contain("take");
            schema.Should().Contain("clamped to 200");

            schema.Should().NotContain("token",
                "the cancellation token is infrastructure and must never reach the model as a parameter");
        }

        [Fact]
        public async Task InvokingABoundReadToolConstructsTheServiceAndReturnsItsResultAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var context = new ServiceAgentBindingContext(dbContext, fx.Seed.UserWithPermission, BuildServices(),
                CancellationToken.None, false,
                Environment.GetEnvironmentVariable("JubeTestConnectionString"));

            var tool = PageAgentToolBinder.Bind(context, ["EntityAnalysisModelTagList"], TestLog.NoOp).Single();

            var result = await tool.Function.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

            (result is null).Should().BeFalse();
        }

        [Fact]
        public async Task InvokingABoundWriteToolReturnsItsResultWithoutWalkingTheAssociationGraphAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelRepository = new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission);

            var model = await modelRepository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Bind{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(), Active = 1, Locked = 0, Deleted = 0
            });

            try
            {
                var context = new ServiceAgentBindingContext(dbContext, fx.Seed.UserWithPermission, BuildServices(),
                    CancellationToken.None, false,
                    Environment.GetEnvironmentVariable("JubeTestConnectionString"));

                var tool = PageAgentToolBinder.Bind(context, ["EntityAnalysisModelTagCreate"], TestLog.NoOp).Single();

                var json = "{\"entityAnalysisModelId\":" + model.Id + ",\"name\":\""
                           + DatabaseFixture.Prefix + "BoundTag\"}";

                var arguments = new AIFunctionArguments
                {
                    ["model"] = JsonSerializer.Deserialize<JsonElement>(json)
                };

                var result = await tool.Function.InvokeAsync(arguments, CancellationToken.None);

                (result is null).Should().BeFalse();

                result!.ToString().Should().NotContain("\"entityAnalysisModel\":{",
                    "a navigation property would drag the whole object graph into the model's context");
            }
            finally
            {
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelTag>()
                    .Where(w => w.EntityAnalysisModelId == model.Id).DeleteAsync();

                await dbContext.EntityAnalysisModel.Where(w => w.Id == model.Id).DeleteAsync();
            }
        }

        [Fact]
        public async Task InvokingABoundToolAsAUserWithoutThePermissionIsRefusedByTheServiceAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var context = new ServiceAgentBindingContext(dbContext, fx.Seed.UserWithoutPermission, BuildServices(),
                CancellationToken.None, false,
                Environment.GetEnvironmentVariable("JubeTestConnectionString"));

            var tool = PageAgentToolBinder.Bind(context, ["EntityAnalysisModelTagList"], TestLog.NoOp).Single();

            var act = async () => await tool.Function.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

            await act.Should().ThrowAsync<Exception>();
        }
    }
}