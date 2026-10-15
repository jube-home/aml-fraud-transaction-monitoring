var datasetEndpoint = "/api/ExhaustiveInstanceDataset";
var datasetKindEndpoints = {Csv: "/api/ExhaustiveInstanceCsv", Json: "/api/ExhaustiveInstanceJson"};
var datasetUploaded = null;

var processingTypeItems = [
    {text: "Payload", value: 1},
    {text: "Dictionary", value: 2},
    {text: "TtlCounter", value: 3},
    {text: "Sanction", value: 4},
    {text: "Abstraction", value: 5},
    {text: "AbstractionCalculation", value: 6}
];

function DatasetReferenceValue(dataset) {
    return dataset.kind + ":" + dataset.id;
}

function DatasetKindEndpoint(kind) {
    return datasetKindEndpoints[kind];
}

function DatasetKindFromFileName(fileName) {
    return /\.json$/i.test(fileName) ? "Json" : "Csv";
}

var datasetPicker = $("#ExhaustiveDatasetReference").kendoDropDownList({
    dataTextField: "label",
    dataValueField: "reference",
    optionLabel: "Select a dataset",
    autoBind: false,
    width: "480px",
    dataSource: {
        transport: {
            read: {
                url: datasetEndpoint,
                dataType: "json",
                cache: false
            }
        },
        schema: {
            parse: function (response) {
                return response.map(function (d) {
                    d.reference = DatasetReferenceValue(d);
                    d.label = d.name + " (" + d.kind + ")";
                    return d;
                });
            }
        },
        filter: {field: "statusId", operator: "eq", value: 1}
    },
    change: function () {
        RefreshExhaustiveFilterBuilder();

        const value = this.value();
        if (!value) {
            ResetDatasetUpload();
            CollapseDatasetUpload();
            return;
        }

        const parts = value.split(":");
        $.get(DatasetKindEndpoint(parts[0]) + "/" + parts[1], function (dataset) {
            ShowDatasetInspection(dataset);
            ExpandDatasetUpload();
        }).fail(function (jqXHR) {
            ShowDatasetErrors(jqXHR);
        });
    }
}).data("kendoDropDownList");

function GetDataSourceId() {
    const checked = $("input[name=DataSourceId]:checked").val();
    return checked === undefined ? 0 : Number(checked);
}

function SetDataSourceId(value) {
    $("input[name=DataSourceId][value=" + (value || 0) + "]").prop("checked", true);
}

function ExhaustiveFilterCompletionsUrl(csvId, jsonId) {
    if (csvId) {
        return "../api/Completions/ByExhaustiveInstanceDataset?kind=Csv&id=" + csvId;
    }
    if (jsonId) {
        return "../api/Completions/ByExhaustiveInstanceDataset?kind=Json&id=" + jsonId;
    }
    return null;
}

function RefreshExhaustiveFilterBuilder() {
    if (typeof initExhaustiveFilterBuilder !== "function" || typeof GetSelectedParentID !== "function") {
        return;
    }

    const source = getExhaustiveDataSource();
    if (source.dataSourceId !== 2) {
        initExhaustiveFilterBuilder(GetSelectedParentID());
        return;
    }

    const url = ExhaustiveFilterCompletionsUrl(source.exhaustiveInstanceCsvId, source.exhaustiveInstanceJsonId);
    if (url) {
        initExhaustiveFilterBuilder(GetSelectedParentID(), null, url);
    }
}

$("input[name=DataSourceId]").on("change", function () {
    ExpandCollapseDataset();
    SyncFilterRequiredForDataset();
    RefreshExhaustiveFilterBuilder();
});

function ExpandDatasetUpload() {
    $(".datasetUploadRow").show();
}

function CollapseDatasetUpload() {
    $(".datasetUploadRow").hide();
}

function ShowDatasetMappingRows(visible) {
    $(".datasetMappingRow")[visible ? "show" : "hide"]();
}

var datasetDistinctValuesTemplate = kendo.template(
    "# if (distinctValues && distinctValues.length) { #" +
    "<table class='dataset-distinct-values-table'>" +
    "<thead><tr><th>Value</th><th>Count</th></tr></thead>" +
    "<tbody>" +
    "# for (var i = 0; i < distinctValues.length; i++) { #" +
    "<tr><td>#: distinctValues[i].value #</td><td>#: distinctValues[i].count #</td></tr>" +
    "# } #" +
    "</tbody>" +
    "</table>" +
    "# } else { #" +
    "<div class='dataset-distinct-values-empty'>No distinct value breakdown for this column.</div>" +
    "# } #"
);

var datasetColumnsGrid = $("#DatasetColumnsGrid").kendoGrid({
    dataSource: {
        data: [],
        schema: {
            model: {
                id: "index",
                fields: {
                    index: {type: "number", editable: false},
                    header: {type: "string", editable: false},
                    kind: {type: "string", editable: false},
                    missingCount: {type: "number", editable: false},
                    min: {type: "number", editable: false},
                    max: {type: "number", editable: false},
                    isCategorical: {type: "boolean", editable: false},
                    include: {type: "boolean"},
                    name: {type: "string"},
                    processingTypeId: {type: "number"}
                }
            }
        }
    },
    editable: "incell",
    height: 360,
    scrollable: true,
    resizable: true,
    columns: [
        {
            field: "include", title: "Include", width: "70px",
            template: function (item) {
                return '<span class="k-switch k-widget ' + (item.include ? "k-switch-on" : "k-switch-off") +
                    '" style="pointer-events: none;">' +
                    '<span class="k-switch-container">' +
                    '<span class="k-switch-label-on">On</span>' +
                    '<span class="k-switch-label-off">Off</span>' +
                    '<span class="k-switch-handle"></span>' +
                    '</span></span>';
            },
            editor: function (container, options) {
                $('<input type="checkbox" name="' + options.field + '"/>')
                    .appendTo(container)
                    .kendoSwitch({
                        checked: !!options.model.get(options.field),
                        change: function (e) {
                            options.model.set(options.field, e.checked);
                        }
                    });
            }
        },
        {field: "header", title: "Column", width: "160px"},
        {field: "kind", title: "Type", width: "80px"},
        {field: "isCategorical", title: "Categorical", width: "90px"},
        {field: "missingCount", title: "Missing", width: "80px"},
        {field: "min", title: "Min", width: "80px"},
        {field: "max", title: "Max", width: "80px"},
        {field: "name", title: "Variable name", width: "160px"},
        {
            field: "processingTypeId",
            title: "Processing type",
            width: "190px",
            values: processingTypeItems
        }
    ],
    detailTemplate: datasetDistinctValuesTemplate,
    dataBound: function () {
        const grid = this;
        grid.tbody.find(".k-hierarchy-cell").each(function () {
            const item = grid.dataItem($(this).closest("tr"));
            if (!item.distinctValues || item.distinctValues.length === 0) {
                $(this).empty();
            }
        });
    }
}).data("kendoGrid");

var datasetUpload = $("#DatasetFile").kendoUpload({
    multiple: false,
    async: {
        saveUrl: DatasetKindEndpoint("Csv") + "/Upload",
        autoUpload: true
    },
    select: function (e) {
        ShowDatasetMessage("", false);
        this.options.async.saveUrl = DatasetKindEndpoint(DatasetKindFromFileName(e.files[0].name)) + "/Upload";
    },
    upload: function () {
        ShowDatasetMessage("Uploading and validating.", false);
    },
    success: function (e) {
        if (e.operation !== "upload") {
            return;
        }

        let dataset;
        try {
            dataset = JSON.parse(e.XMLHttpRequest.responseText);
        } catch (ex) {
            ShowDatasetMessage("The dataset could not be processed.", true);
            return;
        }

        ShowDatasetMessage("Validated. Review the mapping and confirm.", false);
        ShowDatasetInspection(dataset);
    },
    error: function (e) {
        ShowDatasetErrors(e.XMLHttpRequest);
    }
}).data("kendoUpload");

$("#DatasetConfirmButton").kendoButton({enable: false});
$("#DatasetCancelButton").kendoButton();
$("#DatasetOpenButton").kendoButton();
$("#DatasetDeleteButton").kendoButton();

function ExpandCollapseDataset() {
    if (GetDataSourceId() === 2) {
        $("#DatasetTable").show();
    } else {
        $("#DatasetTable").hide();
    }
}

function SyncFilterRequiredForDataset() {
    const isDataset = GetDataSourceId() === 2;
    const isAnomaly = $("#Anomaly").prop("checked");
    const required = isDataset && !isAnomaly;
    $("#FilterDatasetHint").toggle(isDataset);

    const $filter = $("#Filter");
    const switchWidget = $filter.data("kendoSwitch");
    if (!switchWidget) {
        return;
    }

    if (required) {
        switchWidget.check(true);
        switchWidget.enable(false);
    } else {
        switchWidget.enable(true);
    }

    if (typeof ExpandCollapseFilter === "function") {
        ExpandCollapseFilter();
    }
}

function IsDataSourceEditable(statusId) {
    const status = statusId || 0;
    return status === 0 || status === 20;
}

function GetCalibrationMethod() {
    const checked = $("input[name=CalibrationMethod]:checked").val();
    return checked === undefined ? "automatic" : checked;
}

function SetCalibrationMethod(value) {
    $("input[name=CalibrationMethod][value=" + (value || "automatic") + "]").prop("checked", true);
}

function LockDataSource(locked) {
    $("input[name=DataSourceId]").prop("disabled", locked);
    $("input[name=CalibrationMethod]").prop("disabled", locked);
    datasetPicker.enable(!locked);
    $("#DatasetOpenButton").data("kendoButton").enable(!locked);
    $("#DatasetDeleteButton").data("kendoButton").enable(!locked);

    $("#Bootstrap").prop("disabled", locked);
    const bootstrapSwitchWidget = $("#Bootstrap").data("kendoSwitch");
    if (bootstrapSwitchWidget) {
        bootstrapSwitchWidget.enable(!locked);
    }
    if (typeof bootstrapTo !== "undefined") {
        bootstrapTo.data("kendoNumericTextBox").enable(!locked);
    }
    if (typeof bootstrapWeightFactor !== "undefined") {
        bootstrapWeightFactor.data("kendoSlider").enable(!locked);
    }

    $("#Anomaly").prop("disabled", locked);
    const anomalySwitchWidget = $("#Anomaly").data("kendoSwitch");
    if (anomalySwitchWidget) {
        anomalySwitchWidget.enable(!locked);
    }
    if (typeof anomalyProbability !== "undefined") {
        anomalyProbability.data("kendoSlider").enable(!locked);
    }
    if (typeof anomalyAlgorithmId !== "undefined") {
        anomalyAlgorithmId.data("kendoDropDownList").enable(!locked);
    }
    if (typeof anomalyReduction !== "undefined") {
        anomalyReduction.data("kendoDropDownList").enable(!locked);
    }

    $("#Filter").prop("disabled", locked);
    const filterSwitchWidget = $("#Filter").data("kendoSwitch");
    if (filterSwitchWidget) {
        filterSwitchWidget.enable(!locked);
    }

    if (typeof challengerModelBaseProbability !== "undefined") {
        challengerModelBaseProbability.data("kendoSlider").enable(!locked);
    }
}

function initExhaustiveDataSource(data) {
    if (data) {
        SetDataSourceId(data.dataSourceId);
        SetCalibrationMethod(data.calibrationMethod);
        datasetPicker.dataSource.read().then(function () {
            if (data.exhaustiveInstanceCsvId) {
                datasetPicker.value("Csv:" + data.exhaustiveInstanceCsvId);
            } else if (data.exhaustiveInstanceJsonId) {
                datasetPicker.value("Json:" + data.exhaustiveInstanceJsonId);
            } else {
                datasetPicker.value("");
            }
        });

        LockDataSource(!IsDataSourceEditable(data.statusId));
    } else {
        SetDataSourceId(0);
        SetCalibrationMethod("automatic");
        datasetPicker.dataSource.read();
        LockDataSource(false);
    }

    ExpandCollapseDataset();
    SyncFilterRequiredForDataset();
}

function getExhaustiveDataSource() {
    const source = GetDataSourceId();
    const result = {dataSourceId: source, calibrationMethod: GetCalibrationMethod()};
    if (source === 2) {
        const value = datasetPicker.value();
        result.exhaustiveInstanceCsvId = null;
        result.exhaustiveInstanceJsonId = null;
        if (value !== "") {
            const parts = value.split(":");
            if (parts[0] === "Json") {
                result.exhaustiveInstanceJsonId = Number(parts[1]);
            } else {
                result.exhaustiveInstanceCsvId = Number(parts[1]);
            }
        }
    }

    return result;
}

function ShowDatasetMessage(text, isError) {
    $("#DatasetMessage").text(text).css("color", isError ? "#b00020" : "inherit");
}

function ShowDatasetErrors(jqXHR) {
    let messages = [];
    try {
        const response = JSON.parse(jqXHR.responseText);
        if (response.errors) {
            messages = response.errors.map(function (e) {
                return e.message || e.errorMessage;
            });
        } else if (response.message) {
            messages = [response.message];
        }
    } catch (e) {
        messages = [];
    }

    if (jqXHR.status === 413) {
        messages = ["The file is larger than the permitted size."];
    }

    if (messages.length === 0) {
        messages = ["The dataset could not be processed."];
    }

    ShowDatasetMessage(messages.join(" "), true);
}

function ResetDatasetUpload() {
    datasetUploaded = null;
    datasetUpload.clearAllFiles();
    $("#DatasetSummary").text("");
    $("#DatasetWarnings").empty();
    ShowDatasetMappingRows(false);
    datasetColumnsGrid.dataSource.data([]);
    $("#DatasetConfirmButton").data("kendoButton").enable(false);
    ShowDatasetMessage("", false);
}

function ShowDatasetInspection(dataset) {
    datasetUploaded = dataset;

    $("#DatasetSummary").text(dataset.kind + ": " + dataset.rowCount + " rows, " + dataset.columnCount + " columns" +
        (dataset.delimiter ? ", delimiter '" + dataset.delimiter + "'" : "") + ", " + dataset.sizeBytes + " bytes.");

    const $warnings = $("#DatasetWarnings").empty();
    for (let i = 0; i < dataset.warnings.length; i++) {
        $("<li></li>").text(dataset.warnings[i]).appendTo($warnings);
    }

    datasetColumnsGrid.dataSource.data(dataset.columns);
    ShowDatasetMappingRows(true);
    $("#DatasetConfirmButton").data("kendoButton").enable(true);
}

$("#DatasetOpenButton").on("click", function () {
    ResetDatasetUpload();
    ExpandDatasetUpload();
});

$("#DatasetCancelButton").on("click", function () {
    const dataset = datasetUploaded;
    CollapseDatasetUpload();
    if (dataset && dataset.statusId === 0) {
        $.ajax({url: DatasetKindEndpoint(dataset.kind) + "/" + dataset.id, type: "DELETE"});
    }
});

$("#DatasetConfirmButton").on("click", function () {
    if (!datasetUploaded) {
        return;
    }

    const items = datasetColumnsGrid.dataSource.data();
    const mapping = {
        id: datasetUploaded.id,
        columns: []
    };

    for (let i = 0; i < items.length; i++) {
        mapping.columns.push({
            index: items[i].index,
            include: items[i].include === true,
            name: items[i].name,
            processingTypeId: Number(items[i].processingTypeId)
        });
    }

    $.ajax({
        url: DatasetKindEndpoint(datasetUploaded.kind) + "/Confirm",
        type: "PUT",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify(mapping),
        success: function (dataset) {
            datasetUploaded = null;
            CollapseDatasetUpload();
            datasetPicker.dataSource.read().then(function () {
                datasetPicker.value(DatasetReferenceValue(dataset));
                SetDataSourceId(2);
                ExpandCollapseDataset();
                RefreshExhaustiveFilterBuilder();
            });
        },
        error: function (jqXHR) {
            let messages = [];
            try {
                const response = JSON.parse(jqXHR.responseText);
                messages = (response.errors || []).map(function (e) {
                    return e.errorMessage;
                });
                if (response.message) {
                    messages.push(response.message);
                }
            } catch (e) {
                messages = [];
            }

            ShowDatasetMessage(messages.length > 0 ? messages.join(" ") : "The mapping could not be saved.", true);
        }
    });
});

$("#DatasetDeleteButton").on("click", function () {
    const value = datasetPicker.value();
    if (value === "" || !confirm("Are you sure you want to delete this dataset?")) {
        return;
    }

    const parts = value.split(":");
    $.ajax({
        url: DatasetKindEndpoint(parts[0]) + "/" + parts[1],
        type: "DELETE",
        success: function () {
            datasetPicker.dataSource.read().then(function () {
                datasetPicker.value("");
            });
        },
        error: function () {
            alert("The dataset could not be deleted. It may still be used by an Exhaustive Adaptation that has not finished training.");
        }
    });
});
