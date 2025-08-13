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

$(document).ready(function () {
    $("#FormChange").kendoValidator({
        errorTemplate: "<span class='errorMessage'>#=message#</span>",
        rules: {
            verifyPasswords: function (input) {
                if (input.is("#VerifyNewPassword")) {
                    return input.val() === $("#NewPassword").val();
                }
                return true;
            }
        }
    });

    $("#MessageChange").hide();

    $("#Change").kendoButton({
        click: function (e) {
            $("#MessageAuthenticate").html("");
            if ($("#FormChange").data("kendoValidator").validate()) {
                $("#Change").data("kendoButton").enable(false);

                const message = $("#DoneMessage");
                message.css('color', 'green');
                message.html("Changing.")

                PostAuthentication();
            }
        }
    });

    function PostAuthentication() {
        let data = {
            userName: $("#UserName").val(),
            password: $("#ExistingPassword").val(),
            newPassword: $("#NewPassword").val()
        };

        $.ajax({
            url: "../api/Authentication/ChangePassword",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(data),
            statusCode: {
                200: function (response) {
                    const message = $("#DoneMessage");
                    message.css('color', 'green');
                    message.html("Done.");
                    $("#Change").data("kendoButton").enable(true);
                },
                401: function (response) {
                    $("#FormChange").data("kendoValidator").reset();
                    $("#Change").data("kendoButton").enable(true);
                    $("#DoneMessage").html("");
                    $("#MessageAuthenticate").html("Invalid Login.  Check existing password and try again.")
                },
                400: function (response) {
                    $("#Change").data("kendoButton").enable(true);
                    $("#MessageChange").show();
                    $("#DoneMessage").html("");
                    $("#FormChange").data("kendoValidator").reset();
                }
            }
        });
    }
});