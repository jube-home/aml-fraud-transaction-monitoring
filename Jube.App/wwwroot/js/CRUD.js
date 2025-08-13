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

var processingFailed = "Processing failed.  Please contact Support to check logs for the source of the error.";
var keyNotFound = "Attempted to update something that does not exist.";
var id;
var hasResponsePayload;
var hasReportTable;
var parentKey;
var active = $("#Active").kendoSwitch();
var locked = $("#Locked").kendoSwitch();
var responsePayload = $("#ResponsePayload");
var deleteButton = $("#Delete").kendoButton();
var addButton = $("#Add").kendoButton();
var updateButton = $("#Update").kendoButton();
var validator = $("#Form").kendoValidator({validateOnBlur: false}).data("kendoValidator");
var reportTable = $("#ReportTable");

if (typeof GetSelectedChildID !== "undefined") {
    id = GetSelectedChildID();
}

if (typeof GetSelectedParentID !== "undefined") {
    parentKey = GetSelectedParentID();
}

if (responsePayload.length > 0) {
    responsePayload.kendoSwitch();
    hasResponsePayload = true;
}

if (reportTable.length > 0) {
    reportTable.kendoSwitch();
    hasReportTable = true;
}

function AddTemplateElements(data, keyName, parentKeyName) {
    data["active"] = active.prop("checked");
    data["locked"] = locked.prop("checked");

    if (hasResponsePayload) {
        data["responsePayload"] = responsePayload.prop("checked");
    }

    if (hasReportTable) {
        data["reportTable"] = reportTable.prop("checked");
    }

    const name = $("#Name");
    if (name.length > 0) {
        data["name"] = name.val();
    }

    data["id"] = id;

    if (typeof parentKeyName !== "undefined") {
        if (typeof data[parentKeyName] === "undefined") {
            data[parentKeyName] = parentKey;
        }
    }

    return data;
}

function DisplayServerValidationErrors(responseObject) {
    let errorMessage = $("#ErrorMessage");
    errorMessage.html("Server validation errors occured:").append('<br/>')
    let list = errorMessage.append("<ul>");
    for (let key in responseObject.errors) {
        list.append('<li>' + responseObject.errors[key].propertyName + ": " + responseObject.errors[key].errorMessage + '.</li>')
    }
}

function Create(endpoint, data, keyName, parentKeyName, callback) {
    $("#ErrorMessage").html('');
    $.ajax({
        url: endpoint,
        type: "POST",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify(AddTemplateElements(data, keyName, parentKeyName)),
        error: function (jqXHR, textStatus, errorThrown) {
            if (jqXHR.status === 400) {
                let responseObject = jQuery.parseJSON(jqXHR.responseText);
                DisplayServerValidationErrors(responseObject);
            }
            if (jqXHR.status === 204) {
                $("#ErrorMessage").html(keyNotFound);
            } else {
                $("#ErrorMessage").html(processingFailed);
            }
        },
        success: function (data) {
            if (typeof parentKeyName !== "undefined") {
                parentKey = data[parentKeyName];
            }

            id = data["id"];

            if (data.version === 1) {
                if (typeof AddNode !== "undefined") {
                    if ($("#Name").length > 0) {
                        AddNode(data[parentKeyName], id, data.name);
                    } else {
                        AddNode(data[parentKeyName], id, Name);
                    }
                }
            }

            addButton.hide();
            updateButton.show();
            deleteButton.show();

            const guid = $("#Guid");
            if (typeof guid !== "undefined") {
                guid.html(data.guid);
            }

            $("#Version").html(data.version);
            $("#CreatedUser").html(data.createdUser);
            $("#CreatedDate").html(new Date(data.createdDate).toLocaleString());

            SetTable();

            if (locked.prop('checked')) {
                Lock(true);
            } else {
                Lock(false);
            }

            if (typeof callback !== "undefined") {
                callback(data);
            }
        }
    });
}

function Update(endpoint, data, keyName, parentKeyName) {
    $("#ErrorMessage").html('');
    $.ajax({
        url: endpoint,
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify(AddTemplateElements(data, keyName, parentKeyName)),
        error: function (jqXHR, textStatus, errorThrown) {
            if (jqXHR.status === 400) {
                let responseObject = jQuery.parseJSON(jqXHR.responseText);
                DisplayServerValidationErrors(responseObject);
            } else {
                $("#ErrorMessage").html(processingFailed);
            }
        },
        success: function (data) {
            id = data["id"];

            $("#Version").html(data.version);
            $("#CreatedUser").html(data.createdUser);
            $("#CreatedDate").html(new Date(data.createdDate).toLocaleString());

            addButton.hide();
            updateButton.show();
            deleteButton.show();

            if (locked.prop('checked')) {
                Lock(true);
            } else {
                Lock(false);
            }

            if (typeof callback !== "undefined") {
                callback(data);
            }
        }
    });
}

function Delete(endpoint, key) {
    $("#ErrorMessage").html('');
    $.ajax({
        url: endpoint + "/" + key,
        type: "DELETE",
        error: function () {
            $("#ErrorMessage").html(processingFailed);
        },
        success: function () {
            if (typeof DeleteNode !== "undefined") {
                DeleteNode(key, 1);
            } else {
                if (typeof showHomePage !== "undefined") {
                    showHomePage();
                }
            }
        }
    });
}

function SetTable() {
    let rows = $('#TemplateTable tr').length;

    if ($("#Guid").length > 0) {
        rows = rows - 5;
    } else {
        rows = rows - 4;
    }

    if (typeof id !== "undefined") {
        $("#TemplateTable tr:gt(" + rows + ")").show();
    } else {
        $("#TemplateTable tr:gt(" + rows + ")").hide();
    }
}

function Lock(locked) {
    if (locked) {
        deleteButton.hide();
        updateButton.hide();
    } else {
        deleteButton.show();
        updateButton.show();
    }
}

function ReadyNew() {
    id = (function () {
    })(); //undefined.
    $("#Name").val("");
    $("#Version").html("");
    $("#CreatedDate").html("");
    $("#CreatedUser").html("");

    updateButton.hide();
    deleteButton.hide();
    addButton.show();

    active.data("kendoSwitch").check(false);

    if (locked.length > 0) {
        locked.data("kendoSwitch").check(false);
        locked.data("kendoSwitch").enable(false);
    }

    SetTable();
}

function ReadyExisting(data) {
    id = data.id;
    $("#Name").val(data.name);

    if (hasResponsePayload) {
        if (data.responsePayload) {
            responsePayload.data("kendoSwitch").check(true);
        } else {
            responsePayload.data("kendoSwitch").check(false);
        }
    }

    if (hasReportTable) {
        if (data.reportTable) {
            reportTable.data("kendoSwitch").check(true);
        } else {
            reportTable.data("kendoSwitch").check(false);
        }
    }

    if (data.active) {
        active.data("kendoSwitch").check(true);
    } else {
        active.data("kendoSwitch").check(false);
    }

    const guid = $("#Guid");
    if (typeof guid !== "undefined") {
        guid.html(data.guid);
    }

    $("#Version").html(data.version);
    $("#CreatedDate").html(new Date(data.createdDate).toLocaleString());
    $("#CreatedUser").html(data.createdUser);

    SetTable();

    addButton.hide();
    updateButton.show();
    deleteButton.show();

    if (locked.length > 0) {
        if (data.locked) {
            locked.data("kendoSwitch").check(true);
            Lock(true);
        } else {
            locked.data("kendoSwitch").check(false);
            Lock(false);
        }
        locked.data("kendoSwitch").enable(false);
    }
}

//# sourceURL=CRUD.js