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
            url: "/api/EtcdMemberStatus",
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
                endpoint: {type: "string", editable: false},
                memberId: {type: "string", editable: false},
                name: {type: "string", editable: false},
                peerUrls: {type: "string", editable: false},
                clientUrls: {type: "string", editable: false},
                isLearner: {type: "boolean", editable: false},
                version: {type: "string", editable: false},
                clusterVersion: {type: "string", editable: false},
                healthOk: {type: "boolean", editable: false},
                healthReason: {type: "string", editable: false},
                leaderId: {type: "string", editable: false},
                isLeader: {type: "boolean", editable: false},
                hasLeader: {type: "boolean", editable: false},
                leaderChangesTotal: {type: "number", editable: false},
                dbSizeBytes: {type: "number", editable: false},
                dbSizeInUseBytes: {type: "number", editable: false},
                raftIndex: {type: "number", editable: false},
                raftTerm: {type: "number", editable: false},
                raftAppliedIndex: {type: "number", editable: false},
                proposalsCommittedTotal: {type: "number", editable: false},
                proposalsAppliedTotal: {type: "number", editable: false},
                proposalsPendingCount: {type: "number", editable: false},
                proposalsFailedTotal: {type: "number", editable: false},
                walFsyncAvgMicroseconds: {type: "number", editable: false},
                backendCommitAvgMicroseconds: {type: "number", editable: false},
                slowApplyTotal: {type: "number", editable: false},
                slowReadIndexesTotal: {type: "number", editable: false},
                alarmCount: {type: "number", editable: false},
                alarms: {type: "string", editable: false},
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
            '<label style="display:flex;align-items:center;gap:4px;">Search <input type="text" id="searchBox" placeholder="Endpoint, name, health reason, alarms..." /></label>' +
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
            {field: "endpoint", title: "Endpoint", width: 160},
            {field: "memberId", title: "Member Id", width: 160},
            {field: "name", title: "Name", width: 120},
            {field: "peerUrls", title: "Peer URLs", width: 200},
            {field: "clientUrls", title: "Client URLs", width: 200},
            {field: "isLearner", title: "Is Learner", width: 100},
            {field: "version", title: "Version", width: 100},
            {field: "clusterVersion", title: "Cluster Version", width: 130},
            {field: "healthOk", title: "Health OK", width: 100},
            {field: "healthReason", title: "Health Reason", width: 160},
            {field: "leaderId", title: "Leader Id", width: 160},
            {field: "isLeader", title: "Is Leader", width: 100},
            {field: "hasLeader", title: "Has Leader", width: 100},
            {field: "leaderChangesTotal", title: "Leader Changes Total", width: 160},
            {field: "dbSizeBytes", title: "DB Size Bytes", width: 140},
            {field: "dbSizeInUseBytes", title: "DB Size In Use Bytes", width: 160},
            {field: "raftIndex", title: "Raft Index", width: 120},
            {field: "raftTerm", title: "Raft Term", width: 110},
            {field: "raftAppliedIndex", title: "Raft Applied Index", width: 150},
            {field: "proposalsCommittedTotal", title: "Proposals Committed Total", width: 190},
            {field: "proposalsAppliedTotal", title: "Proposals Applied Total", width: 180},
            {field: "proposalsPendingCount", title: "Proposals Pending Count", width: 180},
            {field: "proposalsFailedTotal", title: "Proposals Failed Total", width: 170},
            {field: "walFsyncAvgMicroseconds", title: "WAL Fsync Avg Microseconds", width: 200},
            {field: "backendCommitAvgMicroseconds", title: "Backend Commit Avg Microseconds", width: 220},
            {field: "slowApplyTotal", title: "Slow Apply Total", width: 140},
            {field: "slowReadIndexesTotal", title: "Slow Read Indexes Total", width: 180},
            {field: "alarmCount", title: "Alarm Count", width: 110},
            {field: "alarms", title: "Alarms", width: 140},
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
        exportGridToCsv($grid.data("kendoGrid"), "/api/EtcdMemberStatus", {
            from: $fromDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            to: $toDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            search: $searchBox.val() || null
        }, "EtcdMemberStatus.csv");
    });

    dataSourceEntity.read();
});

//# sourceURL=EtcdMemberStatus.js
