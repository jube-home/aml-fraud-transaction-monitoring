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
    $("#SandboxRegistration").kendoValidator({
        errorTemplate: "<span class='errorMessage'>#=message#</span>"
    });

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

    function PostSandboxRegistration() {
        let data = {
            userName: $("#UserName").val(),
            password: $("#Password").val(),
            repeatPassword: $("#RepeatPassword").val()
        };

        $.ajax({
            url: "../api/SandboxRegistration/Register",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(data),
            statusCode: {
                200: function (response) {
                    document.location.replace("/Watcher/Activation");
                },
                409: function (response) {
                    $("#Register").data("kendoButton").enable(true);
                    $("#MessageSandboxRegistration").css('color', 'red');
                    let errorString = '</br></br>Processing errors in registration:</br></br><ul>';
                    errorString = errorString + '<li>Username already exists.  Please contact support@jube.io or use a different email address.</li>';
                    $("#MessageSandboxRegistration").html(errorString);
                },
                400: function (response) {
                    $("#Register").data("kendoButton").enable(true);
                    $("#MessageSandboxRegistration").css('color', 'red');

                    let errors = JSON.parse(response.responseText).errors;
                    let i = 0;
                    let errorString = '</br></br>Validation errors in registration:</br></br><ul>';
                    while (i < errors.length) {
                        errorString = errorString + '<li>' + errors[i].errorMessage + '</li>';
                        i++;
                    }
                    errorString = errorString + '</ul>';

                    $("#MessageSandboxRegistration").html(errorString);
                    $("#SandboxRegistration").data("kendoValidator").reset();
                }
            }
        });
    }

    $("#Register").kendoButton({
        click: function (e) {
            if ($("#SandboxRegistration").data("kendoValidator").validate()) {
                $("#Register").data("kendoButton").enable(false);
                $("#MessageSandboxRegistration").css('color', 'green');
                $("#MessageSandboxRegistration").html("Registering.");
                PostSandboxRegistration();
            }
        }
    });
});

//# sourceURL=SandboxRegistration.js