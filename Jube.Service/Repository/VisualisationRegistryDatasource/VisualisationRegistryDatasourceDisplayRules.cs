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

using Jube.Data.Reporting;
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.VisualisationRegistryDatasource;
using Jube.Resources;
using Jube.Service.Charts.Datasource;
using Jube.Service.Charts.Datasource.Template;
using Jube.Service.Charts.Datasource.Models;
using Jube.Service.Exceptions.Repository.VisualisationRegistryDatasource;
using Jube.Validations.Repository.VisualisationRegistryDatasource;
using log4net;
using Microsoft.Extensions.Localization;
using IntrospectColumn =
    Jube.Data.Query.IntrospectVisualisationRegistryDatasourceQuery.IntrospectVisualisationRegistryDatasourceQueryDto;
using IntrospectQuery =
    Jube.Data.Query.IntrospectVisualisationRegistryDatasourceQuery.IntrospectVisualisationRegistryDatasourceQuery;

namespace Jube.Service.Repository.VisualisationRegistryDatasource
{
    internal sealed class VisualisationRegistryDatasourceDisplayRules(
        DbContext dbContext,
        int tenantRegistryId,
        string userName,
        DatasourceChartText chartText,
        IStringLocalizer strings,
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log,
        Func<IReadOnlyDictionary<string, string>?> allowedTables)
    {
        internal void DescribeChartShape(VisualisationRegistryDatasourceIntrospectionDto result,
            List<IntrospectColumn> columns, VisualisationRegistryDatasourceIntrospectionRequestDto request)
        {
            var frame = new DatasourceFrame(
                columns.Select(c => new DatasourceColumn(c.Name, c.Ordinal, c.DataTypeName, c.DataType)).ToList(),
                []);

            foreach (var kind in DatasourceChartKinds.All)
            {
                try
                {
                    DatasourceChartMappingResolver.Resolve(kind, frame, null);

                    result.Kinds.Add(new DatasourceKindFitDto { KindId = (int)kind.Kind, FitsByPosition = true });
                }
                catch (DatasourceChartException ex)
                {
                    result.Kinds.Add(new DatasourceKindFitDto
                    {
                        KindId = (int)kind.Kind,
                        FitsByPosition = false,
                        Reason = chartText.Describe(ex)
                    });
                }
            }

            if (request.DisplayKindId == 0)
            {
                return;
            }

            if (!DatasourceChartKinds.TryGet(request.DisplayKindId, out var selected))
            {
                result.MappingError = chartText.Message(DatasourceMessages.KindUnknown, request.DisplayKindId);

                return;
            }

            if (selected.Kind == DatasourceDisplayKind.Template)
            {
                try
                {
                    DatasourceTemplate.Validate(request.TemplateText);
                }
                catch (DatasourceChartException ex)
                {
                    result.TemplateError = chartText.Describe(ex);
                }
            }

            var overrides = new Dictionary<DatasourceChartRole, IReadOnlyList<string>>();

            foreach (var entry in DatasourceInput.Maybe(request.Mapping) ?? [])
            {
                if (DatasourceInput.Maybe(entry) is null)
                {
                    result.MappingError = chartText.Message(DatasourceMessages.MappingEntryEmpty);

                    return;
                }

                if (!Enum.IsDefined(typeof(DatasourceChartRole), entry.RoleId))
                {
                    result.MappingError = chartText.Message(DatasourceMessages.RoleUndefined, entry.RoleId);

                    return;
                }

                if (!overrides.TryAdd((DatasourceChartRole)entry.RoleId, DatasourceInput.Maybe(entry.Columns) ?? []))
                {
                    result.MappingError = chartText.Message(DatasourceMessages.RoleAssignedTwice,
                        DatasourceChartText.RoleTerm((DatasourceChartRole)entry.RoleId));

                    return;
                }
            }

            try
            {
                var mapping = DatasourceChartMappingResolver.Resolve(selected, frame, overrides);

                result.ResolvedMapping =
                [
                    .. selected.Roles.Where(r => mapping.Has(r.Role)).Select(r => new DatasourceResolvedRoleDto
                    {
                        RoleId = (int)r.Role,
                        Columns = [.. mapping.Many(r.Role)],
                        Explicit = overrides.TryGetValue(r.Role, out var named) && named.Count > 0
                    })
                ];
            }
            catch (DatasourceChartException ex)
            {
                result.MappingError = chartText.Describe(ex);
            }
        }

        internal void ValidateIntrospectionRequest(VisualisationRegistryDatasourceIntrospectionRequestDto request)
        {
            var failures = new List<ValidationFailure>();

            if (request.VisualisationRegistryId <= 0)
            {
                failures.Add(new ValidationFailure(nameof(request.VisualisationRegistryId),
                    strings[VisualisationRegistryDatasourceResources.VisualisationRegistryIdInvalid])
                {
                    ErrorCode = "VisualisationRegistryIdInvalid"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Command))
            {
                failures.Add(new ValidationFailure(nameof(request.Command),
                    strings[VisualisationRegistryDatasourceResources.CommandRequired])
                {
                    ErrorCode = "CommandNotEmpty"
                });
            }
            else if (request.Command.Length > VisualisationRegistryDatasourceDtoValidator.MaxCommandLength)
            {
                failures.Add(new ValidationFailure(nameof(request.Command),
                    string.Format(strings[VisualisationRegistryDatasourceResources.CommandMaxLength],
                        VisualisationRegistryDatasourceDtoValidator.MaxCommandLength))
                {
                    ErrorCode = "CommandMaximumLength"
                });
            }

            if (failures.Count > 0)
            {
                throw new DtoValidationException(new ValidationResult(failures));
            }
        }

        internal (DatasourceDisplayKind? Kind, Dictionary<DatasourceChartRole, IReadOnlyList<string>> Mapping)
            ValidatePreviewRequest(DatasourcePreviewRequestDto request)
        {
            var failures = new List<ValidationFailure>();

            if (request.VisualisationRegistryId <= 0)
            {
                failures.Add(new ValidationFailure(nameof(request.VisualisationRegistryId),
                    strings[VisualisationRegistryDatasourceResources.VisualisationRegistryIdInvalid])
                {
                    ErrorCode = "VisualisationRegistryIdInvalid"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Command))
            {
                failures.Add(new ValidationFailure(nameof(request.Command),
                    strings[VisualisationRegistryDatasourceResources.CommandRequired])
                {
                    ErrorCode = "CommandNotEmpty"
                });
            }
            else if (request.Command.Length > VisualisationRegistryDatasourceDtoValidator.MaxCommandLength)
            {
                failures.Add(new ValidationFailure(nameof(request.Command),
                    string.Format(strings[VisualisationRegistryDatasourceResources.CommandMaxLength],
                        VisualisationRegistryDatasourceDtoValidator.MaxCommandLength))
                {
                    ErrorCode = "CommandMaximumLength"
                });
            }

            DatasourceDisplayKind? kind = null;

            if (request.DisplayKindId != 0)
            {
                if (DatasourceChartKinds.TryGet(request.DisplayKindId, out var found))
                {
                    kind = found.Kind;
                }
                else
                {
                    failures.Add(new ValidationFailure(nameof(request.DisplayKindId),
                        chartText.Message(DatasourceMessages.KindUnknown, request.DisplayKindId))
                    {
                        ErrorCode = "DisplayKindIdInvalid"
                    });
                }
            }

            var mapping = new Dictionary<DatasourceChartRole, IReadOnlyList<string>>();

            foreach (var entry in DatasourceInput.Maybe(request.Mapping) ?? [])
            {
                if (DatasourceInput.Maybe(entry) is null)
                {
                    failures.Add(new ValidationFailure(nameof(request.Mapping), chartText.Message(DatasourceMessages.MappingEntryEmpty))
                    {
                        ErrorCode = "MappingEntryEmpty"
                    });
                }
                else if (!Enum.IsDefined(typeof(DatasourceChartRole), entry.RoleId))
                {
                    failures.Add(new ValidationFailure(nameof(request.Mapping),
                        chartText.Message(DatasourceMessages.RoleUndefined, entry.RoleId)) { ErrorCode = "RoleIdInvalid" });
                }
                else if (!mapping.TryAdd((DatasourceChartRole)entry.RoleId, DatasourceInput.Maybe(entry.Columns) ?? []))
                {
                    failures.Add(new ValidationFailure(nameof(request.Mapping),
                        chartText.Message(DatasourceMessages.RoleAssignedTwice,
                            DatasourceChartText.RoleTerm((DatasourceChartRole)entry.RoleId)))
                    {
                        ErrorCode = "RoleIdDuplicate"
                    });
                }
            }

            request.Card = DatasourceInput.Maybe(request.Card) ?? new DatasourceCardRequestDto();
            request.Card.Parameters = DatasourceInput.Maybe(request.Card.Parameters) ?? [];

            if (request.Card.Parameters.Any(p => DatasourceInput.Maybe(p) is null))
            {
                failures.Add(new ValidationFailure("Card.Parameters", chartText.Message(DatasourceMessages.ParameterEmpty))
                {
                    ErrorCode = "ParameterEmpty"
                });
            }
            else if (request.Card.Parameters.GroupBy(p => p.Id).Any(g => g.Count() > 1))
            {
                failures.Add(new ValidationFailure("Card.Parameters", chartText.Message(DatasourceMessages.ParameterDuplicate))
                {
                    ErrorCode = "ParameterDuplicate"
                });
            }

            if (failures.Count > 0)
            {
                throw new DtoValidationException(new ValidationResult(failures));
            }

            return (kind, mapping);
        }

        internal async Task<IReadOnlyDictionary<int, IReadOnlyList<string>>?> ResolveRoleColumnsAsync(
            VisualisationRegistryDatasourceDto model, CancellationToken token)
        {
            var entries = (DatasourceInput.Maybe(model.Mapping) ?? []).Where(e => e.Columns is { Count: > 0 }).ToList();

            if (model.DisplayKindId == 0 && entries.Count == 0)
            {
                return null;
            }

            var introspected = await new IntrospectQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(model.VisualisationRegistryId, model.Command!,
                    dynamicEnvironment.AppSettings("ReportConnectionString") ?? dbContext.Connection.ConnectionString,
                    log, allowedTables(), token).ConfigureAwait(false);

            var known = introspected.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
            var failures = new List<ValidationFailure>();

            foreach (var name in entries.SelectMany(e => e.Columns).Where(c => !known.Contains(c)).Distinct())
            {
                failures.Add(new ValidationFailure(nameof(model.Mapping),
                    chartText.Message(DatasourceMessages.MappingColumnUnknown, name))
                {
                    ErrorCode = "MappingColumnUnknown"
                });
            }

            if (failures.Count == 0 && model.DisplayKindId > 0)
            {
                if (!DatasourceChartKinds.TryGet(model.DisplayKindId, out var kind))
                {
                    failures.Add(new ValidationFailure(nameof(model.DisplayKindId),
                        strings[VisualisationRegistryDatasourceResources.DisplayKindIdInvalid])
                    {
                        ErrorCode = "DisplayKindIdInvalid"
                    });
                }
                else
                {
                    if (kind.Kind == DatasourceDisplayKind.Template)
                    {
                        try
                        {
                            DatasourceTemplate.Validate(model.TemplateText);
                        }
                        catch (DatasourceChartException ex)
                        {
                            failures.Add(new ValidationFailure(nameof(model.TemplateText), chartText.Describe(ex))
                            {
                                ErrorCode = "TemplateInvalid"
                            });
                        }
                    }

                    var frame = new DatasourceFrame(
                        introspected.Select(c => new DatasourceColumn(c.Name, c.Ordinal, c.DataTypeName, c.DataType))
                            .ToList(), []);

                    var overrides = entries.ToDictionary(e => (DatasourceChartRole)e.RoleId,
                        e => (IReadOnlyList<string>)e.Columns);

                    try
                    {
                        DatasourceChartMappingResolver.Resolve(kind, frame, overrides);
                    }
                    catch (DatasourceChartException ex)
                    {
                        failures.Add(new ValidationFailure(nameof(model.Mapping), chartText.Describe(ex))
                        {
                            ErrorCode = "MappingUnsatisfied"
                        });
                    }
                }
            }

            if (failures.Count > 0)
            {
                throw new DtoValidationException(new ValidationResult(failures));
            }

            return entries.ToDictionary(e => e.RoleId, e => (IReadOnlyList<string>)e.Columns);
        }

        internal async Task AttachMappingAsync(VisualisationRegistryDatasourceDto dto, CancellationToken token)
        {
            var series = await new VisualisationRegistryDatasourceSeriesRepository(dbContext, userName)
                .GetByVisualisationRegistryDatasourceIdAsync(dto.Id, token).ConfigureAwait(false);

            dto.Mapping = series.Where(s => s.RoleId != null).GroupBy(s => s.RoleId!.Value).OrderBy(g => g.Key)
                .Select(g => new DatasourceMappingEntryDto
                {
                    RoleId = g.Key,
                    Columns = g.OrderBy(s => s.RoleOrder).Select(s => s.Name ?? string.Empty).ToList()
                }).ToList();
        }
    }
}
