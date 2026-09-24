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
using Jube.Data.QueryBuilder;
using Jube.Dto.Filter;
using Jube.Service.Agent;
using Jube.Service.Agent.ServiceToolCatalogue;
using Xunit;

namespace Jube.Test.Service.Agent
{
    [Trait("Category", "Unit")]
    public sealed class FilterOperationCoverageTests
    {
        private static readonly HashSet<string> excludedServices =
            ["UserInTenantService", "PermissionSpecificationService", "CaseService"];

        private static IEnumerable<Type> ListingServices()
        {
            return typeof(ServiceOperationAttribute).Assembly.GetTypes()
                .Where(t => t.IsClass && t.Name.EndsWith("Service"))
                .Where(t => t.GetMethods().Any(m => m.Name == "ListAsync" && m.ReturnType.IsGenericType &&
                                                    m.ReturnType.GetGenericArguments()[0].IsGenericType &&
                                                    m.ReturnType.GetGenericArguments()[0].GetGenericTypeDefinition() ==
                                                    typeof(PagedResult<>)))
                .Where(t => !excludedServices.Contains(t.Name));
        }

        private static Type RowType(Type service)
        {
            return service.GetMethod("ListAsync")!.ReturnType.GetGenericArguments()[0].GetGenericArguments()[0];
        }

        [Fact]
        public void EveryListingServiceExposesFilterFieldsFilterAndCountOverItsListDto()
        {
            var services = ListingServices().ToList();

            services.Should().HaveCount(39);
            foreach (var service in services)
            {
                var row = RowType(service);
                service.GetMethod("FilterFieldsAsync")!.ReturnType.Should()
                    .Be(typeof(Task<List<FilterFieldDto>>), service.Name);
                service.GetMethod("FilterAsync")!.ReturnType.Should()
                    .Be(typeof(Task<>).MakeGenericType(typeof(FilterResultDto<>).MakeGenericType(row)), service.Name);
                service.GetMethod("CountAsync")!.ReturnType.Should()
                    .Be(typeof(Task<FilterCountResultDto>), service.Name);
            }
        }

        [Fact]
        public void EveryFilterOperationIsARepeatableReadNamedAfterTheListAreaAndCatalogued()
        {
            var catalogue = ServiceToolCatalogue.All.ToList();

            foreach (var service in ListingServices())
            {
                var list = service.GetMethod("ListAsync")!.GetCustomAttribute<ServiceOperationAttribute>()!.Name;
                var area = list[..^"List".Length];

                foreach (var (method, suffix) in new[]
                         {
                             ("FilterFieldsAsync", "FilterFields"), ("FilterAsync", "Filter"), ("CountAsync", "Count")
                         })
                {
                    var attribute = service.GetMethod(method)!.GetCustomAttribute<ServiceOperationAttribute>();
                    attribute.Should().NotBeNull(service.Name);
                    attribute!.Name.Should().Be(area + suffix, service.Name);
                    attribute.Kind.Should().Be(OperationKind.Read, service.Name);
                    attribute.Idempotent.Should().BeTrue(service.Name);
                    catalogue.Where(t => t.Name == attribute.Name).Should().ContainSingle(attribute.Name)
                        .Which.Kind.Should().Be(OperationKind.Read);
                }
            }
        }

        [Fact]
        public void EveryListDtoOffersAtLeastTheIdToFilterOn()
        {
            foreach (var service in ListingServices())
            {
                BuilderFilter.Fields(RowType(service)).Should()
                    .Contain(f => f.Id == "Id", service.Name);
            }
        }
    }
}