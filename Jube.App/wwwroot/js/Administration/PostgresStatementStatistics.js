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

let $grid, $searchBox, $applyFilters, $clearFilters, $refreshButton, $exportCsv;

const dataSourceEntity = new kendo.data.DataSource({
    transport: {
        read: {
            url: "/api/PostgresStatementStatistics",
            type: "GET",
            dataType: "json",
            data: function () {
                const sort = dataSourceEntity.sort();
                return {
                    search: $searchBox.val() || null,
                    sortField: sort && sort[0] ? sort[0].field : null,
                    sortDirection: sort && sort[0] ? sort[0].dir : null
                };
            }
        }
    },
    serverSorting: true,
    schema: {
        data: "rows",
        total: "total",
        model: {
            id: "queryId",
            fields: {
                queryId: {type: "number", editable: false},
                databaseName: {type: "string", editable: false},
                userName: {type: "string", editable: false},
                query: {type: "string", editable: false},
                calls: {type: "number", editable: false},
                rows: {type: "number", editable: false},
                totalExecTimeMilliseconds: {type: "number", editable: false},
                meanExecTimeMilliseconds: {type: "number", editable: false},
                minExecTimeMilliseconds: {type: "number", editable: false},
                maxExecTimeMilliseconds: {type: "number", editable: false},
                stddevExecTimeMilliseconds: {type: "number", editable: false},
                sharedBlksHitBytes: {type: "number", editable: false},
                sharedBlksReadBytes: {type: "number", editable: false},
                tempBlksReadBytes: {type: "number", editable: false},
                tempBlksWrittenBytes: {type: "number", editable: false},
                walBytes: {type: "number", editable: false}
            }
        }
    },
    pageSize: 100
});

dataSourceEntity.bind("requestEnd", function (e) {
    if (e.type === "read" && e.response) {
        renderGridStatisticsTable("statistics", e.response.statistics);
    }
});

$(document).ready(function () {
    $grid = $("#grid");

    $grid.kendoGrid({
        dataSource: dataSourceEntity,
        autoBind: false,
        pageable: {pageSizes: [50, 100, 200, 500], buttonCount: 10},
        height: $(window).height() - 210,
        scrollable: true,
        filterable: true,
        sortable: true,
        toolbar: kendo.template(
            '<div class="jube-toolbar-filters" style="display:flex;align-items:center;gap:12px;padding:6px 4px;flex-wrap:wrap;">' +
            '<label style="display:flex;align-items:center;gap:4px;">Search <input type="text" id="searchBox" placeholder="User, database or query text..." /></label>' +
            '<button type="button" id="applyFilters" class="k-button">Apply</button>' +
            '<button type="button" id="clearFilters" class="k-button">Clear</button>' +
            '<button type="button" id="refreshButton" class="k-button">Refresh</button>' +
            '<button type="button" id="exportCsv" class="k-button">Export CSV</button>' +
            '</div>'
        ),
        dataBound: function () {
            for (let i = 0; i < this.columns.length; i++) {
                this.autoFitColumn(i);
            }
        },
        columns: [
            {field: "queryId", title: "Query Id", width: 140},
            {field: "databaseName", title: "Database", width: 120},
            {field: "userName", title: "User", width: 120},
            {field: "calls", title: "Calls", width: 100, format: "{0:n0}"},
            {field: "rows", title: "Rows", width: 100, format: "{0:n0}"},
            {field: "totalExecTimeMilliseconds", title: "Total Time (ms)", width: 140, format: "{0:n1}"},
            {field: "meanExecTimeMilliseconds", title: "Mean Time (ms)", width: 140, format: "{0:n1}"},
            {field: "minExecTimeMilliseconds", title: "Min Time (ms)", width: 130, format: "{0:n1}"},
            {field: "maxExecTimeMilliseconds", title: "Max Time (ms)", width: 130, format: "{0:n1}"},
            {field: "stddevExecTimeMilliseconds", title: "Stddev Time (ms)", width: 140, format: "{0:n1}"},
            {field: "sharedBlksHitBytes", title: "Cache Hit (bytes)", width: 150, format: "{0:n0}"},
            {field: "sharedBlksReadBytes", title: "Disk Read (bytes)", width: 150, format: "{0:n0}"},
            {field: "tempBlksReadBytes", title: "Temp Read (bytes)", width: 150, format: "{0:n0}"},
            {field: "tempBlksWrittenBytes", title: "Temp Written (bytes)", width: 160, format: "{0:n0}"},
            {field: "walBytes", title: "WAL (bytes)", width: 130, format: "{0:n0}"},
            {field: "query", title: "Query"}
        ]
    });

    $searchBox = $("#searchBox");
    $applyFilters = $("#applyFilters");
    $clearFilters = $("#clearFilters");
    $refreshButton = $("#refreshButton");
    $exportCsv = $("#exportCsv");

    $searchBox.kendoTextBox();
    $applyFilters.kendoButton();
    $clearFilters.kendoButton();
    $refreshButton.kendoButton();
    $exportCsv.kendoButton();

    $applyFilters.on("click", function () {
        dataSourceEntity.read();
    });

    $clearFilters.on("click", function () {
        $searchBox.val("");
        dataSourceEntity.read();
    });

    $refreshButton.on("click", function () {
        dataSourceEntity.read();
    });

    $searchBox.on("keypress", function (e) {
        if (e.which === 13) {
            dataSourceEntity.read();
        }
    });

    $exportCsv.on("click", function () {
        exportGridToCsv($grid.data("kendoGrid"), "/api/PostgresStatementStatistics", {
            search: $searchBox.val() || null
        }, "PostgresStatementStatistics.csv");
    });

    dataSourceEntity.read();
});

//# sourceURL=PostgresStatementStatistics.js
