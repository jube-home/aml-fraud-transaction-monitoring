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

var ApprovalKinds = {
    "/api/EntityAnalysisModel": {kind: 1, historyRoute: "EntityAnalysisModelVersionHistory"},
    "/api/EntityAnalysisModelList": {kind: 2, historyRoute: "EntityAnalysisModelListVersionHistory"},
    "/api/EntityAnalysisModelDictionary": {kind: 4, historyRoute: "EntityAnalysisModelDictionaryVersionHistory"},
    "/api/EntityAnalysisModelRequestXPath": {kind: 6, historyRoute: "EntityAnalysisModelRequestXpathVersionHistory"},
    "/api/EntityAnalysisModelInlineScript": {kind: 7, historyRoute: "EntityAnalysisModelInlineScriptVersionHistory"},
    "/api/EntityAnalysisModelInlineFunction": {
        kind: 8,
        historyRoute: "EntityAnalysisModelInlineFunctionVersionHistory"
    },
    "/api/EntityAnalysisModelGatewayRule": {kind: 9, historyRoute: "EntityAnalysisModelGatewayRuleVersionHistory"},
    "/api/EntityAnalysisModelSanction": {kind: 10, historyRoute: "EntityAnalysisModelSanctionVersionHistory"},
    "/api/EntityAnalysisModelAbstractionRule": {
        kind: 11,
        historyRoute: "EntityAnalysisModelAbstractionRuleVersionHistory"
    },
    "/api/EntityAnalysisModelAbstractionCalculation": {
        kind: 12,
        historyRoute: "EntityAnalysisModelAbstractionCalculationVersionHistory"
    },
    "/api/EntityAnalysisModelTtlCounter": {kind: 13, historyRoute: "EntityAnalysisModelTtlCounterVersionHistory"},
    "/api/EntityAnalysisModelAdaptation": {kind: 14, historyRoute: "EntityAnalysisModelHttpAdaptationVersionHistory"},
    "/api/ExhaustiveSearchInstance": {kind: 15, historyRoute: "ExhaustiveSearchInstanceVersionHistory"},
    "/api/EntityAnalysisModelActivationRule": {
        kind: 16,
        historyRoute: "EntityAnalysisModelActivationRuleVersionHistory"
    },
    "/api/EntityAnalysisModelTag": {kind: 17, historyRoute: "EntityAnalysisModelTagVersionHistory"}
};

(function ($) {
    var kendo = window.kendo,
        ui = kendo.ui,
        Widget = ui.Widget;

    function text(value) {
        return $("<span></span>").text(value === null || value === undefined ? "" : String(value));
    }

    var styles = [
        ".k-approval-manager { margin: 12px 0; width: 100%; }",
        ".k-approval-manager .k-grid { width: 100%; }",
        ".k-approval-manager .k-grid td, .k-approval-manager .k-grid th { padding: 8px 12px; vertical-align: middle; }",
        ".k-approval-manager .k-grid .k-button { min-width: 72px; margin: 0 2px; }",
        ".k-approval-manager .k-approval-status { margin-bottom: 8px; line-height: 1.5; }",
        ".k-approval-manager .approval-pending { color: #b35c00; font-weight: 600; }",
        ".k-approval-manager .approval-rejected { color: #b00020; font-weight: 600; }",
        ".k-approval-manager .approval-meta { color: #5f6b7a; font-size: 12px; }",
        ".k-approval-manager .k-approval-actions { margin: 8px 0; display: flex; gap: 8px; align-items: flex-start; }",
        ".k-approval-manager .approval-note { width: 100%; min-width: 180px; }",
        ".k-approval-manager .k-approval-error { color: #b00020; margin: 6px 0; }",
        ".k-approval-manager .k-approval-notice { color: #1d6b34; font-weight: 600; margin: 6px 0; }",
        ".k-approval-manager .k-approval-section { margin: 20px 0 0; }",
        ".k-approval-manager .approval-heading { margin: 0 0 8px; font-size: 14px; font-weight: 600; }",
        ".k-approval-manager .approval-table { border-collapse: collapse; margin-top: 6px; }",
        ".k-approval-manager .approval-table th, .k-approval-manager .approval-table td { border: 1px solid #d5dbe3; padding: 4px 8px; text-align: left; }",
        ".k-approval-manager .changes { margin: 6px 0 0 0; padding-left: 18px; font-size: 12px; }"
    ].join("\n");

    function ensureStyles() {
        if (document.getElementById("approval-manager-styles")) {
            return;
        }

        $("<style id=\"approval-manager-styles\"></style>").text(styles).appendTo("head");
    }

    function when(value) {
        return value ? new Date(value).toLocaleString() : "";
    }

    function field(row, name) {
        var key = Object.keys(row).find(function (k) {
            return k.toLowerCase() === name.toLowerCase();
        });
        return key === undefined ? undefined : row[key];
    }

    var ApprovalManager = Widget.extend({
        init: function (element, options) {
            var that = this;
            Widget.fn.init.call(that, element, options);
            ensureStyles();
            that._create();
            that.refresh();
        },

        options: {
            name: "ApprovalManager",
            kind: null,
            entityId: null,
            historyRoute: null
        },

        events: ["changed"],

        _create: function () {
            var that = this;

            that.element.addClass("k-approval-manager");
            that.element.html(
                '<div class="k-approval-notice" role="status"></div>' +
                '<div class="k-approval-error" role="alert"></div>' +
                '<div class="k-approval-section k-approval-history"><h4 class="approval-heading">Approval history</h4><div class="history"></div></div>' +
                '<div class="k-approval-section k-approval-versions"><h4 class="approval-heading">Superseded versions</h4><div class="versions"></div></div>'
            );

            that.element.on("click", ".approve-btn", function (e) {
                e.preventDefault();
                that._decide("Approve");
            });

            that.element.on("click", ".reject-btn", function (e) {
                e.preventDefault();
                that._decide("Reject");
            });

        },

        refresh: function () {
            var that = this,
                options = that.options;

            $.ajax({
                url: "/api/EntityApproval/Status",
                method: "GET",
                data: {kind: options.kind, entityId: options.entityId},
                success: function (status) {
                    that.element.show();
                    that._renderStatus(status);
                    that.element.find(".k-approval-error").empty();
                    that._loadHistory();
                    that._loadVersions();
                },
                error: function () {
                    that.element.hide();
                    $("#TemplateTable tr.approval-row").remove();
                }
            });
        },

        _renderStatus: function (status) {
            var that = this,
                deleted = !!status.deleted,
                running = status.effectiveVersion === null || status.effectiveVersion === undefined
                    ? "Not running yet"
                    : String(status.effectiveVersion),
                state = status.rejected
                    ? {label: deleted ? "Deletion rejected" : "Rejected", css: "approval-rejected"}
                    : status.pending
                        ? {label: deleted ? "Deletion awaiting approval" : "Awaiting approval", css: "approval-pending"}
                        : {label: deleted ? "Deleted" : "Approved", css: ""},
                rows = [
                    ["Current version", String(status.currentVersion), ""],
                    ["Running version", running, ""],
                    ["State", state.label, state.css],
                    ["Made by", status.makerUser || "unknown", ""],
                    ["Approvals", status.approvalsRecorded + " of " + status.approvalsRequired, ""]
                ],
                form = $("#TemplateTable");

            that.element.data("currentVersion", status.currentVersion);
            that.element.data("canApprove", !!status.canApprove);
            that.element.data("makerUser", status.makerUser);
            that.element.data("deleted", deleted);
            $("#Delete").toggle(!deleted);

            form.find("tr.approval-row").remove();
            rows.forEach(function (row) {
                form.append($("<tr class=\"approval-row\"></tr>")
                    .append($("<td></td>").append($("<label></label>").text(row[0])))
                    .append($("<td></td>").append($("<span></span>").addClass(row[2]).text(row[1]))));
            });
        },

        _decide: function (action) {
            var that = this,
                options = that.options;

            var body = {
                kind: options.kind,
                entityId: options.entityId,
                version: that._currentVersion(),
                note: that.element.find(".approval-note").val() || null
            };

            $.ajax({
                url: "/api/EntityApproval/" + action,
                method: "POST",
                contentType: "application/json",
                data: JSON.stringify(body),
                success: function () {
                    that.element.find(".k-approval-notice").text("");
                    that.refresh();
                    that.trigger("changed");
                },
                error: function (xhr) {
                    var reasons = xhr.responseJSON && xhr.responseJSON.reasons
                        ? xhr.responseJSON.reasons
                        : [xhr.responseJSON && xhr.responseJSON.message ? xhr.responseJSON.message : "The " + action.toLowerCase() + " was refused."];
                    var error = that.element.find(".k-approval-error").empty();
                    $.each(reasons, function (_, reason) {
                        error.append($("<div></div>").append(text(reason)));
                    });
                }
            });
        },

        _currentVersion: function () {
            return this.element.data("currentVersion");
        },

        _pendingRow: function () {
            if (!this.element.data("canApprove")) {
                return [];
            }

            return [{
                version: this.element.data("currentVersion"),
                decision: "Awaiting decision",
                by: this.element.data("makerUser"),
                when: "",
                note: "",
                pending: true
            }];
        },

        _grid: function (target, data, columns, emptyText) {
            var existing = target.data("kendoGrid");
            if (existing) {
                existing.destroy();
            }

            target.empty().kendoGrid({
                dataSource: {data: data},
                sortable: true,
                scrollable: false,
                noRecords: {template: emptyText},
                columns: columns
            });
        },

        _loadHistory: function () {
            var that = this,
                options = that.options,
                target = that.element.find(".k-approval-history .history"),
                historyCall = $.ajax({
                    url: "/api/EntityApproval/History",
                    method: "GET",
                    data: {kind: options.kind, entityId: options.entityId}
                }),
                versionsCall = options.historyRoute
                    ? $.ajax({url: "/api/" + options.historyRoute + "/ByParent/" + options.entityId, method: "GET"})
                    : null;

            $.when(historyCall, versionsCall || $.Deferred().resolve([])).done(function (historyResult, versionsResult) {
                var rows = historyResult[0],
                    versionRows = versionsCall ? versionsResult[0] : [],
                    deletedAt = {};

                versionRows.forEach(function (row) {
                    if (field(row, "deletedUser") || field(row, "deleted")) {
                        deletedAt[field(row, "version")] = true;
                    }
                });

                var changeOf = function (version, isCurrent) {
                    if (isCurrent && that.element.data("deleted")) {
                        return "Deletion";
                    }
                    if (version === 1) {
                        return "Created";
                    }
                    if (deletedAt[version]) {
                        return "Deletion";
                    }
                    if (deletedAt[version - 1]) {
                        return "Restore";
                    }
                    return "Update";
                };

                var pending = that._pendingRow().map(function (row) {
                    row.change = changeOf(row.version, true);
                    return row;
                });

                that._grid(target, pending.concat(rows.map(function (row) {
                    var version = field(row, "entityVersion");
                    return {
                        version: version,
                        change: changeOf(version, false),
                        decision: field(row, "state"),
                        by: field(row, "createdUser"),
                        when: when(field(row, "createdDate")),
                        note: field(row, "note") || ""
                    };
                })), [
                    {field: "version", title: "Version", width: 90},
                    {field: "change", title: "Change", width: 110},
                    {field: "decision", title: "Decision", width: 130},
                    {field: "by", title: "By", width: 150},
                    {field: "when", title: "When", width: 170},
                    {
                        field: "note",
                        title: "Note",
                        template: function (d) {
                            return d.pending
                                ? "<input type=\"text\" class=\"k-textbox approval-note\" placeholder=\"Note (optional)\"/>"
                                : kendo.htmlEncode(d.note || "");
                        }
                    },
                    {
                        title: "",
                        width: 200,
                        template: function (d) {
                            return d.pending
                                ? "<button type=\"button\" class=\"k-button approve-btn\">Approve</button> " +
                                "<button type=\"button\" class=\"k-button reject-btn\">Reject</button>"
                                : "";
                        }
                    }
                ], "No approvals or rejections recorded.");
            }).fail(function () {
                target.empty().append(text("The approval history is not available to you."));
            });
        },

        _loadVersions: function () {
            var that = this,
                options = that.options,
                target = that.element.find(".k-approval-versions .versions");

            if (!options.historyRoute) {
                return;
            }

            $.ajax({
                url: "/api/" + options.historyRoute + "/ByParent/" + options.entityId,
                method: "GET",
                success: function (rows) {
                    var ordered = rows.slice().sort(function (a, b) {
                        return field(a, "version") - field(b, "version");
                    });

                    var data = ordered.map(function (row, index) {
                        return {
                            version: field(row, "version"),
                            by: field(row, "createdUser"),
                            when: when(field(row, "createdDate")),
                            id: field(row, "id"),
                            previousId: index > 0 ? field(ordered[index - 1], "id") : null
                        };
                    });

                    var existing = target.data("kendoGrid");
                    if (existing) {
                        existing.destroy();
                    }

                    target.empty().kendoGrid({
                        dataSource: {data: data},
                        sortable: true,
                        scrollable: false,
                        noRecords: {template: "No superseded versions yet."},
                        columns: [
                            {field: "version", title: "Version", width: 90},
                            {field: "by", title: "By", width: 150},
                            {field: "when", title: "When", width: 170}
                        ],
                        detailInit: function (e) {
                            var detail = e.detailCell;

                            if (e.data.previousId === null || e.data.previousId === undefined) {
                                detail.append(text("This is the first version, so there is nothing earlier to compare with."));
                                return;
                            }

                            var changes = $("<div></div>").appendTo(detail);
                            $.ajax({
                                url: "/api/" + options.historyRoute + "/Compare",
                                method: "GET",
                                data: {fromId: e.data.previousId, toId: e.data.id},
                                success: function (list) {
                                    changes.kendoGrid({
                                        dataSource: {
                                            data: list.map(function (change) {
                                                return {
                                                    field: field(change, "propertyName"),
                                                    earlier: field(change, "fromValue"),
                                                    version: field(change, "toValue")
                                                };
                                            })
                                        },
                                        scrollable: false,
                                        noRecords: {template: "No visible field changed."},
                                        columns: [
                                            {field: "field", title: "Field", width: 200},
                                            {field: "earlier", title: "Earlier version"},
                                            {field: "version", title: "This version"}
                                        ]
                                    });
                                }
                            });
                        }
                    });
                },
                error: function () {
                    target.empty().append(text("The version history is not available to you."));
                }
            });
        },

    });

    ui.plugin(ApprovalManager);
})(jQuery);

//# sourceURL=ApprovalManager.js
