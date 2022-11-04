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

var endpoint = "/api/EntityAnalysisModelTtlCounter";
var parentKeyName = "entityAnalysisModelId";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var ttlCounterDataName = $("#TtlCounterDataName").kendoDropDownList({
    dataTextField: "text",
    dataValueField: "value"
});

var liveForever = $("#LiveForever").kendoSwitch({
    change: function () {
        SetLiveForever();
    }
});

var ttlCounterValue = $("#TtlCounterValue").kendoNumericTextBox({
    format: "#",
    decimals: 0
});

var onlineAggregation = $("#OnlineAggregation").kendoSwitch();

function SetLiveForever() {
    let liveForever = $('#LiveForever');
    let table = $('.sTStyle');
    if (liveForever.prop('checked')) {
        table.hide();
    } else {
        table.show();
    }
}

$.get("../api/EntityAnalysisModelRequestXPath/ByEntityAnalysisModelId/" + parentKey + "/ByDataType/1",
    function (data) {
        for (const value of data) {
            ttlCounterDataName.getKendoDropDownList().dataSource.add({
                "value": value.name,
                "text": value.name
            });
        }

        if (typeof id === "undefined") {
            SetLiveForever();
            ReadyNew();
        } else {
            $.get(endpoint + "/" + id,
                function (data) {
                    $("input[name=TtlCounterInterval][value=" +
                        data.ttlCounterInterval +
                        "]").prop('checked', true);

                    ttlCounterValue.data("kendoNumericTextBox").value(data.ttlCounterValue);

                    ttlCounterDataName.data("kendoDropDownList")
                        .value(data.ttlCounterDataName);

                    if (data.onlineAggregation) {
                        onlineAggregation.data("kendoSwitch").check(true);
                    } else {
                        onlineAggregation.data("kendoSwitch").check(false);
                    }

                    if (data.enableLiveForever) {
                        liveForever.data("kendoSwitch").check(true);
                    } else {
                        liveForever.data("kendoSwitch").check(false);
                    }
                    
                    SetLiveForever();
                    ReadyExisting(data);
                });
        }
    }
);

$(function () {
    deleteButton
        .click(function () {
            if (confirm('Are you sure you want to delete?')) {
                Delete(endpoint, id);
            }
        });
});

function GetData() {
    return {
        onlineAggregation: onlineAggregation.prop("checked"),
        enableLiveForever: liveForever.prop("checked"),
        ttlCounterDataName: ttlCounterDataName.data("kendoDropDownList").value(),
        ttlCounterValue: ttlCounterValue.val(),
        ttlCounterInterval: $('input[name=TtlCounterInterval]:checked').val()
    };
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

//# sourceURL=EntityAnalysisModelTtlCounter.js