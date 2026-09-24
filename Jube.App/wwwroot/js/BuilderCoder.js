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

let langTools;
let ruleTextBuilder = "";
let ruleTextCoder = "";
let builderInvalid = false;
let CompileInProgress = false;
let FirstCompile = true;
let completions = [];
let builder;
let PendingCoderChangedCompileTimer;
let coder;
let showBuilder = false;
let ruleType;
let valid;
let builderValidationInterval;
let tabStrip;
let entityAnalysisModelId;
let ruleParseType;
let builderReady = false;
let builderOperatorCatalogue = [];
let builderOperatorsPromise;
let builderFieldTypes = {};

function loadScript(url) {
    return new Promise(function (resolve, reject) {
        $.getScript(url).done(resolve).fail(reject);
    });
}

function loadBuilderOperators() {
    if (!builderOperatorsPromise) {
        builderOperatorsPromise = new Promise(function (resolve, reject) {
            $.getJSON("/api/QueryBuilder/Operators")
                .done(function (operators) {
                    builderOperatorCatalogue = operators;
                    resolve(operators);
                })
                .fail(reject);
        });
    }
    return builderOperatorsPromise;
}

function builderOperatorsFor(dataType) {
    return builderOperatorCatalogue.filter(function (o) {
        return o.applyTo.indexOf(dataType) >= 0;
    }).map(function (o) {
        return o.type;
    });
}

function loadCompletions() {
    return new Promise(function (resolve, reject) {
        $.getJSON("../api/Completions/ByEntityAnalysisModelIdParseTypeId",
            {
                entityAnalysisModelId: entityAnalysisModelId,
                parseTypeId: ruleParseType
            })
            .done(resolve)
            .fail(reject);
    });
}

function getBuilderCoder() {
    let value;
    if (showBuilder) {
        createBuilderRuleText();

        value = {
            ruleTextBuilder: ruleTextBuilder,
            ruleJsonBuilder: JSON.stringify(builder.queryBuilder('getRules'), null, 2),
            ruleTextCoder: ruleTextCoder,
            ruleType: ruleType
        };
    } else {
        value = {
            ruleTextCoder: ruleTextCoder,
            ruleType: ruleType
        };
    }

    return value;
}

function validateBuilderCoder() {
    return true;
}

function checkDivergence() {
    if (showBuilder) {
        if (ruleType === 2) {
            if (ruleTextBuilder !== ruleTextCoder) {
                tabStrip.disable(tabStrip.tabGroup.children().eq(0));
            } else {
                tabStrip.enable(tabStrip.tabGroup.children().eq(0));
            }
        }
    }
}

function CoderChanged() {
    ruleTextCoder = coder.getValue().trim();
    checkDivergence();
    if (typeof PendingCoderChangedCompileTimer !== "undefined") {
        clearTimeout(PendingCoderChangedCompileTimer);
    }
    if (FirstCompile) {
        FirstCompile = false;
        CoderChangedCompile();
    } else {
        PendingCoderChangedCompileTimer = setTimeout('CoderChangedCompile();', 1300);
    }
}

function converge() {
    coder.setValue(createBuilderRuleText());
    $("#BuilderRuleTypeWrapper").show();
    $("#ResetBuilderRuleTypeWrapper").hide();
}

function CoderChangedCompile() {
    if (!CompileInProgress) {
        CompileInProgress = true;
        let coderText = coder.getValue();

        let compileStatus = $('#CompileStatus');
        if (coderText.trim() === '') {
            coder.getSession().clearAnnotations();
            compileStatus.html("Return False");
            compileStatus.css("color", "blue");
        } else {
            $.ajax({
                url: "../api/Parser",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: JSON.stringify({
                    RuleParseType: ruleParseType,
                    RuleText: coderText,
                    EntityAnalysisModelId: entityAnalysisModelId
                }),
                error: function () {
                    coder.getSession().clearAnnotations();
                    compileStatus.html("Disconnected");
                    compileStatus.css("color", "red");
                },
                success: function (data) {
                    const annotations = [];

                    if (data.errorSpans != null) {
                        data.errorSpans.forEach(function (errorSpan) {
                                annotations.push({
                                    row: errorSpan.line,
                                    column: 0,
                                    text: errorSpan.message,
                                    type: "error"
                                })
                            }
                        );
                        coder.getSession().setAnnotations(annotations);

                        compileStatus.html("Errors");
                        compileStatus.css("color", "red");
                    } else {
                        coder.getSession().clearAnnotations();
                        compileStatus.html("Compiled");
                        compileStatus.css("color", "green");
                    }
                    CompileInProgress = false;
                }
            });
        }
    }
}

function validateBuilder() {
    if (!builderReady || !builder) {
        return false;
    }
    const instance = builder.data('queryBuilder');
    if (!instance || !instance.filters || instance.filters.length === 0) {
        return false;
    }
    return builder.queryBuilder('validate');
}

function setRuleType(denormIndex) {
    ruleType = denormIndex;
    const $builderWarnings = $('#BuilderWarnings');
    if (ruleType === 2) {
        coder.session.setValue(createBuilderRuleText());
        $builderWarnings.hide();
    } else {
        $builderWarnings.show();
    }
}

function createBuilderRuleText() {
    if (validateBuilder()) {
        ruleTextBuilder = builderRuleText(builder.queryBuilder('getRules'));
    } else {
        ruleTextBuilder = "Return False";
        builderInvalid = true;
    }
    return ruleTextBuilder;
}

function FilterExists(name, filters) {
    for (let i = 0; i < filters.length; i++) {
        if (filters[i].id === name) {
            return true;
        }
    }
    return false;
}

function builderFiltersFromCompletions(source) {
    let filters = [];
    let listSelects = {};

    for (const completion of source) {
        if (completion.dataType === "string") {
            listSelects[completion.name] = completion.name;
        }
    }

    for (const completion of source) {
        if (FilterExists(completion.name, filters)) {
            continue;
        }

        const operators = builderOperatorsFor(completion.dataType);
        if (operators.length === 0) {
            continue;
        }

        let filter = {
            optgroup: completion.group,
            id: completion.name,
            name: completion.name,
            operators: operators,
            value_separator: ','
        };

        if (completion.dataType === "string") {
            filter.type = 'string';
        } else if (completion.dataType === "integer") {
            filter.type = 'integer';
        } else if (completion.dataType === "double") {
            filter.type = 'double';
        } else if (completion.dataType === "datetime") {
            filter.type = 'datetime';
        } else if (completion.dataType === "boolean") {
            filter.type = 'string';
            filter.input = "radio";
            filter.default_value = "True";
            filter.values = {'True': 'Yes', 'False': 'No'};
        } else if (completion.dataType === "list") {
            filter.type = 'string';
            filter.input = "select";
            filter.values = listSelects;
        } else {
            continue;
        }

        builderFieldTypes[completion.name] = completion.dataType;
        filters.push(filter);
    }

    return filters;
}

function builderOptions(filters) {
    const operatorLabels = {};
    const groupLabels = {};
    const operators = builderOperatorCatalogue.map(function (o) {
        operatorLabels[o.type] = o.label;
        groupLabels[o.group] = o.group;
        return {
            type: o.type,
            nb_inputs: o.arguments.length,
            multiple: false,
            apply_to: ['string', 'number', 'datetime', 'boolean'],
            optgroup: o.group
        };
    });

    return {
        plugins: [
            'not-group'
        ],
        filters: filters,
        operators: operators,
        lang: {operators: operatorLabels, optgroups: groupLabels}
    };
}

function builderOperator(type) {
    return builderOperatorCatalogue.find(function (o) {
        return o.type === type;
    });
}

function builderArgumentKind(argument, dataType) {
    if (argument.kind !== 'value') {
        return argument.kind;
    }
    switch (dataType) {
        case 'integer':
            return 'integer';
        case 'double':
            return 'number';
        case 'datetime':
            return 'date';
        case 'string':
            return 'text';
        case 'boolean':
            return 'boolean';
        default:
            return 'field';
    }
}

function builderReferable(kind, dataType) {
    switch (kind) {
        case 'list':
            return ['list'];
        case 'field':
            return dataType === 'list' ? ['string'] : [dataType];
        case 'number':
            return ['double', 'integer'];
        case 'integer':
            return ['integer'];
        case 'date':
            return ['datetime'];
        default:
            return [];
    }
}

function builderFieldsOf(types) {
    return Object.keys(builderFieldTypes).filter(function (id) {
        return types.indexOf(builderFieldTypes[id]) >= 0;
    }).sort();
}

function builderIsCustomised(rule) {
    const operator = rule.operator && builderOperator(rule.operator.type);
    return !!operator && operator.arguments.length > 0 && rule.filter && rule.filter.input !== 'radio';
}

function builderInputHtml(rule, index) {
    const dataType = builderFieldTypes[rule.filter.id];
    const argument = builderOperator(rule.operator.type).arguments[index];
    const kind = builderArgumentKind(argument, dataType);
    const name = rule.id + '_value_' + index;
    const referable = builderFieldsOf(builderReferable(kind, dataType));

    if (kind === 'list' || kind === 'field') {
        return '<select class="form-control" name="' + name + '" title="' + escapeBuilderHtml(argument.name) + '">' +
            '<option value="">' + escapeBuilderHtml(argument.name) + '</option>' +
            referable.map(function (id) {
                return '<option value="' + escapeBuilderHtml(id) + '">' + escapeBuilderHtml(id) + '</option>';
            }).join('') + '</select>';
    }

    let placeholder = argument.name + (argument.many ? ', separated by commas' : '');
    if (kind === 'date') {
        placeholder += ' (2026-01-31T00:00:00)';
    }
    const listId = referable.length > 0 ? name + '_fields' : null;
    return '<input class="form-control" type="text" name="' + name + '" placeholder="' +
        escapeBuilderHtml(placeholder) + '" title="' + escapeBuilderHtml(placeholder +
            (referable.length > 0 ? ', or a field' : '')) + '"' + (listId ? ' list="' + listId + '"' : '') + '>' +
        (listId ? '<datalist id="' + listId + '">' + referable.map(function (id) {
            return '<option value="' + escapeBuilderHtml(id) + '">';
        }).join('') + '</datalist>' : '');
}

function escapeBuilderHtml(text) {
    return String(text).replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function builderItems(argument, value) {
    if (!argument.many) {
        return [value];
    }
    return (Array.isArray(value) ? value : String(value === undefined || value === null ? '' : value).split(','))
        .map(function (item) {
            return String(item).trim();
        }).filter(function (item) {
            return item.length > 0;
        });
}

function builderIsoDate(text) {
    const trimmed = String(text).trim();
    const zoned = /([zZ]|[+-]\d\d:?\d\d)$/.test(trimmed) || !/\d[T ]\d/.test(trimmed) ? trimmed : trimmed + 'Z';
    const date = new Date(/[T ]/.test(zoned) || /[zZ]$/.test(zoned) ? zoned : zoned + 'T00:00:00Z');
    if (isNaN(date.getTime())) {
        return null;
    }
    return date.toISOString().replace(/\.000Z$/, 'Z').replace(/(\.\d*?)0+Z$/, '$1Z');
}

function builderItemError(kind, dataType, item) {
    const text = String(item === undefined || item === null ? '' : item).trim();
    if (text.length === 0) {
        return 'A value is needed.';
    }
    if (builderFieldsOf(builderReferable(kind, dataType)).indexOf(text) >= 0) {
        return null;
    }
    switch (kind) {
        case 'integer':
            return /^[+-]?\d+$/.test(text) ? null : 'A whole number is needed.';
        case 'number':
            return isFinite(Number(text)) ? null : 'A number is needed.';
        case 'date':
            return builderIsoDate(text) ? null : 'A date and time such as 2026-01-31T00:00:00 is needed.';
        case 'list':
            return 'Choose a list.';
        case 'field':
            return 'Choose a field.';
        case 'text':
            return /[\u0000-\u001f\u007f]/.test(text) ? 'Line breaks and control characters cannot be used.' : null;
        default:
            return null;
    }
}

function bindBuilderInputs($element) {
    $element.off('.builderInputs')
        .on('getRuleInput.queryBuilder.filter.builderInputs', function (e, rule, name) {
            if (builderIsCustomised(rule)) {
                e.value = builderInputHtml(rule, Number(name.substring(name.lastIndexOf('_') + 1)));
            }
        })
        .on('getRuleValue.queryBuilder.filter.builderInputs', function (e, rule) {
            if (!builderIsCustomised(rule)) {
                return;
            }
            const container = rule.$el.find('.rule-value-container');
            const values = [];
            for (let i = 0; i < rule.operator.nb_inputs; i++) {
                values.push(container.find('[name="' + rule.id + '_value_' + i + '"]').val());
            }
            e.value = values.length === 1 ? values[0] : values;
        })
        .on('validateValue.queryBuilder.filter.builderInputs', function (e, value, rule) {
            if (!builderIsCustomised(rule)) {
                return;
            }
            const operator = builderOperator(rule.operator.type);
            const dataType = builderFieldTypes[rule.filter.id];
            const inputs = operator.arguments.length === 1 ? [value] : value;
            for (let i = 0; i < operator.arguments.length; i++) {
                const argument = operator.arguments[i];
                const kind = builderArgumentKind(argument, dataType);
                const items = builderItems(argument, inputs[i]);
                if (items.length === 0) {
                    e.value = [argument.name + ': a value is needed.'];
                    return;
                }
                for (const item of items) {
                    const error = builderItemError(kind, dataType, item);
                    if (error) {
                        e.value = [argument.name + ': ' + error];
                        return;
                    }
                }
            }
            e.value = true;
        });
}

function builderQuote(text) {
    return '"' + String(text).split('"').join('""') + '"';
}

function builderLiteral(kind, dataType, item) {
    const text = String(item).trim();
    if (builderFieldsOf(builderReferable(kind, dataType)).indexOf(text) >= 0) {
        return text;
    }
    switch (kind) {
        case 'integer':
            return String(parseInt(text, 10));
        case 'number':
            return String(Number(text));
        case 'date':
            return builderQuote(builderIsoDate(text)) + '.ToIsoDateTime()';
        case 'boolean':
            return text === 'True' ? 'True' : 'False';
        case 'list':
        case 'field':
            return text;
        default:
            return builderQuote(item);
    }
}

function builderRuleExpression(rule) {
    const operator = builderOperator(rule.operator);
    const dataType = builderFieldTypes[rule.id];
    const inputs = operator.arguments.length === 1 ? [rule.value] : (rule.value || []);
    const rendered = operator.arguments.map(function (argument, i) {
        const kind = builderArgumentKind(argument, dataType);
        return builderItems(argument, inputs[i]).map(function (item) {
            return builderLiteral(kind, dataType, item);
        }).join(', ');
    }).join(', ');
    return rule.id + operator.template.split('?').join(rendered);
}

function builderGroupExpression(group) {
    const parts = group.rules.map(function (node) {
        return node.rules ? '( ' + builderGroupExpression(node) + ' ) ' : builderRuleExpression(node);
    });
    const expression = parts.join(' ' + group.condition + ' ');
    return group.not ? 'NOT ( ' + expression + ' )' : expression;
}

function builderRuleText(rules) {
    return 'If (' + builderGroupExpression(rules) + ') Then\n  Return True\nEnd If';
}

function createQueryBuilder(selector, filters, rules) {
    const settings = builderOptions(filters);
    settings.allow_empty = true;
    settings.rules = {condition: 'AND', rules: []};
    bindBuilderInputs($(selector));
    const instance = $(selector).queryBuilder(settings);
    if (rules && rules.rules && rules.rules.length > 0) {
        try {
            instance.queryBuilder('setRules', rules);
        } catch (e) {
            console.warn('Could not restore builder rules:', e.message);
        }
    }
    return instance;
}

function initBuilder(data) {
    if (!completions || completions.length === 0) {
        console.warn('initBuilder called with no completions — aborting.');
        return;
    }

    let rules;
    if (data) {
        rules = data.ruleJsonBuilder;
    }

    let filters = builderFiltersFromCompletions(completions);

    bindBuilderInputs($('#Builder'));
    builder = $('#Builder').queryBuilder(builderOptions(filters));

    if (rules) {
        const dropped = [];

        try {
            const parsedRules = typeof rules === 'string' ? JSON.parse(rules) : rules;

            function stripUnknownFilters(group) {
                if (!group || !Array.isArray(group.rules)) return group;
                group.rules = group.rules.filter(function (rule) {
                    if (rule.rules) {
                        stripUnknownFilters(rule);
                        return true;
                    }
                    if (!FilterExists(rule.id, filters)) {
                        dropped.push(rule.id);
                        return false;
                    }
                    return true;
                });
                return group;
            }

            const cleanedRules = stripUnknownFilters(parsedRules);

            if (cleanedRules && cleanedRules.rules && cleanedRules.rules.length > 0) {
                builder.queryBuilder('setRules', cleanedRules);
            } else if (dropped.length === 0) {
                console.warn('Could not restore builder rules: empty or malformed rule object');
            }
        } catch (e) {
            console.warn('Could not restore builder rules:', e.message);
        }

        if (dropped.length > 0) {
            const msg = '⚠ The following fields are no longer available and have been removed from the rule: '
                + dropped.join(', ')
                + '. This rule should be reviewed.';
            console.warn(msg);
            $('#BuilderWarnings').text(msg);
        }
    }

    builderReady = true;

    if (builderValidationInterval) {
        clearInterval(builderValidationInterval);
    }
    builderValidationInterval = setInterval(validateBuilder, 1300);
}

function initCoder(data) {
    langTools = ace.require('ace/ext/language_tools');

    const completer = {
        getCompletions: (editor, session, pos, prefix, callback) => {
            if (prefix.length === 0) {
                callback(null, []);
                return;
            }

            callback(null, completions.map(function (ea) {
                return {
                    name: ea.name,
                    value: ea.value,
                    score: ea.score,
                    meta: ea.meta
                };
            }));
        }
    };

    coder = ace.edit('Coder');
    coder.setTheme('ace/theme/sqlserver');
    coder.getSession().setMode('ace/mode/vbscript');
    coder.getSession().on('change', CoderChanged);
    coder.setReadOnly(false);
    langTools.setCompleters([]);
    coder.setOptions({
        enableBasicAutocompletion: true,
        enableSnippets: true,
        enableLiveAutocompletion: true
    });

    if (data) {
        coder.setValue(data.ruleTextCoder);
    } else {
        coder.setValue(ruleTextCoder);
    }
    langTools.addCompleter(completer);
}

function initBuilderCoder(parseType, modelId, data) {
    ruleParseType = parseType;
    switch (ruleParseType) {
        case 1:
            showBuilder = false;
            break;
        case 2:
            showBuilder = true;
            break;
        case 3:
            showBuilder = true;
            break;
        case 4:
            showBuilder = false;
            break;
        case 5:
            showBuilder = true;
            break;
        default:
            showBuilder = false;
            break;
    }

    entityAnalysisModelId = modelId;

    let ruleDiv = $("#rule");

    if (showBuilder) {
        ruleDiv.append("<div id='RuleType'></div>");
        let ruleTypeDiv = $("#RuleType");
        ruleTypeDiv.append("<ul id='RuleTypeList'></ul>");
        let ruleTypeList = $("#RuleTypeList");
        ruleTypeList.append("<li>Builder</li>");
        ruleTypeDiv.append("<div id='Builder'></div>");
        ruleTypeList.append("<li>Coder</li>");
        ruleTypeDiv.append("<div id='CoderWrapper' style='padding-top: .3em'></div>");
        let coderWrapper = $("#CoderWrapper");
        coderWrapper.append("<div class='validationLink'><a href='#' onclick='converge()'>Reset</a></div>");
        coderWrapper.append("<div id='Coder' class='coder'></div>");
        coderWrapper.append("<div id='CompileStatus' class='compileErrors'></div>");
        $('#CompileStatus').html("Return False").css("color", "blue");
        tabStrip = ruleTypeDiv.kendoTabStrip({animation: false}).data("kendoTabStrip");
        ruleDiv.append("<div id='BuilderWarnings' style='color: darkorange; font-size: 0.85em; padding: 0.3em 0;'></div>");
    } else {
        ruleDiv.append("<div id='CoderWrapper' style='padding-top: .3em'></div>");
        let coderWrapper = $("#CoderWrapper");
        coderWrapper.append("<div id='Coder' class='coder'></div>");
        coderWrapper.append("<div id='CompileStatus' class='compileErrors'></div>");
        $('#CompileStatus').html("Return False").css("color", "blue");
    }

    const scriptPromises = [
        loadScript('/js/ace/ace.js').then(function () {
            ace.config.set('basePath', '/js/ace/');
            return loadScript('/js/ace/ext-language_tools.js');
        })
    ];

    if (showBuilder) {
        scriptPromises.push(
            loadScript('/js/builder/query-builder.standalone.min.js').then(function () {
                $('<link/>', {
                    rel: 'stylesheet',
                    type: 'text/css',
                    href: '/styles/query-builder.default.min.css'
                }).appendTo('head');
            })
        );
    }

    if (showBuilder) {
        scriptPromises.push(loadBuilderOperators());
    }

    Promise.all([loadCompletions(), ...scriptPromises])
        .then(function (results) {
            completions = results[0];

            initCoder(data);

            if (showBuilder) {
                initBuilder(data);

                if (data) {
                    ruleType = data.ruleType;
                    tabStrip.select(ruleType - 1);
                    checkDivergence();
                } else {
                    tabStrip.select(0);
                    ruleType = 1;
                }

                const $builderWarnings = $("#BuilderWarnings");
                if (ruleType === 2) {
                    $builderWarnings.hide();
                } else {
                    $builderWarnings.show();
                }

                tabStrip.bind("select", function (e) {
                    setRuleType($(e.item).index() + 1);
                });
            }
        })
        .catch(function (err) {
            console.error('Failed to initialise builder/coder:', err);
            $('#BuilderWarnings').text('⚠ Failed to load rule editor. Please refresh the page.');
        });
}

//# sourceURL=BuilderCoder.js