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

var dataSourceEntity = new kendo.data.DataSource({
    transport: {
        read: {
            url: "/api/EntityAnalysisAsynchronousQueueBalance",
            type:"GET",
            dataType: "json"
        },
        parameterMap: function(options, operation) {
            if (operation !== "read" && options.models) {
                return { models: kendo.stringify(options.models) };
            }
        }
    },
    schema: {
        model: {
            id: "entityAnalysisAsynchronousQueueBalanceId",
            fields: {
                instance: { type: "string" },
                createdDate: { type: "date" },
                tagging: { type: "number" },
                asynchronousEntityInvoke: { type: "number" }
            }
        }
    }
});

$(document).ready(function() {
    $("#grid").kendoGrid({
        groupable: true,
        dataSource: dataSourceEntity,
        pageable: false,
        height: $(window).height() - 210,
        scrollable: true,
        filterable: true,
        dataBound: function() {
            for (let i = 0; i < this.columns.length; i++) {
                this.autoFitColumn(i);
            }
        },
        columns: [
            { field: "instance", title: "Instance" },
            { field: "createdDate", title: "Created Date" },
            { field: "tagging", title: "Tagging" },
            { field: "asynchronousEntityInvoke", title: "Asynchronous Model Invoke" }
        ]
    });
});

//# sourceURL=EntityAnalysisAsynchronousQueueBalances.js