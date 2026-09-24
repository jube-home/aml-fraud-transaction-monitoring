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

using FluentValidation;
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Parser.Dependency;
using Jube.Resources;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Dependency
{
    public sealed record ModelEntityDelete(
        ModelEntityKind Kind,
        int Id,
        int? EntityAnalysisModelId,
        Guid? EntityAnalysisModelGuid = null);

    public sealed class ModelEntityDeleteValidator : AbstractValidator<ModelEntityDelete>
    {
        private const string ErrorCode = "HasDependents";

        private static readonly ILog log = LogManager.GetLogger(typeof(ModelEntityDeleteValidator));

        public ModelEntityDeleteValidator(DbContext dbContext, int tenantRegistryId, string userName,
            IStringLocalizer localiser)
        {
            RuleFor(p => p).CustomAsync(async (request, context, token) =>
            {
                var modelId = request.EntityAnalysisModelId;
                if (modelId == null && request.EntityAnalysisModelGuid is { } guid)
                {
                    modelId = (await new EntityAnalysisModelRepository(dbContext, tenantRegistryId)
                        .GetByGuidAsync(guid, token).ConfigureAwait(false))?.Id;
                }

                if (modelId is not > 0)
                {
                    return;
                }

                var graph = await ModelDependencyGraphBuilder
                    .BuildAsync(dbContext, tenantRegistryId, userName, modelId.Value, log, token)
                    .ConfigureAwait(false);

                foreach (var dependency in graph.DependentsOf(request.Kind, request.Id)
                             .Where(d => d.Dependent.Kind != request.Kind || d.Dependent.Id != request.Id)
                             .OrderBy(d => d.Dependent.Kind).ThenBy(d => d.Dependent.Name))
                {
                    var through = dependency.Kind == ModelDependencyKind.RuleText
                        ? $"{dependency.Namespace}.{dependency.Name}"
                        : dependency.Kind.ToString();

                    context.AddFailure(new ValidationFailure(nameof(ModelEntityDelete.Id),
                        string.Format(localiser[ModelDependencyResources.HasDependents], dependency.Dependent.Kind,
                            dependency.Dependent.Name, through))
                    {
                        ErrorCode = ErrorCode,
                        CustomState = dependency
                    });
                }
            });
        }
    }
}