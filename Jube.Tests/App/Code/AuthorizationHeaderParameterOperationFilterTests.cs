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
using FluentAssertions;
using Jube.App.Code;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace Jube.Test.App.Code
{
    [Trait("Category", "Unit")]
    public sealed class AuthorizationHeaderParameterOperationFilterTests
    {
        private static class ControllerActionExamples
        {
            [AllowAnonymous]
            public static void Anonymous()
            {
            }

            public static void Authenticated()
            {
            }
        }

        private static OperationFilterContext BuildContext(string relativePath,
            System.Reflection.MethodInfo? methodInfo)
        {
            var apiDescription = new ApiDescription
            {
                RelativePath = relativePath
            };

            return new OperationFilterContext(apiDescription, null!, new SchemaRepository(), methodInfo!);
        }

        [Fact]
        public void ApplyDoesNotThrowAndAddsSecurityRequirementsForAMinimalApiEndpointWithNoMethodInfo()
        {
            var operation = new OpenApiOperation();
            var context = BuildContext("api/ApplicationLogEntry", null);

            var act = () => new AuthorizationHeaderParameterOperationFilter().Apply(operation, context);

            act.Should().NotThrow();
            operation.Security.Should().HaveCount(1);
            operation.Security[0].Keys.Select(key => key.Reference.Id)
                .Should().BeEquivalentTo("Bearer", "ApiKey");
        }

        [Fact]
        public void ApplyAddsSecurityRequirementsForAControllerActionWithoutAllowAnonymous()
        {
            var operation = new OpenApiOperation();
            var methodInfo = typeof(ControllerActionExamples).GetMethod(nameof(ControllerActionExamples.Authenticated));
            var context = BuildContext("api/SomeController", methodInfo);

            new AuthorizationHeaderParameterOperationFilter().Apply(operation, context);

            operation.Security.Should().HaveCount(1);
        }

        [Fact]
        public void ApplyDoesNotAddSecurityRequirementsForAControllerActionDecoratedWithAllowAnonymous()
        {
            var operation = new OpenApiOperation();
            var methodInfo = typeof(ControllerActionExamples).GetMethod(nameof(ControllerActionExamples.Anonymous));
            var context = BuildContext("api/Authentication/SomeAnonymousAction", methodInfo);

            new AuthorizationHeaderParameterOperationFilter().Apply(operation, context);

            operation.Security.Should().BeEmpty();
        }

        [Fact]
        public void ApplyDoesNotAddSecurityRequirementsForTheUnauthenticatedLoginPathEvenWithNoMethodInfo()
        {
            var operation = new OpenApiOperation();
            var context = BuildContext("api/Authentication/ByUserNamePassword", null);

            var act = () => new AuthorizationHeaderParameterOperationFilter().Apply(operation, context);

            act.Should().NotThrow();
            operation.Security.Should().BeEmpty();
        }
    }
}