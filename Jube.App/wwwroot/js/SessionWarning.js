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

(function () {
    var SessionWarningThresholdMs = 5 * 60 * 1000;
    var SessionCheckIntervalMs = 10 * 1000;
    var SessionActivityPingThrottleMs = 3 * 60 * 1000;

    var warningElement = null;
    var extendInFlight = false;
    var lastActivityPingAt = 0;

    function GetExpiryMs() {
        if (!window.JubeSessionExpiry) {
            return null;
        }

        var parsed = window.JubeSessionExpiry.get();
        return typeof parsed === "number" && !isNaN(parsed) ? parsed : null;
    }

    function ShowWarning() {
        if (warningElement !== null) {
            return;
        }

        warningElement = document.createElement("div");
        warningElement.id = "jubeSessionWarning";
        warningElement.className = "jubeSessionWarning";

        var text = document.createElement("span");
        text.textContent = "Your session is about to expire.";
        warningElement.appendChild(text);

        var link = document.createElement("a");
        link.href = "#";
        link.className = "jubeSessionWarningLink";
        link.textContent = "Extend Attended Session";
        link.addEventListener("click", function (e) {
            e.preventDefault();
            ExtendSessionAsync();
        });
        warningElement.appendChild(link);

        document.body.appendChild(warningElement);
    }

    function HideWarning() {
        if (warningElement === null) {
            return;
        }

        warningElement.remove();
        warningElement = null;
    }

    function ExtendSessionAsync() {
        if (extendInFlight) {
            return;
        }

        extendInFlight = true;
        $.get("/api/Session/Extend").always(function () {
            extendInFlight = false;
        }).done(function () {
            HideWarning();
        }).fail(function (xhr) {
            if (xhr.status === 401 || xhr.status === 403) {
                window.location.href = "/Account/Login";
            }
        });
    }

    function OnUserActivity() {
        var now = Date.now();
        if (now - lastActivityPingAt < SessionActivityPingThrottleMs) {
            return;
        }

        var expiryMs = GetExpiryMs();
        if (expiryMs === null) {
            return;
        }

        lastActivityPingAt = now;
        $.get("/api/Session/Extend");
    }

    function CheckSession() {
        var expiryMs = GetExpiryMs();
        if (expiryMs === null) {
            HideWarning();
            return;
        }

        var remainingMs = expiryMs - Date.now();
        if (remainingMs <= SessionWarningThresholdMs) {
            ShowWarning();
        } else {
            HideWarning();
        }
    }

    $(function () {
        document.addEventListener("click", OnUserActivity, true);
        document.addEventListener("change", OnUserActivity, true);
        document.addEventListener("submit", OnUserActivity, true);

        setInterval(CheckSession, SessionCheckIntervalMs);
        CheckSession();
    });
})();
