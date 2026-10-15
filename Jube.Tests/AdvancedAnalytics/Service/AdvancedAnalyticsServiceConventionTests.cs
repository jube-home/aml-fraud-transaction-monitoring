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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.Models;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Repository.AdvancedAnalyticsInstance;
using Jube.Service.Repository.AdvancedAnalyticsModel;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Xunit;
using Jube.Service.Repository.AdvancedAnalyticsInstance.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    public class AdvancedAnalyticsServiceConventionTests
    {
        private static readonly Type[] serviceTypes =
        [
            typeof(AdvancedAnalyticsInstanceService), typeof(AdvancedAnalyticsModelService),
            typeof(AdvancedAnalyticsTrainingService)
        ];

        private static IEnumerable<MethodInfo> Operations()
        {
            return serviceTypes.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance
                    | BindingFlags.DeclaredOnly))
                .Where(m => m.GetCustomAttribute<ServiceOperationAttribute>() != null);
        }

        [Fact]
        public void EveryOperationIsCataloguedWithMatchingMetadataAndAUniqueName()
        {
            var catalogue = ServiceToolCatalogue.All.ToDictionary(t => t.Name);
            var operations = Operations().ToList();
            operations.Should().HaveCount(20);

            foreach (var method in operations)
            {
                var attribute = method.GetCustomAttribute<ServiceOperationAttribute>()!;
                catalogue.Should().ContainKey(attribute.Name);
                var descriptor = catalogue[attribute.Name];
                descriptor.Kind.Should().Be(attribute.Kind);
                descriptor.Idempotent.Should().Be(attribute.Idempotent);
                descriptor.Destructive.Should().Be(attribute.Destructive);
                attribute.Name.Should().StartWith("AdvancedAnalytics").And.NotContain("_");
            }

            operations.Select(m => m.GetCustomAttribute<ServiceOperationAttribute>()!.Name)
                .Should().OnlyHaveUniqueItems();

            ServiceToolCatalogue.All.Select(t => t.Name).Should().OnlyHaveUniqueItems();
            ServiceToolCatalogue.All.Count(t => t.Name.StartsWith("AdvancedAnalytics")).Should().Be(22);
        }

        [Fact]
        public void ServiceToolNamesNeverCollideWithTheTrainerToolNames()
        {
            var trainerTools = AdvancedAnalyticsToolCatalogue.Discover(typeof(LogisticRegressionTools).Assembly)
                .Select(d => d.ToolName);

            ServiceToolCatalogue.All.Select(t => t.Name).Intersect(trainerTools).Should().BeEmpty();
        }

        [Fact]
        public void OperationsAndTheirParametersAreDescribedInPlainWords()
        {
            foreach (var method in Operations())
            {
                var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
                description.Should().NotBeNullOrWhiteSpace($"{method.Name} needs a description");
                description!.Length.Should().BeGreaterThan(AdvancedAnalyticsToolCatalogue.MinimumDescriptionLength);
                description.Should().NotContainEquivalentOf("accord");

                foreach (var parameter in method.GetParameters()
                             .Where(p => p.ParameterType != typeof(CancellationToken)))
                {
                    parameter.GetCustomAttribute<DescriptionAttribute>()?.Description.Should()
                        .NotBeNullOrWhiteSpace($"{method.Name}.{parameter.Name} needs a description");
                }

                method.GetCustomAttributes<AdvancedAnalyticsRequiresPermissionAttribute>().Should().NotBeEmpty();
            }
        }

        [Fact]
        public void TrainingIsNotADuplicateToolAndCarriesItsOwnDescription()
        {
            var method = typeof(AdvancedAnalyticsTrainingService).GetMethod(
                nameof(AdvancedAnalyticsTrainingService.TrainLogisticRegressionAsync))!;

            method.GetCustomAttribute<ServiceOperationAttribute>().Should().BeNull();
            method.GetCustomAttribute<DescriptionAttribute>()!.Description.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void EveryDtoPropertyCarriesADescription()
        {
            var dtoTypes = typeof(AdvancedAnalyticsInstanceDto).Assembly.GetTypes()
                .Where(t => (t.Namespace == "Jube.Service.Repository.AdvancedAnalyticsModel.Models" && t.IsPublic)
                            || ((t.Namespace == "Jube.Service.Repository.AdvancedAnalyticsInstance.Models" ||
                                 t.Namespace == "Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models" ||
                                 t.Namespace == "Jube.Service.Query.AnalyticsTrainingRun.Models") && t.IsPublic &&
                                t.Name.EndsWith("Dto")))
                .ToList();
            dtoTypes.Should().HaveCountGreaterThan(8);

            foreach (var type in dtoTypes)
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance
                             | BindingFlags.DeclaredOnly))
                {
                    var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description;
                    description.Should().NotBeNullOrWhiteSpace($"{type.Name}.{property.Name} needs a description");
                    description.Should().NotContainEquivalentOf("accord");
                }
            }
        }
    }
}