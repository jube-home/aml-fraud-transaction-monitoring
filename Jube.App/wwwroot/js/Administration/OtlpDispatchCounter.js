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

function defaultFromDate() {
    const date = new Date();
    date.setHours(date.getHours() - 1);
    return date;
}

function defaultToDate() {
    return new Date();
}

let $grid, $fromDate, $toDate, $signalFilter, $applyFilters, $clearFilters, $exportCsv;

const dataSourceEntity = new kendo.data.DataSource({
    transport: {
        read: {
            url: "/api/OtlpDispatchCounter",
            type: "GET",
            dataType: "json",
            data: function () {
                const sort = dataSourceEntity.sort();
                return {
                    from: $fromDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
                    to: $toDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
                    signalId: $signalFilter.val() || null,
                    sortField: sort && sort[0] ? sort[0].field : null,
                    sortDirection: sort && sort[0] ? sort[0].dir : null,
                    take: 2000
                };
            }
        }
    },
    serverSorting: true,
    sort: [{field: "createdDate", dir: "desc"}],
    schema: {
        data: "rows",
        total: "total",
        model: {
            id: "id",
            averageMicroseconds: function () {
                const total = this.get("totalMicroseconds");
                const count = this.get("count");
                return count > 0 ? (total / count) : 0;
            },
            failureRate: function () {
                const count = this.get("count");
                const failures = this.get("failureCount");
                return count > 0 ? (failures / count) : 0;
            },
            fields: {
                signalName: {type: "string", editable: false},
                count: {type: "number", editable: false},
                successCount: {type: "number", editable: false},
                failureCount: {type: "number", editable: false},
                itemCount: {type: "number", editable: false},
                droppedCount: {type: "number", editable: false},
                totalMicroseconds: {type: "number", editable: false},
                minMicroseconds: {type: "number", editable: false},
                maxMicroseconds: {type: "number", editable: false},
                createdDate: {type: "date", editable: false},
                instance: {type: "string", editable: false}
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
            '<label style="display:flex;align-items:center;gap:4px;">From <input type="text" id="fromDate" /></label>' +
            '<label style="display:flex;align-items:center;gap:4px;">To <input type="text" id="toDate" /></label>' +
            '<label style="display:flex;align-items:center;gap:4px;">Signal <select id="signalFilter">' +
            '<option value="">All</option>' +
            '<option value="1">Traces</option>' +
            '<option value="2">Metrics</option>' +
            '<option value="3">Logs</option>' +
            '</select></label>' +
            '<button type="button" id="applyFilters" class="k-button">Apply</button>' +
            '<button type="button" id="clearFilters" class="k-button">Clear</button>' +
            '<button type="button" id="exportCsv" class="k-button">Export CSV</button>' +
            '</div>'
        ),
        dataBound: function () {
            for (let i = 0; i < this.columns.length; i++) {
                this.autoFitColumn(i);
            }
        },
        columns: [
            {field: "signalName", title: "Signal"},
            {field: "createdDate", title: "Created Date"},
            {field: "count", title: "Dispatch Count"},
            {field: "successCount", title: "Success Count"},
            {field: "failureCount", title: "Failure Count"},
            {
                field: "failureRate()",
                title: "Failure Rate",
                format: "{0:p1}"
            },
            {field: "itemCount", title: "Item Count"},
            {field: "droppedCount", title: "Dropped Count (Queue Full)"},
            {field: "totalMicroseconds", title: "Total Microseconds"},
            {field: "minMicroseconds", title: "Min Microseconds"},
            {field: "maxMicroseconds", title: "Max Microseconds"},
            {
                field: "averageMicroseconds()",
                title: "Average Microseconds",
                format: "{0:0}"
            },
            {field: "instance", title: "Instance"}
        ]
    });

    $fromDate = $("#fromDate");
    $toDate = $("#toDate");
    $signalFilter = $("#signalFilter");
    $applyFilters = $("#applyFilters");
    $clearFilters = $("#clearFilters");
    $exportCsv = $("#exportCsv");

    $fromDate.kendoDateTimePicker({format: "MM/dd/yyyy HH:mm", interval: 15});
    $toDate.kendoDateTimePicker({format: "MM/dd/yyyy HH:mm", interval: 15});
    $fromDate.data("kendoDateTimePicker").value(defaultFromDate());
    $toDate.data("kendoDateTimePicker").value(defaultToDate());
    $signalFilter.kendoDropDownList();
    $applyFilters.kendoButton();
    $clearFilters.kendoButton();
    $exportCsv.kendoButton();

    $applyFilters.on("click", function () {
        dataSourceEntity.read();
    });

    $clearFilters.on("click", function () {
        $fromDate.data("kendoDateTimePicker").value(defaultFromDate());
        $toDate.data("kendoDateTimePicker").value(defaultToDate());
        $signalFilter.data("kendoDropDownList").value("");
        dataSourceEntity.read();
    });

    $exportCsv.on("click", function () {
        exportGridToCsv($grid.data("kendoGrid"), "/api/OtlpDispatchCounter", {
            from: $fromDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            to: $toDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            signalId: $signalFilter.val() || null
        }, "OtlpDispatchCounter.csv");
    });

    dataSourceEntity.read();
});

//# sourceURL=OtlpDispatchCounter.js
