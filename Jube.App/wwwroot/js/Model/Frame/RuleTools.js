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

var RuleTools = (function () {
    let options;
    let filterBuilder;
    let classBuilder;
    let instancesGrid;
    let pollTimer;
    let selectedInstanceId;
    let shownInstanceId;

    const windows = [
        {text: 'Last 7 days', value: 0, interval: 'd', amount: 7},
        {text: 'Last 30 days', value: 1, interval: 'd', amount: 30},
        {text: 'Last 90 days', value: 2, interval: 'd', amount: 90},
        {text: 'Last 12 months', value: 3, interval: 'm', amount: 12},
        {text: 'All archived transactions', value: 4, interval: null, amount: null}
    ];

    function modelId() {
        return options.modelId();
    }

    function ruleId() {
        const value = options.ruleId();
        return typeof value === 'undefined' || value === null ? null : value;
    }

    const backtestApi = '/api/EntityAnalysisModelBacktest';

    function url(path, params) {
        const present = {};
        Object.keys(params || {}).forEach(function (key) {
            if (params[key] !== null && typeof params[key] !== 'undefined') {
                present[key] = params[key];
            }
        });
        const query = $.param(present);
        return query ? path + '?' + query : path;
    }

    function get(path, params) {
        return $.getJSON(url(path, params));
    }

    function post(path, body) {
        return $.ajax({
            url: path,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(body || {})
        });
    }

    function escape(value) {
        return $('<div>').text(value === null || typeof value === 'undefined' ? '' : String(value)).html();
    }

    function number(value) {
        return value === null || typeof value === 'undefined' ? '' : Number(value).toLocaleString();
    }

    function ratio(numerator, denominator) {
        return denominator > 0 ? numerator / denominator : null;
    }

    function percent(value) {
        return value === null ? 'n/a' : (value * 100).toFixed(1) + '%';
    }

    function badge(text, css) {
        return '<span class="ruleToolsBadge ' + css + '">' + escape(text) + '</span>';
    }

    function section(id, title, body) {
        return '<details id="' + id + '" class="ruleToolsSection"><summary>' + title + '</summary>' +
            '<div class="ruleToolsBody">' + body + '</div></details>';
    }

    function grid(container, settings) {
        return $('<div/>').appendTo(container).kendoGrid(Object.assign({
            sortable: true,
            scrollable: false
        }, settings)).data('kendoGrid');
    }

    function init(settings) {
        options = settings;
        let html = '';

        if (options.canBacktest) {
            html += section('BacktestSection', 'Backtest',
                '<p class="ruleToolsHint">Run this rule, as it is in the editor, over archived transactions. ' +
                'Choose which transactions with the first builder and what counts as a positive, such as a tag, ' +
                'with the second.</p>' +
                '<h4>Which transactions</h4><div id="BacktestFilterBuilder"></div>' +
                '<h4>What counts as positive</h4><div id="BacktestClassBuilder"></div>' +
                '<div class="ruleToolsControls">' +
                '<label>Window <input id="BacktestWindow" style="width:16em"/></label>' +
                '<label>Up to <input id="BacktestLimit" style="width:10em"/> transactions</label>' +
                '<label>Examples <input id="BacktestSampleSize" style="width:6em"/></label>' +
                '<button type="button" id="BacktestRun" class="ButtonDefault">Run Backtest</button>' +
                '</div>' +
                '<div id="BacktestMessage" class="ruleToolsMessage"></div>' +
                '<div id="BacktestInstances"></div>' +
                '<div id="BacktestResult"></div>');
        }

        html += section('DependenciesSection', 'Dependencies', '<div id="RuleDependencies"></div>');

        if (options.canCheckIntegrity) {
            html += section('IntegritySection', 'Integrity', '<div id="RuleIntegrity"></div>');
        }

        $('#RuleTools').html(html);

        $('#BacktestSection').on('toggle', function () {
            if (this.open) {
                openBacktest();
            }
        });
        $('#DependenciesSection').on('toggle', function () {
            if (this.open) {
                loadDependencies();
            }
        });
        $('#IntegritySection').on('toggle', function () {
            if (this.open) {
                loadIntegrity();
            }
        });
    }

    function ensureQueryBuilder() {
        if ($.fn.queryBuilder) {
            return Promise.resolve();
        }
        return loadScript('/js/builder/query-builder.standalone.min.js').then(function () {
            $('<link/>', {rel: 'stylesheet', type: 'text/css', href: '/styles/query-builder.default.min.css'})
                .appendTo('head');
        });
    }

    function toFilters(fields) {
        return builderFiltersFromCompletions(fields.map(function (f) {
            return {name: f.name, dataType: f.dataType.toLowerCase(), group: f.name.split('.')[0]};
        }));
    }

    function openBacktest() {
        if (filterBuilder) {
            loadInstances();
            return;
        }

        $('#BacktestWindow').kendoDropDownList({
            dataTextField: 'text', dataValueField: 'value', dataSource: windows, value: 1
        });
        $('#BacktestLimit').kendoNumericTextBox({format: 'n0', decimals: 0, min: 1, value: 100000});
        $('#BacktestSampleSize').kendoNumericTextBox({format: 'n0', decimals: 0, min: 0, max: 50, value: 10});
        $('#BacktestRun').kendoButton({click: submit});
        initInstancesGrid();

        Promise.all([ensureQueryBuilder(), get(backtestApi + '/Fields/' + modelId()),
            loadBuilderOperators()])
            .then(function (results) {
                const fields = results[1];
                filterBuilder = createQueryBuilder('#BacktestFilterBuilder', toFilters(fields.filterFields), null);
                classBuilder = createQueryBuilder('#BacktestClassBuilder', toFilters(fields.classFields), null);
                const firstTag = fields.classFields.find(function (f) {
                    return f.name.indexOf('Tag.') === 0;
                });
                if (firstTag) {
                    classBuilder.queryBuilder('setRules', {
                        condition: 'AND',
                        rules: [{id: firstTag.name, operator: 'equal', value: 'True'}]
                    });
                }
                loadInstances();
            })
            .catch(function () {
                message('The backtest fields could not be loaded.', true);
            });
    }

    function builderJson(builder, name) {
        const rules = builder.queryBuilder('getRules', {allow_empty: true, allow_invalid: false});
        if (rules === null) {
            throw new Error('The ' + name + ' builder has an incomplete rule.');
        }
        return rules.rules && rules.rules.length > 0 ? JSON.stringify(rules) : null;
    }

    function message(text, error) {
        $('#BacktestMessage').text(text || '').toggleClass('ruleToolsError', !!error);
    }

    function submit() {
        let filterJson;
        let classJson;
        try {
            filterJson = builderJson(filterBuilder, 'transactions');
            classJson = builderJson(classBuilder, 'positive');
        } catch (e) {
            message(e.message, true);
            return;
        }

        const chosen = windows[Number($('#BacktestWindow').data('kendoDropDownList').value())];
        const body = {
            request: {
                entityAnalysisModelId: modelId(),
                ruleType: options.ruleType,
                ruleId: ruleId(),
                ruleText: options.ruleText(),
                filterJson: filterJson,
                classJson: classJson,
                limit: $('#BacktestLimit').data('kendoNumericTextBox').value() || 100000,
                sampleSize: $('#BacktestSampleSize').data('kendoNumericTextBox').value() || 0
            },
            windowInterval: chosen.interval,
            windowValue: chosen.amount
        };

        message('Submitting…');
        post(backtestApi, body)
            .done(function (result) {
                if (!result.valid) {
                    message(result.errors.map(function (e) {
                        return e.propertyName + ': ' + e.message + (e.line !== null ? ' (line ' + e.line + ')' : '');
                    }).join(' '), true);
                    return;
                }
                message('Submitted. The backtest runs in the background; the grid updates as it goes.');
                selectedInstanceId = result.instance.id;
                loadInstances();
            })
            .fail(function (xhr) {
                message(xhr.status === 403 ? 'You do not have permission to run backtests.' :
                    'The backtest could not be submitted.', true);
            });
    }

    function instanceRow(r) {
        const classed = r.truePositives !== null;
        return {
            id: r.id,
            instance: r,
            createdDate: new Date(r.createdDate),
            createdUser: r.createdUser,
            window: r.request.from || r.request.to
                ? (r.request.from ? new Date(r.request.from).toLocaleDateString() : 'start') + ' – ' +
                (r.request.to ? new Date(r.request.to).toLocaleDateString() : 'now')
                : 'All',
            status: r.status,
            progress: r.progress,
            error: r.error,
            edited: r.request.ruleText !== options.ruleText(),
            scanned: r.scanned,
            evaluated: r.evaluated,
            fired: r.fired,
            truePositives: r.truePositives,
            falsePositives: r.falsePositives,
            falseNegatives: r.falseNegatives,
            trueNegatives: r.trueNegatives,
            precision: classed ? ratio(r.truePositives, r.truePositives + r.falsePositives) : null,
            recall: classed ? ratio(r.truePositives, r.truePositives + r.falseNegatives) : null,
            stoppable: r.status === 'Pending' || r.status === 'Running'
        };
    }

    function initInstancesGrid() {
        instancesGrid = $('#BacktestInstances').kendoGrid({
            dataSource: {data: [], schema: {model: {id: 'id'}}},
            selectable: 'row',
            sortable: true,
            scrollable: false,
            noRecords: {template: 'No backtests of this rule yet.'},
            change: function () {
                const item = this.dataItem(this.select());
                if (item) {
                    selectedInstanceId = item.id;
                    showResult(item.instance);
                }
            },
            dataBound: function () {
                const g = this;
                g.tbody.find('.ruleToolsStop').kendoButton({
                    click: function (e) {
                        e.event.stopPropagation();
                        post(backtestApi + '/' + this.element.data('id') + '/Stop').always(loadInstances);
                    }
                });
                if (selectedInstanceId) {
                    const item = g.dataSource.get(selectedInstanceId);
                    if (item) {
                        g.tbody.find('tr[data-uid="' + item.uid + '"]').addClass('k-selected k-state-selected');
                    }
                }
            },
            columns: [
                {
                    field: 'createdDate', title: 'Submitted', format: '{0:yyyy-MM-dd HH:mm}', width: 170,
                    template: function (r) {
                        return kendo.toString(r.createdDate, 'yyyy-MM-dd HH:mm') +
                            (r.edited ? ' ' + badge('edited', '') : '');
                    }
                },
                {field: 'createdUser', title: 'By', width: 130},
                {field: 'window', title: 'Window', width: 180},
                {
                    field: 'status', title: 'Status', width: 170, template: function (r) {
                        return badge(r.status, 'ruleToolsStatus' + r.status) + (r.status === 'Running'
                            ? '<div class="ruleToolsProgress"><div style="width:' + Math.round(r.progress * 100) +
                            '%"></div></div>'
                            : r.error ? '<div class="ruleToolsSmall">' + escape(r.error) + '</div>' : '');
                    }
                },
                {field: 'scanned', title: 'Read', format: '{0:n0}', width: 100},
                {field: 'evaluated', title: 'Evaluated', format: '{0:n0}', width: 110},
                {field: 'fired', title: 'Fired', format: '{0:n0}', width: 90},
                {field: 'truePositives', title: 'TP', format: '{0:n0}', width: 80},
                {field: 'falsePositives', title: 'FP', format: '{0:n0}', width: 80},
                {field: 'falseNegatives', title: 'FN', format: '{0:n0}', width: 80},
                {field: 'trueNegatives', title: 'TN', format: '{0:n0}', width: 90},
                {field: 'precision', title: 'Precision', format: '{0:p1}', width: 110},
                {field: 'recall', title: 'Recall', format: '{0:p1}', width: 100},
                {
                    title: '', width: 100, template: function (r) {
                        return r.stoppable
                            ? '<button type="button" class="ButtonDefault ruleToolsStop" data-id="' + r.id +
                            '">Stop</button>'
                            : '';
                    }
                }
            ]
        }).data('kendoGrid');
    }

    function loadInstances() {
        get(backtestApi + '/ByEntityAnalysisModelId/' + modelId(), {ruleType: options.ruleType, ruleId: ruleId()})
            .done(function (data) {
                instancesGrid.dataSource.data(data.map(instanceRow));
                const active = data.some(function (r) {
                    return r.status === 'Pending' || r.status === 'Running' || r.status === 'Cancelling';
                });
                clearTimeout(pollTimer);
                if (active) {
                    pollTimer = setTimeout(loadInstances, 3000);
                }
                const selected = data.find(function (r) {
                    return r.id === selectedInstanceId;
                });
                if (selected && selected.status === 'Succeeded' && shownInstanceId !== selected.id) {
                    showResult(selected);
                }
            });
    }

    function showResult(instance) {
        const panel = $('#BacktestResult');
        shownInstanceId = instance.status === 'Succeeded' ? instance.id : null;
        if (instance.status !== 'Succeeded') {
            panel.html('<p class="ruleToolsHint">This backtest is ' + escape(instance.status) +
                (instance.error ? ': ' + escape(instance.error) : '.') + '</p>');
            return;
        }
        panel.html('<p class="ruleToolsHint">Loading…</p>');
        get(backtestApi + '/' + instance.id + '/Result').done(function (result) {
            panel.html(renderResult(result));
            renderExamples(panel, result, instance);
        });
    }

    function bar(label, value, total, css) {
        const share = total > 0 ? value / total : 0;
        return '<div class="ruleToolsBarRow"><span class="ruleToolsBarLabel">' + escape(label) + '</span>' +
            '<span class="ruleToolsBar"><span class="' + css + '" style="width:' + (share * 100).toFixed(1) +
            '%"></span></span><span class="ruleToolsBarValue">' + number(value) + ' (' + percent(total > 0 ?
                share : null) + ')</span></div>';
    }

    function gauge(label, value, help) {
        return '<div class="ruleToolsBarRow" title="' + escape(help) + '"><span class="ruleToolsBarLabel">' +
            escape(label) + '</span><span class="ruleToolsBar"><span class="ruleToolsGauge" style="width:' +
            (value === null ? 0 : (value * 100).toFixed(1)) + '%"></span></span><span class="ruleToolsBarValue">' +
            percent(value) + '</span></div>';
    }

    function cell(label, value, total, css) {
        const share = total > 0 ? value / total : 0;
        return '<td class="ruleToolsCell ' + css + '" style="--share:' + Math.max(0.12, share).toFixed(3) + '">' +
            '<div class="ruleToolsCellLabel">' + label + '</div><div class="ruleToolsCellValue">' + number(value) +
            '</div><div class="ruleToolsSmall">' + percent(total > 0 ? share : null) + '</div></td>';
    }

    function renderResult(r) {
        let html = '<div class="ruleToolsResult">';

        html += '<div class="ruleToolsPanel"><h4>From archive to firing</h4>' +
            bar('Read', r.scanned, r.scanned, 'ruleToolsNeutral') +
            bar('Kept by the filter', r.evaluated, r.scanned, 'ruleToolsNeutral') +
            bar('Fired', r.fired, r.evaluated, 'ruleToolsFired') +
            (r.classDefined ? bar('Positive', r.positives, r.evaluated, 'ruleToolsPositive') : '') +
            (r.runtimeErrors > 0 ? bar('Rule errors', r.runtimeErrors, r.evaluated, 'ruleToolsErrorBar') : '') +
            '<div class="ruleToolsSmall">' +
            escape(r.earliestReferenceDate ? new Date(r.earliestReferenceDate).toLocaleString() : '') + ' – ' +
            escape(r.latestReferenceDate ? new Date(r.latestReferenceDate).toLocaleString() : '') +
            (r.limitReached ? ' · limit reached, older transactions not read' : '') +
            (r.aborted ? ' · stopped early: ' + escape(r.abortReason) : '') + '</div></div>';

        if (r.classDefined) {
            const precision = ratio(r.truePositives, r.truePositives + r.falsePositives);
            const recall = ratio(r.truePositives, r.truePositives + r.falseNegatives);
            const specificity = ratio(r.trueNegatives, r.trueNegatives + r.falsePositives);
            const f1 = precision !== null && recall !== null && precision + recall > 0 ?
                2 * precision * recall / (precision + recall) : null;

            html += '<div class="ruleToolsPanel"><h4>Confusion matrix</h4><table class="ruleToolsMatrix">' +
                '<tr><th></th><th>Positive</th><th>Not positive</th></tr>' +
                '<tr><th>Fired</th>' + cell('True positive', r.truePositives, r.evaluated, 'ruleToolsTp') +
                cell('False positive', r.falsePositives, r.evaluated, 'ruleToolsFp') + '</tr>' +
                '<tr><th>Not fired</th>' + cell('False negative', r.falseNegatives, r.evaluated, 'ruleToolsFn') +
                cell('True negative', r.trueNegatives, r.evaluated, 'ruleToolsTn') + '</tr></table></div>';

            html += '<div class="ruleToolsPanel"><h4>Rates</h4>' +
                gauge('Precision', precision, 'Of the transactions it fired on, the share that were positive.') +
                gauge('Recall', recall, 'Of the positive transactions, the share it fired on.') +
                gauge('F1', f1, 'The balance of precision and recall.') +
                gauge('Specificity', specificity, 'Of the transactions that were not positive, the share it left alone.') +
                gauge('Fired rate', ratio(r.fired, r.evaluated), 'The share of evaluated transactions it fired on.') +
                gauge('Positive rate', ratio(r.positives, r.evaluated), 'The share of evaluated transactions that were positive.') +
                '</div>';
        }

        if (r.tagsInSample.length > 0) {
            html += '<div class="ruleToolsPanel"><h4>Tags on the evaluated transactions</h4>' +
                r.tagsInSample.map(function (t) {
                    return bar(t.name, t.count, r.evaluated, 'ruleToolsTag');
                }).join('') + '</div>';
        }

        return html + '</div><div id="BacktestExamples"></div>';
    }

    function renderExamples(panel, r, instance) {
        const container = panel.find('#BacktestExamples');
        [
            {title: 'True positives', rows: r.truePositiveSamples},
            {title: 'False positives', rows: r.falsePositiveSamples},
            {title: 'False negatives', rows: r.falseNegativeSamples},
            {title: 'Errors', rows: r.errorSamples}
        ].forEach(function (group) {
            if (!group.rows || group.rows.length === 0) {
                return;
            }
            const details = $('<details class="ruleToolsExamples"><summary>' + group.title + ' (' +
                group.rows.length + ' examples)</summary></details>').appendTo(container);
            grid(details, {
                dataSource: {
                    data: group.rows.map(function (s) {
                        return Object.assign({}, s, {referenceDate: s.referenceDate ? new Date(s.referenceDate) : null});
                    })
                },
                detailInit: function (e) {
                    explain(e.detailCell, instance.request, e.data.entityAnalysisModelInstanceEntryGuid);
                },
                columns: [
                    {field: 'entryKeyValue', title: 'Entry'},
                    {field: 'referenceDate', title: 'Reference Date', format: '{0:yyyy-MM-dd HH:mm:ss}', width: 190},
                    {field: 'fired', title: 'Fired', width: 90, template: '#= fired ? "Yes" : "No" #'},
                    {field: 'positive', title: 'Positive', width: 100, template: '#= positive ? "Yes" : "No" #'},
                    {field: 'error', title: 'Error'}
                ]
            });
        });
    }

    function flag(label, value) {
        return badge(label + ': ' + (value ? 'Yes' : 'No'), value ? 'ruleToolsYes' : 'ruleToolsNo');
    }

    function explain(container, request, guid) {
        container.html('<p class="ruleToolsHint">Loading…</p>');
        post(backtestApi + '/Explain', {request: request, entityAnalysisModelInstanceEntryGuid: guid})
            .done(function (e) {
                if (!e.valid) {
                    container.html('<p class="ruleToolsError">' + escape(e.errors.map(function (x) {
                        return x.message;
                    }).join(' ')) + '</p>');
                    return;
                }
                container.html('<div>' + flag('Kept by the filter', e.passedFilter) + ' ' + flag('Fired', e.fired) +
                    ' ' + (e.positive === null ? '' : flag('Positive', e.positive)) + '</div>' +
                    (e.filterError ? '<div class="ruleToolsError">Filter error: ' + escape(e.filterError) + '</div>' : '') +
                    (e.ruleError ? '<div class="ruleToolsError">Rule error: ' + escape(e.ruleError) + '</div>' : '') +
                    '<div class="ruleToolsSmall">Tags: ' + (e.tags.length ? escape(e.tags.join(', ')) : 'none') +
                    '</div>');
                grid(container, {
                    dataSource: {data: e.values},
                    columns: [
                        {field: 'name', title: 'Value Read'},
                        {
                            field: 'value', title: 'On This Transaction',
                            template: '#= value === null ? "<em>no value</em>" : kendo.htmlEncode(value) #'
                        }
                    ]
                });
            })
            .fail(function () {
                container.html('<p class="ruleToolsError">The transaction could not be explained.</p>');
            });
    }

    function loadDependencies() {
        const target = $('#RuleDependencies');
        if (ruleId() === null) {
            target.html('<p class="ruleToolsHint">Save the rule to see what it uses and what uses it.</p>');
            return;
        }
        target.html('<p class="ruleToolsHint">Loading…</p>');
        const entity = modelId() + '/' + options.ruleType + '/' + ruleId();
        $.when(get('/api/EntityAnalysisModelDependency/Dependents/' + entity),
            get('/api/EntityAnalysisModelDependency/Dependencies/' + entity)).done(function (dependents, dependencies) {
            const d = {dependents: dependents[0], dependencies: dependencies[0]};
            target.empty();
            $('<h4>Uses</h4>').appendTo(target);
            grid(target, {
                dataSource: {
                    data: d.dependencies.dependencies.map(function (u) {
                        return {
                            name: u.name,
                            entity: u.target ? u.target.kind + ' ' + u.target.name + (u.target.active ? '' : ' (inactive)')
                                : 'Does not exist',
                            missing: !u.target,
                            how: u.dependencyKind,
                            line: u.line === null ? null : u.line + 1
                        };
                    })
                },
                noRecords: {template: 'Nothing.'},
                columns: [
                    {field: 'name', title: 'Name'},
                    {
                        field: 'entity', title: 'Entity',
                        template: '#= missing ? "<span class=\\"ruleToolsBadge ruleToolsSeverityError\\">" + kendo.htmlEncode(entity) + "</span>" : kendo.htmlEncode(entity) #'
                    },
                    {field: 'how', title: 'How', width: 160},
                    {field: 'line', title: 'Line', width: 80}
                ]
            });
            $('<h4>Used by</h4>').appendTo(target);
            grid(target, {
                dataSource: {
                    data: d.dependents.dependents.map(function (u) {
                        return {
                            entity: u.dependent.kind + ' ' + u.dependent.name,
                            through: u.via || u.name,
                            depth: u.depth
                        };
                    })
                },
                noRecords: {template: 'Nothing uses this rule.'},
                columns: [
                    {field: 'entity', title: 'Entity'},
                    {field: 'through', title: 'Through'},
                    {field: 'depth', title: 'Depth', width: 90}
                ]
            });
        }).fail(function () {
            target.html('<p class="ruleToolsError">The dependencies could not be loaded.</p>');
        });
    }

    function loadIntegrity() {
        const target = $('#RuleIntegrity');
        if (ruleId() === null) {
            target.html('<p class="ruleToolsHint">Save the rule to check its integrity.</p>');
            return;
        }
        target.html('<p class="ruleToolsHint">Loading…</p>');
        get('/api/EntityAnalysisModelIntegrity/' + modelId()).done(function (report) {
            const checks = report.checks.filter(function (c) {
                return c.entityKind === options.ruleType && c.entityId === ruleId();
            });
            target.empty();
            grid(target, {
                dataSource: {data: checks},
                noRecords: {template: 'No findings for this rule.'},
                columns: [
                    {
                        field: 'severity', title: 'Severity', width: 120,
                        template: '#= "<span class=\\"ruleToolsBadge ruleToolsSeverity" + severity + "\\">" + severity + "</span>" #'
                    },
                    {field: 'code', title: 'Code', width: 280},
                    {field: 'message', title: 'Finding'}
                ]
            });
        }).fail(function () {
            target.html('<p class="ruleToolsError">The integrity findings could not be loaded.</p>');
        });
    }

    return {init: init};
})();
