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

const valueGridStyles = [
    ".value-grid .k-grid td, .value-grid .k-grid th { padding: 8px 12px; vertical-align: middle; }",
    ".value-grid .k-grid .k-button { min-width: 64px; margin: 0 2px; }",
    ".value-grid .k-grid td.k-command-cell .k-button { min-width: 64px; }",
    ".value-grid .k-grid-edit-row td { padding: 8px 12px; white-space: nowrap; }",
    ".value-grid .k-grid-edit-row .k-textbox, .value-grid .k-grid-edit-row .k-numerictextbox { width: 100%; min-width: 0; }",
    ".value-grid .k-grid td.k-command-cell { white-space: nowrap; }",
    ".value-grid .k-grid td.k-command-cell .k-button { min-width: 64px; }"
].join("\n");

function ensureValueGridStyles() {
    if (document.getElementById("value-grid-styles")) {
        return;
    }

    $("<style id=\"value-grid-styles\"></style>").text(valueGridStyles).appendTo("head");
}

function createValueGrid(options) {
    ensureValueGridStyles();

    const host = $("#valueGrid").addClass("value-grid");
    const existing = host.data("kendoGrid");
    if (existing) {
        existing.destroy();
    }
    host.empty();

    const statusById = {};
    let statusUnavailable = false;
    let deletedIds = {};
    let statusRequestSeq = 0;
    let statusLoaded = false;

    function isVisible(id) {
        if (!deletedIds[id]) {
            return true;
        }
        const status = statusById[id];
        return !!(status && (status.pending || status.rejected));
    }

    function reasonOf(xhr, fallback) {
        if (xhr.responseJSON && xhr.responseJSON.reasons) {
            return xhr.responseJSON.reasons.join(" ");
        }
        return (xhr.responseJSON && xhr.responseJSON.message) || fallback;
    }

    function report(text) {
        $("#ValueMessage").text(text || "");
    }

    function payloadOf(model) {
        const body = {};
        body[options.parentField] = options.parentId;
        options.valueFields.forEach(function (field) {
            body[field.field] = model[field.field];
        });
        body.deleteExpiryDate = model.deleteExpiryDate;
        return body;
    }

    function modelFields() {
        const fields = {
            id: {editable: false, nullable: true},
            deleteExpiryDate: {type: "date", defaultValue: null},
            state: {type: "string", defaultValue: "", editable: false},
            madeByUser: {type: "string", defaultValue: "", editable: false}
        };
        options.valueFields.forEach(function (field) {
            fields[field.field] = {type: field.type};
        });
        return fields;
    }

    function syncComputedFields() {
        dataSource.data().forEach(function (row) {
            const status = statusById[row.id];
            const state = stateOf(row).label;
            const madeByUser = (status && status.makerUser) || "";
            if (row.state !== state) {
                row.set("state", state);
            }
            if (row.madeByUser !== madeByUser) {
                row.set("madeByUser", madeByUser);
            }
        });
    }

    function loadStatus() {
        const seq = ++statusRequestSeq;

        function applyIfCurrent(apply) {
            if (seq !== statusRequestSeq) {
                return;
            }
            apply();
            statusLoaded = true;
            syncComputedFields();
            const grid = host.data("kendoGrid");
            if (grid && !host.find(".k-grid-edit-row").length) {
                grid.refresh();
            }
        }

        return $.ajax({
            url: "/api/EntityApproval/ValueStatus",
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify({kind: options.parentKind, entityId: options.parentId})
        })
            .done(function (rows) {
                applyIfCurrent(function () {
                    statusUnavailable = false;
                    Object.keys(statusById).forEach(function (key) {
                        delete statusById[key];
                    });
                    rows.forEach(function (row) {
                        statusById[row.entityId] = row;
                    });
                });
            })
            .fail(function () {
                applyIfCurrent(function () {
                    statusUnavailable = true;
                });
            });
    }

    function stateOf(d) {
        if (statusUnavailable) {
            return {label: "Status unavailable, reload to check", css: "value-state-pending", colour: "orange"};
        }
        const status = statusById[d.id];
        const deleted = !!d.deletedUser;
        if (status && status.pending) {
            return deleted
                ? {label: "Deletion awaiting approval", css: "value-state-pending", colour: "orange"}
                : {label: "Awaiting approval", css: "value-state-pending", colour: "orange"};
        }
        if (status && status.rejected) {
            return deleted
                ? {label: "Deletion rejected, edit to restore", css: "value-state-rejected", colour: "purple"}
                : {label: "Rejected", css: "value-state-rejected", colour: "purple"};
        }
        return {label: "Approved", css: "value-state-approved", colour: ""};
    }

    const dataSource = new kendo.data.DataSource({
        pageSize: 25,
        transport: {
            read: {
                url: options.readUrl,
                type: "GET"
            },
            create: {
                url: options.endpoint,
                dataType: "json",
                contentType: "application/json",
                type: "POST"
            },
            update: {
                url: options.endpoint,
                dataType: "json",
                contentType: "application/json",
                type: "PUT"
            },
            destroy: {
                url: options.endpoint,
                type: "DELETE"
            },
            parameterMap: function (data, operation) {
                if (operation === "read") {
                    const query = {includeDeleted: true};
                    query[options.readParameter] = options.parentId;
                    return query;
                }
                if (operation === "create") {
                    return JSON.stringify(payloadOf(data.models[0]));
                }
                if (operation === "update") {
                    return JSON.stringify(Object.assign({id: data.models[0].id}, payloadOf(data.models[0])));
                }
                if (operation === "destroy") {
                    return {id: data.models[0].id};
                }
            }
        },
        batch: true,
        schema: {
            model: {
                id: "id",
                fields: modelFields()
            }
        }
    });

    const rawTotal = dataSource.total.bind(dataSource);
    dataSource.total = function () {
        if (!statusLoaded) {
            return rawTotal();
        }
        return dataSource.data().filter(function (row) {
            return isVisible(row.id);
        }).length;
    };

    dataSource.bind("change", function () {
        deletedIds = {};
        dataSource.data().forEach(function (row) {
            if (row.deletedUser) {
                deletedIds[row.id] = true;
            }
        });
    });

    dataSource.bind("requestEnd", function (e) {
        if (e.type !== "read") {
            dataSource.read();
        } else {
            loadStatus();
        }
    });

    dataSource.bind("error", function (e) {
        HandleDataSourceError(e);
    });

    const valueColumns = options.valueFields.map(function (field) {
        return {
            field: field.field,
            title: field.title,
            width: 220,
            template: function (d) {
                return kendo.htmlEncode(d[field.field] === null || d[field.field] === undefined ? "" : String(d[field.field]));
            }
        };
    });

    const grid = host.kendoGrid({
        dataSource: dataSource,
        editable: {
            mode: "inline",
            confirmation: "Delete this value? It stays listed as a pending deletion until a checker approves it."
        },
        sortable: true,
        filterable: true,
        pageable: {pageSizes: [10, 25, 50, 100], buttonCount: 5},
        scrollable: false,
        noRecords: {template: "No values match the current filter."},
        dataBound: function (e) {
            e.sender.tbody.find("tr").each(function () {
                const dataItem = e.sender.dataItem(this);
                if (dataItem && !isVisible(dataItem.id)) {
                    $(this).hide();
                    return;
                }
                const colour = stateOf(dataItem).colour || "";
                $(this).css("color", colour);
                $(this).children("td").css("color", colour);
            });
        },
        columns: valueColumns.concat([
            {
                field: "deleteExpiryDate",
                title: "Delete expiry",
                width: 150,
                format: "{0:yyyy-MM-dd HH:mm}"
            },
            {
                field: "state",
                title: "State",
                width: 230,
                editable: false,
                template: function (d) {
                    const status = statusById[d.id];
                    const progress = status && status.pending ? " (" + status.approvalsRecorded + " of " + status.approvalsRequired + ")" : "";
                    return kendo.htmlEncode(stateOf(d).label + progress);
                }
            },
            {
                field: "madeByUser",
                title: "Made by",
                width: 130,
                editable: false,
                template: function (d) {
                    const status = statusById[d.id];
                    return kendo.htmlEncode(status && status.makerUser ? status.makerUser : "");
                }
            },
            {
                title: "",
                width: 170,
                editable: false,
                template: function (d) {
                    const status = statusById[d.id];
                    if (!status || !status.pending) {
                        return "";
                    }
                    if (!status.canApprove) {
                        return "<span class=\"value-state-pending\">Awaiting a checker</span>";
                    }
                    return "<button type=\"button\" class=\"k-button value-approve\" data-id=\"" + d.id + "\">Approve</button> " +
                        "<button type=\"button\" class=\"k-button value-reject\" data-id=\"" + d.id + "\">Reject</button>";
                }
            },
            {
                command: [
                    {name: "edit", text: "Edit"},
                    {name: "destroy", text: "Delete"}
                ],
                title: "",
                width: 160
            }
        ])
    }).data("kendoGrid");

    host.off("click.valueGrid").on("click.valueGrid", ".value-approve, .value-reject", function (e) {
        e.preventDefault();
        const status = statusById[$(this).data("id")];
        if (!status) {
            return;
        }
        const action = $(this).hasClass("value-approve") ? "Approve" : "Reject";
        $.ajax({
            url: "/api/EntityApproval/" + action,
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                kind: status.kind,
                entityId: status.entityId,
                version: status.currentVersion,
                note: null
            }),
            success: function () {
                report("");
                loadStatus();
            },
            error: function (xhr) {
                report(reasonOf(xhr, "The " + action.toLowerCase() + " was refused."));
            }
        });
    });

    $("#valueGridToolbar").empty().append(
        $("<button type=\"button\" class=\"k-button\">Approve all pending</button>").kendoButton({
            click: function () {
                $.ajax({
                    url: "/api/EntityApproval/ApproveValues",
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify({kind: options.parentKind, entityId: options.parentId}),
                    success: function (result) {
                        report(result.refusals && result.refusals.length ? result.refusals.join(" ") : "");
                        loadStatus();
                    },
                    error: function (xhr) {
                        report(reasonOf(xhr, "The values could not be approved."));
                    }
                });
            }
        }));

    return grid;
}
