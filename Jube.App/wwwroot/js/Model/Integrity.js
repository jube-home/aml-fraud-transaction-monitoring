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

const severityRank = {Error: 0, Warning: 1, Info: 2};

let $model;
let findingsGrid;
let engineGrid;
let dependenciesGrid;
let pendingGrid;
let pendingGraph = null;
let dependencyIndex = {};
let highlightedDependency = null;

function escapeHtml(value) {
    return $('<div>').text(value === null || typeof value === 'undefined' ? '' : String(value)).html();
}

function severityBadge(severity) {
    return severity ? '<span class="ruleToolsBadge ruleToolsSeverity' + severity + '">' + severity + '</span>' : '';
}

function requestedModelId() {
    const value = new URLSearchParams(window.location.search).get('EntityAnalysisModelId');
    return value ? Number(value) : null;
}

function findingRows(report) {
    const groups = {};
    report.checks.forEach(function (check) {
        const kind = check.entityKind || (check.category === 'Engine' && check.entityName ? 'Engine instance' : 'Model');
        const name = check.entityName || report.modelName;
        const key = kind + '|' + (check.entityId === null ? name : check.entityId);
        if (!groups[key]) {
            groups[key] = {
                kind: kind,
                name: name,
                entityId: check.entityId,
                checks: [],
                errors: 0,
                warnings: 0,
                infos: 0
            };
        }
        const group = groups[key];
        group.checks.push(check);
        if (check.severity === 'Error') {
            group.errors++;
        } else if (check.severity === 'Warning') {
            group.warnings++;
        } else {
            group.infos++;
        }
    });

    return Object.keys(groups).map(function (key) {
        const group = groups[key];
        group.worst = group.errors > 0 ? 'Error' : group.warnings > 0 ? 'Warning' : 'Info';
        group.worstRank = severityRank[group.worst];
        group.categories = [...new Set(group.checks.map(function (c) {
            return c.category;
        }))].join(', ');
        return group;
    }).sort(function (a, b) {
        return a.worstRank - b.worstRank || a.kind.localeCompare(b.kind) || a.name.localeCompare(b.name);
    });
}

function engineRows(state) {
    const rows = {};
    state.nodes.forEach(function (node) {
        rows[node.instance] = {
            instance: node.instance, heartbeatDate: node.heartbeatDate ? new Date(node.heartbeatDate) : null,
            synchronisedDate: node.synchronisedDate ? new Date(node.synchronisedDate) : null,
            model: 'Not loaded', loadedTotal: 0, loaded: []
        };
    });
    state.instances.forEach(function (instance) {
        const row = rows[instance.instance] || (rows[instance.instance] = {
            instance: instance.instance, heartbeatDate: null, synchronisedDate: null
        });
        row.model = instance.started ? 'Started' : 'Loaded, not started';
        row.loaded = instance.loaded;
        row.loadedTotal = instance.loaded.reduce(function (total, l) {
            return total + l.count;
        }, 0);
        row.capturedDate = new Date(instance.capturedDate);
    });
    return Object.values(rows);
}

function dependencyRows(graph) {
    const labels = {};
    graph.nodes.forEach(function (node) {
        labels[node.id] = node.kind === 'Missing' ? node.label + ' (does not exist)' : node.kind + ' ' + node.label;
    });

    return graph.nodes.filter(function (node) {
        return node.kind !== 'Missing';
    }).map(function (node) {
        const uses = graph.edges.filter(function (e) {
            return e.from === node.id;
        }).map(function (e) {
            return {
                entity: labels[e.to] || e.to, through: e.label, how: e.dashed ? 'Setting' : 'Rule text',
                missing: e.to.indexOf('Missing:') === 0
            };
        });
        const usedBy = graph.edges.filter(function (e) {
            return e.to === node.id;
        }).map(function (e) {
            return {entity: labels[e.from] || e.from, through: e.label, how: e.dashed ? 'Setting' : 'Rule text'};
        });
        return {
            id: node.id, kind: node.kind, name: node.label, active: node.active, uses: uses, usedBy: usedBy,
            usesCount: uses.length, usedByCount: usedBy.length,
            missingCount: uses.filter(function (u) {
                return u.missing;
            }).length
        };
    });
}

const kindColours = {
    Missing: "#c0392b", RequestXPath: "#2e86c1", InlineScriptProperty: "#17a589", InlineFunction: "#17a589",
    InlineScript: "#138d75", GatewayRule: "#1f618d", AbstractionRule: "#d68910", AbstractionCalculation: "#ca6f1e",
    ActivationRule: "#7d3c98", TtlCounter: "#229954", Sanction: "#a93226", List: "#5d6d7e", Dictionary: "#5d6d7e",
    HttpAdaptation: "#2874a6", ExhaustiveAdaptation: "#6c3483", ReprocessingRule: "#b9770e", CaseWorkflow: "#566573"
};

function dependenciesVisible() {
    return $("#DependenciesDiagram").parent().is(":visible");
}

function fitDependencyDiagram(diagram) {
    if (!diagram || diagram.shapes.length === 0) {
        return;
    }

    const element = diagram.element;
    diagram.zoom(1);
    const box = diagram.boundingBox(diagram.shapes);
    const zoom = Math.max(0.5, Math.min(1, (element.width() - 40) / box.width));
    const height = box.height * zoom + 60;
    element.height(Math.min(900, Math.max(260, height)));
    diagram.resize();
    diagram.zoom(zoom);

    if (height > 900) {
        diagram.pan(new kendo.dataviz.diagram.Point(
            (element.width() - box.width * zoom) / 2 - box.x * zoom, 20 - box.y * zoom));
    } else {
        diagram.bringIntoView(diagram.shapes, {align: "center middle"});
    }
}

function indexDependencies(graph) {
    const labels = {};
    graph.nodes.forEach(function (node) {
        labels[node.id] = node.kind === 'Missing' ? node.label + ' (does not exist)' : node.kind + ' ' + node.label;
    });

    dependencyIndex = {};
    graph.nodes.forEach(function (node) {
        dependencyIndex[node.id] = {node: node, uses: [], usedBy: []};
    });
    graph.edges.forEach(function (edge) {
        const how = edge.dashed ? 'setting' : 'rule text';
        if (dependencyIndex[edge.from]) {
            dependencyIndex[edge.from].uses.push({
                id: edge.to,
                entity: labels[edge.to] || edge.to,
                through: edge.label,
                how: how
            });
        }
        if (dependencyIndex[edge.to]) {
            dependencyIndex[edge.to].usedBy.push({
                id: edge.from,
                entity: labels[edge.from] || edge.from,
                through: edge.label,
                how: how
            });
        }
    });
}

function showDependencyDiagram(graph) {
    indexDependencies(graph);
    highlightedDependency = null;
    pendingGraph = graph;
    if (dependenciesVisible()) {
        drawDependencyDiagram(pendingGraph);
        pendingGraph = null;
    }
}

function tooltipList(title, items) {
    if (items.length === 0) {
        return '';
    }
    const shown = items.slice(0, 8).map(function (item) {
        return '<li>' + escapeHtml(item.entity) + ' <span class="integrityTooltipHow">through ' +
            escapeHtml(item.through) + ' (' + item.how + ')</span></li>';
    }).join('');
    const more = items.length > 8 ? '<li class="integrityTooltipHow">and ' + (items.length - 8) + ' more</li>' : '';
    return '<div class="integrityTooltipTitle">' + title + ' (' + items.length + ')</div><ul>' + shown + more + '</ul>';
}

function shapeTooltip(id) {
    const entry = dependencyIndex[id];
    if (!entry) {
        return '';
    }
    const node = entry.node;
    if (node.kind === 'Missing') {
        return '<div class="integrityTooltipHead">' + escapeHtml(node.label) + '</div>' +
            '<div class="integrityTooltipHow">A name that does not exist.</div>' +
            tooltipList('Named by', entry.usedBy);
    }
    return '<div class="integrityTooltipKind">' + escapeHtml(node.kind) + (node.active === false ? ' · inactive' : '') +
        '</div><div class="integrityTooltipHead">' + escapeHtml(node.label) + '</div>' +
        tooltipList('Uses', entry.uses) + tooltipList('Used by', entry.usedBy) +
        '<div class="integrityTooltipHow">Click to highlight and show it in the table.</div>';
}

function connectionTooltip(edge) {
    const from = dependencyIndex[edge.from];
    const to = dependencyIndex[edge.to];
    return '<div class="integrityTooltipHead">' + escapeHtml(from ? from.node.label : edge.from) + ' → ' +
        escapeHtml(to ? to.node.label : edge.to) + '</div><div class="integrityTooltipHow">through ' +
        escapeHtml(edge.label) + ' (' + (edge.dashed ? 'setting' : 'rule text') + ')</div>';
}

function dependencyTooltip() {
    let tooltip = $('#DependenciesTooltip');
    if (tooltip.length === 0) {
        tooltip = $('<div id="DependenciesTooltip" class="integrityTooltip"></div>').appendTo(document.body).hide();
    }
    return tooltip;
}

function highlightDependency(id) {
    const diagram = $("#DependenciesDiagram").data("kendoDiagram");
    if (!diagram) {
        return;
    }

    highlightedDependency = id;
    const entry = id ? dependencyIndex[id] : null;
    const related = {};
    if (entry) {
        related[id] = true;
        entry.uses.concat(entry.usedBy).forEach(function (item) {
            related[item.id] = true;
        });
    }

    diagram.shapes.forEach(function (shape) {
        shape.visual.drawingElement.opacity(!entry || related[shape.dataItem.id] ? 1 : 0.15);
    });
    diagram.connections.forEach(function (connection) {
        const edge = connection.dataItem;
        const touches = entry && (edge.from === id || edge.to === id);
        connection.redraw({
            stroke: {
                color: touches ? "#2c3e50" : "#95a5a6", width: touches ? 2.2 : 1.2,
                dashType: edge.dashed ? "dash" : "solid"
            }
        });
        connection.visual.drawingElement.opacity(!entry || touches ? 1 : 0.1);
    });
}

function centreDependency(id) {
    const diagram = $("#DependenciesDiagram").data("kendoDiagram");
    if (!diagram) {
        return;
    }
    const shape = diagram.shapes.find(function (s) {
        return s.dataItem.id === id;
    });
    if (shape) {
        const zoom = diagram.zoom();
        diagram.bringIntoView(shape, {align: "center middle"});
        diagram.zoom(zoom);
    }
}

function focusDependencyRow(id) {
    const dataSource = dependenciesGrid.dataSource;
    const item = dataSource.get(id);
    if (!item) {
        dependenciesGrid.clearSelection();
        return;
    }

    const ordered = new kendo.data.Query(dataSource.data())
        .filter(dataSource.filter() || [])
        .sort(dataSource.sort() || [])
        .data;
    const index = ordered.indexOf(item);
    if (index < 0) {
        return;
    }

    const page = Math.floor(index / dataSource.pageSize()) + 1;
    const select = function () {
        const row = dependenciesGrid.tbody.find("tr[data-uid='" + item.uid + "']");
        dependenciesGrid.clearSelection();
        dependenciesGrid.select(row);
        dependenciesGrid.expandRow(row);
        row[0].scrollIntoView({block: "nearest", behavior: "smooth"});
    };

    if (dataSource.page() !== page) {
        dependenciesGrid.one("dataBound", select);
        dataSource.page(page);
    } else {
        select();
    }
}

function drawDependencyDiagram(graph) {
    const element = $("#DependenciesDiagram");
    const existing = element.data("kendoDiagram");
    if (existing) {
        existing.destroy();
        element.empty();
    }

    const connected = {};
    graph.edges.forEach(function (edge) {
        connected[edge.from] = true;
        connected[edge.to] = true;
    });
    const nodes = graph.nodes.filter(function (node) {
        return connected[node.id];
    });

    if (nodes.length === 0) {
        element.hide();
        return;
    }
    element.show();

    const tooltip = dependencyTooltip();

    element.off('.dependencyTooltip').on('mousemove.dependencyTooltip', function (e) {
        tooltip.css({left: e.pageX + 16, top: e.pageY + 12});
    }).on('mouseleave.dependencyTooltip', function () {
        tooltip.hide();
    });

    element.kendoDiagram({
        dataSource: {
            data: nodes.map(function (node) {
                return {id: node.id, kind: node.kind, label: node.label, active: node.active};
            }),
            schema: {model: {id: "id"}}
        },
        connectionsDataSource: {
            data: graph.edges.map(function (edge, index) {
                return {id: index, from: edge.from, to: edge.to, label: edge.label, dashed: edge.dashed};
            })
        },
        layout: {type: "layered", subtype: "right", layerSeparation: 70, nodeDistance: 14},
        editable: false,
        selectable: false,
        pannable: {key: "none"},
        zoomRate: 0.1,
        shapeDefaults: {
            visual: function (options) {
                const data = options.dataItem;
                const colour = data.kind === "Missing" ? kindColours.Missing :
                    data.active === false ? "#aab7b8" : (kindColours[data.kind] || "#566573");
                const title = data.kind === "Missing" ? data.label + " (does not exist)" : data.label;
                const group = new kendo.dataviz.diagram.Group();
                group.append(new kendo.dataviz.diagram.Rectangle({
                    width: 190, height: 42, cornerRadius: 4,
                    stroke: {color: colour, width: 2, dashType: data.active === false ? "dash" : "solid"},
                    fill: {color: "#ffffff"}
                }));
                group.append(new kendo.dataviz.diagram.TextBlock({
                    text: data.kind, x: 8, y: 5, fontSize: 10, color: colour
                }));
                group.append(new kendo.dataviz.diagram.TextBlock({
                    text: title.length > 26 ? title.substring(0, 25) + "…" : title,
                    x: 8, y: 20, fontSize: 12, color: "#222222"
                }));
                return group;
            }
        },
        connectionDefaults: {
            type: "polyline",
            endCap: "ArrowEnd",
            stroke: {color: "#95a5a6", width: 1.2}
        },
        mouseEnter: function (e) {
            const html = e.item instanceof kendo.dataviz.diagram.Shape
                ? shapeTooltip(e.item.dataItem.id)
                : connectionTooltip(e.item.dataItem);
            tooltip.html(html).show();
        },
        mouseLeave: function () {
            tooltip.hide();
        },
        click: function (e) {
            if (e.item instanceof kendo.dataviz.diagram.Shape) {
                const id = e.item.dataItem.id;
                highlightDependency(id);
                focusDependencyRow(id);
            } else if (!e.item) {
                highlightDependency(null);
                dependenciesGrid.clearSelection();
            }
        },
        dataBound: function () {
            this.connections.forEach(function (connection) {
                if (connection.dataItem && connection.dataItem.dashed) {
                    connection.redraw({stroke: {color: "#95a5a6", width: 1.2, dashType: "dash"}});
                }
            });
            const diagram = this;
            setTimeout(function () {
                fitDependencyDiagram(diagram);
            }, 0);
        }
    });
}

function childGrid(container, title, data, columns) {
    $('<h4>').text(title).appendTo(container);
    if (data.length === 0) {
        $('<p class="ruleToolsHint">').text('None.').appendTo(container);
        return;
    }
    $('<div/>').appendTo(container).kendoGrid({
        dataSource: {data: data},
        sortable: true,
        scrollable: false,
        columns: columns
    });
}

function initGrids() {
    findingsGrid = $("#FindingsGrid").kendoGrid({
        dataSource: {data: [], sort: [{field: "worstRank", dir: "asc"}]},
        sortable: true,
        filterable: true,
        scrollable: false,
        noRecords: {template: "No findings."},
        detailInit: function (e) {
            childGrid(e.detailCell, "Findings", e.data.checks.slice().sort(function (a, b) {
                return severityRank[a.severity] - severityRank[b.severity];
            }), [
                {field: "severity", title: "Severity", width: 110, template: "#= severityBadge(severity) #"},
                {field: "title", title: "Check", width: 320},
                {field: "category", title: "Category", width: 130},
                {field: "message", title: "Finding"}
            ]);
        },
        columns: [
            {field: "worstRank", title: "Severity", width: 110, template: "#= severityBadge(worst) #"},
            {field: "kind", title: "Kind", width: 200},
            {field: "name", title: "Entity"},
            {field: "categories", title: "Categories", width: 240},
            {field: "errors", title: "Errors", width: 90},
            {field: "warnings", title: "Warnings", width: 100},
            {field: "infos", title: "Information", width: 110}
        ]
    }).data("kendoGrid");

    engineGrid = $("#EngineGrid").kendoGrid({
        dataSource: {data: []},
        sortable: true,
        scrollable: false,
        noRecords: {template: "No engine instance has synchronised this tenant's models."},
        detailInit: function (e) {
            const loaded = e.data.loaded || [];
            $('<h4>').text("Loaded for this model").appendTo(e.detailCell);
            if (loaded.length === 0) {
                $('<p class="ruleToolsHint">').text('None.').appendTo(e.detailCell);
                return;
            }
            $('<div/>').appendTo(e.detailCell).kendoGrid({
                dataSource: {data: loaded},
                sortable: true,
                scrollable: false,
                detailInit: function (inner) {
                    childGrid(inner.detailCell, "Entities", inner.data.entities || [], [
                        {field: "name", title: "Name"},
                        {field: "id", title: "Id", width: 100},
                        {
                            field: "guid", title: "Guid", width: 330,
                            template: function (d) {
                                return d.guid ? escapeHtml(d.guid) :
                                    '<span class="ruleToolsHint">Not in the database</span>';
                            }
                        }
                    ]);
                },
                columns: [
                    {field: "kind", title: "Kind"},
                    {field: "count", title: "Loaded", width: 120}
                ]
            });
        },
        columns: [
            {field: "instance", title: "Instance"},
            {field: "heartbeatDate", title: "Last Reported", format: "{0:yyyy-MM-dd HH:mm:ss}", width: 190},
            {field: "synchronisedDate", title: "Last Synchronised", format: "{0:yyyy-MM-dd HH:mm:ss}", width: 190},
            {field: "model", title: "Model", width: 180},
            {field: "loadedTotal", title: "Entities Loaded", width: 150}
        ]
    }).data("kendoGrid");

    pendingGrid = $("#PendingGrid").kendoGrid({
        dataSource: {data: []},
        sortable: true,
        scrollable: false,
        noRecords: {template: "Nothing is awaiting approval in this model."},
        detailInit: function (e) {
            $('<div/>').appendTo(e.detailCell).kendoGrid({
                dataSource: {data: e.data.entities},
                sortable: true,
                scrollable: false,
                noRecords: {template: "Nothing is awaiting approval."},
                selectable: "row",
                change: function () {
                    const item = this.dataItem(this.select());
                    if (item) {
                        openReview(item.kind, item.entityId, item.name);
                    }
                },
                columns: [
                    {
                        field: "name", title: "Name", template: function (d) {
                            return escapeHtml(d.name);
                        }
                    },
                    {
                        field: "change", title: "Change", width: 160,
                        template: function (d) {
                            return escapeHtml(d.change);
                        }
                    },
                    {
                        field: "makerUser", title: "Made By", width: 180,
                        template: function (d) {
                            return escapeHtml(d.makerUser || "");
                        }
                    },
                    {field: "valueCount", title: "Values", width: 90}
                ]
            });
        },
        columns: [
            {
                field: "kindName", title: "Kind", width: 240, template: function (d) {
                    return escapeHtml(d.kindName);
                }
            },
            {field: "count", title: "Waiting (entities and values)", width: 240},
            {field: "deletions", title: "Of which deletions", width: 180}
        ]
    }).data("kendoGrid");
    $("#PendingGrid, #PendingReview").on("click", ".pending-approve-values", function (e) {
        e.preventDefault();
        approveValues($(this));
    }).on("click", ".pending-approve", function (e) {
        e.preventDefault();
        decideApproval($(this), "Approve");
    }).on("click", ".pending-reject", function (e) {
        e.preventDefault();
        decideApproval($(this), "Reject");
    });
    dependenciesGrid = $("#DependenciesGrid").kendoGrid({
        dataSource: {
            data: [], pageSize: 50, schema: {model: {id: "id"}},
            sort: [{field: "missingCount", dir: "desc"}, {field: "kind", dir: "asc"}]
        },
        sortable: true,
        selectable: "row",
        change: function () {
            const item = this.dataItem(this.select());
            if (item && item.id !== highlightedDependency) {
                highlightDependency(item.id);
                centreDependency(item.id);
            }
        },
        filterable: true,
        pageable: {pageSizes: [25, 50, 100, 200]},
        scrollable: false,
        noRecords: {template: "The model has no entities."},
        detailInit: function (e) {
            childGrid(e.detailCell, "Uses", e.data.uses, [
                {field: "entity", title: "Entity"},
                {field: "through", title: "Through", width: 320},
                {field: "how", title: "How", width: 120}
            ]);
            childGrid(e.detailCell, "Used by", e.data.usedBy, [
                {field: "entity", title: "Entity"},
                {field: "through", title: "Through", width: 320},
                {field: "how", title: "How", width: 120}
            ]);
        },
        columns: [
            {field: "kind", title: "Kind", width: 200},
            {field: "name", title: "Entity"},
            {field: "active", title: "Active", width: 90, template: "#= active ? 'Yes' : 'No' #"},
            {field: "usesCount", title: "Uses", width: 90},
            {field: "usedByCount", title: "Used By", width: 100},
            {
                field: "missingCount", title: "Missing Names", width: 140,
                template: "#= missingCount > 0 ? '<span class=\"ruleToolsBadge ruleToolsSeverityError\">' + missingCount + '</span>' : '0' #"
            }
        ]
    }).data("kendoGrid");
}

const approvalKindNames = {
    1: "Model", 2: "List", 3: "List value", 4: "Dictionary", 5: "Dictionary pair", 6: "Request XPath",
    7: "Inline script", 8: "Inline function", 9: "Gateway rule", 10: "Sanction", 11: "Abstraction rule",
    12: "Abstraction calculation", 13: "TTL counter", 14: "HTTP adaptation", 15: "Exhaustive instance",
    16: "Activation rule", 17: "Tag"
};

const approvalValueKinds = {2: 3, 4: 5};

function endpointOf(kind) {
    const match = Object.keys(ApprovalKinds).find(function (endpoint) {
        return ApprovalKinds[endpoint].kind === kind;
    });
    return match || null;
}

function historyRouteOf(kind) {
    const match = Object.values(ApprovalKinds).find(function (entry) {
        return entry.kind === kind;
    });
    return match ? match.historyRoute : null;
}

function findPendingEntity(kind, entityId) {
    let found = null;
    pendingGrid.dataSource.data().forEach(function (group) {
        group.entities.forEach(function (entity) {
            if (entity.kind === kind && entity.entityId === entityId) {
                found = entity;
            }
        });
    });
    return found;
}

function closeReview() {
    $("#PendingReview").empty().hide();
}

function openReview(kind, entityId, name) {
    const route = historyRouteOf(kind);
    const panel = $("#PendingReview").empty().show();
    panel[0].scrollIntoView({behavior: "smooth", block: "start"});
    panel.append($("<h3></h3>").text("Review: " + name));
    const note = $("<div class='pending-review-note'></div>").appendTo(panel);
    const grid = $("<div></div>").appendTo(panel);
    const widget = $("<div></div>").appendTo(panel);
    widget.kendoApprovalManager({kind: kind, entityId: entityId, historyRoute: route}).data("kendoApprovalManager")
        .bind("changed", function () {
            closeReview();
            loadPending($model.value());
        });

    const entity = findPendingEntity(kind, entityId);
    const values = entity ? entity.values : [];
    if (values.length && approvalValueKinds[kind] !== undefined) {
        grid.before($("<div></div>").append(
            "<button type=\"button\" class=\"ButtonDefault pending-approve-values\" data-kind=\"" + kind +
            "\" data-id=\"" + entityId + "\">Approve values</button>"));
    }

    function valueRows() {
        return values.map(function (value) {
            return {
                label: value.name,
                before: value.version > 1 || value.deleted ? value.name : "",
                after: value.deleted ? "" : value.name,
                status: value.deleted ? "Removed" : (value.version > 1 ? "Changed" : "Added"),
                actions: approvalActions(value)
            };
        });
    }

    function render(rows) {
        grid.kendoGrid({
            dataSource: {data: rows},
            sortable: true,
            scrollable: false,
            noRecords: {template: "Nothing differs from the earlier version."},
            columns: [
                {
                    field: "label", title: "Item", template: function (d) {
                        return escapeHtml(d.label);
                    }
                },
                {
                    field: "before", title: "Earlier version", width: 240,
                    template: function (d) {
                        return escapeHtml(d.before);
                    }
                },
                {
                    field: "after", title: "Pending version", width: 240,
                    template: function (d) {
                        return escapeHtml(d.after);
                    }
                },
                {field: "status", title: "Change", width: 110},
                {
                    title: "", width: 260, template: function (d) {
                        return d.actions || "";
                    }
                }
            ],
            dataBound: function (e) {
                const colours = {Added: "#e3f4e8", Removed: "#f9dcdc", Changed: "#fdebd0"};
                e.sender.tbody.find("tr").each(function () {
                    const status = e.sender.dataItem(this).status;
                    $(this).children("td").css("background-color", colours[status] || "");
                });
            }
        });
    }

    if (endpointOf(kind) === null) {
        note.text("This kind has no earlier versions to compare.");
        render(valueRows());
        return;
    }

    $.getJSON("/api/EntityApproval/Changes", {kind: kind, entityId: entityId, version: entity ? entity.version : null})
        .done(function (result) {
            const rows = [];
            if (entity && entity.deleted) {
                rows.push({label: "This row", before: "present", after: "deleted", status: "Removed", actions: ""});
            }
            if (!result.hasEarlierVersion) {
                note.text("There is no earlier version to compare with.");
            } else {
                result.changes.forEach(function (change) {
                    rows.push({
                        label: change.field, before: change.before, after: change.after,
                        status: change.status, actions: ""
                    });
                });
            }
            render(rows.concat(valueRows()));
        }).fail(function () {
        note.text("The earlier version could not be loaded.");
        render(valueRows());
    });
}

function pendingChange(row) {
    return row.deleted ? "Deletion" : "Edit to version " + row.version;
}

function pendingGroups(rows) {
    const valueParentKind = {3: 2, 5: 4};
    const entities = rows.map(function (row) {
        return {
            kind: row.kind, entityId: row.entityId, name: row.name, version: row.version,
            deleted: row.deleted, makerUser: row.makerUser, change: pendingChange(row),
            parentId: row.parentId, values: [], valueCount: 0
        };
    });

    const parents = {};
    entities.forEach(function (entity) {
        if (approvalValueKinds[entity.kind] !== undefined) {
            parents[entity.kind + "|" + entity.entityId] = entity;
        }
    });

    const groups = [];
    const groupByKind = {};
    entities.forEach(function (entity) {
        const parentKind = valueParentKind[entity.kind];
        const parent = parentKind !== undefined ? parents[parentKind + "|" + entity.parentId] : undefined;
        if (parent) {
            parent.values.push(entity);
            parent.valueCount = parent.values.length;
            return;
        }
        if (!groupByKind[entity.kind]) {
            groupByKind[entity.kind] = {
                kind: entity.kind, kindName: approvalKindNames[entity.kind] || String(entity.kind),
                entities: []
            };
            groups.push(groupByKind[entity.kind]);
        }
        groupByKind[entity.kind].entities.push(entity);
    });

    return groups.map(function (group) {
        group.count = group.entities.reduce(function (total, e) {
            return total + 1 + e.values.length;
        }, 0);
        group.deletions = group.entities.filter(function (e) {
            return e.deleted;
        }).length;
        return group;
    });
}

function approvalActions(entity) {
    const bulk = (entity.kind === 2 || entity.kind === 4) && entity.valueCount > 0
        ? ' <button type="button" class="ButtonDefault pending-approve-values" data-kind="' + entity.kind +
        '" data-id="' + entity.entityId + '">Approve values</button>'
        : '';
    return '<button type="button" class="ButtonDefault pending-approve" data-kind="' + entity.kind +
        '" data-id="' + entity.entityId + '" data-version="' + entity.version + '">Approve</button> ' +
        '<button type="button" class="ButtonDefault pending-reject" data-kind="' + entity.kind +
        '" data-id="' + entity.entityId + '" data-version="' + entity.version + '">Reject</button>' + bulk;
}

function approveValues(button) {
    $.ajax({
        url: "/api/EntityApproval/ApproveValues",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify({kind: Number(button.data("kind")), entityId: Number(button.data("id"))}),
        success: function (result) {
            if (button.closest("#PendingReview").length) {
                closeReview();
            }
            loadPending($model.value());
            const message = $("#PendingMessage").empty();
            message.append($("<div></div>").text("Approved " + result.approved + " value(s)."));
            result.refusals.forEach(function (reason) {
                message.append($("<div></div>").text(reason));
            });
        },
        error: function (xhr) {
            const message = $("#PendingMessage").empty();
            const reasons = xhr.responseJSON && xhr.responseJSON.reasons
                ? xhr.responseJSON.reasons
                : [(xhr.responseJSON && xhr.responseJSON.message) || "The values could not be approved."];
            reasons.forEach(function (reason) {
                message.append($("<div></div>").text(reason));
            });
        }
    });
}

function loadPending(modelId) {
    $("#PendingMessage").text("");
    $.getJSON("/api/EntityApproval/Pending", {modelId: modelId}).done(function (rows) {
        pendingGrid.dataSource.data(pendingGroups(rows));
    }).fail(function (xhr) {
        pendingGrid.dataSource.data([]);
        $("#PendingMessage").text(xhr.status === 403
            ? "You do not have permission to view pending approvals."
            : "The pending approvals could not be loaded.");
    });
}

function decideApproval(button, action) {
    const body = {
        kind: Number(button.data("kind")),
        entityId: Number(button.data("id")),
        version: Number(button.data("version")),
        note: null
    };

    $.ajax({
        url: "/api/EntityApproval/" + action,
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(body),
        success: function () {
            $("#PendingMessage").text("");
            loadPending($model.value());
        },
        error: function (xhr) {
            let reasons;
            if (xhr.responseJSON && xhr.responseJSON.reasons) {
                reasons = xhr.responseJSON.reasons;
            } else if (xhr.status === 403) {
                reasons = ["You do not have permission to " + action.toLowerCase() + " this change."];
            } else {
                reasons = [(xhr.responseJSON && xhr.responseJSON.message) || "The " + action.toLowerCase() + " was refused."];
            }
            $("#PendingMessage").empty();
            reasons.forEach(function (reason) {
                $("#PendingMessage").append($("<div></div>").text(reason));
            });
        }
    });
}

function load() {
    const modelId = $model.value();
    if (!modelId) {
        return;
    }
    loadPending(modelId);

    $("#IntegritySummary").text("Checking…");

    $.getJSON("/api/EntityAnalysisModelIntegrity/" + modelId).done(function (report) {
        $("#IntegritySummary").html(
            '<strong>' + report.errors + '</strong> errors, <strong>' + report.warnings + '</strong> warnings, <strong>' +
            report.infos + '</strong> for information · checked ' +
            escapeHtml(new Date(report.checkedDate).toLocaleString()));
        findingsGrid.dataSource.data(findingRows(report));
    }).fail(function (xhr) {
        $("#IntegritySummary").text(xhr.status === 403 ? "You do not have permission to view model integrity." :
            "The model could not be checked.");
    });

    $.getJSON("/api/EntityAnalysisModelEngineState/" + modelId).done(function (state) {
        engineGrid.dataSource.data(engineRows(state));
    });

    $.getJSON("/api/EntityAnalysisModelDependencyGraph/" + modelId).done(function (graph) {
        $("#DependenciesNote").text("Each entity points to the entities it uses; dashed lines are settings, dashed " +
            "boxes are inactive and red boxes are names that do not exist. Scroll to zoom and drag to move." +
            (graph.truncated ? " Only the " + graph.maxNodes + " most connected entities are shown." : ""));
        dependenciesGrid.dataSource.data(dependencyRows(graph));
        showDependencyDiagram(graph);
    });
}

$(document).on('keydown', function (e) {
    if (e.key === 'Escape' && highlightedDependency) {
        highlightDependency(null);
        dependenciesGrid.clearSelection();
    }
});

$(document).ready(function () {
    $("#IntegrityTabs").kendoTabStrip({
        animation: false,
        activate: function () {
            if (pendingGraph && dependenciesVisible()) {
                drawDependencyDiagram(pendingGraph);
                pendingGraph = null;
            } else {
                fitDependencyDiagram($("#DependenciesDiagram").data("kendoDiagram"));
            }
        }
    }).data("kendoTabStrip").select(0);
    $("#IntegrityRefresh").kendoButton({click: load});
    if ($("#SyncPicker").length) {
        initSynchronisation();
    }
    initGrids();

    $model = $("#IntegrityModel").kendoDropDownList({
        dataTextField: "name",
        dataValueField: "id",

        dataSource: {
            transport: {read: {url: "/api/EntityAnalysisModelIntegrity/Models", dataType: "json"}},
            schema: {model: {id: "id"}}
        },
        change: load,
        dataBound: function () {
            const requested = requestedModelId();
            if (requested !== null && this.dataSource.get(requested) !== undefined) {
                this.value(requested);
            } else if (this.dataSource.data().length > 0) {
                this.select(0);
            }
            load();
        }
    }).data("kendoDropDownList");
});

//# sourceURL=Integrity.js

function colourSyncRows(e) {
    e.sender.tbody.children().each(function () {
        const row = $(this);
        const item = e.sender.dataItem(row);
        if (item.get("synchronisationPending") && item.get("instanceAvailable")) {
            row.css("color", "orange");
        } else if (item.get("instanceAvailable")) {
            row.css("color", "green");
        } else {
            row.css("color", "red");
        }
    });
}

function loadSyncSchedule() {
    $.get("/api/EntityAnalysisModelSynchronisationSchedule/ByCurrent", function (data) {
        $("#SyncPicker").data("kendoDateTimePicker").value(kendo.parseDate(data.scheduleDate));
    });
}

function refreshSync() {
    $("#SyncMessage").text("");
    $("#SyncGrid").data("kendoGrid").dataSource.read();
    loadSyncSchedule();
}

function postSync(body) {
    $("#SyncMessage").text("");
    $.ajax({
        url: "/api/EntityAnalysisModelSynchronisationSchedule",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify(body),
        success: function (data) {
            $("#SyncPicker").data("kendoDateTimePicker").value(kendo.parseDate(data.scheduleDate));
            refreshSync();
        },
        error: function (xhr) {
            const refused = xhr.responseText && xhr.responseText.indexOf("ScheduleDateTooFarInFuture") >= 0;
            let message = "The synchronisation could not be scheduled.";
            if (refused) {
                message = "The schedule is too far in the future. Choose a nearer date.";
            } else if (xhr.status === 403) {
                message = "You do not have permission to schedule synchronisation.";
            }
            $("#SyncMessage").text(message);
        }
    });
}

function initSynchronisation() {
    $("#SyncPicker").kendoDateTimePicker({
        value: new Date(),
        parseFormats: ["yyyy-MM-ddThh:mm:ss"],
        format: "yyyy-MM-dd HH:mm:ss",
        dateInput: true
    });

    $("#SyncSchedule, #SyncNow, #SyncRefresh").kendoButton();

    $("#SyncGrid").kendoGrid({
        dataSource: {
            transport: {
                read: {
                    url: "/api/GetEntityAnalysisModelSynchronisationNodeStatusEntries",
                    dataType: "json"
                }
            },
            schema: {
                model: {
                    id: "id",
                    fields: {
                        instance: {type: "string"},
                        heartbeatDate: {type: "date"},
                        synchronisedDate: {type: "date"}
                    }
                }
            }
        },
        pageable: false,
        height: 240,
        scrollable: true,
        filterable: true,
        dataBound: colourSyncRows,
        columns: [
            {field: "instance", title: "Instance"},
            {field: "heartbeatDate", title: "Heartbeat"},
            {field: "synchronisedDate", title: "Synchronised"}
        ]
    });

    $("#SyncSchedule").click(function () {
        const picked = $("#SyncPicker").data("kendoDateTimePicker").value();
        if (!picked) {
            $("#SyncMessage").text("Choose a date and time first.");
            return;
        }
        postSync({ScheduleDate: picked.toISOString()});
    });

    $("#SyncNow").click(function () {
        postSync({});
    });

    $("#SyncRefresh").click(refreshSync);

    refreshSync();
}
