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

const endpoint = "/api/TenantRegistry";
const validationFail = "There is invalid data in the form. Please check fields and correct.";

function getRecords() {
    $("#Tenants").html("");

    $.get("/api/TenantRegistry",
        function (data) {
            for (const value of data) {
                $("#Tenants").prepend("<a href='#' onclick='loadTemplate(" + value.id + ");'>" + value.name + "</a></br>");
            }
        }
    );
}

function loadTemplate(id) {
    $.get(endpoint + "/" + id,
        function (data) {
            ReadyExisting(data);
            validator.validate();

            $("#Template").show();
            $("#Homepage").hide();
        });
}

function showTemplate() {
    ReadyNew();
    $("#Template").show();
    $("#Homepage").hide();
}

function showHomePage() {
    $("#Template").hide();
    $("#Homepage").show();
    getRecords();
}

$(document).ready(function () {
    showHomePage();

    $.getScript('/js/CRUD.js', function () {
        $("#Back").kendoButton({
            click: function () {
                showHomePage();
            }
        });

        $(function () {
            deleteButton
                .click(function () {
                    if (confirm('Are you sure you want to delete?')) {
                        Delete(endpoint, id);
                    }
                });
        });

        function GetData() {
            return {};
        }

        $(function () {
            addButton
                .click(function () {
                    if (validator.validate()) {
                        Create(endpoint, GetData(), "id");
                    } else {
                        $("#ErrorMessage").html(validationFail);
                    }
                });
        });

        $(function () {
            updateButton
                .click(function () {
                    if (validator.validate()) {
                        Update(endpoint, GetData(), "id");
                    } else {
                        $("#ErrorMessage").html(validationFail);
                    }
                });
        });
    });

    $("#Template").hide();
    $("#Homepage").show();

    $("#New").kendoButton({
        click: function () {
            showTemplate();
        }
    });
});

//# sourceURL=TenantRegistry.js