document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("login-form");

    if (!form) {
        console.error("❌ Login form not found!");
        return;
    }

    form.addEventListener("submit", async function (e) {
        e.preventDefault(); // Prevent default form submission

        const email = document.getElementById("username").value.trim();
        const password = document.getElementById("password").value.trim();

        if (!email || !password) {
            showError("Please enter both email and password.");
            return;
        }

        const loginData = { email, password };

        // 🔹 Determine API Endpoint Based on Email Type (Admin vs Regular User)
        const loginEndpoint = email.includes("admin")
            ? "http://localhost:5215/api/Admin/login"
            : "http://localhost:5215/api/Auth/login";

        try {
            const response = await fetch(loginEndpoint, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(loginData),
            });

            console.log("Response Status:", response.status);

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`Login failed: ${errorText || "Invalid credentials"}`);
            }

            const data = await response.json();

            // ✅ Ensure Token is Valid Before Proceeding
            if (!data.token || !isValidJwt(data.token)) {
                throw new Error("Invalid token format received.");
            }

            console.log("✅ Received Token:", data.token);
            localStorage.setItem("token", data.token);
            sessionStorage.setItem("token", data.token);
            document.cookie = `token=${data.token}; path=/; Secure`; // Removed HttpOnly

            // ✅ Decode JWT Token
            const userPayload = parseJwt(data.token);
            if (!userPayload || !userPayload.role) {
                throw new Error("Invalid token format. Missing role data.");
            }

            console.log("✅ Extracted Role:", userPayload.role);

            Swal.fire({
                icon: "success",
                title: "Login Successful!",
                text: "Redirecting to dashboard...",
                timer: 2000,
                showConfirmButton: false
            }).then(() => {
                redirectToDashboard(userPayload.role);
            });

        } catch (error) {
            console.error("Login Error:", error.message);
            showError(error.message);
        }
    });

    // 🔹 Function to Show Error Messages
    function showError(message) {
        Swal.fire({ icon: "error", title: "Login Failed", text: message });
    }

    // 🔹 Function to Validate JWT Token Format
    function isValidJwt(token) {
        const parts = token.split(".");
        return parts.length === 3 && parts.every(part => /^[A-Za-z0-9-_]+$/.test(part));
    }

    // 🔹 Function to Parse JWT Token Safely
    function parseJwt(token) {
        try {
            const base64Url = token.split(".")[1];
            if (!base64Url) throw new Error("Invalid token format");
    
            const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
            const payload = JSON.parse(atob(base64));
    
            console.log("🔍 Full Decoded Payload:", payload); // Debugging line
    
            // ✅ Find the correct role claim dynamically
            const roleClaimKeys = Object.keys(payload).find(key => key.toLowerCase().includes("role"));
            const extractedRole = roleClaimKeys ? payload[roleClaimKeys] : null;
    
            console.log("✅ Extracted Role:", extractedRole); // Debugging line
    
            payload.role = extractedRole; // Assign extracted role
    
            return payload;
        } catch (e) {
            console.error("❌ Error decoding JWT:", e);
            return null;
        }
    }

    // 🔹 Function to Redirect User Based on Role
    function redirectToDashboard(role) {
        if (!role) {
            console.error("❌ No role found, redirecting to login.");
            Swal.fire({ icon: "error", title: "Login Failed", text: "User role is missing. Please log in again." });
            localStorage.removeItem("token");
            window.location.href = "login.html";
            return;
        }
    
        role = role.toLowerCase().trim();
        console.log("🔄 Redirecting Based on Role:", role);
    
        if (role === "admin") {
            console.log("🚀 Redirecting to Admin Dashboard");
            window.location.replace("admin/admin.html");
        } else if (role === "eventmanager" || role === "event_manager") {
            console.log("🚀 Redirecting to Event Manager Dashboard");
            window.location.replace("dashboard.html");
        } else {
            console.log("🚀 Redirecting to User Profile");
            console.log("✅ Calling window.location.href now...");
            setTimeout(() => {
                window.location.href = "userProfile.html";
            }, 1000);
        }
    }
    
    
});
