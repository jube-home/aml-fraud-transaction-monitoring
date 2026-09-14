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
            url: "/api/RedisMetric",
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
    sort: [{field: "createdDate", dir: "desc"}],
    schema: {
        data: "rows",
        total: "total",
        model: {
            id: "id",
            fields: {
                createdDate: {type: "date", editable: false},
                instance: {type: "string", editable: false},
                connectedClients: {type: "number", editable: false},
                blockedClients: {type: "number", editable: false},
                usedMemoryBytes: {type: "number", editable: false},
                usedMemoryRssBytes: {type: "number", editable: false},
                maxMemoryBytes: {type: "number", editable: false},
                instantaneousOpsPerSecond: {type: "number", editable: false},
                totalCommandsProcessed: {type: "number", editable: false},
                totalConnectionsReceived: {type: "number", editable: false},
                keyspaceHits: {type: "number", editable: false},
                keyspaceMisses: {type: "number", editable: false},
                hitRatePercent: {type: "number", editable: false},
                evictedKeys: {type: "number", editable: false},
                expiredKeys: {type: "number", editable: false},
                connectedReplicas: {type: "number", editable: false},
                masterReplicationOffset: {type: "number", editable: false},
                uptimeSeconds: {type: "number", editable: false},
                totalKeys: {type: "number", editable: false},
                memoryFragmentationRatio: {type: "number", editable: false},
                rdbLastSaveAgeSeconds: {type: "number", editable: false},
                aofEnabled: {type: "boolean", editable: false},
                lastAofRewriteDate: {type: "date", editable: false},
                lastBgSaveDate: {type: "date", editable: false}
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
            '<label style="display:flex;align-items:center;gap:4px;">Search <input type="text" id="searchBox" placeholder="Instance (hostname)..." /></label>' +
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
            {field: "createdDate", title: "Created Date", width: 180},
            {field: "instance", title: "Instance", width: 160},
            {field: "connectedClients", title: "Connected Clients"},
            {field: "blockedClients", title: "Blocked Clients"},
            {field: "usedMemoryBytes", title: "Used Memory Bytes"},
            {field: "usedMemoryRssBytes", title: "Used Memory RSS Bytes"},
            {field: "maxMemoryBytes", title: "Max Memory Bytes"},
            {field: "instantaneousOpsPerSecond", title: "Ops/Sec"},
            {field: "totalCommandsProcessed", title: "Total Commands Processed"},
            {field: "totalConnectionsReceived", title: "Total Connections Received"},
            {field: "keyspaceHits", title: "Keyspace Hits"},
            {field: "keyspaceMisses", title: "Keyspace Misses"},
            {field: "hitRatePercent", title: "Hit Rate %", format: "{0:0.00}"},
            {field: "evictedKeys", title: "Evicted Keys"},
            {field: "expiredKeys", title: "Expired Keys"},
            {field: "connectedReplicas", title: "Connected Replicas"},
            {field: "masterReplicationOffset", title: "Master Replication Offset"},
            {field: "uptimeSeconds", title: "Uptime Seconds"},
            {field: "totalKeys", title: "Total Keys"},
            {field: "memoryFragmentationRatio", title: "Memory Fragmentation Ratio", format: "{0:0.00}"},
            {field: "rdbLastSaveAgeSeconds", title: "RDB Last Save Age Seconds"},
            {field: "aofEnabled", title: "AOF Enabled"},
            {field: "lastAofRewriteDate", title: "Last AOF Rewrite"},
            {field: "lastBgSaveDate", title: "Last BGSAVE (BgCopy)"}
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
        exportGridToCsv($grid.data("kendoGrid"), "/api/RedisMetric", {
            from: $fromDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            to: $toDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            search: $searchBox.val() || null
        }, "RedisMetric.csv");
    });

    dataSourceEntity.read();
});

//# sourceURL=RedisMetric.js
