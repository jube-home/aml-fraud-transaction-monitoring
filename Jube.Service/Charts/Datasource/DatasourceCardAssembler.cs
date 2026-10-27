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

using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Data.Reporting;
using Jube.Data.Reporting.Models;
using Jube.Dto.Options;
using Jube.Dto.Repository.VisualisationRegistryDatasource;
using Jube.Service.Charts.Datasource.Models;

namespace Jube.Service.Charts.Datasource
{
    public static class DatasourceCardAssembler
    {
        public const int MaximumRows = 5000;
        public const double DefaultWidth = 640;
        public const double DefaultHeight = 400;
        private const double MinimumSide = 160;
        private const double MaximumSide = 2000;

        public static DatasourceCardDto Build(DatasourceCardIdentity identity, PostgresResult? result,
            int milliseconds, string? error, DatasourceDisplayKind? kind,
            IReadOnlyDictionary<DatasourceChartRole, IReadOnlyList<string>>? mapping, bool includeGrid,
            DatasourceCardRequestDto request, DatasourceChartText text, string? templateText = null)
        {
            var card = new DatasourceCardDto
            {
                Id = identity.Id,
                Guid = identity.Guid,
                Name = identity.Name,
                ColumnSpan = identity.ColumnSpan,
                RowSpan = identity.RowSpan,
                Priority = identity.Priority,
                ExecutionMilliseconds = milliseconds,
                Style = new DatasourceCardStyleDto { ModeId = (int)DatasourceStyleMode.External }
            };

            if (result == null)
            {
                card.StateId = (int)DatasourceCardState.Error;
                card.Message = error ?? text.Message(DatasourceMessages.CardFailed);

                return card;
            }

            var frame = new DatasourceFrame(
                result.Columns.Select((c, i) => new DatasourceColumn(c.Name, i + 1, c.DataTypeName,
                    PostgresDataTypeClassifier.Classify(c.DataTypeName))).ToList(),
                result.Rows);

            card.RowCount = result.Rows.Count;
            card.Truncated = result.Truncated;

            if (includeGrid)
            {
                card.Grid = new DatasourceCardGridDto
                {
                    Columns = frame.Columns.Select(c => new DatasourceCardGridColumnDto
                    {
                        Name = c.Name,
                        Ordinal = c.Ordinal,
                        DataTypeId = (int)c.Type
                    }).ToList(),
                    Rows = result.Rows.Select(row => row.Select(DatasourceCardValue.Normalise).ToArray()).ToList()
                };
            }

            if (result.Rows.Count == 0)
            {
                card.StateId = (int)DatasourceCardState.Empty;
                card.Message = text.Message(DatasourceMessages.CardEmpty);

                return card;
            }

            card.StateId = (int)DatasourceCardState.Ready;

            if (kind == null)
            {
                return card;
            }

            var presentation = PresentationOf(request) with
            {
                TemplateText = templateText,
                Truncated = result.Truncated
            };

            try
            {
                var markup = DatasourceChartRenderer.Render(kind.Value, frame, mapping, presentation);
                var markupKind = DatasourceChartKinds.Get(kind.Value).Markup;

                card.Display = new DatasourceCardDisplayDto
                {
                    KindId = (int)kind.Value,
                    MarkupKindId = (int)markupKind,
                    Markup = markup
                };

                if (markupKind == DatasourceMarkupKind.Svg)
                {
                    card.Style = StyleOf(presentation, request);
                }
            }
            catch (DatasourceChartException e)
            {
                card.StateId = (int)DatasourceCardState.Error;
                card.Message = text.Describe(e);
            }

            return card;
        }

        private static DatasourceChartPresentation PresentationOf(DatasourceCardRequestDto request)
        {
            return new DatasourceChartPresentation(
                request.StyleModeId == (int)DatasourceStyleMode.Inline
                    ? ChartStyleMode.Inline
                    : ChartStyleMode.External,
                request.ThemeId switch
                {
                    (int)DatasourceTheme.Light => ThemeMode.Light,
                    (int)DatasourceTheme.Dark => ThemeMode.Dark,
                    _ => ThemeMode.Auto
                },
                Side(request.Width, DefaultWidth),
                Side(request.Height, DefaultHeight));
        }

        private static double Side(double requested, double fallback)
        {
            return double.IsFinite(requested) && requested > 0
                ? Math.Clamp(requested, MinimumSide, MaximumSide)
                : fallback;
        }

        private static DatasourceCardStyleDto StyleOf(DatasourceChartPresentation presentation,
            DatasourceCardRequestDto request)
        {
            if (presentation.StyleMode == ChartStyleMode.Inline)
            {
                return new DatasourceCardStyleDto { ModeId = (int)DatasourceStyleMode.Inline };
            }

            var version = ChartStylesheet.Version;

            return new DatasourceCardStyleDto
            {
                ModeId = (int)DatasourceStyleMode.External,
                Version = version,
                Css = string.Equals(request.KnownStylesheetVersion, version, StringComparison.Ordinal)
                    ? null
                    : ChartStylesheet.Css(true)
            };
        }
    }
}
