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

using System.Globalization;
using Jube.Charting.Exceptions;
using Jube.Data.Reporting;
using Jube.Dto.Options;
using Jube.Service.Charts.Datasource.Models;

namespace Jube.Service.Charts.Datasource
{
    public static class DatasourceChartRenderer
    {
        public static string Render(DatasourceDisplayKind kind, DatasourceFrame frame,
            IReadOnlyDictionary<DatasourceChartRole, IReadOnlyList<string>>? overrides,
            DatasourceChartPresentation presentation)
        {
            ArgumentNullException.ThrowIfNull(frame);
            ArgumentNullException.ThrowIfNull(presentation);

            var chartKind = DatasourceChartKinds.Get(kind);

            if (frame.Rows.Count == 0)
            {
                throw new DatasourceChartException(DatasourceMessages.NoRows);
            }

            var mapping = DatasourceChartMappingResolver.Resolve(chartKind, frame, overrides);

            try
            {
                return chartKind.Render(new DatasourceChartInput(frame, mapping, presentation));
            }
            catch (ChartLimitException e)
            {
                throw new DatasourceChartException(
                    [new DatasourceChartProblem(DatasourceMessages.LimitExceeded,
                    [
                        DatasourceChartText.KindTerm(chartKind), e.Actual.ToString("N0", CultureInfo.InvariantCulture),
                        e.Maximum.ToString("N0", CultureInfo.InvariantCulture), e.Limit
                    ])]);
            }
            catch (ChartSpecificationException e)
            {
                throw new DatasourceChartException(e.Message, e);
            }
        }
    }
}
