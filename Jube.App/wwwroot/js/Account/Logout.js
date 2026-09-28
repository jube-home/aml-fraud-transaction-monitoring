$(function () {
    const $logoutButton = $("#LogoutButton");
    if ($logoutButton.length === 0) {
        return;
    }

    const $stayLoggedInButton = $("#StayLoggedInButton");
    const $error = $("#LogoutError");

    $stayLoggedInButton.addClass("k-primary").kendoButton({
        click: function () {
            window.location.href = "/";
        }
    });

    $logoutButton.kendoButton({
        click: async function () {
            $logoutButton.data("kendoButton").enable(false);
            $error.hide();

            try {
                const response = await fetch("/api/Authentication/Logout", {
                    method: "POST",
                    credentials: "same-origin"
                });
                if (!response.ok) {
                    throw new Error("Status " + response.status);
                }

                window.location.href = "/Account/Logout";
            } catch (e) {
                $error.text("Could not reach the server to log out. Check the connection and try again.").show();
                $logoutButton.data("kendoButton").enable(true);
            }
        }
    });
});
