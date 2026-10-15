function ShowBatchRecallMessage(text, isError) {
    $("#BatchRecallMessage").text(text).css("color", isError ? "#b00020" : "inherit");
}

function BatchRecallErrorText(jqXHR) {
    try {
        const response = JSON.parse(jqXHR.responseText);
        if (response.errors && response.errors.length > 0) {
            return response.errors.join(" ");
        }

        if (response.message) {
            return response.message;
        }
    } catch (e) {
        return "The file could not be processed.";
    }

    return "The file could not be processed.";
}

function RenderBatchRecallResult(data) {
    const summaryParts = [
        data.rowCount + " row" + (data.rowCount === 1 ? "" : "s") + ", " + data.scoredCount + " scored"
    ];

    if (data.unmappedVariables && data.unmappedVariables.length > 0) {
        summaryParts.push("No column found for: " + data.unmappedVariables.join(", "));
    }

    if (data.warnings && data.warnings.length > 0) {
        summaryParts.push(data.warnings.join(" "));
    }

    $("#BatchRecallSummary").text(summaryParts.join(". "));

    $("#BatchRecallGrid").kendoGrid({
        dataSource: {data: data.rows || []},
        height: 420,
        scrollable: true,
        sortable: true,
        columns: [
            {field: "rowNumber", title: "Row", width: "80px"},
            {field: "score", title: "Score", width: "100px"},
            {field: "baseValue", title: "Base value", width: "110px"},
            {field: "error", title: "Error"}
        ]
    });
}

var batchRecallUpload = $("#BatchRecallFile").kendoUpload({
    multiple: false,
    async: {
        saveUrl: "../api/Invoke/ExhaustiveSearchInstance/" + Guid + "/Batch",
        autoUpload: true
    },
    select: function () {
        ShowBatchRecallMessage("", false);
        $("#BatchRecallSummary").text("");
    },
    upload: function (e) {
        ShowBatchRecallMessage("Scoring " + e.files[0].name + "...", false);
    },
    success: function (e) {
        if (e.operation !== "upload") {
            return;
        }

        let result;
        try {
            result = JSON.parse(e.XMLHttpRequest.responseText);
        } catch (ex) {
            ShowBatchRecallMessage("The file could not be processed.", true);
            return;
        }

        ShowBatchRecallMessage("", false);
        RenderBatchRecallResult(result);
    },
    error: function (e) {
        ShowBatchRecallMessage(BatchRecallErrorText(e.XMLHttpRequest), true);
    },
    complete: function () {
        batchRecallUpload.clearAllFiles();
    }
}).data("kendoUpload");
