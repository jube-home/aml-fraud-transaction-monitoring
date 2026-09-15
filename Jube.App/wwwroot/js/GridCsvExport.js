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

function exportGridToCsv(grid, url, params, filename) {
    const exportParams = Object.assign({take: 100000}, params);

    $.ajax({
        url: url,
        type: "GET",
        dataType: "json",
        data: exportParams
    }).done(function (response) {
        const rows = Array.isArray(response) ? response : response.rows;
        downloadCsvFile(gridToCsvString(grid, rows), filename);
    }).fail(function () {
        kendo.alert("CSV export failed. Please try again.");
    });
}

function gridToCsvString(grid, rows) {
    const columns = grid.columns.filter(function (column) {
        return !!column.field;
    });

    const escapeCsvValue = function (value) {
        if (value === null || value === undefined) {
            return "";
        }

        const text = value instanceof Date ? value.toISOString() : String(value);
        return /[",\r\n]/.test(text) ? '"' + text.replace(/"/g, '""') + '"' : text;
    };

    const lines = [columns.map(function (column) {
        return escapeCsvValue(column.title || column.field);
    }).join(",")];

    rows.forEach(function (item) {
        lines.push(columns.map(function (column) {
            return escapeCsvValue(item[column.field]);
        }).join(","));
    });

    return lines.join("\r\n");
}

function downloadCsvFile(csv, filename) {
    const blob = new Blob(["\ufeff" + csv], {type: "text/csv;charset=utf-8;"});
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}

//# sourceURL=GridCsvExport.js
