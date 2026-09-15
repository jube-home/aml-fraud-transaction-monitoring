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

let $grid, $fromDate, $toDate, $eventTypeFilter, $searchBox, $applyFilters, $clearFilters, $exportCsv;

const dataSourceEntity = new kendo.data.DataSource({
    transport: {
        read: {
            url: "/api/EtcdClusterEvent",
            type: "GET",
            dataType: "json",
            data: function () {
                const sort = dataSourceEntity.sort();
                return {
                    from: $fromDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
                    to: $toDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
                    eventTypeId: $eventTypeFilter.val() || null,
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
                eventTypeName: {type: "string", editable: false},
                previousValue: {type: "string", editable: false},
                newValue: {type: "string", editable: false},
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
            '<label style="display:flex;align-items:center;gap:4px;">Event <select id="eventTypeFilter">' +
            '<option value="">All</option>' +
            '<option value="1">LeaderChanged</option>' +
            '<option value="2">HealthChanged</option>' +
            '<option value="3">AlarmRaised</option>' +
            '<option value="4">AlarmCleared</option>' +
            '</select></label>' +
            '<label style="display:flex;align-items:center;gap:4px;">Search <input type="text" id="searchBox" placeholder="Endpoint, name..." /></label>' +
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
            {field: "eventTypeName", title: "Event Type", width: 140},
            {field: "previousValue", title: "Previous Value", width: 180},
            {field: "newValue", title: "New Value", width: 180},
            {field: "instance", title: "Instance", width: 160},
            {field: "createdDate", title: "Flushed Date", width: 180}
        ]
    });

    $fromDate = $("#fromDate");
    $toDate = $("#toDate");
    $eventTypeFilter = $("#eventTypeFilter");
    $searchBox = $("#searchBox");
    $applyFilters = $("#applyFilters");
    $clearFilters = $("#clearFilters");
    $exportCsv = $("#exportCsv");

    $fromDate.kendoDateTimePicker({format: "MM/dd/yyyy HH:mm", interval: 15});
    $toDate.kendoDateTimePicker({format: "MM/dd/yyyy HH:mm", interval: 15});
    $fromDate.data("kendoDateTimePicker").value(defaultFromDate());
    $toDate.data("kendoDateTimePicker").value(defaultToDate());
    $eventTypeFilter.kendoDropDownList();
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
        $eventTypeFilter.data("kendoDropDownList").value("");
        $searchBox.val("");
        dataSourceEntity.read();
    });

    $searchBox.on("keypress", function (e) {
        if (e.which === 13) {
            dataSourceEntity.read();
        }
    });

    $exportCsv.on("click", function () {
        exportGridToCsv($grid.data("kendoGrid"), "/api/EtcdClusterEvent", {
            from: $fromDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            to: $toDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            eventTypeId: $eventTypeFilter.val() || null,
            search: $searchBox.val() || null
        }, "EtcdClusterEvent.csv");
    });

    dataSourceEntity.read();
});

//# sourceURL=EtcdClusterEvent.js
