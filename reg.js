document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("signup-form");
    const roleSelect = document.getElementById("user-role");
    const eventManagerFields = document.getElementById("event-manager-fields");

    // Hide/show Event Manager fields
    toggleFields();
    roleSelect.addEventListener("change", toggleFields);

    function toggleFields() {
        eventManagerFields.style.display = roleSelect.value === "event_manager" ? "block" : "none";
    }

    form.addEventListener("submit", async function (e) {
        e.preventDefault(); // Prevent form submission

        // Get form values
        const fullName = document.getElementById("fullname").value.trim();
        const email = document.getElementById("email").value.trim();
        const phone = document.getElementById("phone").value.trim();
        const password = document.getElementById("password").value.trim();
        const confirmPassword = document.getElementById("confirm-password").value.trim();

        const organization = document.getElementById("organization")?.value.trim() || "";
        const experience = document.getElementById("experience-years")?.value.trim() || "";
        const certifications = document.getElementById("certifications")?.value.trim() || "";

        // Clear error messages
        document.querySelectorAll(".error-message").forEach(msg => msg.style.display = "none");

        let isValid = true;

        // Basic Validations
        if (!fullName) { showError("fullname-error", "Full Name is required"); isValid = false; }
        if (!validateEmail(email)) { showError("email-error", "Invalid email format"); isValid = false; }
        if (!/^\d{10}$/.test(phone)) { showError("phone-error", "Enter a valid 10-digit phone number"); isValid = false; }
        if (password.length < 6) { showError("password-error", "Password must be at least 6 characters"); isValid = false; }
        if (password !== confirmPassword) { showError("confirm-password-error", "Passwords do not match"); isValid = false; }

        if (roleSelect.value === "event_manager") {
            if (!organization) { showError("organization-error", "Organization is required"); isValid = false; }
            if (!experience || isNaN(experience) || experience < 0) { showError("experience-error", "Enter valid years of experience"); isValid = false; }
        }

        if (!isValid) return;

        const userData = {
            fullName,
            email,
            phoneNumber: phone,
            password,
            role: roleSelect.value
        };

        if (roleSelect.value === "event_manager") {
            userData.eventManager = {
                organization,
                experienceYears: parseInt(experience) || 0,
                certifications
            };
        }

        console.log("📤 Sending Data to Backend:", JSON.stringify(userData, null, 2));

        try {
            const response = await fetch("http://localhost:5215/api/Registration/register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(userData)
            });

            const data = await response.json();

            if (!response.ok) {
                console.error("❌ Registration Failed:", data);
                throw new Error(data.message || "Registration failed. Please try again.");
            }

            // 🎉 Animated success popup
            Swal.fire({
                title: "🎉 Success!",
                text: "Your registration is complete.",
                icon: "success",
                confirmButtonText: "OK",
                timer: 3000,
                showClass: {
                    popup: "animate__animated animate__zoomIn"
                },
                hideClass: {
                    popup: "animate__animated animate__zoomOut"
                }
            }).then(() => {
                window.location.href = "login.html"; // Redirect to login page
            });

            form.reset();
            eventManagerFields.style.display = "none";

        } catch (error) {
            console.error("❌ Registration Error:", error.message);
            Swal.fire({
                title: "⚠️ Error!",
                text: error.message || "Something went wrong.",
                icon: "error",
                confirmButtonText: "Try Again"
            });
        }
    });

    function showError(id, message) {
        const errorElement = document.getElementById(id);
        errorElement.textContent = message;
        errorElement.style.display = "block";
    }

    function validateEmail(email) {
        return /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test(email);
    }
});
