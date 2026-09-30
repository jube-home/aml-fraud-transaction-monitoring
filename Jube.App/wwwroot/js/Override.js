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

let key;
let keyValue;

const processingFailed = "Processing failed.  Please contact Support to check logs for the source of the error.";
const permissionRefused = "You do not have permission to do that.  Applying a Force override needs the Apply Force Override permission, which is separate from the ordinary override permission.";

const overrideKinds = [{value: 0, text: "Suppress"}, {value: 1, text: "Force"}];

function OverrideKindDropDown(element, dataItem, enabled, onChanged, allowForce) {
    const kinds = allowForce === false && dataItem.overrideKind !== 1
        ? overrideKinds.filter(function (kind) {
            return kind.value !== 1;
        })
        : overrideKinds;

    let previous = dataItem.overrideKind === undefined ? 0 : dataItem.overrideKind;

    const dropDown = element.kendoDropDownList({
        dataTextField: "text",
        dataValueField: "value",
        dataSource: kinds,
        value: dataItem.overrideKind === undefined ? 0 : dataItem.overrideKind,
        change: function (e) {
            const chosen = parseInt(e.sender.value(), 10);
            onChanged(chosen, previous);
            previous = chosen;
        }
    }).data("kendoDropDownList");

    dropDown.enable(enabled);
    return dropDown;
}

function SyncOverrideKindDropDown(dropDown, enabled) {
    if (!dropDown) {
        return;
    }

    if (!enabled) {
        dropDown.value(0);
    }

    dropDown.enable(enabled);
}

function onChange() {
    const grid = $("#grid").getKendoGrid();
    grid.dataSource.sync();
}

function ExistsSelect(test) {
    let exists = false;
    $($("#OverrideKey").data("kendoDropDownList").dataItems()).each(function () {
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
                    url: "../api/GetEntityAnalysisModelActivationRuleOverrideQuery",
                    data: {
                        overrideKey: key,
                        overrideKeyValue: keyValue,
                        entityAnalysisModelGuid: e.data.entityAnalysisModelGuid
                    },
                    dataType: "json"
                }
            },
            schema: {
                model: {
                    id: "id",
                    fields: {
                        hasOverride: {type: "boolean"},
                        name: {type: "string", editable: false},
                        enableForce: {type: "boolean"},
                        overrideKind: {type: "number", defaultValue: 0},
                        deleteExpiryDate: {type: "date", defaultValue: null}
                    }
                }
            }
        },
        dataBound: function () {
            const grid = this;

            grid.tbody.find("tr").each(function () {
                const row = $(this);
                const toggleSwitchElement = row.find(".toggleOverrideActivationRule");
                const dateInputElement = row.find(".expiryDateOverrideActivationRule");

                if (!toggleSwitchElement.length) {
                    return;
                }

                const dataItem = grid.dataItem(row);

                const datePicker = dateInputElement.kendoDateTimePicker({
                    value: dataItem.deleteExpiryDate,
                    change: function (e) {
                        const newValue = e.sender.value();
                        const $errorMessage = $("#ErrorMessage");
                        if (!IsFutureDate(newValue)) {
                            $errorMessage.html(
                                '<div class="server-error-box"><div class="server-error-title">Validation Errors:</div>' +
                                '<div class="server-error-line">Delete Expiry Date must be greater than the current date and time.</div></div>');
                            e.sender.value(dataItem.deleteExpiryDate);
                            return;
                        }
                        $errorMessage.empty();
                        UpdateOverrideActivationRuleDeleteExpiryDate(dataItem.entityAnalysisModelGuid,
                            dataItem.name, newValue, e.sender, dataItem.deleteExpiryDate);
                    }
                }).data("kendoDateTimePicker");

                datePicker.enable(dataItem.hasOverride === true);

                const overrideKindDropDown = OverrideKindDropDown(row.find(".overrideKindActivationRule"),
                    dataItem, dataItem.hasOverride === true, function (overrideKind, previousKind) {
                        UpdateOverrideActivationRuleKind(dataItem.entityAnalysisModelGuid,
                            dataItem.name, overrideKind, row.find(".overrideKindActivationRule"), previousKind);
                    }, dataItem.enableForce === true);

                toggleSwitchElement.kendoSwitch({
                    change: function (e) {
                        SyncExpiryDatePicker(datePicker, e.checked);
                        SyncOverrideKindDropDown(overrideKindDropDown, e.checked);
                        UpdateOverrideActivationRule(toggleSwitchElement.attr("EntityAnalysisModelGuid"),
                            toggleSwitchElement.attr("Name"), e.checked);
                    }
                });
            });
        },
        columns: [
            {
                field: "hasOverride",
                template:
                    '<input Name="#=name#" EntityAnalysisModelGuid="#=entityAnalysisModelGuid#" type="checkbox" class="toggleOverrideActivationRule" #= (hasOverride==true) ? checked="checked" : "" # />',
                width: 110,
                title: "Override"
            },
            {
                field: "overrideKind",
                title: "Kind",
                width: 150,
                template: '<input type="text" class="overrideKindActivationRule" />'
            },
            {
                field: "deleteExpiryDate",
                title: "Delete Expiry Date",
                width: 230,
                template: '<input type="text" class="expiryDateOverrideActivationRule expiryDateCell" />'
            },
            {
                field: "name",
                title: "Activation Rule"
            }
        ]
    });
}

function IsFutureDate(value) {
    return !value || value > new Date();
}

function SyncExpiryDatePicker(picker, enabled) {
    if (!picker) {
        return;
    }

    picker.value(null);
    picker.enable(enabled);
}

function DisplayValidationErrors(jqXHR) {
    const $errorContainer = $("#ErrorMessage");
    $errorContainer.empty();

    if (jqXHR.status === 401 || jqXHR.status === 403) {
        $errorContainer.html(
            '<div class="server-error-box"><div class="server-error-title">Refused:</div>' +
            '<div class="server-error-line">' + permissionRefused + '</div></div>');
        return;
    }

    if (jqXHR.status !== 400) {
        $errorContainer.html(processingFailed);
        return;
    }

    try {
        const response = JSON.parse(jqXHR.responseText);
        const $container = $(
            '<div class="server-error-box"><div class="server-error-title">Validation Errors:</div></div>');

        if (response.errors) {
            Object.values(response.errors).forEach(function (e) {
                $container.append('<div class="server-error-line">' + e.errorMessage + '</div>');
            });
        }

        $errorContainer.append($container);
    } catch (e) {
        $errorContainer.html(processingFailed);
    }
}

function UpdateOverrideModel(EntityAnalysisModelGuid, checked) {
    $('#Updating').show();
    $.ajax({
        url: "/api/EntityAnalysisModelOverride",
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            entityAnalysisModelGuid: EntityAnalysisModelGuid,
            overrideKeyValue: keyValue,
            overrideKey: key,
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

function UpdateOverrideActivationRule(EntityAnalysisModelGuid, name, checked) {
    $('#Updating').show();
    $.ajax({
        url: "/api/EntityAnalysisModelActivationRuleOverride",
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            entityAnalysisModelGuid: EntityAnalysisModelGuid,
            overrideKeyValue: keyValue,
            overrideKey: key,
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

function UpdateOverrideActivationRuleKind(EntityAnalysisModelGuid, name, overrideKind, element, previousKind) {
    $('#Updating').show();
    $.ajax({
        url: "/api/EntityAnalysisModelActivationRuleOverride/OverrideKind",
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            entityAnalysisModelGuid: EntityAnalysisModelGuid,
            overrideKeyValue: keyValue,
            overrideKey: key,
            overrideKind: overrideKind,
            entityAnalysisModelActivationRuleName: name
        }),
        error: function (jqXHR) {
            $('#Updating').fadeOut();
            DisplayValidationErrors(jqXHR);

            const dropDown = element ? element.data("kendoDropDownList") : null;
            if (dropDown) {
                dropDown.value(previousKind === undefined ? 0 : previousKind);
            }
        },
        success: function () {
            $('#Updating').fadeOut();
            $("#ErrorMessage").empty();
        }
    });
}

function UpdateOverrideModelDeleteExpiryDate(EntityAnalysisModelGuid, deleteExpiryDate, picker, previousValue) {
    $('#Updating').show();
    $.ajax({
        url: "/api/EntityAnalysisModelOverride/DeleteExpiryDate",
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            entityAnalysisModelGuid: EntityAnalysisModelGuid,
            overrideKeyValue: keyValue,
            overrideKey: key,
            deleteExpiryDate: deleteExpiryDate
        }),
        error: function (jqXHR) {
            $('#Updating').fadeOut();
            DisplayValidationErrors(jqXHR);
            if (picker) {
                picker.value(previousValue || null);
            }
        },
        success: function () {
            $('#Updating').fadeOut();
            $("#ErrorMessage").empty();
        }
    });
}

function UpdateOverrideActivationRuleDeleteExpiryDate(EntityAnalysisModelGuid, name, deleteExpiryDate, picker,
                                                      previousValue) {
    $('#Updating').show();
    $.ajax({
        url: "/api/EntityAnalysisModelActivationRuleOverride/DeleteExpiryDate",
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            entityAnalysisModelGuid: EntityAnalysisModelGuid,
            overrideKeyValue: keyValue,
            overrideKey: key,
            deleteExpiryDate: deleteExpiryDate,
            entityAnalysisModelActivationRuleName: name
        }),
        error: function (jqXHR) {
            $('#Updating').fadeOut();
            DisplayValidationErrors(jqXHR);
            if (picker) {
                picker.value(previousValue || null);
            }
        },
        success: function () {
            $('#Updating').fadeOut();
            $("#ErrorMessage").empty();
        }
    });
}

function Fetch() {
    keyValue = $("#OverrideKeyValue").val();
    key = $("#OverrideKey").data("kendoDropDownList").value();

    $("#ErrorMessage").empty();

    if (!key) {
        ShowKeysGrid();
        return;
    }

    if (!keyValue) {
        ShowValuesGrid();
        return;
    }

    ShowModelsGrid();

    const grid = $('#grid').data('kendoGrid');
    grid.dataSource.options.transport.read.data.OverrideKeyValue = keyValue;
    grid.dataSource.options.transport.read.data.OverrideKey = key;
    grid.dataSource.read();
}

function ShowModelsGrid() {
    $("#keysGrid").attr("hidden", "hidden");
    $("#valuesGrid").attr("hidden", "hidden");
    $("#grid").removeAttr("hidden");
}

function ShowValuesGrid() {
    $("#keysGrid").attr("hidden", "hidden");
    $("#grid").attr("hidden", "hidden");
    $("#valuesGrid").removeAttr("hidden");

    const valuesGrid = $("#valuesGrid").data("kendoGrid");
    valuesGrid.dataSource.options.transport.read.data.overrideKey = key;
    valuesGrid.dataSource.read();
}

function ShowKeysGrid() {
    $("#grid").attr("hidden", "hidden");
    $("#valuesGrid").attr("hidden", "hidden");
    $("#keysGrid").removeAttr("hidden");
    $("#keysGrid").data("kendoGrid").dataSource.read();
}

function DrillIntoKey(overrideKey) {
    key = overrideKey;
    keyValue = "";
    $("#OverrideKeyValue").val("");
    $("#OverrideKey").data("kendoDropDownList").value(overrideKey);
    $("#ErrorMessage").empty();
    ShowValuesGrid();
}

function OverrideKindText(overrideKind) {
    return overrideKind === 1 ? "Force" : "Suppress";
}

$(document).ready(function () {
    $("#Fetch").kendoButton({
        click: Fetch
    });

    $("#OverrideKey").kendoDropDownList({
        dataTextField: "text",
        dataValueField: "value"
    });

    $.get("/api/EntityAnalysisModelRequestXPath/ByOverrideKey",
        function (data) {
            $.each(data,
                function (i, value) {
                    if (ExistsSelect(value.name) === false) {
                        $("#OverrideKey").getKendoDropDownList().dataSource.add({
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
                    url: "../api/GetEntityAnalysisModelOverrideQuery",
                    data: {overrideKeyValue: keyValue, overrideKey: key},
                    dataType: "json"
                }
            },
            schema: {
                model: {
                    id: "id",
                    fields: {
                        hasOverride: {type: "boolean"},
                        name: {type: "string", editable: false},
                        overrideKind: {type: "number", defaultValue: 0},
                        deleteExpiryDate: {type: "date", defaultValue: null}
                    }
                }
            }
        },
        height: 600,
        sortable: true,
        autoBind: false,
        detailInit: detailInit,
        dataBound: function () {
            const grid = this;

            grid.tbody.find("tr").each(function () {
                const row = $(this);
                const toggleSwitchElement = row.find(".toggleOverride");
                const dateInputElement = row.find(".expiryDateOverride");

                if (!toggleSwitchElement.length) {
                    return;
                }

                const dataItem = grid.dataItem(row);

                const datePicker = dateInputElement.kendoDateTimePicker({
                    value: dataItem.deleteExpiryDate,
                    change: function (e) {
                        const newValue = e.sender.value();
                        const $errorMessage = $("#ErrorMessage");
                        if (!IsFutureDate(newValue)) {
                            $errorMessage.html(
                                '<div class="server-error-box"><div class="server-error-title">Validation Errors:</div>' +
                                '<div class="server-error-line">Delete Expiry Date must be greater than the current date and time.</div></div>');
                            e.sender.value(dataItem.deleteExpiryDate);
                            return;
                        }
                        $errorMessage.empty();
                        UpdateOverrideModelDeleteExpiryDate(dataItem.entityAnalysisModelGuid, newValue,
                            e.sender, dataItem.deleteExpiryDate);
                    }
                }).data("kendoDateTimePicker");

                datePicker.enable(dataItem.hasOverride === true);

                toggleSwitchElement.kendoSwitch({
                    change: function (e) {
                        SyncExpiryDatePicker(datePicker, e.checked);
                        UpdateOverrideModel(toggleSwitchElement.attr("EntityAnalysisModelGuid"), e.checked);
                    }
                });
            });
        },
        columns: [
            {
                field: "hasOverride",
                template:
                    '<input EntityAnalysisModelGuid="#=entityAnalysisModelGuid#" type="checkbox" class="toggleOverride" #= (hasOverride==true) ? checked="checked" : "" # />',
                width: 110,
                title: "Override"
            },
            {
                field: "overrideKind",
                title: "Kind",
                width: 150,
                template: '#= OverrideKindText(overrideKind) #'
            },
            {
                field: "deleteExpiryDate",
                title: "Delete Expiry Date",
                width: 230,
                template: '<input type="text" class="expiryDateOverride expiryDateCell" />'
            },
            {
                field: "name",
                title: "Model"
            }
        ]
    });

    // noinspection JSObsoletePrivateAccessSyntax
    $("#keysGrid").kendoGrid({
        dataSource: {
            transport: {
                read: {
                    url: "../api/GetEntityAnalysisModelOverrideQuery/Keys",
                    dataType: "json"
                }
            },
            schema: {
                model: {
                    fields: {
                        overrideKey: {type: "string"},
                        enabledOnModels: {type: "number"},
                        values: {type: "number"},
                        overrides: {type: "number"},
                        forced: {type: "number"},
                        activationRules: {type: "number"},
                        allActivationRules: {type: "number"},
                        nextExpiryDate: {type: "date", defaultValue: null},
                        lastCreatedDate: {type: "date", defaultValue: null}
                    }
                }
            }
        },
        height: 600,
        sortable: true,
        filterable: true,
        autoBind: true,
        noRecords: {
            template: "No Request XPath on any Model has Enable Override switched on."
        },
        columns: [
            {
                field: "overrideKey",
                title: "Override Key"
            },
            {
                field: "values",
                title: "Values",
                width: 110
            },
            {
                field: "overrides",
                title: "Overrides",
                width: 120
            },
            {
                field: "forced",
                title: "Forced",
                width: 110
            },
            {
                field: "activationRules",
                title: "Activation Rules",
                width: 160
            },
            {
                field: "allActivationRules",
                title: "All Rules",
                width: 120
            },
            {
                field: "enabledOnModels",
                title: "Models",
                width: 110
            },
            {
                field: "lastCreatedDate",
                title: "Last Created",
                width: 190,
                format: "{0:yyyy-MM-dd HH:mm}"
            },
            {
                field: "nextExpiryDate",
                title: "Next Expiry",
                width: 190,
                format: "{0:yyyy-MM-dd HH:mm}"
            }
        ],
        change: function () {
            const selected = this.dataItem(this.select());
            if (!selected) {
                return;
            }

            this.clearSelection();
            DrillIntoKey(selected.overrideKey);
        },
        selectable: "row"
    });

    // noinspection JSObsoletePrivateAccessSyntax
    $("#valuesGrid").kendoGrid({
        dataSource: {
            transport: {
                read: {
                    url: "../api/GetEntityAnalysisModelOverrideQuery/Values",
                    data: {overrideKey: key},
                    dataType: "json"
                }
            },
            schema: {
                model: {
                    fields: {
                        overrideKeyValue: {type: "string"},
                        name: {type: "string"},
                        overrideKind: {type: "number"},
                        createdUser: {type: "string"},
                        createdDate: {type: "date"},
                        deleteExpiryDate: {type: "date", defaultValue: null}
                    }
                }
            }
        },
        height: 600,
        sortable: true,
        filterable: true,
        autoBind: false,
        noRecords: {
            template: "There are no values held against this override key."
        },
        columns: [
            {
                field: "overrideKeyValue",
                title: "Override Key Value"
            },
            {
                field: "name",
                title: "Model"
            },
            {
                field: "overrideKind",
                title: "Kind",
                width: 130,
                template: '#= OverrideKindText(overrideKind) #'
            },
            {
                field: "createdUser",
                title: "Created User",
                width: 180
            },
            {
                field: "createdDate",
                title: "Created Date",
                width: 200,
                format: "{0:yyyy-MM-dd HH:mm}"
            },
            {
                field: "deleteExpiryDate",
                title: "Delete Expiry Date",
                width: 200,
                format: "{0:yyyy-MM-dd HH:mm}"
            }
        ],
        change: function () {
            const selected = this.dataItem(this.select());
            if (!selected) {
                return;
            }

            this.clearSelection();
            $("#OverrideKeyValue").val(selected.overrideKeyValue);
            Fetch();
        },
        selectable: "row"
    });
});

//# sourceURL=Override.js