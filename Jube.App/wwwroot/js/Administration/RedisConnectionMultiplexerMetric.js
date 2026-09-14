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
            url: "/api/RedisConnectionMultiplexerMetric",
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
                clientName: {type: "string", editable: false},
                timeoutMilliseconds: {type: "number", editable: false},
                isConnected: {type: "boolean", editable: false},
                isConnecting: {type: "boolean", editable: false},
                endPointCount: {type: "number", editable: false},
                connectedEndPointCount: {type: "number", editable: false},
                totalOperationCount: {type: "number", editable: false},
                totalOutstanding: {type: "number", editable: false},
                interactivePendingUnsentItems: {type: "number", editable: false},
                interactiveSentItemsAwaitingResponse: {type: "number", editable: false},
                interactiveResponsesAwaitingAsyncCompletion: {type: "number", editable: false},
                interactiveTotalOutstanding: {type: "number", editable: false},
                interactiveCompletedAsynchronously: {type: "number", editable: false},
                interactiveCompletedSynchronously: {type: "number", editable: false},
                interactiveFailedAsynchronously: {type: "number", editable: false},
                interactiveNonPreferredEndpointCount: {type: "number", editable: false},
                interactiveSocketCount: {type: "number", editable: false},
                interactiveWriterCount: {type: "number", editable: false},
                subscriptionPendingUnsentItems: {type: "number", editable: false},
                subscriptionSentItemsAwaitingResponse: {type: "number", editable: false},
                subscriptionResponsesAwaitingAsyncCompletion: {type: "number", editable: false},
                subscriptionTotalOutstanding: {type: "number", editable: false},
                subscriptionCompletedAsynchronously: {type: "number", editable: false},
                subscriptionCompletedSynchronously: {type: "number", editable: false},
                subscriptionFailedAsynchronously: {type: "number", editable: false},
                subscriptionSocketCount: {type: "number", editable: false},
                subscriptionCount: {type: "number", editable: false},
                connectionFailedCount: {type: "number", editable: false},
                connectionRestoredCount: {type: "number", editable: false},
                errorMessageCount: {type: "number", editable: false},
                internalErrorCount: {type: "number", editable: false},
                configurationChangedCount: {type: "number", editable: false},
                configurationChangedBroadcastCount: {type: "number", editable: false}
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
            '<label style="display:flex;align-items:center;gap:4px;">Search <input type="text" id="searchBox" placeholder="Instance or client name..." /></label>' +
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
            {field: "clientName", title: "Client Name", width: 160},
            {field: "timeoutMilliseconds", title: "Timeout Milliseconds"},
            {field: "isConnected", title: "Is Connected"},
            {field: "isConnecting", title: "Is Connecting"},
            {field: "endPointCount", title: "End Points"},
            {field: "connectedEndPointCount", title: "Connected End Points"},
            {field: "totalOperationCount", title: "Total Operation Count"},
            {field: "totalOutstanding", title: "Total Outstanding"},
            {field: "interactivePendingUnsentItems", title: "Interactive Pending Unsent"},
            {field: "interactiveSentItemsAwaitingResponse", title: "Interactive Sent Awaiting Response"},
            {field: "interactiveResponsesAwaitingAsyncCompletion", title: "Interactive Awaiting Completion"},
            {field: "interactiveTotalOutstanding", title: "Interactive Total Outstanding"},
            {field: "interactiveCompletedAsynchronously", title: "Interactive Completed Async"},
            {field: "interactiveCompletedSynchronously", title: "Interactive Completed Sync"},
            {field: "interactiveFailedAsynchronously", title: "Interactive Failed Async"},
            {field: "interactiveNonPreferredEndpointCount", title: "Interactive Non-Preferred Endpoint Count"},
            {field: "interactiveSocketCount", title: "Interactive Socket Count"},
            {field: "interactiveWriterCount", title: "Interactive Writer Count"},
            {field: "subscriptionPendingUnsentItems", title: "Subscription Pending Unsent"},
            {field: "subscriptionSentItemsAwaitingResponse", title: "Subscription Sent Awaiting Response"},
            {field: "subscriptionResponsesAwaitingAsyncCompletion", title: "Subscription Awaiting Completion"},
            {field: "subscriptionTotalOutstanding", title: "Subscription Total Outstanding"},
            {field: "subscriptionCompletedAsynchronously", title: "Subscription Completed Async"},
            {field: "subscriptionCompletedSynchronously", title: "Subscription Completed Sync"},
            {field: "subscriptionFailedAsynchronously", title: "Subscription Failed Async"},
            {field: "subscriptionSocketCount", title: "Subscription Socket Count"},
            {field: "subscriptionCount", title: "Subscription Count"},
            {field: "connectionFailedCount", title: "Connection Failed Count"},
            {field: "connectionRestoredCount", title: "Connection Restored Count"},
            {field: "errorMessageCount", title: "Error Message Count"},
            {field: "internalErrorCount", title: "Internal Error Count"},
            {field: "configurationChangedCount", title: "Configuration Changed Count"},
            {field: "configurationChangedBroadcastCount", title: "Configuration Changed Broadcast Count"}
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
        exportGridToCsv($grid.data("kendoGrid"), "/api/RedisConnectionMultiplexerMetric", {
            from: $fromDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            to: $toDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            search: $searchBox.val() || null
        }, "RedisConnectionMultiplexerMetric.csv");
    });

    dataSourceEntity.read();
});

//# sourceURL=RedisConnectionMultiplexerMetric.js
