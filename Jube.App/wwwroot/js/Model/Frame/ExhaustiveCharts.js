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

function loadExhaustiveChart(selector, url) {
    const container = $(selector);
    const request = (container.data("chartRequest") || 0) + 1;
    container.data("chartRequest", request);
    kendo.ui.progress(container, true);

    $.ajax({
        url: url,
        type: "GET",
        dataType: "text"
    }).done(function (svg) {
        if (container.data("chartRequest") !== request) {
            return;
        }

        if (typeof svg === "string" && svg.trim().indexOf("<svg") === 0) {
            container.html(svg);
        } else {
            container.html('<div class="jc-message">Chart could not be loaded.</div>');
        }
    }).fail(function () {
        if (container.data("chartRequest") !== request) {
            return;
        }

        container.html('<div class="jc-message">Chart could not be loaded.</div>');
    }).always(function () {
        if (container.data("chartRequest") === request) {
            kendo.ui.progress(container, false);
        }
    });
}

function loadExhaustiveChartWithBody(selector, url, body) {
    const container = $(selector);
    const request = (container.data("chartRequest") || 0) + 1;
    container.data("chartRequest", request);
    kendo.ui.progress(container, true);

    $.ajax({
        url: url,
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(body),
        dataType: "text"
    }).done(function (svg) {
        if (container.data("chartRequest") !== request) {
            return;
        }

        if (typeof svg === "string" && svg.trim().indexOf("<svg") === 0) {
            container.html(svg);
        } else {
            container.html('<div class="jc-message">Chart could not be loaded.</div>');
        }
    }).fail(function () {
        if (container.data("chartRequest") !== request) {
            return;
        }

        container.html('<div class="jc-message">Chart could not be loaded.</div>');
    }).always(function () {
        if (container.data("chartRequest") === request) {
            kendo.ui.progress(container, false);
        }
    });
}

const exhaustivePerformanceCharts = [
    ["#TopologyChart", "/api/Chart/Exhaustive/Topology/"],
    ["#PerformanceMeasures", "/api/Chart/Exhaustive/Measures/"],
    ["#PerformanceConfusion", "/api/Chart/Exhaustive/PerformanceConfusion/"],
    ["#PrecisionRecallChart", "/api/Chart/Exhaustive/PrecisionRecall/"],
    ["#CalibrationChart", "/api/Chart/Exhaustive/Calibration/"],
    ["#ThresholdSweepChart", "/api/Chart/Exhaustive/ThresholdSweep/"]
];

function loadExhaustivePerformanceCharts(id) {
    exhaustivePerformanceCharts.forEach(function (chart) {
        loadExhaustiveChart(chart[0], chart[1] + id);
    });
}

const exhaustiveValueDetectionState = {instanceId: 0, variableId: null, variableName: null};

function initExhaustiveValueDetection(instanceId) {
    exhaustiveValueDetectionState.instanceId = instanceId;
    const select = $("#PerformanceValueVariable");
    if (select.length === 0) {
        return;
    }

    if (select.data("kendoDropDownList")) {
        reloadExhaustiveValueDetectionCharts();
        return;
    }

    $.get("/api/GetExhaustiveSearchInstanceVariableQuery/" + instanceId, function (data) {
        const variables = [{id: null, name: "Count"}].concat(data || []);
        select.kendoDropDownList({
            dataTextField: "name",
            dataValueField: "id",
            dataSource: variables,
            width: "260px",
            change: function () {
                const item = select.data("kendoDropDownList").dataItem();
                exhaustiveValueDetectionState.variableId = item.id;
                exhaustiveValueDetectionState.variableName = item.id === null ? null : item.name;
                reloadExhaustiveValueDetectionCharts();
            }
        });
        reloadExhaustiveValueDetectionCharts();
    });
}

function reloadExhaustiveValueDetectionCharts() {
    const instanceId = exhaustiveValueDetectionState.instanceId;
    const variableId = exhaustiveValueDetectionState.variableId;
    const variableName = exhaustiveValueDetectionState.variableName;
    const suffix = variableId
        ? "?variableId=" + variableId + "&variableName=" + encodeURIComponent(variableName)
        : "";
    loadExhaustiveChart("#PerformanceRocChart", "/api/Chart/Exhaustive/PerformanceRoc/" + instanceId + suffix);
    loadExhaustiveChart("#GainsChart", "/api/Chart/Exhaustive/Gains/" + instanceId + suffix);
    loadExhaustiveChart("#ValueCapturedChart", "/api/Chart/Exhaustive/ValueCaptured/" + instanceId + suffix);
    loadExhaustiveChart("#LiftChart", "/api/Chart/Exhaustive/Lift/" + instanceId + suffix);
    loadExhaustiveChart("#ScoreDistributionChart", "/api/Chart/Exhaustive/ScoreDistribution/" + instanceId + suffix);
    loadExhaustiveChart("#KsChart", "/api/Chart/Exhaustive/Ks/" + instanceId + suffix);
}

const exhaustiveNarrativeGroups = [
    ["#NarrativePerformance", "Performance"],
    ["#NarrativeCalibration", "Calibration"],
    ["#NarrativeSensitivity", "Sensitivity"],
    ["#NarrativePrescription", "Prescription"],
    ["#NarrativeConfusion", "Confusion"],
    ["#NarrativeDiscrimination", "Discrimination"],
    ["#NarrativeKs", "Ks"],
    ["#NarrativeThresholdSweep", "ThresholdSweep"],
    ["#NarrativeScoreDistribution", "ScoreDistribution"],
    ["#NarrativeGains", "Gains"],
    ["#NarrativePrecisionRecall", "PrecisionRecall"],
    ["#NarrativeLift", "Lift"],
    ["#NarrativeLearningCurve", "LearningCurve"]
];

function renderNarrativeParagraphs(selector, paragraphs) {
    const $container = $(selector).empty();
    (paragraphs || []).forEach(function (paragraph) {
        $("<p></p>").text(paragraph).appendTo($container);
    });
}

function loadExhaustiveNarrative(id) {
    $.ajax({
        url: "/api/Chart/Exhaustive/Narrative/" + id,
        type: "GET",
        dataType: "json"
    }).done(function (narrative) {
        renderNarrativeParagraphs("#NarrativeHolistic", narrative.holistic);
        exhaustiveNarrativeGroups.forEach(function (group) {
            renderNarrativeParagraphs(group[0], narrative.groups && narrative.groups[group[1]]);
        });
    }).fail(function () {
        renderNarrativeParagraphs("#NarrativeHolistic", []);
        exhaustiveNarrativeGroups.forEach(function (group) {
            renderNarrativeParagraphs(group[0], []);
        });
    });
}

