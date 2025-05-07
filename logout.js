document.getElementById("logout-button").addEventListener("click", function () {
    localStorage.removeItem("token");
    window.location.href = "login.html";
});
