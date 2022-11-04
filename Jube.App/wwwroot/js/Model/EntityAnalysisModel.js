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

var endpoint = "/api/EntityAnalysisModel";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

function getRecords() {
    $("#Models").html("");

    $.get(endpoint,
        function (data) {
            for (const value of data) {
                $("#Models").prepend("<a href='#' onclick='loadTemplate(" + value.id + ");'>" + value.name + "</a></br>");
            }
        }
    );
}

function loadTemplate(id) {
    $.get(endpoint + "/" + id,
        function (data) {
            $("input[name=EntryPayloadLocationTypeId][value='" + data.entryPayloadLocationTypeId + "']").prop('checked', true);
            $("input[name=ReferenceDatePayloadLocationTypeId][value='" + data.referenceDatePayloadLocationTypeId + "']").prop('checked', true);
            $("#MaxResponseElevation").data("kendoNumericTextBox").value(data.maxResponseElevation);
            $("#CacheFetchLimit").data("kendoNumericTextBox").value(data.cacheFetchLimit);
            $("input[name=MaxActivationWatcherInterval][value='" + data.maxActivationWatcherInterval + "']").prop('checked', true);
            $("#MaxActivationWatcherValue").data("kendoNumericTextBox").value(data.maxActivationWatcherValue);
            $("#MaxActivationWatcherThreshold").data("kendoNumericTextBox").value(data.maxActivationWatcherThreshold);
            $("input[name=MaxResponseElevationInterval][value='" + data.maxResponseElevationInterval + "']").prop('checked', true);
            $("#MaxResponseElevationValue").data("kendoNumericTextBox").value(data.maxResponseElevationValue);
            $("#MaxResponseElevationThreshold").data("kendoNumericTextBox").value(data.maxResponseElevationThreshold);
            $("#ActivationWatcherSample").data("kendoSlider").value(data.activationWatcherSample);
            $("#ReferenceDateXPath").val(data.referenceDateXPath);
            $("#ReferenceDateName").val(data.referenceDateName);
            $("#EntryXPath").val(data.entryXPath);
            $("#EntryName").val(data.entryName);

            if (data.enableCache) {
                $("#EnableCache").data("kendoSwitch").check(true);
            } else {
                $("#EnableCache").data("kendoSwitch").check(false);
            }

            if (data.enableTtlCounter) {
                $("#EnableTtlCounter").data("kendoSwitch").check(true);
            } else {
                $("#EnableTtlCounter").data("kendoSwitch").check(false);
            }

            if (data.enableSanctionCache) {
                $("#EnableSanctionCache").data("kendoSwitch").check(true);
            } else {
                $("#EnableSanctionCache").data("kendoSwitch").check(false);
            }

            if (data.enableResponseElevationLimit) {
                $("#EnableResponseElevationLimit").data("kendoSwitch").check(true);
            } else {
                $("#EnableResponseElevationLimit").data("kendoSwitch").check(false);
            }
            ExpandCollapseResponseElevationLimit();


            if (data.enableRdbmsArchive) {
                $("#EnableRdbmsArchive").data("kendoSwitch").check(true);
            } else {
                $("#EnableRdbmsArchive").data("kendoSwitch").check(false);
            }

            if (data.enableActivationWatcher) {
                $("#EnableActivationWatcher").data("kendoSwitch").check(true);
            } else {
                $("#EnableActivationWatcher").data("kendoSwitch").check(false);
            }
            ExpandCollapseActivationWatcherLimit();

            ReadyExisting(data);
            validator.validate();

            $("#Template").show();
            $("#Homepage").hide();
        });
}

function showTemplate() {
    $("input[name=EntryPayloadLocationTypeId][value='1']").prop('checked', true);
    $("input[name=ReferenceDatePayloadLocationTypeId][value='1']").prop('checked', true);
    $("#MaxResponseElevation").data("kendoNumericTextBox").value(10);
    $("input[name=MaxActivationWatcherInterval][value='d']").prop('checked', true);
    $("#MaxActivationWatcherValue").data("kendoNumericTextBox").value(1);
    $("#MaxActivationWatcherThreshold").data("kendoNumericTextBox").value(100);
    $("input[name=MaxResponseElevationInterval][value='d']").prop('checked', true);
    $("#MaxResponseElevationValue").data("kendoNumericTextBox").value(1);
    $("#MaxResponseElevationThreshold").data("kendoNumericTextBox").value(100);
    $("#ActivationWatcherSample").data("kendoSlider").value(1);
    $("#ReferenceDateXPath").val("");
    $("#ReferenceDateName").val("");
    $("#CacheFetchLimit").data("kendoNumericTextBox").value(100);
    $("#EntryXPath").val("");
    $("#EntryName").val("");
    $("#EnableCache").data("kendoSwitch").check(false);
    $("#EnableTtlCounter").data("kendoSwitch").check(false);
    $("#EnableSanctionCache").data("kendoSwitch").check(false);
    $("#EnableResponseElevationLimit").data("kendoSwitch").check(false);
    ExpandCollapseResponseElevationLimit();
    $("#EnableRdbmsArchive").data("kendoSwitch").check(false);
    $("#EnableActivationWatcher").data("kendoSwitch").check(false);
    ExpandCollapseActivationWatcherLimit();
    $("#ErrorMessage").html("");

    ReadyNew();
    $("#Template").show();
    $("#Homepage").hide();
}

function showHomePage() {
    $("#Template").hide();
    $("#Homepage").show();
    getRecords();
}

function ExpandCollapseResponseElevationLimit() {
    if ($('#EnableResponseElevationLimit').prop('checked')) {
        $("#ResponseElevationLimitTable").show();
    } else {
        $("#ResponseElevationLimitTable").hide();
    }
}

function ExpandCollapseActivationWatcherLimit() {
    if ($('#EnableActivationWatcher').prop('checked')) {
        $("#ActivationWatcherLimitTable").show();
    } else {
        $("#ActivationWatcherLimitTable").hide();
    }
}

function GetData() {
    return {
        entryPayloadLocationTypeId: $('input[name=EntryPayloadLocationTypeId]:checked').val(),
        referenceDatePayloadLocationTypeId: $('input[name=ReferenceDatePayloadLocationTypeId]:checked').val(),
        maxResponseElevation: $("#MaxResponseElevation").val(),
        maxResponseElevationInterval: $('input[name=MaxResponseElevationInterval]:checked').val(),
        maxResponseElevationValue: $("#MaxResponseElevationValue").val(),
        maxResponseElevationThreshold: $("#MaxResponseElevationThreshold").val(),
        maxActivationWatcherInterval: $('input[name=MaxActivationWatcherInterval]:checked').val(),
        maxActivationWatcherValue: $("#MaxActivationWatcherValue").val(),
        maxActivationWatcherThreshold: $("#MaxActivationWatcherThreshold").val(),
        enableCache: $("#EnableCache").prop("checked"),
        enableTtlCounter: $("#EnableTtlCounter").prop("checked"),
        enableSanctionCache: $("#EnableSanctionCache").prop("checked"),
        enableResponseElevationLimit: $("#EnableResponseElevationLimit").prop("checked"),
        enableActivationWatcher: $("#EnableActivationWatcher").prop("checked"),
        enableRdbmsArchive: $("#EnableRdbmsArchive").prop("checked"),
        cacheFetchLimit: $("#CacheFetchLimit").val(),
        referenceDateXPath: $("#ReferenceDateXPath").val(),
        referenceDateName: $("#ReferenceDateName").val(),
        entryXPath: $("#EntryXPath").val(),
        entryName: $("#EntryName").val(),
        entityXPath: $("#EntityXPath").val(),
        entityName: $("#EntityName").val(),
        activationWatcherSample: $("#ActivationWatcherSample").data("kendoSlider").value()
    };
}

$(document).ready(function () {
    showHomePage();

    $.getScript('/js/CRUD.js', function () {
        $("#Back").kendoButton({
            click: function () {
                showHomePage();
            }
        });

        $("#ActivationWatcherSample").kendoSlider({
            increaseButtonTitle: "Right",
            decreaseButtonTitle: "Left",
            min: 0,
            max: 1,
            smallStep: 0.01,
            largeStep: 0.05
        }).data("kendoSlider");

        $("#EnableCache").kendoSwitch();
        $("#EnableTtlCounter").kendoSwitch();
        $("#EnableSanctionCache").kendoSwitch();
        $("#EnableResponseElevationLimit").kendoSwitch({
            change: function () {
                ExpandCollapseResponseElevationLimit();
            }
        });
        $("#EnableRdbmsArchive").kendoSwitch();
        $("#EnableActivationWatcher").kendoSwitch({
            change: function () {
                ExpandCollapseActivationWatcherLimit();
            }
        });

        $("#MaxActivationWatcherValue").kendoNumericTextBox({
            format: "#"
        });

        $("#MaxActivationWatcherThreshold").kendoNumericTextBox({
            format: "#"
        });

        $("#MaxResponseElevationThreshold").kendoNumericTextBox({
            format: "#"
        });

        $("#MaxResponseElevationValue").kendoNumericTextBox({
            format: "#"
        });

        $("#MaxResponseElevation").kendoNumericTextBox({
            format: "#"
        });

        $("#CacheFetchLimit").kendoNumericTextBox({
            format: "#"
        });

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
                        Create(endpoint, GetData(), id);
                    } else {
                        $("#ErrorMessage").html(validationFail);
                    }
                });
        });

        $(function () {
            updateButton
                .click(function () {
                    if (validator.validate()) {
                        Update(endpoint, GetData(), id);
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

//# sourceURL=EntityAnalysisModel.js