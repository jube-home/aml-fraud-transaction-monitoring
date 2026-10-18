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

var endpoint = "/api/EntityAnalysisModelInlineScript";
var keyName = "entityAnalysisModelInlineScriptId";
var parentKeyName = "entityAnalysisModelId";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var inlineScript = $("#InlineScript").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

$.get("/api/EntityAnalysisInlineScript",
    function (data) {
        for (const value of data) {
            inlineScript.getKendoDropDownList().dataSource.add({
                "value": value.id,
                "text": value.name
            });
        }

        if (typeof id === "undefined") {
            ReadyNew();
            window.AskJoobySetPageContext?.("EntityAnalysisModelInlineScript", "new", {[parentKeyName]: parentKey});
        } else {
            $.get(endpoint + "/" + id,
                function (data) {
                    inlineScript.data("kendoDropDownList").value(data.entityAnalysisInlineScriptId);

                    ReadyExisting(data);
                    window.AskJoobySetPageContext?.("EntityAnalysisModelInlineScript", "edit", {
                        id: data.id,
                        guid: data.guid,
                        name: data.name,
                        [parentKeyName]: parentKey
                    });
                });
        }
    }
);

function GetData() {
    return {
        entityAnalysisInlineScriptId: inlineScript.data("kendoDropDownList").value()
    };
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
                Create(endpoint, GetData(), keyName, parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

$(function () {
    updateButton
        .click(function () {
            if (validator.validate()) {
                Update(endpoint, GetData(), keyName, parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

//# sourceURL=EntityAnalysisModelInlineScript.js