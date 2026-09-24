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
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dto.Validation;
using Jube.Service.Agent;
using Jube.Service.Agent.ServiceToolCatalogue;
using Xunit;

namespace Jube.Test.Service.Agent
{
    [Trait("Category", "Unit")]
    public sealed class ValidateOperationCoverageTests
    {
        private static readonly HashSet<string> excludedServices =
        [
            "AuthenticationLoginService", "EntityAnalysisModelSampleService", "ArchiveService",
            "UserInTenantService", "ExhaustiveSearchInstancePromotedTrialInstanceService",
            "SessionCaseSearchCompiledSqlService", "CaseService", "UserRegistryApiKeyService",
            "CaseWorkflowActionRoleService", "CaseWorkflowDisplayRoleService", "CaseWorkflowFilterRoleService",
            "CaseWorkflowFormRoleService", "CaseWorkflowMacroRoleService", "CaseWorkflowRoleService",
            "CaseWorkflowStatusRoleService", "CaseWorkflowXPathRoleService", "EntityAnalysisModelRoleService",
            "VisualisationRegistryDatasourceRoleService", "VisualisationRegistryParameterRoleService",
            "VisualisationRegistryRoleService"
        ];

        private static IEnumerable<Type> ServicesWithValidators()
        {
            return typeof(ServiceOperationAttribute).Assembly.GetTypes()
                .Where(t => t.IsClass && t.Name.EndsWith("Service"))
                .Where(t => t.GetField("validator", BindingFlags.Instance | BindingFlags.NonPublic) != null)
                .Where(t => t.GetMethod("InsertAsync") != null)
                .Where(t => !excludedServices.Contains(t.Name));
        }

        private static MethodInfo ValidateMethod(Type service)
        {
            return service.GetMethods().Single(m => m.Name == "ValidateAsync"
                                                    && m.ReturnType == typeof(Task<ValidationResultDto>));
        }

        [Fact]
        public void EveryAuthoredEntityServiceExposesValidate()
        {
            var services = ServicesWithValidators().ToList();

            services.Should().HaveCount(41);
            services.Should().OnlyContain(s => s.GetMethods().Any(m =>
                m.Name == "ValidateAsync" && m.ReturnType == typeof(Task<ValidationResultDto>)));
        }

        [Fact]
        public void ValidateIsARepeatableReadNamedAfterTheCreateOperationsArea()
        {
            foreach (var service in ServicesWithValidators())
            {
                var validate = ValidateMethod(service).GetCustomAttribute<ServiceOperationAttribute>();
                var create = service.GetMethod("InsertAsync")!.GetCustomAttribute<ServiceOperationAttribute>();

                validate.Should().NotBeNull(service.Name);
                validate!.Kind.Should().Be(OperationKind.Read, service.Name);
                validate.Idempotent.Should().BeTrue(service.Name);
                validate.Destructive.Should().BeFalse(service.Name);
                validate.Name.Should().EndWith("Validate", service.Name);
                create.Should().NotBeNull(service.Name);
            }
        }

        [Fact]
        public void ValidateTakesTheSameDtoAsCreate()
        {
            foreach (var service in ServicesWithValidators())
            {
                var createDto = service.GetMethod("InsertAsync")!.GetParameters()[0].ParameterType;
                var validateDto = ValidateMethod(service).GetParameters()[0].ParameterType;

                validateDto.Should().Be(createDto, service.Name);
            }
        }

        [Fact]
        public void EveryValidateOperationIsCataloguedOnceAsARead()
        {
            var catalogue = ServiceToolCatalogue.All.ToList();

            foreach (var service in ServicesWithValidators())
            {
                var name = ValidateMethod(service).GetCustomAttribute<ServiceOperationAttribute>()!.Name;

                catalogue.Where(t => t.Name == name).Should().ContainSingle(name)
                    .Which.Kind.Should().Be(OperationKind.Read);
            }
        }

        [Theory]
        [InlineData("EntityAnalysisModelGatewayRule")]
        [InlineData("EntityAnalysisModelAbstractionRule")]
        [InlineData("EntityAnalysisModelAbstractionCalculation")]
        [InlineData("EntityAnalysisModelActivationRule")]
        [InlineData("EntityAnalysisModelInlineFunction")]
        [InlineData("EntityAnalysisModelReprocessingRule")]
        public void EveryVbNetRuleServiceExposesACataloguedParseRule(string area)
        {
            var service = typeof(ServiceOperationAttribute).Assembly.GetTypes().Single(t => t.Name == area + "Service");
            var parse = service.GetMethod("ParseRuleAsync");

            parse.Should().NotBeNull();
            parse!.ReturnType.Should().Be(typeof(Task<ValidationResultDto>));
            var attribute = parse.GetCustomAttribute<ServiceOperationAttribute>()!;
            attribute.Name.Should().Be(area + "ParseRule");
            attribute.Kind.Should().Be(OperationKind.Read);
            ServiceToolCatalogue.All.Where(t => t.Name == attribute.Name).Should().ContainSingle();
        }

        [Fact]
        public void OnlyTheVbNetRuleServicesExposeParseRule()
        {
            typeof(ServiceOperationAttribute).Assembly.GetTypes()
                .Where(t => t.GetMethod("ParseRuleAsync") != null)
                .Should().HaveCount(6);
        }
    }
}