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

// Renders a grid's double-column summary statistics (Min/Max/Mean/Median/StdDev/10-bucket histogram) below
// the main grid, one row per column. Mirrors the Exhaustive Adaptation results page's own pattern exactly
// (Jube.App/wwwroot/js/Model/Frame/Exhaustive.js, the "#statistics" kendoGrid with a templated ".histogram"
// column wired to a per-row kendoChart in dataBound) rather than a plain HTML table, so the histogram is a
// real bar chart, not a text dump of bucket counts.
function renderGridStatisticsTable(elementId, statistics) {
    const $container = $("#" + elementId);
    $container.empty();

    if (!statistics || !statistics.columns || Object.keys(statistics.columns).length === 0) {
        return;
    }

    const rows = Object.keys(statistics.columns).map(function (columnName) {
        const s = statistics.columns[columnName];
        return {
            column: columnName,
            min: s.min,
            max: s.max,
            mean: s.mean,
            median: s.median,
            standardDeviation: s.standardDeviation,
            histogramValues: s.histogram
        };
    });

    $container.kendoGrid({
        dataSource: {data: rows},
        scrollable: false,
        dataBound: function () {
            const grid = this;
            $container.find(".histogram").each(function () {
                const chart = $(this);
                const model = grid.dataItem(chart.closest("tr"));
                chart.kendoChart({
                    legend: {visible: false},
                    dataSource: {data: model.histogramValues},
                    series: [{field: "frequency", name: "Frequency"}],
                    valueAxis: {labels: {format: "{0}"}},
                    tooltip: {visible: true, template: "Bin: ${category} Frequency: ${value}"},
                    categoryAxis: {field: "bin", visible: false}
                });
            });
        },
        columns: [
            {field: "column", title: "Column", width: "220px"},
            {template: '<div class="histogram" style="height:150px"></div>', width: 300},
            {field: "min", title: "Min", format: "{0:n2}", width: "100px"},
            {field: "max", title: "Max", format: "{0:n2}", width: "100px"},
            {field: "mean", title: "Mean", format: "{0:n2}", width: "100px"},
            {field: "median", title: "Median", format: "{0:n2}", width: "100px"},
            {field: "standardDeviation", title: "Std Dev", format: "{0:n2}", width: "100px"}
        ]
    });
}

//# sourceURL=GridStatisticsTable.js
