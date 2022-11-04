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

var endpoint = "../api/CaseWorkflowFilter";
var parentKeyName = "caseWorkflowId";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

if (typeof id === "undefined") {
    initCaseFilterBuilder(parentKey);
    ReadyNew();
} else {
    $.get(endpoint + "/" + id,
        function (data) {
            const casesFilterBuilder = {
                filterJson: JSON.parse(data.filterJson),
                selectJson: JSON.parse(data.selectJson)
            };

            initCaseFilterBuilder(parentKey, casesFilterBuilder);

            ReadyExisting(data);
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
    const builderResult = getCasesFilter();
    return {
        selectJson: builderResult.selectJson,
        filterJson: builderResult.filterJson,
        filterSql: builderResult.filterSql,
        filterTokens: builderResult.filterTokens
    };
}

$(function () {
    addButton
        .click(function () {
            if (validator.validate()) {
                Create(endpoint, GetData(), 'id', parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

$(function () {
    updateButton
        .click(function () {
            if (validator.validate()) {
                Update(endpoint, GetData(), 'id', parentKeyName);
            } else {
                $("#ErrorMessage").html(validationFail);
            }
        });
});

//# sourceURL=CaseWorkflowFilter.js