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

let $grid, $fromDate, $toDate, $searchBox, $applyFilters, $clearFilters, $exportCsv;

const dataSourceEntity = new kendo.data.DataSource({
    transport: {
        read: {
            url: "/api/DockerContainerMetric",
            type: "GET",
            dataType: "json",
            data: function () {
                const sort = dataSourceEntity.sort();
                return {
                    from: $fromDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
                    to: $toDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
                    search: $searchBox.val() || null,
                    sortField: sort && sort[0] ? sort[0].field : null,
                    sortDirection: sort && sort[0] ? sort[0].dir : null,
                    take: 2000
                };
            }
        }
    },
    serverSorting: true,
    sort: [{field: "occurredDate", dir: "desc"}],
    schema: {
        data: "rows",
        total: "total",
        model: {
            id: "id",
            fields: {
                occurredDate: {type: "date", editable: false},
                containerId: {type: "string", editable: false},
                name: {type: "string", editable: false},
                image: {type: "string", editable: false},
                state: {type: "string", editable: false},
                status: {type: "string", editable: false},
                restartCount: {type: "number", editable: false},
                oomKilled: {type: "boolean", editable: false},
                exitCode: {type: "number", editable: false},
                startedAt: {type: "date", editable: false},
                healthStatus: {type: "string", editable: false},
                cpuUsagePercent: {type: "number", editable: false},
                onlineCpus: {type: "number", editable: false},
                memoryUsageBytes: {type: "number", editable: false},
                memoryLimitBytes: {type: "number", editable: false},
                memoryPercent: {type: "number", editable: false},
                networkRxBytes: {type: "number", editable: false},
                networkTxBytes: {type: "number", editable: false},
                blockReadBytes: {type: "number", editable: false},
                blockWriteBytes: {type: "number", editable: false},
                pidsCurrent: {type: "number", editable: false},
                pidsLimit: {type: "number", editable: false},
                instance: {type: "string", editable: false},
                createdDate: {type: "date", editable: false}
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
            '<label style="display:flex;align-items:center;gap:4px;">Search <input type="text" id="searchBox" placeholder="Name, image, state or status..." /></label>' +
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
            {field: "occurredDate", title: "Occurred Date", width: 180},
            {field: "name", title: "Name", width: 180},
            {field: "image", title: "Image", width: 180},
            {field: "state", title: "State", width: 100},
            {field: "status", title: "Status", width: 200},
            {field: "healthStatus", title: "Health", width: 100},
            {field: "restartCount", title: "Restart Count", width: 130},
            {field: "oomKilled", title: "OOM Killed", width: 110},
            {field: "exitCode", title: "Exit Code", width: 100},
            {field: "cpuUsagePercent", title: "CPU %", width: 100, format: "{0:n2}"},
            {field: "onlineCpus", title: "Online CPUs", width: 110},
            {field: "memoryUsageBytes", title: "Memory Usage Bytes", width: 170},
            {field: "memoryLimitBytes", title: "Memory Limit Bytes", width: 170},
            {field: "memoryPercent", title: "Memory %", width: 110, format: "{0:n2}"},
            {field: "networkRxBytes", title: "Network Rx Bytes", width: 160},
            {field: "networkTxBytes", title: "Network Tx Bytes", width: 160},
            {field: "blockReadBytes", title: "Block Read Bytes", width: 160},
            {field: "blockWriteBytes", title: "Block Write Bytes", width: 160},
            {field: "pidsCurrent", title: "Pids Current", width: 120},
            {field: "pidsLimit", title: "Pids Limit", width: 110},
            {field: "startedAt", title: "Started At", width: 180},
            {field: "containerId", title: "Container Id", width: 280},
            {field: "instance", title: "Instance", width: 160},
            {field: "createdDate", title: "Flushed Date", width: 180}
        ]
    });

    $fromDate = $("#fromDate");
    $toDate = $("#toDate");
    $searchBox = $("#searchBox");
    $applyFilters = $("#applyFilters");
    $clearFilters = $("#clearFilters");
    $exportCsv = $("#exportCsv");

    $fromDate.kendoDateTimePicker({format: "MM/dd/yyyy HH:mm", interval: 15});
    $toDate.kendoDateTimePicker({format: "MM/dd/yyyy HH:mm", interval: 15});
    $fromDate.data("kendoDateTimePicker").value(defaultFromDate());
    $toDate.data("kendoDateTimePicker").value(defaultToDate());
    $searchBox.kendoTextBox();
    $applyFilters.kendoButton();
    $clearFilters.kendoButton();
    $exportCsv.kendoButton();

    $applyFilters.on("click", function () {
        dataSourceEntity.read();
    });

    $clearFilters.on("click", function () {
        $fromDate.data("kendoDateTimePicker").value(defaultFromDate());
        $toDate.data("kendoDateTimePicker").value(defaultToDate());
        $searchBox.val("");
        dataSourceEntity.read();
    });

    $searchBox.on("keypress", function (e) {
        if (e.which === 13) {
            dataSourceEntity.read();
        }
    });

    $exportCsv.on("click", function () {
        exportGridToCsv($grid.data("kendoGrid"), "/api/DockerContainerMetric", {
            from: $fromDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            to: $toDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            search: $searchBox.val() || null
        }, "DockerContainerMetric.csv");
    });

    dataSourceEntity.read();
});

//# sourceURL=DockerContainerMetric.js
