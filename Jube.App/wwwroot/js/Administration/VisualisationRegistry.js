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

var endpoint = "/api/VisualisationRegistry";
var childKeyName = "visualisationRegistryGuid";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

function getRecords() {
    $("#Visualisations").html("");

    $.get("/api/VisualisationRegistry",
        function (data) {
            for (const value of data) {
                $("#Visualisations").prepend("<a href='#' onclick='loadTemplate(" + value.id + ");'>" + value.name + "</a></br>");
            }
        }
    );
}

function loadTemplate(id) {
    $.get(endpoint + "/" + id,
        function (data) {
            key = data["id"];

            $("#ContextKindId").data("kendoDropDownList").value(data.contextKindId || 1);

            $("#Columns").data("kendoNumericTextBox").value(data.columns);
            $("#ColumnWidth").data("kendoNumericTextBox").value(data.columnWidth);
            $("#RowHeight").data("kendoNumericTextBox").value(data.rowHeight);

            ReadyExisting(data);
            window.AskJoobySetPageContext?.("VisualisationRegistry", "edit", {
                id: data.id,
                guid: data.guid,
                name: data.name
            });
            validator.validate();

            $("#Template").show();
            $("#Homepage").hide();
        });
}

function showTemplate() {
    $("#ContextKindId").data("kendoDropDownList").value(1);
    $("#Columns").data("kendoNumericTextBox").value(0);
    $("#ColumnWidth").data("kendoNumericTextBox").value(0);
    $("#RowHeight").data("kendoNumericTextBox").value(0);

    ReadyNew();
    window.AskJoobySetPageContext?.("VisualisationRegistry", "new", null);
    $("#Template").show();
    $("#Homepage").hide();
}

function showHomePage() {
    window.AskJoobySetPageContext?.("VisualisationRegistry", "list", null);
    $("#Template").hide();
    $("#Homepage").show();
    getRecords();
}

$(document).ready(function () {
    showHomePage();

    $.getScript('/js/CRUD.js', function () {
        $("#Back").kendoButton({
            click: function () {
                showHomePage();
                clearFieldErrorStyles();
                hideRoles();
            }
        });

        var contextKindId = $("#ContextKindId").kendoDropDownList({
            dataSource: [{text: "Directory", value: 1}, {text: "Case", value: 2}],
            dataTextField: "text",
            dataValueField: "value"
        });

        var columns = $("#Columns").kendoNumericTextBox({
            format: "#"
        });

        var rowHeight = $("#RowHeight").kendoNumericTextBox({
            format: "#"
        });

        var columnWidth = $("#ColumnWidth").kendoNumericTextBox({
            format: "#"
        });

        $(function () {
            deleteButton
                .click(function () {
                    if (confirm('Are you sure you want to delete?')) {
                        Delete(endpoint, key);
                    }
                });
        });

        function GetData() {
            return {
                contextKindId: Number(contextKindId.data("kendoDropDownList").value()),
                rowHeight: rowHeight.val(),
                columnWidth: columnWidth.val(),
                columns: columns.val()
            };
        }

        $(function () {
            addButton
                .click(function () {
                    if (validator.validate()) {
                        Create(endpoint, GetData(), "id");
                    } else {
                        $("#ErrorMessage").html(validationFail);
                    }
                });
        });

        $(function () {
            updateButton
                .click(function () {
                    if (validator.validate()) {
                        Update(endpoint, GetData(), "id");
                    } else {
                        $("#ErrorMessage").html(validationFail);
                    }
                });
        });
    });

    $("#Template").hide();
    $("#Homepage").show();

    $("#New").kendoButton({
        click: function () {
            showTemplate();
        }
    });
});

//# sourceURL=VisualisationRegistry.js