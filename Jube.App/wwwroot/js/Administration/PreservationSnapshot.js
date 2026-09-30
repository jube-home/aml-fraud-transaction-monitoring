// noinspection ES6ConvertVarToLetConst,JSUnresolvedVariable,HtmlUnknownAttribute

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

const processingFailed = "Processing failed.  Please contact Support to check logs for the source of the error.";

const snapshotSources = {1: "Preservation Page", 2: "Model Sync", 3: "AskJooby"};

function SnapshotSourceText(snapshotSourceId) {
    return snapshotSources[snapshotSourceId] || snapshotSourceId;
}

function BytesText(bytes) {
    if (bytes === null || bytes === undefined) {
        return "";
    }

    if (bytes < 1024) {
        return bytes + " B";
    }

    if (bytes < 1048576) {
        return (bytes / 1024).toFixed(1) + " KB";
    }

    return (bytes / 1048576).toFixed(1) + " MB";
}

function ShowError(message) {
    $("#ErrorMessage").html('<div class="server-error-box"><div class="server-error-title">' + message + '</div></div>');
}

function Refresh() {
    $("#grid").data("kendoGrid").dataSource.read();
}

function TakeSnapshot() {
    $('#Updating').show();
    $("#ErrorMessage").empty();

    $.ajax({
        url: "/api/PreservationSnapshot",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({
            Name: $("#Name").val(),
            SnapshotSourceId: 1,
            Exhaustive: false,
            Lists: true,
            Dictionaries: true,
            Visualisations: true
        }),
        error: function () {
            $('#Updating').fadeOut();
            ShowError(processingFailed);
        },
        success: function () {
            $('#Updating').fadeOut();
            $("#Name").val("");
            Refresh();
        }
    });
}

function ImportSnapshot(id) {
    $('#Updating').show();
    $("#ErrorMessage").empty();

    $.ajax({
        url: "/api/PreservationSnapshot/" + id + "/Import",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        error: function () {
            $('#Updating').fadeOut();
            ShowError(processingFailed);
        },
        success: function () {
            $('#Updating').fadeOut();
            Refresh();
        }
    });
}

$(document).ready(function () {
    $("#Take").kendoButton({click: TakeSnapshot});

    // noinspection JSObsoletePrivateAccessSyntax
    $("#grid").kendoGrid({
        dataSource: {
            transport: {
                read: {
                    url: "/api/PreservationSnapshot",
                    dataType: "json"
                }
            },
            schema: {
                model: {
                    id: "id",
                    fields: {
                        id: {type: "number"},
                        snapshotSourceId: {type: "number"},
                        name: {type: "string"},
                        entityAnalysisModelCount: {type: "number"},
                        bytes: {type: "number"},
                        inError: {type: "boolean"},
                        createdUser: {type: "string"},
                        createdDate: {type: "date"},
                        completedDate: {type: "date"}
                    }
                }
            }
        },
        height: 600,
        sortable: true,
        filterable: true,
        noRecords: {
            template: "No snapshots have been taken for this tenant."
        },
        dataBound: function () {
            const grid = this;

            grid.tbody.find(".importSnapshot").each(function () {
                const button = $(this);
                const dataItem = grid.dataItem(button.closest("tr"));

                button.kendoButton({
                    click: function () {
                        ImportSnapshot(dataItem.id);
                    }
                });

                const importable = dataItem.inError !== true && dataItem.completedDate !== null;

                if (!importable || canImportSnapshot !== true) {
                    button.data("kendoButton").enable(false);
                }

                if (canImportSnapshot !== true) {
                    button.attr("title", "Importing a snapshot requires the Import Preservation Snapshot permission.");
                }
            });
        },
        columns: [
            {
                field: "id",
                title: "Id",
                width: 90
            },
            {
                title: "Import",
                width: 110,
                filterable: false,
                sortable: false,
                template: '<button type="button" class="importSnapshot">Import</button>'
            },
            {
                field: "snapshotSourceId",
                title: "Source",
                width: 170,
                template: '#= SnapshotSourceText(snapshotSourceId) #'
            },
            {
                field: "name",
                title: "Name"
            },
            {
                field: "entityAnalysisModelCount",
                title: "Models",
                width: 110
            },
            {
                field: "bytes",
                title: "Size",
                width: 120,
                filterable: false,
                template: '#= BytesText(bytes) #'
            },
            {
                field: "createdUser",
                title: "Created User",
                width: 180
            },
            {
                field: "createdDate",
                title: "Created Date",
                width: 200,
                format: "{0:yyyy-MM-dd HH:mm}"
            },
            {
                field: "inError",
                title: "In Error",
                width: 110,
                template: "#= inError ? 'Yes' : 'No' #"
            }
        ]
    });
});

//# sourceURL=PreservationSnapshot.js
