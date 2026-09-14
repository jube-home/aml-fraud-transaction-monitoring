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

let $grid, $fromDate, $toDate, $stageFilter, $applyFilters, $clearFilters, $exportCsv;

const dataSourceEntity = new kendo.data.DataSource({
    transport: {
        read: {
            url: "/api/EntityAnalysisModelResponseTimePipelineCounter",
            type: "GET",
            dataType: "json",
            data: function () {
                const sort = dataSourceEntity.sort();
                return {
                    from: $fromDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
                    to: $toDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
                    stageId: $stageFilter.val() || null,
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
                const invokes = this.get("invokeCount");
                return invokes > 0 ? (total / invokes) : 0;
            },
            averageAllocatedBytes: function () {
                const total = this.get("totalAllocatedBytes");
                const invokes = this.get("invokeCount");
                return invokes > 0 ? (total / invokes) : 0;
            },
            fields: {
                entityAnalysisModelName: {type: "string", editable: false},
                stageName: {type: "string", editable: false},
                sequenceNumber: {type: "number", editable: false},
                totalMicroseconds: {type: "number", editable: false},
                minMicroseconds: {type: "number", editable: false},
                maxMicroseconds: {type: "number", editable: false},
                totalAllocatedBytes: {type: "number", editable: false},
                minAllocatedBytes: {type: "number", editable: false},
                maxAllocatedBytes: {type: "number", editable: false},
                invokeCount: {type: "number", editable: false},
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
            '<label style="display:flex;align-items:center;gap:4px;">Stage <select id="stageFilter">' +
            '<option value="">All</option>' +
            '<option value="1">Parse</option>' +
            '<option value="2">CheckIntegrityAndUpsert</option>' +
            '<option value="3">InlineFunctions</option>' +
            '<option value="4">InlineScripts</option>' +
            '<option value="5">Gateway</option>' +
            '<option value="6">CacheDbStorage</option>' +
            '<option value="10">JoinReadTasks</option>' +
            '<option value="11">AbstractionRulesWithoutSearchKeys</option>' +
            '<option value="12">AbstractionCalculations</option>' +
            '<option value="13">ExhaustiveAdaptation</option>' +
            '<option value="14">HttpAdaptation</option>' +
            '<option value="15">Activation</option>' +
            '<option value="16">JoinWriteTasks</option>' +
            '<option value="17">WriteResponse</option>' +
            '<option value="18">BuildArchivePayload</option>' +
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
            {field: "entityAnalysisModelName", title: "Entity Analysis Model"},
            {field: "sequenceNumber", title: "Sequence"},
            {field: "stageName", title: "Stage"},
            {field: "createdDate", title: "Created Date"},
            {field: "totalMicroseconds", title: "Total Microseconds"},
            {field: "minMicroseconds", title: "Min Microseconds"},
            {field: "maxMicroseconds", title: "Max Microseconds"},
            {
                field: "averageMicroseconds()",
                title: "Average Microseconds",
                format: "{0:0}"
            },
            {field: "totalAllocatedBytes", title: "Total Allocated Bytes"},
            {field: "minAllocatedBytes", title: "Min Allocated Bytes"},
            {field: "maxAllocatedBytes", title: "Max Allocated Bytes"},
            {
                field: "averageAllocatedBytes()",
                title: "Average Allocated Bytes",
                format: "{0:0}"
            },
            {field: "invokeCount", title: "Invoke Count"},
            {field: "instance", title: "Instance"}
        ]
    });

    $fromDate = $("#fromDate");
    $toDate = $("#toDate");
    $stageFilter = $("#stageFilter");
    $applyFilters = $("#applyFilters");
    $clearFilters = $("#clearFilters");
    $exportCsv = $("#exportCsv");

    $fromDate.kendoDateTimePicker({format: "MM/dd/yyyy HH:mm", interval: 15});
    $toDate.kendoDateTimePicker({format: "MM/dd/yyyy HH:mm", interval: 15});
    $fromDate.data("kendoDateTimePicker").value(defaultFromDate());
    $toDate.data("kendoDateTimePicker").value(defaultToDate());
    $stageFilter.kendoDropDownList();
    $applyFilters.kendoButton();
    $clearFilters.kendoButton();
    $exportCsv.kendoButton();

    $applyFilters.on("click", function () {
        dataSourceEntity.read();
    });

    $clearFilters.on("click", function () {
        $fromDate.data("kendoDateTimePicker").value(defaultFromDate());
        $toDate.data("kendoDateTimePicker").value(defaultToDate());
        $stageFilter.data("kendoDropDownList").value("");
        dataSourceEntity.read();
    });

    $exportCsv.on("click", function () {
        exportGridToCsv($grid.data("kendoGrid"), "/api/EntityAnalysisModelResponseTimePipelineCounter", {
            from: $fromDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            to: $toDate.data("kendoDateTimePicker").value()?.toISOString() ?? null,
            stageId: $stageFilter.val() || null
        }, "EntityAnalysisModelResponseTimePipelineCounter.csv");
    });

    dataSourceEntity.read();
});

//# sourceURL=EntityAnalysisModelResponseTimePipelineCounter.js
