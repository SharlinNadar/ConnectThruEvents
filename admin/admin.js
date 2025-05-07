// Function to toggle the sidebar
function toggleSidebar() {
    document.querySelector('.sidebar').classList.toggle('show');
}

document.addEventListener("DOMContentLoaded", async function () {
    const token = localStorage.getItem("token");

    if (!token) {
        console.warn("No token found. Redirecting to login.");
        window.location.href = "../login.html";
        return;
    }

    console.log("Token Found:", token); // Debug: Check if token exists

    try {
        const response = await fetch("http://localhost:5215/api/admin/profile", {
            method: "GET",
            headers: { 
                "Authorization": `Bearer ${token}`,
                "Content-Type": "application/json"
            }
        });

        console.log("API Response Status:", response.status);

        if (response.status === 401) {
            throw new Error("Unauthorized access. Token might be invalid.");
        }

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`API Error: ${errorText}`);
        }

        const adminData = await response.json();
        console.log("Admin Data:", adminData);

        // Ensure elements exist before updating them
        const adminNameElement = document.getElementById("admin-name");
        const adminEmailElement = document.getElementById("admin-email");

        if (adminNameElement && adminData.fullName) {
            adminNameElement.innerText = adminData.fullName;
        }
        if (adminEmailElement && adminData.email) {
            adminEmailElement.innerText = adminData.email;
        }

    } catch (error) {
        console.error("Error fetching admin profile:", error.message);
        alert("Session expired. Please log in again."); // Show a user-friendly message
        localStorage.removeItem("token"); // Clear invalid token
        window.location.href = "../login.html";
    }
});

// Logout Function
document.getElementById("logout-btn").addEventListener("click", function () {
    console.log("Logging out...");
    localStorage.removeItem("token");
    window.location.href = "../login.html";
});
