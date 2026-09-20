(function () {
    const button = document.getElementById("LogoutButton");
    if (!button) {
        return;
    }

    const error = document.getElementById("LogoutError");

    button.addEventListener("click", async function () {
        button.disabled = true;
        error.style.display = "none";

        try {
            const response = await fetch("/api/Authentication/Logout", {method: "POST", credentials: "same-origin"});
            if (!response.ok) {
                throw new Error("Status " + response.status);
            }

            window.location.href = "/Account/Logout";
        } catch (e) {
            error.textContent = "Could not reach the server to log out. Check the connection and try again.";
            error.style.display = "block";
            button.disabled = false;
        }
    });
})();
