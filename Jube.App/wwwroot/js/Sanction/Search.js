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

var dataSourceCases = new kendo.data.DataSource({
    transport: {
        read: {
            url: "../api/Invoke/Sanction",
            data: {
                multiPartString: "",
                distance: 0
            },
            dataType: "json"
        }
    },
    batch: true,
    pageSize: 20,
    schema: {
        model: {
            id: "id",
            fields: {
                id: {type: "number"},
                distance: {type: "number"},
                value: {type: "string"},
                source: {type: "string"},
                reference: {type: "string"}
            }
        }
    }
});

$(document).ready(function () {
    $("#Distance").kendoSlider({
        increaseButtonTitle: "Right",
        decreaseButtonTitle: "Left",
        value: 2,
        min: 0,
        max: 5,
        smallStep: 1,
        largeStep: 1
    });

    $("#Sanctions").kendoGrid({
        dataSource: dataSourceCases,
        pageable: false,
        height: 500,
        autoBind: false,
        columns: [
            {field: "id", title: "Id"},
            {field: "distance", title: "Distance"},
            {field: "value", title: "Element"},
            {field: "source", title: "Source"},
            {field: "reference", title: "Reference"}
        ]
    });

    $("#Check").kendoButton({
        click: function () {
            const grid = $('#Sanctions').data('kendoGrid');
            grid.dataSource.options.transport.read.data.multiPartString = $("#MultiPartString").val();
            grid.dataSource.options.transport.read.data.distance = $("#Distance").data("kendoSlider").value();
            grid.dataSource.read();
        }
    });
});
