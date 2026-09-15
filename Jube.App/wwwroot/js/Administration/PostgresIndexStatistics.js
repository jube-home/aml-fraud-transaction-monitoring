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

let $grid, $exportCsv;

const dataSourceEntity = new kendo.data.DataSource({
    transport: {
        read: {
            url: "/api/PostgresIndexStatistics",
            type: "GET",
            dataType: "json",
            data: function () {
                const sort = dataSourceEntity.sort();
                return {
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
            id: "indexName",
            fields: {
                schemaName: {type: "string", editable: false},
                tableName: {type: "string", editable: false},
                indexName: {type: "string", editable: false},
                indexScans: {type: "number", editable: false},
                indexTuplesRead: {type: "number", editable: false},
                indexTuplesFetched: {type: "number", editable: false},
                indexSizeBytes: {type: "number", editable: false},
                isUnique: {type: "boolean", editable: false},
                isPrimary: {type: "boolean", editable: false},
                isUnused: {type: "boolean", editable: false}
            }
        }
    }
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
        toolbar: kendo.template(
            '<div class="jube-toolbar-filters" style="display:flex;align-items:center;gap:12px;padding:6px 4px;flex-wrap:wrap;">' +
            '<button type="button" id="exportCsv" class="k-button">Export CSV</button>' +
            '</div>'
        ),
        pageable: {pageSizes: [50, 100, 200, 500], buttonCount: 10},
        height: $(window).height() - 210,
        scrollable: true,
        filterable: true,
        sortable: true,
        dataBound: function () {
            for (let i = 0; i < this.columns.length; i++) {
                this.autoFitColumn(i);
            }
        },
        columns: [
            {field: "schemaName", title: "Schema"},
            {field: "tableName", title: "Table"},
            {field: "indexName", title: "Index"},
            {field: "isUnused", title: "Unused"},
            {field: "isUnique", title: "Unique"},
            {field: "isPrimary", title: "Primary Key"},
            {field: "indexScans", title: "Index Scans"},
            {field: "indexTuplesRead", title: "Index Tuples Read"},
            {field: "indexTuplesFetched", title: "Index Tuples Fetched"},
            {field: "indexSizeBytes", title: "Index Size Bytes"}
        ]
    });

    $exportCsv = $("#exportCsv");

    $exportCsv.kendoButton();
    $exportCsv.on("click", function () {
        exportGridToCsv($grid.data("kendoGrid"), "/api/PostgresIndexStatistics", {}, "PostgresIndexStatistics.csv");
    });
});

//# sourceURL=PostgresIndexStatistics.js
