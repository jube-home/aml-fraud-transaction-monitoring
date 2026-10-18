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

function evidenceFormatLeafValue(value) {
    if (value === null || value === undefined) {
        return "";
    }
    if (typeof value === "boolean") {
        return value ? "Yes" : "No";
    }
    if (typeof value === "number") {
        return Number.isInteger(value) ? String(value) : value.toFixed(4);
    }
    return String(value);
}

function evidenceJsonToTreeListRows(value, parentId, key, nextId) {
    const id = nextId.value++;
    const isBranch = value !== null && typeof value === "object";
    const rows = [{
        id: id,
        parentId: parentId,
        key: key,
        value: isBranch ? "" : evidenceFormatLeafValue(value),
        expanded: true
    }];
    if (isBranch) {
        const entries = Array.isArray(value)
            ? value.map(function (v, i) {
                return ["[" + i + "]", v];
            })
            : Object.keys(value).map(function (k) {
                return [k, value[k]];
            });
        entries.forEach(function (entry) {
            rows.push.apply(rows, evidenceJsonToTreeListRows(entry[1], id, entry[0], nextId));
        });
    }
    return rows;
}

function renderEvidenceTreeInto(containerSelector, evidence) {
    const container = $(containerSelector);
    const existing = container.data("kendoTreeList");
    if (existing) {
        existing.destroy();
        container.empty();
    }
    const nextId = {value: 1};
    const rows = Object.keys(evidence || {}).reduce(function (all, key) {
        return all.concat(evidenceJsonToTreeListRows(evidence[key], null, key, nextId));
    }, []);
    container.kendoTreeList({
        dataSource: {
            data: rows,
            schema: {model: {id: "id", parentId: "parentId", expanded: true}}
        },
        columns: [
            {field: "key", title: "Key", width: 320},
            {field: "value", title: "Value"}
        ]
    });
}

