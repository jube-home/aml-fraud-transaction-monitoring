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

const instanceEndpoint = "/api/AdvancedAnalyticsInstance";
const modelEndpoint = "/api/AdvancedAnalyticsModel";
const chartEndpoint = "/api/Chart/AnalyticsModelVersion";
const processingFailed = "Processing failed.  Please contact Support to check logs for the source of the error.";
const noPermission = "You do not have the permission needed for this action.";
const kindNames = {
    1: "Classifier",
    2: "Regressor",
    3: "Anomaly Detector",
    4: "Clusterer",
    5: "Forecaster",
    6: "Sequence Model",
    7: "Dimension Reducer",
    8: "Density Estimator"
};
const sourceNames = {1: "Agent", 2: "Upload"};

let canDelete = false;
let canDeleteAllocated = false;
let canSetActive = false;
let modelNames = {};
let instanceApprovalById = {};
let instanceApprovalUnavailable = false;
let instanceApprovalRequestSeq = 0;
let instanceApprovalStatusLoaded = false;
let instanceDeletedIds = {};

function instanceIsVisible(id) {
    if (!instanceDeletedIds[id]) {
        return true;
    }
    const status = instanceApprovalById[id];
    return !!(status && (status.pending || status.rejected));
}

function esc(value) {
    return kendo.htmlEncode(value === null || value === undefined ? "" : String(value));
}

function showError(message) {
    $("#ErrorMessage").text(message || "");
}

function describeFailure(xhr) {
    if (xhr.status === 401 || xhr.status === 403) {
        return noPermission;
    }
    if (xhr.status === 404) {
        return "The item was not found.";
    }
    if (xhr.status === 409 && xhr.responseJSON) {
        return xhr.responseJSON.message + " (" + xhr.responseJSON.reason + ")";
    }
    if (xhr.status === 400 && xhr.responseJSON && xhr.responseJSON.reasons) {
        return xhr.responseJSON.reasons.join(" ") || xhr.responseJSON.message;
    }
    if (xhr.status === 400 && xhr.responseJSON && xhr.responseJSON.errors) {
        return xhr.responseJSON.errors.map(function (e) {
            return e.errorMessage;
        }).join(" ");
    }
    return processingFailed;
}

function call(method, url, body) {
    const options = {url: url, type: method};
    if (method !== "DELETE") {
        options.dataType = "json";
    }
    if (body !== undefined) {
        options.contentType = "application/json";
        options.data = JSON.stringify(body);
    }
    return $.ajax(options);
}

function fail(xhr) {
    showError(describeFailure(xhr));
}

function loadChart(selector, url) {
    const container = $(selector);
    const request = (container.data("chartRequest") || 0) + 1;
    container.data("chartRequest", request);
    kendo.ui.progress(container, true);
    $.ajax({url: url, type: "GET", dataType: "text"}).done(function (svg) {
        if (container.data("chartRequest") !== request) {
            return;
        }
        if (typeof svg === "string" && svg.trim().indexOf("<svg") === 0) {
            container.html(svg);
        } else {
            container.html('<div class="jc-message">Chart could not be loaded.</div>');
        }
    }).fail(function () {
        if (container.data("chartRequest") === request) {
            container.html('<div class="jc-message">Chart could not be loaded.</div>');
        }
    }).always(function () {
        if (container.data("chartRequest") === request) {
            kendo.ui.progress(container, false);
        }
    });
}

function refreshLookups() {
    return call("GET", "/api/EntityAnalysisModel").done(function (models) {
        modelNames = {};
        models.forEach(function (m) {
            modelNames[m.id] = m.name;
        });
    }).fail(fail);
}

function refreshInstances() {
    return refreshLookups().always(function () {
        $("#InstanceGrid").data("kendoGrid").dataSource.read();
    });
}

function deleteInstance(item) {
    const allowed = item.entityAnalysisModelId ? canDeleteAllocated : canDelete;
    if (!allowed) {
        showError(item.entityAnalysisModelId
            ? "Deleting an allocated instance needs the Advanced Analytics and Promote Model permissions."
            : "Deleting needs the Advanced Analytics permission.");
        return;
    }
    if (!confirm("Delete the Advanced Analytics instance \"" + item.name + "\"?")) {
        return;
    }
    showError("");
    call("DELETE", instanceEndpoint + "/" + item.id).done(function () {
        refreshInstances();
    }).fail(fail);
}

function instanceApprovalStateOf(item) {
    if (instanceApprovalUnavailable) {
        return {label: "Status unavailable, reload to check", colour: "orange"};
    }
    const status = instanceApprovalById[item.id];
    const deleted = !!item.deletedUser;
    if (status && status.pending) {
        return deleted
            ? {label: "Deletion awaiting approval", colour: "orange"}
            : {label: "Awaiting approval", colour: "orange"};
    }
    if (status && status.rejected) {
        return deleted
            ? {label: "Deletion rejected", colour: "purple"}
            : {label: "Rejected", colour: "purple"};
    }
    return {label: "Approved", colour: ""};
}

function loadInstanceApprovalStatus(items) {
    const seq = ++instanceApprovalRequestSeq;

    function applyIfCurrent(apply) {
        if (seq !== instanceApprovalRequestSeq) {
            return;
        }
        apply();
    }

    const modelIds = [];
    items.forEach(function (item) {
        if (item.entityAnalysisModelId && modelIds.indexOf(item.entityAnalysisModelId) === -1) {
            modelIds.push(item.entityAnalysisModelId);
        }
    });

    if (modelIds.length === 0) {
        applyIfCurrent(function () {
            instanceApprovalById = {};
            instanceApprovalUnavailable = false;
            instanceApprovalStatusLoaded = true;
        });
        return $.Deferred().resolve().promise();
    }

    const collected = {};

    return $.when.apply($, modelIds.map(function (modelId) {
        return $.ajax({
            url: "/api/EntityApproval/ValueStatus",
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify({kind: 1, entityId: modelId})
        }).done(function (rows) {
            rows.forEach(function (row) {
                collected[row.entityId] = row;
            });
        });
    })).then(function () {
        applyIfCurrent(function () {
            instanceApprovalById = collected;
            instanceApprovalUnavailable = false;
            instanceApprovalStatusLoaded = true;
        });
    }, function () {
        applyIfCurrent(function () {
            instanceApprovalUnavailable = true;
            instanceApprovalStatusLoaded = true;
        });
    });
}

function approveOrRejectInstance(instanceId, approve, note) {
    const status = instanceApprovalById[instanceId];
    if (!status) {
        return;
    }
    showError("");
    call("POST", "/api/EntityApproval/" + (approve ? "Approve" : "Reject"), {
        kind: status.kind,
        entityId: status.entityId,
        version: status.currentVersion,
        note: note || null
    }).done(function () {
        refreshInstances();
    }).fail(fail);
}

function resubmitOrRetractInstance(instanceId, action, note) {
    showError("");
    call("POST", instanceEndpoint + "/" + instanceId + "/" + action, {note: note || null}).done(function () {
        refreshInstances();
    }).fail(fail);
}

function approveAllPendingInstances() {
    const modelIds = [];
    $("#InstanceGrid").data("kendoGrid").dataSource.data().forEach(function (item) {
        if (item.entityAnalysisModelId && modelIds.indexOf(item.entityAnalysisModelId) === -1) {
            modelIds.push(item.entityAnalysisModelId);
        }
    });
    showError("");
    $.when.apply($, modelIds.map(function (modelId) {
        return $.ajax({
            url: "/api/EntityApproval/ApproveValues",
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify({kind: 1, entityId: modelId})
        });
    })).done(function () {
        refreshInstances();
    }).fail(fail);
}

function setInstanceActive(toggle, wanted) {
    const id = parseInt(toggle.attr("id"), 10);
    const widget = toggle.data("kendoSwitch");
    showError("");
    if (widget) {
        widget.enable(false);
    }
    call("PUT", instanceEndpoint + "/" + id + "/active", {active: wanted}).done(function () {
        refreshInstances();
    }).fail(function (xhr) {
        fail(xhr);
        if (widget) {
            widget.check(!wanted);
            widget.enable(canSetActive);
        }
    });
}

function buildInstanceGrid() {
    const $instanceGrid = $("#InstanceGrid");
    const grid = $instanceGrid.kendoGrid({
        dataSource: {
            transport: {
                read: {
                    url: instanceEndpoint, dataType: "json",
                    data: function () {
                        return {includeDeleted: true};
                    }
                }
            },
            schema: {
                model: {
                    id: "id",
                    fields: {
                        name: {type: "string"},
                        active: {type: "boolean"},
                        entityAnalysisModelId: {type: "number"},
                        responsePayload: {type: "boolean"},
                        reportTable: {type: "boolean"},
                        deletedUser: {type: "string", defaultValue: null},
                        createdDate: {type: "date"}
                    }
                }
            },
            pageSize: 10
        },
        sortable: true,
        pageable: true,
        detailInit: instanceDetailInit,
        dataBound: function () {
            const instanceGrid = this;
            instanceGrid.tbody.find("tr").each(function () {
                const item = instanceGrid.dataItem(this);
                if (item && !instanceIsVisible(item.id)) {
                    $(this).hide();
                    return;
                }
                $(this).toggleClass("aa-off", !!item && !item.active);
            });
            instanceGrid.tbody.find(".aa-instance-active-switch").each(function () {
                const toggle = $(this);
                toggle.kendoSwitch({
                    enabled: canSetActive,
                    change: function (e) {
                        setInstanceActive(toggle, e.checked);
                    }
                });
            });
        },
        scrollable: true,
        columns: [
            {field: "name", title: "Name", width: 200},
            {
                field: "active", title: "Active", width: 130, template: function (d) {
                    return '<input id="' + d.id + '" type="checkbox" class="aa-instance-active-switch"' +
                        (d.active === true ? ' checked="checked"' : '') + '/>';
                }
            },
            {
                field: "entityAnalysisModelId", title: "Allocated Model", width: 200, template: function (d) {
                    return d.entityAnalysisModelId ? esc(modelNames[d.entityAnalysisModelId] || d.entityAnalysisModelId) : "Not allocated";
                }
            },
            {
                field: "responsePayload", title: "Response Payload", width: 150, template: function (d) {
                    return d.responsePayload ? "Yes" : "No";
                }
            },
            {
                field: "reportTable", title: "Report Table", width: 130, template: function (d) {
                    return d.reportTable ? "Yes" : "No";
                }
            },
            {
                title: "Approval", width: 180, template: function (d) {
                    const state = instanceApprovalStateOf(d);
                    const status = instanceApprovalById[d.id];
                    const progress = status && status.pending
                        ? " (" + status.approvalsRecorded + " of " + status.approvalsRequired + ")"
                        : "";
                    return '<span style="color:' + (state.colour || "") + '">' +
                        esc(state.label + progress) + "</span>";
                }
            },
            {
                title: "Made by", width: 120, template: function (d) {
                    const status = instanceApprovalById[d.id];
                    return esc(status && status.makerUser ? status.makerUser : "");
                }
            },
            {
                title: "Checker note", width: 230, template: function (d) {
                    const status = instanceApprovalById[d.id];
                    if (status && status.pending && status.canApprove) {
                        return '<input type="text" class="k-textbox aa-instance-approval-note" ' +
                            'placeholder="Note (optional)" autocomplete="off"/>';
                    }
                    return esc(status && status.note ? status.note : "");
                }
            },
            {
                title: "Return note", width: 230, template: function (d) {
                    const status = instanceApprovalById[d.id];
                    if (status && status.rejected) {
                        const prefix = status.returnedNote ? esc(status.returnedNote) + " " : "";
                        return prefix + '<input type="text" class="k-textbox aa-instance-action-note" ' +
                            'placeholder="Note (optional)" autocomplete="off"/>';
                    }
                    return esc(status && status.returnedNote ? status.returnedNote : "");
                }
            },
            {
                title: "", width: 220, template: function (d) {
                    const status = instanceApprovalById[d.id];
                    if (!status) {
                        return "";
                    }
                    if (status.pending) {
                        if (!status.canApprove) {
                            return '<span style="color:orange;white-space:nowrap">Awaiting a checker</span>';
                        }
                        return '<span style="white-space:nowrap">' +
                            '<button type="button" class="k-button aa-instance-approve" data-id="' + d.id + '">Approve</button> ' +
                            '<button type="button" class="k-button aa-instance-reject" data-id="' + d.id + '">Reject</button>' +
                            '</span>';
                    }
                    if (status.rejected) {
                        let buttons = '<span style="white-space:nowrap">' +
                            '<button type="button" class="k-button aa-instance-resubmit" data-id="' + d.id + '">Resubmit</button> ';
                        if (status.effectiveVersion !== null && status.effectiveVersion !== undefined) {
                            buttons += '<button type="button" class="k-button aa-instance-retract" data-id="' + d.id + '">Retract</button>';
                        }
                        return buttons + "</span>";
                    }
                    return "";
                }
            },
            {field: "createdDate", title: "Created", format: "{0:yyyy-MM-dd HH:mm}", width: 160},
            {
                command: [{
                    name: "delete", text: "Delete", visible: function (d) {
                        return !d.deletedUser;
                    }, click: function (e) {
                        e.preventDefault();
                        const item = this.dataItem($(e.currentTarget).closest("tr"));
                        deleteInstance(item);
                    }
                }], width: 100
            }
        ]
    }).data("kendoGrid");

    const rawTotal = grid.dataSource.total.bind(grid.dataSource);
    grid.dataSource.total = function () {
        if (!instanceApprovalStatusLoaded) {
            return rawTotal();
        }
        return grid.dataSource.data().filter(function (row) {
            return instanceIsVisible(row.id);
        }).length;
    };

    grid.dataSource.bind("change", function () {
        instanceDeletedIds = {};
        grid.dataSource.data().forEach(function (row) {
            if (row.deletedUser) {
                instanceDeletedIds[row.id] = true;
            }
        });
    });

    grid.dataSource.bind("requestEnd", function (e) {
        if (e.type === "read") {
            loadInstanceApprovalStatus(grid.dataSource.data()).done(function () {
                grid.refresh();
            });
        }
    });

    $instanceGrid.off("click.instanceApproval")
        .on("click.instanceApproval", ".aa-instance-approve, .aa-instance-reject", function (e) {
            e.preventDefault();
            const note = $(this).closest("tr").find(".aa-instance-approval-note").val();
            approveOrRejectInstance(parseInt($(this).data("id"), 10), $(this).hasClass("aa-instance-approve"), note);
        });

    $instanceGrid.off("click.instanceAction")
        .on("click.instanceAction", ".aa-instance-resubmit, .aa-instance-retract", function (e) {
            e.preventDefault();
            const id = parseInt($(this).data("id"), 10);
            const action = $(this).hasClass("aa-instance-resubmit") ? "Resubmit" : "Retract";
            const note = $(this).closest("tr").find(".aa-instance-action-note").val();
            resubmitOrRetractInstance(id, action, note);
        });
}

function modelColumns() {
    return [
        {field: "name", title: "Name"},
        {
            field: "kind", title: "Kind", width: 140, template: function (d) {
                return esc(kindNames[d.kind] || d.kind);
            }
        },
        {
            field: "source", title: "Source", width: 100, template: function (d) {
                return esc(sourceNames[d.source] || d.source);
            }
        },
        {
            field: "active", title: "Live", width: 80, template: function (d) {
                return d.active ? "Yes" : "No";
            }
        },
        {
            field: "activeVersionNumber", title: "Live Version", width: 110, template: function (d) {
                return d.activeVersionNumber ? "v" + d.activeVersionNumber : "None";
            }
        },
        {field: "createdDate", title: "Created", format: "{0:yyyy-MM-dd HH:mm}", width: 160}
    ];
}

function versionColumns() {
    return [
        {
            field: "versionNumber", title: "Version", width: 90, template: function (d) {
                return "v" + d.versionNumber + (d.isActive ? " (live)" : "");
            }
        },
        {field: "testRows", title: "Test Rows", width: 100},
        {field: "testRocAuc", title: "Test AUC", format: "{0:n4}", width: 100},
        {field: "testAveragePrecision", title: "Test Avg Precision", format: "{0:n4}", width: 140},
        {field: "testLogLoss", title: "Test Log Loss", format: "{0:n4}", width: 120},
        {field: "testBrierScore", title: "Test Brier", format: "{0:n4}", width: 100},
        {field: "testExpectedCalibrationError", title: "Test ECE", format: "{0:n4}", width: 100},
        {
            field: "warningCodes", title: "Warnings", template: function (d) {
                return esc((d.warningCodes || []).join(", "));
            }
        },
        {field: "transformedVariableCount", title: "Transformed Variables", width: 170},
        {field: "createdDate", title: "Created", format: "{0:yyyy-MM-dd HH:mm}", width: 150}
    ];
}

function versionDetailInit(e) {
    $("<div/>").appendTo(e.detailCell).kendoGrid({
        dataSource: {
            transport: {read: {url: modelEndpoint + "/" + e.data.id + "/versions", dataType: "json"}},
            schema: {
                model: {
                    id: "id",
                    fields: {
                        versionNumber: {type: "number"},
                        isActive: {type: "boolean"},
                        testRows: {type: "number"},
                        testRocAuc: {type: "number"},
                        testAveragePrecision: {type: "number"},
                        testLogLoss: {type: "number"},
                        testBrierScore: {type: "number"},
                        testExpectedCalibrationError: {type: "number"},
                        transformedVariableCount: {type: "string"},
                        createdDate: {type: "date"}
                    }
                }
            }
        },
        sortable: true,
        selectable: "row",
        change: function () {
            const item = this.dataItem(this.select());
            if (item) {
                openEvidence(item);
            }
        },
        dataBound: function () {
            const grid = this;
            grid.tbody.find("tr").each(function () {
                const item = grid.dataItem(this);
                if (item && item.isActive) {
                    $(this).addClass("aa-live");
                }
            });
        },
        columns: versionColumns()
    });
}

function instanceDetailInit(e) {
    const modelGrid = $("<div/>").appendTo(e.detailCell);
    const modelDiagram = $('<div class="aa-model-diagram"></div>').appendTo(e.detailCell);
    modelGrid.kendoGrid({
        dataSource: {
            transport: {
                read: {
                    url: modelEndpoint + "?advancedAnalyticsInstanceId=" + e.data.id,
                    dataType: "json"
                }
            },
            schema: {
                model: {
                    id: "id",
                    fields: {
                        name: {type: "string"},
                        advancedAnalyticsInstanceId: {type: "number"},
                        kind: {type: "number"},
                        source: {type: "number"},
                        active: {type: "boolean"},
                        activeVersionId: {type: "number"},
                        activeVersionNumber: {type: "number"},
                        createdDate: {type: "date"}
                    }
                }
            }
        },
        sortable: true,
        selectable: "row",
        change: function () {
            const item = this.dataItem(this.select());
            if (!item) {
                return;
            }
            if (item.activeVersionId) {
                loadChart(modelDiagram, chartEndpoint + "/ModelDiagram/" + item.activeVersionId);
            } else {
                modelDiagram.html('<div class="jc-message">This model has no live version to diagram.</div>');
            }
        },
        detailInit: versionDetailInit,
        columns: modelColumns()
    });
}

function renderEvidenceTree(evidence) {
    renderEvidenceTreeInto("#EvidenceTree", evidence);
}

function renderNarrativeParagraphs(selector, paragraphs) {
    const $container = $(selector).empty();
    (paragraphs || []).forEach(function (paragraph) {
        $("<p></p>").text(paragraph).appendTo($container);
    });
}

function showChartCard(cardId, narrativeId, chartId, url, paragraphs, alwaysShow) {
    const has = alwaysShow || (paragraphs && paragraphs.length > 0);
    $("#" + cardId).toggleClass("aa-hidden", !has);
    renderNarrativeParagraphs("#" + narrativeId, paragraphs);
    if (has) {
        loadChart("#" + chartId, url);
    }
}

function layoutEvidenceCharts() {
    const visibleCount = $("#EvidenceCharts .aa-chart-card:not(.aa-hidden)").length;
    $("#EvidenceCharts").css("grid-template-columns",
        "repeat(" + Math.max(1, Math.min(visibleCount, 3)) + ", minmax(0, 1fr))");
}

function fetchNarrative(versionId) {
    const deferred = $.Deferred();
    call("GET", chartEndpoint + "/Narrative/" + versionId)
        .done(function (data) {
            deferred.resolve(data);
        })
        .fail(function () {
            deferred.resolve({});
        });
    return deferred.promise();
}

function openEvidence(version) {
    showError("");
    $.when(
        call("GET", modelEndpoint + "/Version/" + version.id),
        call("GET", modelEndpoint + "/Version/" + version.id + "/PromotionEvidence"),
        fetchNarrative(version.id)
    ).done(function (versionResult, promotionResult, narrative) {
        const evidence = versionResult[0].evidence;
        const promotion = promotionResult[0];
        const groups = narrative.groups || {};
        if (!evidence) {
            showError("This version has no stored evidence.");
            return;
        }
        $("#EvidenceEmpty").addClass("aa-hidden");
        $("#EvidencePanel").removeClass("aa-hidden");
        $("#EvidenceTitle").text((promotion.modelName || "Model") + " version " + promotion.versionNumber);
        window.AskJoobySetPageContext?.("AdvancedAnalytics", "readonly", {
            versionId: version.id,
            modelName: promotion.modelName,
            versionNumber: promotion.versionNumber
        });
        $("#EvidenceLive").toggleClass("aa-hidden", !promotion.isActiveVersion);
        $("#EvidenceAllocation").text(promotion.instanceAllocated ? "Instance allocated to Jube model " + (modelNames[promotion.allocatedEntityAnalysisModelId] || promotion.allocatedEntityAnalysisModelId) : "Instance not allocated to a Jube model");
        renderEvidenceTree(evidence);
        renderNarrativeParagraphs("#NarrativeHolistic", narrative.holistic);
        showChartCard("DiagramCard", "NarrativeModelDiagram", "DiagramChart",
            chartEndpoint + "/ModelDiagram/" + version.id, groups.ModelDiagram, true);
        showChartCard("CalibrationCard", "NarrativeCalibration", "CalibrationChart",
            chartEndpoint + "/Calibration/" + version.id, groups.Calibration, false);
        showChartCard("RocCard", "NarrativeRoc", "RocChart",
            chartEndpoint + "/Roc/" + version.id, groups.Roc, false);
        layoutEvidenceCharts();
        document.getElementById("EvidencePanel").scrollIntoView({behavior: "smooth", block: "start"});
    }).fail(fail);
}

$(document).ready(function () {
    window.AskJoobySetPageContext?.("AdvancedAnalytics", "list", null);

    canDelete = $("#CanDelete").val() === "true";
    canDeleteAllocated = $("#CanDeleteAllocated").val() === "true";
    canSetActive = $("#CanSetActive").val() === "true";
    const $refreshInstances = $("#RefreshInstances").kendoButton();
    const $approveAllPendingInstances = $("#ApproveAllPendingInstances").kendoButton();
    buildInstanceGrid();
    $("#InstanceGrid").on("mousedown click", ".k-switch", function (e) {
        e.stopPropagation();
    });
    $refreshInstances.on("click", function () {
        showError("");
        refreshInstances();
    });
    $approveAllPendingInstances.on("click", function () {
        approveAllPendingInstances();
    });
    refreshInstances();
});
