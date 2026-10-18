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

const endpoint = "/api/EntityAnalysisModelTag";
const parentKeyName = "entityAnalysisModelId";
const validationFail = "There is invalid data in the form. Please check fields and correct.";

if (typeof id === "undefined") {
    ReadyNew();
    window.AskJoobySetPageContext?.("EntityAnalysisModelTag", "new", {[parentKeyName]: parentKey});
} else {
    $.get(endpoint + "/" + id,
        function (data) {
            ReadyExisting(data);
            window.AskJoobySetPageContext?.("EntityAnalysisModelTag", "edit", {
                id: data.id,
                guid: data.guid,
                name: data.name,
                [parentKeyName]: parentKey
            });
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

function GetData() {
    return {};
}

$(function () {
    addButton
        .click(function () {
            if (validator.validate()) {
                Create(endpoint, GetData(), "id", parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

$(function () {
    updateButton
        .click(function () {
            if (validator.validate()) {
                Update(endpoint, GetData(), "id", parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

//# sourceURL=EntityAnalysisModelTag.js