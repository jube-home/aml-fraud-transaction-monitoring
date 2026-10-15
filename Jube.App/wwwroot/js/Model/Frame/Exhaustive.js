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

// noinspection JSUnresolvedVariable,ES6ConvertVarToLetConst

var endpoint = "/api/ExhaustiveSearchInstance";
var parentKeyName = "entityAnalysisModelId";
var validationFail = "There is invalid data in the form. Please check fields and correct.";

var dateFields = [];
var HasCreatedChildTables = 0;
var LastScore = -1;
var LastRelevance;
var Params;
var GetEndpointParams = {};
var Guid;
var interval;

function EnsureSwitch($el) {
    return $el.data("kendoSwitch") || $el.kendoSwitch().data("kendoSwitch");
}

function WireActiveToggle(instanceId) {
    const widget = EnsureSwitch($("#Active"));
    widget.unbind("change");
    widget.bind("change", function (e) {
        $.ajax({
            url: endpoint + "/" + instanceId + "/active",
            type: "PUT",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify({active: e.checked})
        }).done(function () {
            $("#ErrorMessage").html("");
        }).fail(function (xhr) {
            widget.check(!e.checked);
            $("#ErrorMessage").html(xhr.responseJSON && xhr.responseJSON.errors
                ? xhr.responseJSON.errors.map(function (er) {
                    return er.errorMessage;
                }).join(" ")
                : "The Active flag could not be updated.");
        });
    });
}

var anomalyProbability = $("#AnomalyProbability").kendoSlider({
    increaseButtonTitle: "Right",
    decreaseButtonTitle: "Left",
    min: 0,
    max: 1,
    value: 0.02,
    smallStep: 0.01,
    largeStep: 0.05
});

var bootstrapTo = $("#BootstrapTo").kendoNumericTextBox({
    format: "#",
    decimals: 0,
    min: 2,
    value: 30000
});

var bootstrapWeightFactor = $("#BootstrapWeightFactor").kendoSlider({
    increaseButtonTitle: "Right",
    decreaseButtonTitle: "Left",
    min: 0,
    max: 1,
    value: 1,
    smallStep: 0.01,
    largeStep: 0.1
});

var challengerModelBaseProbability = $("#ChallengerModelBaseProbability").kendoSlider({
    increaseButtonTitle: "Right",
    decreaseButtonTitle: "Left",
    min: 0,
    max: 1,
    value: 0.1,
    smallStep: 0.01,
    largeStep: 0.1
});

var anomalyAlgorithmId = $("#AnomalyAlgorithmId").kendoDropDownList({
    width: "420px",
    dataTextField: "text",
    dataValueField: "value",
    dataSource: [
        {text: "One Class Support Vector Machine (default)", value: 0},
        {text: "Automatic", value: 100},
        {text: "Isolation Forest", value: 1},
        {text: "Histogram Based Outlier Score", value: 2},
        {text: "Mahalanobis Distance", value: 3},
        {text: "Robust Mahalanobis Distance", value: 4},
        {text: "Principal Component Analysis", value: 5},
        {text: "Gaussian Mixture", value: 6},
        {text: "k Nearest Neighbour Distance", value: 7},
        {text: "Local Outlier Factor", value: 8},
        {text: "Marginal Likelihood", value: 9},
        {text: "Feature Bagging Ensemble", value: 10},
        {text: "Consensus Ensemble", value: 11}
    ],
    value: 0
});

var anomalyReduction = $("#AnomalyReduction").kendoDropDownList({
    width: "340px",
    dataTextField: "text",
    dataValueField: "value",
    dataSource: [
        {text: "Default for the algorithm", value: "default"},
        {text: "None", value: "none"},
        {text: "Standard", value: "standard"},
        {text: "Standard with principal components", value: "components"}
    ],
    value: "default"
});

var anomalyReductionPresets = {
    none: {mode: "None", scaling: "None"},
    standard: {mode: "Standard", scaling: "Robust"},
    components: {mode: "Standard", scaling: "Robust", projection: "ParallelAnalysis"}
};

function anomalyReductionFromParameters(parameters) {
    if (!parameters || !parameters.reduction) {
        return "default";
    }

    const reduction = parameters.reduction;
    if (reduction.projection && reduction.projection !== "None") {
        return "components";
    }

    return reduction.mode === "Standard" ? "standard" : "none";
}

function generateGrid(gridData) {
    if (dateFields.length > 0) {
        // noinspection JSUnusedLocalSymbols
        const parseFunction = function (response) {
            for (let i = 0; i < response.length; i++) {
                for (let fieldIndex = 0; fieldIndex < dateFields.length; fieldIndex++) {
                    let record = response[i];
                    record[dateFields[fieldIndex]] = kendo.parseDate(record[dateFields[fieldIndex]]);
                }
            }
            return response;
        }
    }

    generateModel(gridData[0]);
    generateColumns(gridData[0]);
}

function generateColumns(gridData) {
    const columns = [];
    for (let property in gridData) {
        if (Object.prototype.hasOwnProperty.call(gridData, property)) {
            const column = {};
            column["width"] = "400px";
            column["field"] = property;
            columns.push(column);
        }
    }

    return columns;
}

function generateModel(gridData) {
    const model = {};
    model.id = "ID";
    const fields = {};
    for (let property in gridData) {
        if (Object.prototype.hasOwnProperty.call(gridData, property)) {
            const propType = typeof gridData[property];

            if (propType === "number") {
                fields[property] = {
                    type: "number",
                    validation: {
                        required: true
                    }
                };
            } else if (propType === "boolean") {
                fields[property] = {
                    type: "boolean",
                    validation: {
                        required: true
                    }
                };
            } else if (propType === "string") {
                const parsedDate = kendo.parseDate(gridData[property]);
                if (parsedDate) {
                    fields[property] = {
                        type: "date",
                        validation: {
                            required: true
                        }
                    };
                    dateFields.push(property);
                } else {
                    fields[property] = {
                        validation: {
                            required: true
                        }
                    };
                }
            } else {
                fields[property] = {
                    validation: {
                        required: true
                    }
                };
            }
        }
    }

    model.fields = fields;
    return model;
}

function ModelTypeLabel(kind) {
    if (!kind || kind === "ExhaustiveTopologyNetwork") {
        return "Neural Network (evolved topology)";
    }

    return kind
        .replace(/Pipeline$/, "")
        .replace(/([a-z])([A-Z])/g, "$1 $2");
}

function openTrialEvidence(trial) {
    $("#TrialEvidenceEmpty").hide();
    $("#TrialEvidencePanel").show();
    $("#TrialEvidenceTitle").text(ModelTypeLabel(trial.kind) + " trial, score " +
        (trial.score === null || trial.score === undefined ? "n/a" : trial.score));
    loadExhaustiveChart("#TrialDiagramChart", "/api/Chart/Exhaustive/TrialDiagram/" + trial.id);

    $.get("/api/GetExhaustiveSearchInstanceTrialInstanceDetail/" + trial.id, function (detail) {
        if (detail && detail.evidence) {
            $("#TrialEvidenceTree").show();
            renderEvidenceTreeInto("#TrialEvidenceTree", detail.evidence);
        } else {
            $("#TrialEvidenceTree").hide().empty();
        }
    }).fail(function () {
        $("#TrialEvidenceTree").hide().empty();
    });
}

function CreateModelPerformanceTables() {
    // noinspection HtmlUnknownAttribute
    $("#grid").kendoGrid({
        autoBind: false,
        dataSource: {
            transport: {
                read: {
                    url:
                        "/api/GetExhaustiveSearchInstancePromotedTrialInstanceQuery/" +
                        id,
                    dataType: "json"
                },
                schema: {
                    model: {
                        fields: {
                            id: {
                                type: "number"
                            },
                            active:
                                {
                                    type: "boolean"
                                },
                            createdDate: {
                                type: "date"
                            },
                            score: {
                                type: "number"
                            },
                            topologyComplexity: {
                                type: "number"
                            },
                            kind: {
                                type: "string"
                            }
                        }
                    }
                }
            }
        },
        height: 500,
        detailInit: detailInit,
        dataBound: function () {
            $(".toggle").each(function () {
                const toggleSwitch = $(this);
                toggleSwitch.kendoSwitch({
                    change: function (e) {
                        $.ajax({
                            url: "/api/ExhaustiveSearchInstancePromotedTrialInstance",
                            type: "PUT",
                            contentType: "application/json; charset=utf-8",
                            dataType: "json",
                            data: JSON.stringify(
                                {
                                    active: e.checked,
                                    id: parseInt(e.sender.element.attr("id"), 10)
                                }
                            )
                        }).done(function () {
                            $("#ErrorMessage").html("");
                            $("#grid").data("kendoGrid").dataSource.read();
                        }).fail(function (xhr) {
                            e.sender.check(!e.checked);
                            $("#ErrorMessage").html(xhr.responseJSON && xhr.responseJSON.errors
                                ? xhr.responseJSON.errors.map(function (er) {
                                    return er.errorMessage;
                                }).join(" ")
                                : "The promoted trial instance could not be updated.");
                        });
                    }
                });
            });
        },
        columns: [
            {
                template:
                    '<input id=#=id# type="checkbox" class="toggle" #= (active==true) ? checked="checked" : "" # />',
                title: "Active"
            },
            {
                field: "kind",
                title: "Model Type",
                template: function (item) {
                    return kendo.htmlEncode(ModelTypeLabel(item.kind));
                }
            },
            {
                field: "createdDate",
                title: "Created Date"
            },
            {
                field: "score",
                title: "Score"
            },
            {
                field: "topologyComplexity",
                title: "Topology Complexity"
            }
        ]
    });

    $("#TrialsGrid").kendoGrid({
        autoBind: false,
        dataSource: {
            transport: {
                read: {
                    url:
                        "/api/GetExhaustiveSearchInstanceTrialInstanceListQuery/" +
                        id,
                    dataType: "json"
                },
                schema: {
                    model: {
                        fields: {
                            id: {
                                type: "number"
                            },
                            createdDate: {
                                type: "date"
                            },
                            kind: {
                                type: "string"
                            },
                            score: {
                                type: "number"
                            },
                            promoted: {
                                type: "boolean"
                            }
                        }
                    }
                }
            }
        },
        height: 500,
        sortable: true,
        selectable: "row",
        change: function () {
            const item = this.dataItem(this.select());
            if (item) {
                openTrialEvidence(item);
            }
        },
        columns: [
            {
                field: "kind",
                title: "Model Type",
                template: function (item) {
                    return kendo.htmlEncode(ModelTypeLabel(item.kind));
                }
            },
            {
                field: "createdDate",
                title: "Created Date"
            },
            {
                field: "score",
                title: "Score"
            },
            {
                field: "promoted",
                title: "Promoted",
                template: '#= promoted ? "Yes" : "No" #'
            }
        ]
    });

    $("#statistics").kendoGrid({
        autoBind: false,
        dataSource: {
            transport: {
                read: {
                    url: "/api/GetExhaustiveSearchInstanceVariableQuery/" +
                        id,
                    dataType: "json"
                },
                schema: {
                    model: {
                        fields: {
                            id: {
                                type: "number"
                            },
                            CompletedDate: {
                                type: "date"
                            },
                            Score: {
                                type: "number"
                            },
                            field: {
                                type: "string"
                            }
                        }
                    }
                }
            }
        },
        height: 500,
        detailInit: detailInitStatistics,
        change: function () {
            if (typeof exhaustiveStatisticsLoadDetail === "function") {
                exhaustiveStatisticsLoadDetail();
            }
        },
        selectable: "row",
        dataBound: function () {

            const grid = this;
            if (typeof loadExhaustiveStatisticsGrid === "function") {
                loadExhaustiveStatisticsGrid(grid, id);
                return;
            }

            $(".histogram").each(function () {
                const chart = $(this);
                const model = grid.dataItem(chart.closest('tr'));
                loadExhaustiveChart(chart, "/api/Chart/Exhaustive/VariableHistogram/" + id + "/" + model.id);
            });
        },
        columns: [
            {
                field: "categoricalSourceName",
                title: "Source Field",
                width: "160px"
            },
            {
                field: "categoricalLevel",
                title: "Level",
                width: "120px"
            },
            {
                template: '<div class="histogram" style="height:200px"></div>',
                width: 350
            },
            {
                field: "correlation",
                title: "Correlation",
                width: "110px"
            },
            {
                field: "mean",
                title: "Mean",
                width: "110px"
            },
            {
                field: "standardDeviation",
                title: "Standard Deviation",
                width: "110px"
            },
            {
                field: "maximum",
                title: "Maximum",
                width: "110px"
            },
            {
                field: "minimum",
                title: "Minimum",
                width: "110px"
            },
            {
                field: "kurtosis",
                title: "Kurtosis",
                width: "110px"
            },
            {
                field: "skewness",
                title: "Skewness",
                width: "110px"
            },
            {
                field: "normalisationType",
                title: "Normalisation Type",
                width: "110px"
            },
            {
                field: "distinctValues",
                title: "Distinct Values",
                width: "110px"
            },
            {
                field: "iqr",
                title: "IQR",
                width: "110px"
            }
        ]
    });

    function groupPrescriptionRowsByVariable(rows) {
        const byId = {};
        const order = [];
        $.each(rows || [], function (_, row) {
            let grouped = byId[row.id];
            if (!grouped) {
                grouped = {
                    id: row.id,
                    name: row.name,
                    variableMean: row.variableMean,
                    variableStandardDeviation: row.variableStandardDeviation,
                    variableMaximum: row.variableMaximum,
                    variableMinimum: row.variableMinimum,
                    sensitivity: row.sensitivity,
                    chartId: row.promotedTrialInstanceVariableId
                };
                byId[row.id] = grouped;
                order.push(grouped);
            } else if (row.positive === true) {
                grouped.chartId = row.promotedTrialInstanceVariableId;
            }

            if (row.positive === true) {
                grouped.positivePrescriptionMean = row.prescriptionMean;
                grouped.positivePrescriptionStandardDeviation = row.prescriptionStandardDeviation;
                grouped.positivePrescriptionMaximum = row.prescriptionMaximum;
                grouped.positivePrescriptionMinimum = row.prescriptionMinimum;
            } else if (row.positive === false) {
                grouped.negativePrescriptionMean = row.prescriptionMean;
                grouped.negativePrescriptionStandardDeviation = row.prescriptionStandardDeviation;
                grouped.negativePrescriptionMaximum = row.prescriptionMaximum;
                grouped.negativePrescriptionMinimum = row.prescriptionMinimum;
            } else {
                grouped.positivePrescriptionMean = row.prescriptionMean;
                grouped.positivePrescriptionStandardDeviation = row.prescriptionStandardDeviation;
                grouped.positivePrescriptionMaximum = row.prescriptionMaximum;
                grouped.positivePrescriptionMinimum = row.prescriptionMinimum;
            }
        });

        return order;
    }

    function detailInit(e) {
        $("<div/>").appendTo(e.detailCell).kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        dataType: "json",
                        url:
                            "/api/GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery/" +
                            e.data.id
                    }
                },
                schema: {
                    data: groupPrescriptionRowsByVariable,
                    model: {
                        fields: {
                            name: {
                                type: "string"
                            },
                            variableMean: {
                                type: "number"
                            },
                            variableStandardDeviation: {
                                type: "number"
                            },
                            variableMaximum: {
                                type: "number"
                            },
                            variableMinimum: {
                                type: "number"
                            },
                            positivePrescriptionMean: {
                                type: "number"
                            },
                            positivePrescriptionStandardDeviation: {
                                type: "number"
                            },
                            positivePrescriptionMaximum: {
                                type: "number"
                            },
                            positivePrescriptionMinimum: {
                                type: "number"
                            },
                            negativePrescriptionMean: {
                                type: "number"
                            },
                            negativePrescriptionStandardDeviation: {
                                type: "number"
                            },
                            negativePrescriptionMaximum: {
                                type: "number"
                            },
                            negativePrescriptionMinimum: {
                                type: "number"
                            },
                            sensitivity: {
                                type: "number"
                            },
                            chartId: {
                                type: "number"
                            }
                        }
                    }
                }
            },
            dataBound: function () {
                const grid = this;
                $(this.element).find(".histogram").each(function () {
                    const chart = $(this);
                    const model = grid.dataItem(chart.closest('tr'));
                    loadExhaustiveChart(chart, "/api/Chart/Exhaustive/PrescriptionHistogram/" + e.data.id + "/" +
                        model.chartId);
                });
                $(this.element).find(".prescriptionSummary").each(function () {
                    const chart = $(this);
                    const model = grid.dataItem(chart.closest('tr'));
                    loadExhaustiveChart(chart, "/api/Chart/Exhaustive/PrescriptionSummary/" + e.data.id + "/" +
                        model.chartId);
                });
            },
            columns: [
                {
                    template: '<div style="display:flex;gap:8px;">' +
                        '<div class="histogram" style="height:200px;flex:1;"></div>' +
                        '<div class="prescriptionSummary" style="height:200px;flex:1;"></div>' +
                        '</div>',
                    width: 500
                },
                {
                    field: "variableMean",
                    title: "Variable Mean",
                    width: "110px"
                },
                {
                    field: "variableStandardDeviation",
                    title: "Variable Standard Deviation",
                    width: "110px"
                },
                {
                    field: "variableMaximum",
                    title: "Variable Maximum",
                    width: "110px"
                },
                {
                    field: "variableMinimum",
                    title: "Variable Minimum",
                    width: "110px"
                },
                {
                    field: "positivePrescriptionMean",
                    title: "Positive Prescription Mean",
                    width: "110px"
                },
                {
                    field: "positivePrescriptionStandardDeviation",
                    title: "Positive Prescription Standard Deviation",
                    width: "110px"
                },
                {
                    field: "negativePrescriptionMean",
                    title: "Negative Prescription Mean",
                    width: "110px"
                },
                {
                    field: "negativePrescriptionStandardDeviation",
                    title: "Negative Prescription Standard Deviation",
                    width: "110px"
                },
                {
                    field: "sensitivity",
                    title: "Sensitivity",
                    width: "110px"
                }
            ]
        });
    }

    function setColor() {
        const rows = this.tbody.children();
        for (let j = 0; j < rows.length; j++) {
            const row = $(rows[j]);
            const dataItem = this.dataItem(row);
            const correlation = dataItem.get("correlation");

            let foreColor;
            if (correlation > 0.9) {
                foreColor = '#ff0000';
            } else if (correlation > 0.7) {
                foreColor = '#FFA500';
            } else {
                foreColor = '#008000';
            }

            row.css("color", foreColor);
        }
    }

    function detailInitStatistics(e) {
        $("<div/>").appendTo(e.detailCell).kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        dataType: "json",
                        url:
                            "/api/GetExhaustiveSearchInstanceTrialInstanceVariableVarianceQuery/" +
                            e.data.id
                    },
                    schema: {
                        model: {
                            fields: {
                                name: {
                                    type: "string"
                                },
                                correlation: {
                                    type: "number"
                                },
                                correlationAbsRank: {
                                    type: "number"
                                }
                            }
                        }
                    }
                }
            },
            dataBound: setColor,
            columns: [
                {
                    field: "name",
                    title: "Name",
                    width: "200px"
                },
                {
                    field: "correlation",
                    title: "Correlation",
                    width: "110px"
                }
            ]
        });
    }

    $("#tabstrip").kendoTabStrip({
        animation: {
            open: {
                effects: "fadeIn"
            }
        }
    });
}

var AutoRefreshDelayMs = 5000;

function IsTerminalTrainingStatus(statusId) {
    return statusId === 16 || statusId === 17 || statusId === 19 || statusId === 20 || statusId === undefined
        || statusId === null || statusId > 20;
}

function StopAutoRefresh() {
    if (interval) {
        clearTimeout(interval);
        interval = undefined;
    }
}

function ScheduleAutoRefresh(statusId) {
    StopAutoRefresh();
    if (IsTerminalTrainingStatus(statusId)) {
        return;
    }

    interval = setTimeout(function () {
        RefreshModelSummary();
    }, AutoRefreshDelayMs);
}

function RefreshModelSummary(force) {
    $.get("../api/ExhaustiveSearchInstance/" + id,
        function (data) {
            LoadModelSummery(data, force);
        }).fail(function () {
        ScheduleAutoRefresh(0);
    });
}

function LoadModelSummery(data, force) {
    $('#Refresh').show();
    let stop = $('#Stop').show();
    let currentStatus = $('#CurrentStatus');
    WireActiveToggle(data.id);
    LockDataSource(!IsDataSourceEditable(data.statusId));
    ScheduleAutoRefresh(data.statusId);
    switch (data.statusId) {
        case 0:
            currentStatus.text('Awaiting Server');
            break;
        case 1:
            currentStatus.text('Query for base case data');
            break;
        case 2:
            currentStatus.text('Calculating Statistics for base case');
            break;
        case 3:
            currentStatus.text('Normalising Data for unsupervised learning');
            break;
        case 4:
            currentStatus.text('Unsupervised training');
            break;
        case 5:
            currentStatus.text('Unsupervised training recall');
            break;
        case 6:
            currentStatus.text('Unsupervised training statistics creation');
            break;
        case 7:
            currentStatus.text('Query for class filter data');
            break;
        case 8:
            currentStatus.text('Statistics for class filter data');
            break;
        case 9:
            currentStatus.text('Shuffling data');
            break;
        case 10:
            currentStatus.text('Normalising data');
            break;
        case 11:
            currentStatus.text('Over Sampling \ Symmetric Sampling');
            break;
        case 12:
            currentStatus.text('Storing final data.');
            break;
        case 13:
            currentStatus.text('Correlation analysis');
            break;
        case 14:
            currentStatus.text('Multicollinearity analysis');
            break;
        case 15:
            currentStatus.text('Supervised Learning');
            break;
        case 16:
            currentStatus.text('Finished');
            break;
        case 17:
            currentStatus.text('Finished no class available');
            break;
        case 18:
            currentStatus.text('Stopping');
            stop.data("kendoButton").enable(false);
            break;
        case 19:
            currentStatus.text('Stopped');
            stop.data("kendoButton").enable(false);
            break;
        case 20:
            currentStatus.text('No data found for the configured data source');
            stop.data("kendoButton").enable(false);
            break;
        default:
            currentStatus.text('Stopped for reasons unexpected');
    }

    Guid = data.guid;

    $('#CreatedUser').text(data.createdUser);
    $('#CreatedDate').text(new Date(data.createdDate).toLocaleString());
    $('#CompletedDate').text(new Date(data.completedDate).toLocaleString());
    $('#UpdatedDate').text(new Date(data.updatedDate).toLocaleString());
    $('#Models').text(data.models);
    $('#TopologyComplexity').text(data.topologyComplexity);
    $('#ModelsSinceBest').text(data.modelsSinceBest);
    $('#Score').text(data.score);
    $('#AnomalySummary').text(data.anomalySummary || '');

    if (data.score > LastScore || force) {
        LastScore = data.score;

        const $kpi = $('#KPI');
        const $tabstrip = $('#tabstrip');
        if (data.statusId < 14) {
            $kpi.hide();
            $tabstrip.hide();
        } else {
            $kpi.show();
            $tabstrip.show();
            loadExhaustivePerformanceCharts(id);
            initExhaustiveValueDetection(id);
            loadExhaustiveNarrative(id);
        }

        BuildSliders(id, Guid);

        if (HasCreatedChildTables === 0) {
            CreateModelPerformanceTables();
            HasCreatedChildTables = 1;
        }

        $("#grid").data("kendoGrid").dataSource.read();

        let statistics = $("#statistics");
        let records = statistics.data("kendoGrid").dataSource.view().length;
        if (records === 0) {
            statistics.data("kendoGrid").dataSource.read();
        }

        loadExhaustiveChart("#PromotionChart", "/api/Chart/Exhaustive/PromotionHistory/" + id);
        loadExhaustiveChart("#chart", "/api/Chart/Exhaustive/LearningCurve/" + id);
    }

    if (HasCreatedChildTables === 1) {
        $("#TrialsGrid").data("kendoGrid").dataSource.read();
    }
}

function MergeArray(name, value) {
    Params[name] = value;
}

function sliderOnChange(e) {
    const handle = e.sender,
        handleElement = handle.element,
        handleElementId = handleElement.attr('name');

    MergeArray(handleElementId, e.value);

    GetScore();
}

function GetScore() {
    jQuery.ajax({
        type: 'POST',
        contentType: "application/json; charset=utf-8",
        url: "../api/Invoke/ExhaustiveSearchInstance/" + Guid,
        data: JSON.stringify(Params),
        dataType: "json",
        success: SetScore
    });
}

function SetScore(data) {
    $("#Simulation").text('Score: ' + data.score);
    LastRelevance = data.relevance;
    loadExhaustiveChartWithBody("#TopologyChart", "/api/Chart/Exhaustive/Topology/" + id,
        {nodeRelevance: data.nodeRelevance});
}

function sliderOnSlide(e) {
    const handle = e.sender,
        handleElement = handle.element,
        handleElementId = handleElement.attr('name');

    MergeArray(handleElementId, e.value);
}

function BuildSliders() {
    Params = {};
    $("#SlidersPlaceholder tr").remove();

    $.get('api/GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery/' + id,
        function (data) {
            if (data.length > 0) {
                const categoricalGroups = {};
                $.each(data, function (i, value) {
                    if (value.categoricalSourceName) {
                        let group = categoricalGroups[value.categoricalSourceName];
                        if (!group) {
                            group = {
                                reference: value.categoricalReferenceLabel,
                                labels: [{
                                    text: value.categoricalReferenceLabel,
                                    value: value.categoricalReferenceLabel
                                }]
                            };
                            categoricalGroups[value.categoricalSourceName] = group;
                        }
                        group.labels.push({
                            text: value.categoricalLevel,
                            value: value.categoricalKind === 3 ? "" : value.categoricalLevel
                        });
                        return;
                    }

                    if (!value.emptyRange) {
                        const tb = $('<input>', {
                            type: 'text',
                            value: value.mean,
                            id: 'balSlider_' + value.id,
                            name: value.name
                        }).css("width", "600px");

                        const $tr = $('<tr>').append(
                            $("<td style='vertical-align:top;width: 0;padding-top:12px;padding-bottom:12px;'>")
                                .text(value.name),
                            $("<td style='width:80%;height:100px;vertical-align:top;padding-top:12px;" +
                                "padding-bottom:12px;'>").append(tb)
                        );

                        $("#SlidersPlaceholder").append($tr);

                        $("#balSlider_" + value.id).kendoSlider({
                            increaseButtonTitle: "Right",
                            decreaseButtonTitle: "Left",
                            min: value.minimum,
                            max: value.maximum,
                            change: sliderOnChange,
                            slide: sliderOnSlide,
                            showButtons: false
                        }).data("kendoSlider");

                        Params[value.name] = tb.val();
                    }
                });

                $.each(categoricalGroups, function (source, group) {
                    const select = $('<input>', {
                        type: 'text',
                        name: source
                    });

                    $("#SlidersPlaceholder").append($('<tr>').append(
                        $("<td style='vertical-align:top;width: 0;padding-top:12px;padding-bottom:12px;'>")
                            .text(source),
                        $("<td style='width:80%;vertical-align:top;padding-top:12px;padding-bottom:12px;'>")
                            .append(select)
                    ));

                    select.kendoDropDownList({
                        dataSource: group.labels,
                        dataTextField: "text",
                        dataValueField: "value",
                        value: group.reference,
                        change: function (e) {
                            MergeArray(source, e.sender.value());
                            GetScore();
                        }
                    });

                    Params[source] = group.reference;
                });

                $("#SlidersPlaceholder").resize();

                GetScore();
            }
        });
}

if (typeof id === "undefined") {
    //clearInterval(interval);
    $('#Refresh').hide();
    $('#Stop').hide();
    ReadyNew();
    window.AskJoobySetPageContext?.("Exhaustive", "new", {[parentKeyName]: parentKey});
    initExhaustiveDataSource();

    $("#Bootstrap").prop("checked", true);
    bootstrapTo.data("kendoNumericTextBox").value(30000);
    bootstrapWeightFactor.data("kendoSlider").value(1);
    challengerModelBaseProbability.data("kendoSlider").value(0.1);

    initExhaustiveFilterBuilder(parentKey);
    ExpandCollapseAnomaly();
    ExpandCollapseFilter();
    ExpandCollapseBootstrap();

} else {
    $.get(endpoint + "/" + id,
        function (data) {
            ReadyExisting(data);
            window.AskJoobySetPageContext?.("Exhaustive", "edit", {
                id: data.id,
                guid: data.guid,
                name: data.name,
                [parentKeyName]: parentKey
            });
            initExhaustiveDataSource(data);

            const builderCoderData = {
                filterTokens: data.filterTokens,
                filterJson: JSON.parse(data.filterJson)
            };

            const $anomaly = $("#Anomaly");
            EnsureSwitch($anomaly).check(!!data.anomaly);

            anomalyProbability.data("kendoSlider").value(data.anomalyProbability)
            anomalyAlgorithmId.data("kendoDropDownList").value(data.anomalyAlgorithmId || 0);

            let storedParameters = null;
            if (data.anomalyParametersJson) {
                try {
                    storedParameters = JSON.parse(data.anomalyParametersJson);
                } catch (e) {
                    storedParameters = null;
                }
            }

            anomalyReduction.data("kendoDropDownList").value(anomalyReductionFromParameters(storedParameters));

            const $filter = $("#Filter");
            EnsureSwitch($filter).check(!!data.filter);

            const $reportTable = $("#ReportTable");
            EnsureSwitch($reportTable).check(!!data.reportTable);

            const $bootstrap = $("#Bootstrap");
            EnsureSwitch($bootstrap).check(!!data.bootstrap);
            bootstrapTo.data("kendoNumericTextBox").value(data.bootstrapTo);
            bootstrapWeightFactor.data("kendoSlider").value(
                data.bootstrapWeightFactor === null || data.bootstrapWeightFactor === undefined
                    ? 1
                    : data.bootstrapWeightFactor);
            challengerModelBaseProbability.data("kendoSlider").value(
                data.challengerModelBaseProbability === null || data.challengerModelBaseProbability === undefined
                    ? 0.1
                    : data.challengerModelBaseProbability);

            if (typeof SyncFilterRequiredForDataset === "function") {
                SyncFilterRequiredForDataset();
            }

            ExpandCollapseAnomaly();
            ExpandCollapseFilter();
            ExpandCollapseBootstrap();

            initExhaustiveFilterBuilder(GetSelectedParentID(), builderCoderData,
                ExhaustiveFilterCompletionsUrl(data.exhaustiveInstanceCsvId, data.exhaustiveInstanceJsonId));

            RefreshModelSummary();
            $("#Status").toggle();
            //interval = setInterval(RefreshModelSummary, 30000);
        });
}

function ExpandCollapseAnomaly() {
    const $anomalyTable = $("#AnomalyTable");
    if ($('#Anomaly').prop('checked')) {
        $anomalyTable.show();
    } else {
        $anomalyTable.hide();
    }
}

function ExpandCollapseFilter() {
    const $filterTable = $("#FilterTable");
    if ($('#Filter').prop('checked')) {
        $filterTable.show();
    } else {
        $filterTable.hide();
    }
}

function ExpandCollapseBootstrap() {
    const $bootstrapTable = $("#BootstrapTable");
    if ($('#Bootstrap').prop('checked')) {
        $bootstrapTable.show();
    } else {
        $bootstrapTable.hide();
    }
}

$("#Anomaly").kendoSwitch({
    change: function () {
        ExpandCollapseAnomaly();
        if (typeof SyncFilterRequiredForDataset === "function") {
            SyncFilterRequiredForDataset();
        }
    }
});

$("#Filter").kendoSwitch({
    change: function () {
        ExpandCollapseFilter();
    }
});

$("#Bootstrap").kendoSwitch({
    change: function () {
        ExpandCollapseBootstrap();
    }
});

$(function () {
    deleteButton
        .click(function () {
            if (confirm('Are you sure you want to delete?')) {
                Delete(endpoint, id);
            }
        });
});

function GetData() {
    let data = getExhaustiveDataSource();

    if ($('#Filter').prop('checked')) {
        const builderCoder = getExhaustiveFilter();
        data["filter"] = true;
        data["filterSql"] = builderCoder.filterSql;
        data["filterJson"] = builderCoder.filterJson;
        data["filterTokens"] = builderCoder.filterTokens;
    }

    if ($('#Anomaly').prop('checked')) {
        data["anomaly"] = true;
        data["AnomalyProbability"] = anomalyProbability.data("kendoSlider").value();
        data["anomalyAlgorithmId"] = anomalyAlgorithmId.data("kendoDropDownList").value() * 1;

        const reductionChoice = anomalyReduction.data("kendoDropDownList").value();
        if (reductionChoice !== "default") {
            data["anomalyParametersJson"] = JSON.stringify({reduction: anomalyReductionPresets[reductionChoice]});
        }
    }

    if ($('#Bootstrap').prop('checked')) {
        data["bootstrap"] = true;
        data["bootstrapTo"] = bootstrapTo.data("kendoNumericTextBox").value();
        data["bootstrapWeightFactor"] = bootstrapWeightFactor.data("kendoSlider").value();
    }

    data["challengerModelBaseProbability"] = challengerModelBaseProbability.data("kendoSlider").value();

    return data;
}

$(function () {
    $("#Refresh").kendoButton()
        .click(function () {
            RefreshModelSummary(true);
        });
});

function SetCurrentStatusStopping(data) {
    $("#Stop").data("kendoButton").enable(false);
    let currentStatus = $('#CurrentStatus');
    currentStatus.text("Stopping");
}

$(function () {
    $("#Stop").kendoButton()
        .click(function () {
            jQuery.ajax({
                type: 'PUT',
                contentType: "application/json; charset=utf-8",
                url: "../api/ExhaustiveSearchInstance/stop/" + Guid,
                success: SetCurrentStatusStopping
            });
        });
});

$(function () {
    addButton
        .click(function () {
            if (validator.validate()) {
                Create(endpoint, GetData(), "id", parentKeyName, function (data) {
                    LoadModelSummery(data, true);
                });
                $("#Status").toggle();
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

//# sourceURL=Exhaustive.js