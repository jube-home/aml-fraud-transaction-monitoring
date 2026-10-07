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

const endpoint = "/api/EntityAnalysisModelList";
const endpointValues = "/api/EntityAnalysisModelListValue";
const parentKeyName = "entityAnalysisModelGuid";
const validationFail = "There is invalid data in the form. Please check fields and correct.";

const addValue = $("#AddValue").kendoButton().click(function (e) {
    $("#valueGrid").data("kendoGrid").addRow();
    e.preventDefault();
});

function LoadFiles() {
    $("#Files").kendoUpload({
        async: {
            saveUrl: "/api/EntityAnalysisModelListCsvFileUpload",
            autoUpload: true,
            multiple: false
        },
        success: onSuccess,
        upload: function (e) {
            e.data = {entityAnalysisModelListId: id};
        }
    });
    $("#FilesDiv").show();
}

function onSuccess() {
    $("#valueGrid").data("kendoGrid").dataSource.data([]);
    LoadList();
}

function LoadList() {
    createValueGrid({
        readUrl: endpointValues + "/ByEntityAnalysisModelListId",
        readParameter: "entityAnalysisModelListId",
        endpoint: endpointValues,
        parentField: "entityAnalysisModelListId",
        parentId: id,
        parentKind: 2,
        title: "List value",
        valueFields: [{field: "listValue", title: "Value", type: "string"}]
    });

    $("#ListValuesDiv").show();
}

if (typeof id === "undefined") {
    addValue.hide();
    $("#FilesDiv").hide();
    $("#ListValuesDiv").hide();
    ReadyNew();
} else {
    $.get(endpoint + "/" + id,
        function (data) {
            ReadyExisting(data);
            LoadFiles();
            LoadList();
        });
}

$(function () {
    deleteButton
        .click(function () {
            if (confirm('Are you sure you want to delete?')) {
                Delete(endpoint, id);
            }
        });
});

$(function () {
    addButton
        .click(function () {
            if (validator.validate()) {
                let data = {};

                Create(endpoint, data, "id", parentKeyName);

                LoadFiles();
                LoadList();
                addValue.show();
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

$(function () {
    updateButton
        .click(function () {
            if (validator.validate()) {
                let data = {};

                Update(endpoint, data, "id", parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

//# sourceURL=EntityAnalysisModelList.js