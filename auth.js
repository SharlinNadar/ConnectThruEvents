document.addEventListener("DOMContentLoaded", function () {
    const logoutButton = document.getElementById("logout-btn");

    console.log("🔍 Checking Token in LocalStorage:", localStorage.getItem("token"));

    // ✅ Load User Data from Token
    function loadUserData() {
        const token = getToken();

        if (!token) {
            console.warn("❌ No token found. Redirecting to login...");
            clearTokenAndRedirect();
            return;
        }

        try {
            const user = parseJwt(token);
            if (!user) throw new Error("Invalid token");

            console.log("✅ Parsed User Data:", user);

            // ✅ Extract Role Properly
            const extractedRole = getUserRole(user);

            // ✅ Populate Profile Fields
            updateElementText("fullNameInput", user.name || "Unknown User");
            updateElementText("contactInput", user.phone || "No Contact");
            updateElementText("email", user.email || "No Email");
            updateElementText("bio", user.bio || "No Bio");
            updateElementText("linkedin", user.linkedin || "No LinkedIn");
            updateElementText("twitter", user.twitter || "No Twitter");
            updateElementText("location", user.location || "No Location");

        } catch (error) {
            console.error("❌ Error decoding token:", error);
            clearTokenAndRedirect();
        }
    }

    // ✅ Get Token from Storage
    function getToken() {
        const token = localStorage.getItem("token") || sessionStorage.getItem("token") || getCookie("token");

        console.log("🔍 Checking Token in Storage:");
        console.log("➡️ LocalStorage:", localStorage.getItem("token"));
        console.log("➡️ SessionStorage:", sessionStorage.getItem("token"));
        console.log("➡️ Cookies:", document.cookie);

        if (!token || token === "undefined" || token === "null") {
            console.warn("🚨 No valid token found in storage.");
            return null;
        }
        return token;
    }

    // ✅ Get Token from Cookies
    function getCookie(name) {
        const cookies = document.cookie.split("; ");
        for (let cookie of cookies) {
            const [key, value] = cookie.split("=");
            if (key === name) return value;
        }
        return null;
    }

    // ✅ Parse JWT Token
    function parseJwt(token) {
        try {
            console.log("🔍 Raw JWT Token:", token);

            const base64Url = token.split(".")[1];
            const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
            const decoded = JSON.parse(atob(base64));

            console.log("✅ Decoded Token:", decoded);

            // 🚨 Expiration Check
            if (decoded.exp * 1000 < Date.now()) {
                console.warn("🚨 Token expired! Redirecting...");
                clearTokenAndRedirect();
                return null;
            }

            return decoded;
        } catch (e) {
            console.error("❌ Invalid JWT:", e);
            return null;
        }
    }

    // ✅ Get User Role
    function getUserRole(user) {
        return user["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || user.role || "Unknown Role";
    }

    // ✅ Logout Function
    function logout() {
        console.log("🔴 Logging out...");
        clearTokenAndRedirect();
    }

    // ✅ Clear Token & Redirect
    function clearTokenAndRedirect() {
        console.warn("🚨 Clearing token and redirecting to login...");
        localStorage.removeItem("token");
        sessionStorage.removeItem("token");
        document.cookie = "token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
        window.location.href = "login.html";
    }

    // ✅ Update Text Content or Input Value of Elements Safely
    function updateElementText(elementId, value) {
        const element = document.getElementById(elementId);
        if (element) {
            if (element.tagName === "INPUT" || element.tagName === "TEXTAREA") {
                element.value = value;
            } else {
                element.textContent = value;
            }
            console.log(`✅ Updated ${elementId} with value:`, value);
        } else {
            console.warn(`⚠️ Element '${elementId}' not found in the DOM.`);
        }
    }

    // ✅ Event Listener for Logout Button
    if (logoutButton) {
        logoutButton.addEventListener("click", logout);
    } else {
        console.error("❌ Logout button not found!");
    }

    // ✅ Load User Data When Page Loads
    loadUserData();
});
