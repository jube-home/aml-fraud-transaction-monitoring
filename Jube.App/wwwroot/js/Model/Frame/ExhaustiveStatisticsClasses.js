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

const exhaustiveStatisticsRoute = "/api/Chart/ExhaustiveStatistics/";

const exhaustiveStatisticsState = {counts: false, grid: null, instanceId: 0};

function exhaustiveStatisticsFail(container) {
    container.html('<div class="jc-message">Chart could not be loaded.</div>');
}

function loadExhaustiveStatisticsGrid(grid, instanceId) {
    exhaustiveStatisticsState.grid = grid;
    exhaustiveStatisticsState.instanceId = instanceId;
    exhaustiveStatisticsBuildToolbar();

    const cells = {};
    $(grid.element).find(".histogram").each(function () {
        const cell = $(this);
        const item = grid.dataItem(cell.closest("tr"));
        if (item) {
            cells[item.id] = cell;
            kendo.ui.progress(cell, true);
        }
    });

    const request = (exhaustiveStatisticsState.request || 0) + 1;
    exhaustiveStatisticsState.request = request;

    $.ajax({
        url: exhaustiveStatisticsRoute + "Grid/" + instanceId +
            "?view=classes" +
            "&counts=" + (exhaustiveStatisticsState.counts ? "true" : "false"),
        type: "GET",
        dataType: "json"
    }).done(function (items) {
        if (exhaustiveStatisticsState.request !== request) {
            return;
        }

        $.each(items || [], function (_, item) {
            const cell = cells[item.variableId];
            if (cell) {
                if (typeof item.svg === "string" && item.svg.trim().indexOf("<svg") === 0) {
                    cell.html(item.svg);
                } else {
                    exhaustiveStatisticsFail(cell);
                }
            }
        });
    }).fail(function () {
        $.each(cells, function (_, cell) {
            exhaustiveStatisticsFail(cell);
        });
    }).always(function () {
        $.each(cells, function (_, cell) {
            kendo.ui.progress(cell, false);
        });
    });
}

function exhaustiveStatisticsBuildToolbar() {
    if ($("#statisticsClassesToolbar").length > 0) {
        return;
    }

    const toolbar = $('<div id="statisticsClassesToolbar" class="statistics-classes-toolbar"></div>');

    const classesHint = $('<div class="hint"></div>')
        .text("Positive = rows matching the training instance's Class Definition filter (or the " +
            "anomaly-flagged rows, for an anomaly-trained model). Negative = everything else in the sample.");

    const countsLabel = $('<label class="statistics-classes-counts"></label>');
    const counts = $('<input type="checkbox" id="statisticsClassesCounts" />');
    countsLabel.append(counts).append(" Counts instead of share of class");

    toolbar.append(classesHint).append(countsLabel);
    $("#statistics").before(toolbar);
    $("#statistics").after('<div id="statisticsClassesDetail" class="statistics-classes-detail"></div>');

    counts.on("change", function () {
        exhaustiveStatisticsState.counts = counts.is(":checked");
        exhaustiveStatisticsRefresh();
    });
}

function exhaustiveStatisticsRefresh() {
    if (exhaustiveStatisticsState.grid) {
        loadExhaustiveStatisticsGrid(exhaustiveStatisticsState.grid, exhaustiveStatisticsState.instanceId);
    }

    exhaustiveStatisticsLoadDetail();
}

function exhaustiveStatisticsLoadDetail() {
    const detail = $("#statisticsClassesDetail");
    const grid = exhaustiveStatisticsState.grid;
    if (!grid || detail.length === 0) {
        return;
    }

    const selected = grid.select();
    const item = selected.length > 0 ? grid.dataItem(selected.first()) : null;
    if (!item) {
        detail.empty();
        return;
    }

    detail.empty();
    detail.append('<div class="statistics-classes-primary"></div>');
    const primary = detail.find(".statistics-classes-primary");
    const countsParam = "&counts=" + (exhaustiveStatisticsState.counts ? "true" : "false");
    const route = exhaustiveStatisticsState.instanceId + "/" + item.id;
    loadExhaustiveChart(primary, exhaustiveStatisticsRoute + "VariableClasses/" + route + "?large=true" + countsParam);

    detail.append('<div class="statistics-classes-monte-carlo"></div>');
    const monteCarlo = detail.find(".statistics-classes-monte-carlo");
    loadExhaustiveChart(monteCarlo, exhaustiveStatisticsRoute + "MonteCarlo/" + route + "?large=true" + countsParam);

    detail.append('<div class="statistics-classes-monte-carlo-summary"></div>');
    const monteCarloSummary = detail.find(".statistics-classes-monte-carlo-summary");
    loadExhaustiveChart(monteCarloSummary,
        exhaustiveStatisticsRoute + "MonteCarloSummary/" + route + "?large=true");
}

