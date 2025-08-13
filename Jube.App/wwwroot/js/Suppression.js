// noinspection ES6ConvertVarToLetConst,JSUnresolvedVariable,HtmlUnknownAttribute

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

// noinspection ES6ConvertVarToLetConst

let key;
let keyValue;

function onChange() {
    const grid = $("#grid").getKendoGrid();
    grid.dataSource.sync();
}

function ExistsSelect(test) {
    let exists = false;
    $($("#SuppressionKey").data("kendoDropDownList").dataItems()).each(function () {
        if (this.value === test) {
            exists = true;
        }
    });
    return exists;
}

function detailInit(e) {
    // noinspection JSObsoletePrivateAccessSyntax
    $("<div/>").appendTo(e.detailCell).kendoGrid({
        dataSource: {
            transport: {
                read: {
                    url: "../api/GetEntityAnalysisModelActivationsRuleSuppressionQuery",
                    data: {
                        suppressionKey: key,
                        suppressionKeyValue: keyValue,
                        entityAnalysisModelGuid: e.data.entityAnalysisModelGuid
                    },
                    dataType: "json"
                }
            },
            schema: {
                model: {
                    id: "id",
                    fields: {
                        suppression: {type: "boolean"},
                        name: {type: "string", editable: false}
                    }
                }
            }
        },
        dataBound: function () {
            $(".toggleSuppressionActivationRule").each(function () {
                const toggleSwitch = $(this);
                toggleSwitch.kendoSwitch({
                    change: function (e) {
                        UpdateSuppressionActivationRule(e.sender.element.attr("EntityAnalysisModelGuid"),
                            e.sender.element.attr("Name"), e.checked);
                    }
                });
            });
        },
        columns: [
            {
                field: "suppression",
                template:
                    '<input Name="#=name#" EntityAnalysisModelGuid="#=entityAnalysisModelGuid#" type="checkbox" class="toggleSuppressionActivationRule" #= (suppression==true) ? checked="checked" : "" # />',
                width: 97,
                title: "Suppression"
            },
            {
                field: "name",
                title: "Activation Rule"
            }
        ]
    });
}

function UpdateSuppressionModel(EntityAnalysisModelGuid, checked) {
    $('#Updating').show();
    $.ajax({
        url: "/api/EntityAnalysisModelSuppression",
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            entityAnalysisModelGuid: EntityAnalysisModelGuid,
            suppressionKeyValue: keyValue,
            suppressionKey: key,
            active: checked
        }),
        error: function () {
            $('#Updating').fadeOut();
        },
        success: function () {
            $('#Updating').fadeOut();
        }
    });
}

function UpdateSuppressionActivationRule(EntityAnalysisModelGuid, name, checked) {
    $('#Updating').show();
    $.ajax({
        url: "/api/EntityAnalysisModelActivationRuleSuppression",
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            entityAnalysisModelGuid: EntityAnalysisModelGuid,
            suppressionKeyValue: keyValue,
            suppressionKey: key,
            active: checked,
            entityAnalysisModelActivationRuleName: name
        }),
        error: function () {
            $('#Updating').fadeOut();
        },
        success: function () {
            $('#Updating').fadeOut();
        }
    });
}

$(document).ready(function () {
    $("#Fetch").kendoButton({
        click: function () {
            {
                keyValue = $("#SuppressionKeyValue").val();
                key = $("#SuppressionKey").data("kendoDropDownList").value();

                const grid = $('#grid').data('kendoGrid');
                grid.dataSource.options.transport.read.data.SuppressionKeyValue = keyValue;
                grid.dataSource.options.transport.read.data.SuppressionKey = key;
                grid.dataSource.read();
            }
        }
    });

    $("#SuppressionKey").kendoDropDownList({
        dataTextField: "text",
        dataValueField: "value"
    });

    $.get("/api/EntityAnalysisModelRequestXPath/BySuppressionKey",
        function (data) {
            $.each(data,
                function (i, value) {
                    if (ExistsSelect(value.name) === false) {
                        $("#SuppressionKey").getKendoDropDownList().dataSource.add({
                            "value": value.name,
                            "text": value.name
                        });
                    }
                });
        }
    );

    // noinspection JSObsoletePrivateAccessSyntax
    $("#grid").kendoGrid({
        dataSource: {
            transport: {
                read: {
                    url: "../api/GetEntityAnalysisModelSuppressionQuery",
                    data: {suppressionKeyValue: keyValue, suppressionKey: key},
                    dataType: "json"
                }
            },
            schema: {
                model: {
                    id: "id",
                    fields: {
                        suppression: {type: "boolean"},
                        name: {type: "string", editable: false}
                    }
                }
            }
        },
        height: 600,
        sortable: true,
        autoBind: false,
        detailInit: detailInit,
        dataBound: function () {
            $(".toggleSuppression").each(function () {
                const toggleSwitch = $(this);
                toggleSwitch.kendoSwitch({
                    change: function (e) {
                        UpdateSuppressionModel(e.sender.element.attr("EntityAnalysisModelGuid"), e.checked);
                    }
                });
            });
        },
        columns: [
            {
                field: "suppression",
                template:
                    '<input EntityAnalysisModelGuid="#=entityAnalysisModelGuid#" type="checkbox" class="toggleSuppression" #= (suppression==true) ? checked="checked" : "" # />',
                width: 120,
                title: "Suppression"
            },
            {
                field: "name",
                title: "Model"
            }
        ]
    });
});

//# sourceURL=Suppression.js