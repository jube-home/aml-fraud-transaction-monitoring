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

let where;
let select;

function getExhaustiveFilter() {
    let value;

    let result = where.queryBuilder('getSQL', 'numbered(@)');

    value = {
        filterSql: result.sql,
        filterJson: JSON.stringify(where.queryBuilder('getRules'), null, 2),
        filterTokens: JSON.stringify(result.params, null, 2)
    }

    return value;
}

function validateBuilder() {
    return where.queryBuilder('validate');
}

function FilterExists(name, filters) {
    for (let i = 0; i < filters.length; i++) {
        if (filters[i].id === name) {
            return true;
        }
    }
    return false;
}

function initExhaustiveFilterBuilder(entityAnalysisModelId, data) {
    if (typeof select !== "undefined") {
        select.queryBuilder('destroy');
    }

    if ($("#Where").length === 0) {
        $("#Builder").append("<div id='Where'/>");
    }

    if (typeof where !== "undefined") {
        where.queryBuilder('destroy');
    }

    $.getJSON("../api/Completions/ByEntityAnalysisModelId",
        {
            entityAnalysisModelId: entityAnalysisModelId
        }
        , function (completionsData) {
            completions = completionsData;

            $.getScript('../js/builder/query-builder.standalone.min.js', function () {
                $('<link/>', {
                    rel: 'stylesheet',
                    type: 'text/css',
                    href: '/styles/query-builder.default.min.css'
                }).appendTo('head');

                let rulesFilter;
                if (data) {
                    rulesFilter = data.filterJson;
                }

                let filters = [];

                for (const completion of completions) {
                    if (!FilterExists(completion.name, filters)) {
                        if (completion.dataType === "string") {
                            let filter = {
                                optgroup: completion.group,
                                id: completion.name,
                                field: completion.field,
                                label: completion.name,
                                type: completion.dataType
                            };
                            filters.push(filter);
                        } else if (completion.dataType === "integer") {
                            let filter = {
                                optgroup: completion.group,
                                id: completion.name,
                                field: completion.field,
                                label: completion.name,
                                type: 'integer'
                            };
                            filters.push(filter);
                        } else if (completion.dataType === "double") {
                            let filter = {
                                optgroup: completion.group,
                                id: completion.name,
                                field: completion.field,
                                label: completion.name,
                                type: 'integer'
                            };
                            filters.push(filter);
                        } else if (completion.dataType === "boolean") {
                            let filter = {
                                optgroup: completion.group,
                                id: completion.name,
                                field: completion.field,
                                label: completion.name,
                                type: 'string',
                                input: "radio",
                                default_value: "True",
                                values: {
                                    'True': 'Yes',
                                    'False': 'No'
                                },
                                operators: ['equal']
                            };
                            filters.push(filter);
                        }
                    }
                }

                where = $('#Where').queryBuilder({
                    filters: filters,
                    operators: [
                        {type: 'equal', optgroup: 'basic'},
                        {type: 'less', optgroup: 'basic'},
                        {type: 'less_or_equal', optgroup: 'basic'},
                        {type: 'greater', optgroup: 'basic'},
                        {type: 'begins_with', optgroup: 'basic'},
                        {type: 'contains', optgroup: 'basic'},
                        {type: 'ends_with', optgroup: 'basic'}
                    ],
                    rules: rulesFilter
                });

                validateBuilder()

                builderValidationInterval = setInterval(validateBuilder, 1300);
            });
        });
}

//# sourceURL=ExhaustiveFilterBuilder.js