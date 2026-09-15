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
var childKeyName = "entityAnalysisModelGuid";
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
            $("#CacheTtlIntervalValue").data("kendoNumericTextBox").value(data.cacheTtlIntervalValue);
            $("input[name=CacheTtlInterval][value='" + data.cacheTtlInterval + "']").prop('checked', true);
            $("input[name=MaxActivationWatcherInterval][value='" + data.maxActivationWatcherInterval + "']").prop('checked', true);
            $("#MaxActivationWatcherValue").data("kendoNumericTextBox").value(data.maxActivationWatcherValue);
            $("#MaxActivationWatcherThreshold").data("kendoNumericTextBox").value(data.maxActivationWatcherThreshold);
            $("input[name=MaxResponseElevationInterval][value='" + data.maxResponseElevationInterval + "']").prop('checked', true);
            $("#MaxResponseElevationValue").data("kendoNumericTextBox").value(data.maxResponseElevationValue);
            $("#MaxResponseElevationThreshold").data("kendoNumericTextBox").value(data.maxResponseElevationThreshold);
            $("#ActivationWatcherSample").data("kendoSlider").value(data.activationWatcherSample);
            $("#ImplicitAsyncTimeoutMilliseconds").data("kendoNumericTextBox").value(data.implicitAsyncTimeoutMilliseconds);
            $("#ReferenceDateXPath").val(data.referenceDateXPath);
            $("#ReferenceDateName").val(data.referenceDateName);
            $("#EntryXPath").val(data.entryXPath);
            $("#EntryName").val(data.entryName);

            const $enableCache = $("#EnableCache");
            if (data.enableCache) {
                $enableCache.data("kendoSwitch").check(true);
            } else {
                $enableCache.data("kendoSwitch").check(false);
            }

            const $enableTtlCounter = $("#EnableTtlCounter");
            if (data.enableTtlCounter) {
                $enableTtlCounter.data("kendoSwitch").check(true);
            } else {
                $enableTtlCounter.data("kendoSwitch").check(false);
            }

            const $enableSanctionCache = $("#EnableSanctionCache");
            if (data.enableSanctionCache) {
                $enableSanctionCache.data("kendoSwitch").check(true);
            } else {
                $enableSanctionCache.data("kendoSwitch").check(false);
            }

            const $enableResponseElevationLimit = $("#EnableResponseElevationLimit");
            if (data.enableResponseElevationLimit) {
                $enableResponseElevationLimit.data("kendoSwitch").check(true);
            } else {
                $enableResponseElevationLimit.data("kendoSwitch").check(false);
            }
            ExpandCollapseResponseElevationLimit();

            const $enableRdbmsArchive = $("#EnableRdbmsArchive");
            if (data.enableRdbmsArchive) {
                $enableRdbmsArchive.data("kendoSwitch").check(true);
            } else {
                $enableRdbmsArchive.data("kendoSwitch").check(false);
            }

            const $enableActivationWatcher = $("#EnableActivationWatcher");
            if (data.enableActivationWatcher) {
                $enableActivationWatcher.data("kendoSwitch").check(true);
            } else {
                $enableActivationWatcher.data("kendoSwitch").check(false);
            }
            ExpandCollapseActivationWatcherLimit();

            const $enableImplicitAsync = $("#EnableImplicitAsync");
            if (data.enableImplicitAsync) {
                $enableImplicitAsync.data("kendoSwitch").check(true);
            } else {
                $enableImplicitAsync.data("kendoSwitch").check(false);
            }
            ExpandCollapseImplicitAsync();

            const $enableTrace = $("#EnableTrace");
            if (data.enableTrace) {
                $enableTrace.data("kendoSwitch").check(true);
            } else {
                $enableTrace.data("kendoSwitch").check(false);
            }

            const $enableLogs = $("#EnableLogs");
            $enableLogs.data("kendoSwitch").check(!!data.enableLogs);

            const $enableLogsInfo = $("#EnableLogsInfo");
            $enableLogsInfo.data("kendoSwitch").check(!!data.enableLogsInfo);

            const $enableSampling = $("#EnableSampling");
            $enableSampling.data("kendoSwitch").check(!!data.enableSampling);
            $("#SamplePercentage").data("kendoSlider").value(data.samplePercentage);

            const $enableLogsWarnThreshold = $("#EnableLogsWarnThreshold");
            $enableLogsWarnThreshold.data("kendoSwitch").check(!!data.enableLogsWarnThreshold);
            $("#LogsWarnThresholdMilliseconds").data("kendoSlider").value(data.logsWarnThresholdMilliseconds);

            ExpandCollapseLogs();
            ExpandCollapseLogsInfo();
            ExpandCollapseSamplePercentage();
            ExpandCollapseLogsWarnThreshold();

            const $enableLogsInResponse = $("#EnableLogsInResponse");
            if (data.enableLogsInResponse) {
                $enableLogsInResponse.data("kendoSwitch").check(true);
            } else {
                $enableLogsInResponse.data("kendoSwitch").check(false);
            }

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
    $("#ImplicitAsyncTimeoutMilliseconds").data("kendoNumericTextBox").value(5000);
    $("#ReferenceDateXPath").val("");
    $("#ReferenceDateName").val("");
    $("#CacheFetchLimit").data("kendoNumericTextBox").value(100);
    $("#CacheTtlIntervalValue").data("kendoNumericTextBox").value(3);
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
    $("#EnableImplicitAsync").data("kendoSwitch").check(false);
    ExpandCollapseImplicitAsync();
    $("#EnableTrace").data("kendoSwitch").check(false);
    $("#EnableLogs").data("kendoSwitch").check(false);
    $("#EnableLogsInfo").data("kendoSwitch").check(false);
    $("#EnableSampling").data("kendoSwitch").check(false);
    $("#SamplePercentage").data("kendoSlider").value(100);
    $("#EnableLogsWarnThreshold").data("kendoSwitch").check(false);
    $("#LogsWarnThresholdMilliseconds").data("kendoSlider").value(250);
    ExpandCollapseLogs();
    ExpandCollapseLogsInfo();
    ExpandCollapseSamplePercentage();
    ExpandCollapseLogsWarnThreshold();
    $("#EnableLogsInResponse").data("kendoSwitch").check(false);
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
    const $responseElevationLimitTable = $("#ResponseElevationLimitTable");
    if ($('#EnableResponseElevationLimit').prop('checked')) {
        $responseElevationLimitTable.show();
    } else {
        $responseElevationLimitTable.hide();
    }
}

function ExpandCollapseActivationWatcherLimit() {
    const $activationWatcherLimitTable = $("#ActivationWatcherLimitTable");
    if ($('#EnableActivationWatcher').prop('checked')) {
        $activationWatcherLimitTable.show();
    } else {
        $activationWatcherLimitTable.hide();
    }
}

function ExpandCollapseImplicitAsync() {
    const $implicitAsyncTable = $("#ImplicitAsyncTable");
    if ($('#EnableImplicitAsync').prop('checked')) {
        $implicitAsyncTable.show();
    } else {
        $implicitAsyncTable.hide();
    }
}

function ExpandCollapseLogs() {
    const $logsTable = $("#LogsTable");
    if ($('#EnableLogs').prop('checked')) {
        $logsTable.show();
    } else {
        $logsTable.hide();
    }
}

function ExpandCollapseLogsInfo() {
    const $logsInfoOptionsTable = $("#LogsInfoOptionsTable");
    if ($('#EnableLogsInfo').prop('checked')) {
        $logsInfoOptionsTable.show();
    } else {
        $logsInfoOptionsTable.hide();
    }
}

function ExpandCollapseLogsWarnThreshold() {
    const $logsWarnThresholdTable = $("#LogsWarnThresholdTable");
    if ($('#EnableLogsWarnThreshold').prop('checked')) {
        $logsWarnThresholdTable.show();
    } else {
        $logsWarnThresholdTable.hide();
    }
}

function ExpandCollapseSamplePercentage() {
    const $samplePercentageTable = $("#SamplePercentageTable");
    if ($('#EnableSampling').prop('checked')) {
        $samplePercentageTable.show();
    } else {
        $samplePercentageTable.hide();
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
        enableImplicitAsync: $("#EnableImplicitAsync").prop("checked"),
        implicitAsyncTimeoutMilliseconds: $("#ImplicitAsyncTimeoutMilliseconds").val(),
        enableTrace: $("#EnableTrace").prop("checked"),
        enableLogs: $("#EnableLogs").prop("checked"),
        enableLogsInfo: $("#EnableLogsInfo").prop("checked"),
        enableLogsWarnThreshold: $("#EnableLogsWarnThreshold").prop("checked"),
        logsWarnThresholdMilliseconds: $("#LogsWarnThresholdMilliseconds").data("kendoSlider").value(),
        enableSampling: $("#EnableSampling").prop("checked"),
        samplePercentage: $("#SamplePercentage").data("kendoSlider").value(),
        enableLogsInResponse: $("#EnableLogsInResponse").prop("checked"),
        cacheFetchLimit: $("#CacheFetchLimit").val(),
        cacheTtlIntervalValue: $("#CacheTtlIntervalValue").val(),
        cacheTtlInterval: $('input[name=CacheTtlInterval]:checked').val(),
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
                clearFieldErrorStyles();
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
        $("#EnableImplicitAsync").kendoSwitch({
            change: function () {
                ExpandCollapseImplicitAsync();
            }
        });
        $("#EnableTrace").kendoSwitch();

        $("#EnableLogs").kendoSwitch({
            change: function () {
                ExpandCollapseLogs();
            }
        });
        $("#EnableLogsInfo").kendoSwitch({
            change: function () {
                ExpandCollapseLogsInfo();
            }
        });
        $("#EnableSampling").kendoSwitch({
            change: function () {
                ExpandCollapseSamplePercentage();
            }
        });
        $("#EnableLogsWarnThreshold").kendoSwitch({
            change: function () {
                ExpandCollapseLogsWarnThreshold();
            }
        });
        $("#EnableLogsInResponse").kendoSwitch();

        $("#ImplicitAsyncTimeoutMilliseconds").kendoNumericTextBox({
            format: "#"
        });

        $("#SamplePercentage").kendoSlider({
            increaseButtonTitle: "Right",
            decreaseButtonTitle: "Left",
            min: 0,
            max: 100,
            smallStep: 0.01,
            largeStep: 5
        }).data("kendoSlider");

        $("#LogsWarnThresholdMilliseconds").kendoSlider({
            increaseButtonTitle: "Right",
            decreaseButtonTitle: "Left",
            min: 0,
            max: 5000,
            smallStep: 50,
            largeStep: 250
        }).data("kendoSlider");

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

        $("#CacheTtlIntervalValue").kendoNumericTextBox({
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