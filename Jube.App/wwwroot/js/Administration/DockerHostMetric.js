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
            url: "/api/DockerHostMetric",
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
                containersTotal: {type: "number", editable: false},
                containersRunning: {type: "number", editable: false},
                containersPaused: {type: "number", editable: false},
                containersStopped: {type: "number", editable: false},
                imagesCount: {type: "number", editable: false},
                nCpu: {type: "number", editable: false},
                memTotalBytes: {type: "number", editable: false},
                dockerVersion: {type: "string", editable: false},
                apiVersion: {type: "string", editable: false},
                kernelVersion: {type: "string", editable: false},
                operatingSystem: {type: "string", editable: false},
                osType: {type: "string", editable: false},
                architecture: {type: "string", editable: false},
                layersSizeBytes: {type: "number", editable: false},
                imagesSizeBytes: {type: "number", editable: false},
                reclaimableImagesBytes: {type: "number", editable: false},
                containersDiskBytes: {type: "number", editable: false},
                volumesSizeBytes: {type: "number", editable: false},
                buildCacheSizeBytes: {type: "number", editable: false},
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
            '<label style="display:flex;align-items:center;gap:4px;">Search <input type="text" id="searchBox" placeholder="Instance or operating system..." /></label>' +
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
            {field: "createdDate", title: "Sampled Date", width: 180},
            {field: "instance", title: "Instance", width: 160},
            {field: "containersTotal", title: "Containers Total", width: 140},
            {field: "containersRunning", title: "Containers Running", width: 160},
            {field: "containersPaused", title: "Containers Paused", width: 160},
            {field: "containersStopped", title: "Containers Stopped", width: 160},
            {field: "imagesCount", title: "Images Count", width: 130},
            {field: "nCpu", title: "NCPU", width: 90},
            {field: "memTotalBytes", title: "Mem Total Bytes", width: 150},
            {field: "layersSizeBytes", title: "Layers Size Bytes", width: 160},
            {field: "imagesSizeBytes", title: "Images Size Bytes", width: 160},
            {field: "reclaimableImagesBytes", title: "Reclaimable Images Bytes", width: 200},
            {field: "containersDiskBytes", title: "Containers Disk Bytes", width: 180},
            {field: "volumesSizeBytes", title: "Volumes Size Bytes", width: 170},
            {field: "buildCacheSizeBytes", title: "Build Cache Size Bytes", width: 190},
            {field: "dockerVersion", title: "Docker Version", width: 140},
            {field: "apiVersion", title: "API Version", width: 120},
            {field: "kernelVersion", title: "Kernel Version", width: 200},
            {field: "operatingSystem", title: "Operating System", width: 260},
            {field: "osType", title: "OS Type", width: 100},
            {field: "architecture", title: "Architecture", width: 120}
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
        exportGridToCsv($grid.data("kendoGrid"), "/api/DockerHostMetric", {
            from: $fromDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            to: $toDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            search: $searchBox.val() || null
        }, "DockerHostMetric.csv");
    });

    dataSourceEntity.read();
});

//# sourceURL=DockerHostMetric.js
