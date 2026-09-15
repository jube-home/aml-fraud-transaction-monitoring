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
            url: "/api/PostgresActivity",
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
            id: "pid",
            fields: {
                pid: {type: "number", editable: false},
                databaseName: {type: "string", editable: false},
                userName: {type: "string", editable: false},
                applicationName: {type: "string", editable: false},
                clientAddress: {type: "string", editable: false},
                backendType: {type: "string", editable: false},
                state: {type: "string", editable: false},
                waitEventType: {type: "string", editable: false},
                waitEvent: {type: "string", editable: false},
                blockedByPids: {editable: false},
                backendStartDate: {type: "date", editable: false},
                transactionStartDate: {type: "date", editable: false},
                transactionDurationSeconds: {type: "number", editable: false},
                queryStartDate: {type: "date", editable: false},
                queryDurationSeconds: {type: "number", editable: false},
                query: {type: "string", editable: false}
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
            '<label style="display:flex;align-items:center;gap:4px;">Search <input type="text" id="searchBox" placeholder="User, application or query text..." /></label>' +
            '<button type="button" id="applyFilters" class="k-button">Apply</button>' +
            '<button type="button" id="clearFilters" class="k-button">Clear</button>' +
            '<button type="button" id="refreshButton" class="k-button">Refresh</button>' +
            '<button type="button" id="exportCsv" class="k-button">Export CSV</button>' +
            '<span style="display:flex;align-items:center;gap:4px;">' +
            '<span style="display:inline-block;width:10px;height:10px;background-color:rgb(254,226,226);' +
            'border:1px solid rgb(185,28,28);"></span>' +
            'Highlighted rows are being blocked by another backend -- see the "Blocked By" column</span>' +
            '</div>'
        ),
        dataBound: function () {
            for (let i = 0; i < this.columns.length; i++) {
                this.autoFitColumn(i);
            }

            const grid = this;
            grid.tbody.find("tr").each(function () {
                const item = grid.dataItem(this);
                if (item && item.blockedByPids && item.blockedByPids.length > 0) {
                    $(this).css("background-color", "#fee2e2");
                }
            });
        },
        columns: [
            {field: "pid", title: "PID", width: 80},
            {field: "databaseName", title: "Database", width: 120},
            {field: "userName", title: "User", width: 120},
            {field: "applicationName", title: "Application"},
            {field: "clientAddress", title: "Client Address", width: 140},
            {field: "backendType", title: "Backend Type", width: 160},
            {field: "state", title: "State", width: 140},
            {field: "waitEventType", title: "Wait Type", width: 100},
            {field: "waitEvent", title: "Wait Event", width: 140},
            {
                field: "blockedByPids", title: "Blocked By", width: 120,
                template: "#= blockedByPids && blockedByPids.length ? blockedByPids.join(', ') : '' #"
            },
            {field: "transactionDurationSeconds", title: "Xact Duration (s)", width: 140, format: "{0:n1}"},
            {field: "queryDurationSeconds", title: "Query Duration (s)", width: 140, format: "{0:n1}"},
            {field: "backendStartDate", title: "Connected Since", width: 180},
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
        exportGridToCsv($grid.data("kendoGrid"), "/api/PostgresActivity", {
            search: $searchBox.val() || null
        }, "PostgresActivity.csv");
    });

    dataSourceEntity.read();
});

//# sourceURL=PostgresActivity.js
