const API_URL = "http://localhost:5215/api/user";
const EVENTS_API_URL = "http://localhost:5215/api/publishevents";

// SweetAlert helpers
function showDialog(message, isSuccess = false) {
    Swal.fire({
        title: isSuccess ? "Success!" : "Error!",
        text: message,
        icon: isSuccess ? "success" : "error",
        confirmButtonText: "OK",
        confirmButtonColor: isSuccess ? "#28a745" : "#dc3545",
    });
}

function showLoading(message) {
    Swal.fire({
        title: message,
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
}

function closeLoading() {
    Swal.close();
}

// Auth token utils
function getToken() {
    return localStorage.getItem("token");
}

function isTokenValid() {
    const token = getToken();
    return token !== null && token !== "";
}

// Update social link elements
function updateSocialLink(type, url) {
    const linkElement = document.getElementById(`${type}-link`);
    if (!linkElement) return;

    if (url && url !== "N/A") {
        if (!url.startsWith("http://") && !url.startsWith("https://")) {
            url = "https://" + url;
        }
        linkElement.textContent = url;
        linkElement.href = url;
        linkElement.style.display = "inline";
    } else {
        linkElement.textContent = "N/A";
        linkElement.href = "#";
        linkElement.style.display = "inline";
    }
}

// Render list of events
function loadEvents(events, sectionId) {
    const section = document.getElementById(sectionId);
    if (!section) return;

    section.innerHTML = "";

    if (Array.isArray(events) && events.length > 0) {
        events.forEach(event => {
            const li = document.createElement("li");
            li.classList.add("event-item");

            const formattedDate = event.eventDate
                ? new Date(event.eventDate).toLocaleString()
                : "N/A";

            let eventStatusMessage = "";

            if (event.status === "Pending") {
                eventStatusMessage = `<p>Event is pending. Please wait for approval.</p>`;
            } else if (event.status === "Accepted") {
                eventStatusMessage = `<button class="btn btn-primary" onclick="proceedToPayment('${event.id}')">Proceed to Pay</button>`;
            } else if (event.status === "Denied") {
                eventStatusMessage = `<p class="text-danger"><strong>This event was denied by the Event Manager.</strong></p>`;
            }

            const eventInfo = `
                <div>
                    <h5>${event.eventTitle || "Unnamed Event"}</h5>
                    <p><strong>Date:</strong> ${formattedDate}</p>
                    <p><strong>Location:</strong> ${event.location || "N/A"}</p>
                    <p><strong>Status:</strong> ${event.status}</p>
                    ${eventStatusMessage}
                </div>
            `;

            li.innerHTML = eventInfo;
            section.appendChild(li);
        });
    } else {
        const emptyMsg = document.createElement("li");
        emptyMsg.textContent = "No events available";
        section.appendChild(emptyMsg);
    }
}

// Fetch user profile
async function fetchUserProfile() {
    try {
        const token = getToken();
        if (!isTokenValid()) {
            showDialog("Session expired. Please log in again.");
            return setTimeout(() => window.location.href = "/login.html", 2000);
        }

        const response = await fetch(`${API_URL}/profile`, {
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        if (response.status === 401) {
            showDialog("Session expired. Please log in again.");
            return setTimeout(() => window.location.href = "/login.html", 2000);
        }

        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.message || "Failed to fetch profile");
        }

        const data = await response.json();

        // Profile UI
        document.getElementById("fullName").textContent = data.fullName || "N/A";
        document.getElementById("role").textContent = data.role || "N/A";
        document.getElementById("contact").textContent = data.phoneNumber || "N/A";
        document.getElementById("email").textContent = data.email || "N/A";
        document.getElementById("location").textContent = data.location || "N/A";
        document.getElementById("bio").textContent = data.bio || "N/A";

        // Pre-fill form
        document.getElementById("fullNameInput").value = data.fullName || "";
        document.getElementById("contactInput").value = data.phoneNumber || "";
        document.getElementById("locationInput").value = data.location || "";
        document.getElementById("bioInput").value = data.bio || "";
        document.getElementById("linkedinInput").value = data.socialLinks?.LinkedIn || "";
        document.getElementById("twitterInput").value = data.socialLinks?.Twitter || "";

        updateSocialLink("linkedin", data.socialLinks?.LinkedIn || "N/A");
        updateSocialLink("twitter", data.socialLinks?.Twitter || "N/A");

        const profilePic = document.getElementById("profile-pic");
        if (profilePic) {
            profilePic.src = data.profilePictureUrl || "https://picsum.photos/150";
            profilePic.onerror = () => {
                profilePic.src = "/images/default-profile.jpg";
            };
        }

        loadEvents(data.enrolledEvents || [], "enrolled-events");
        loadEvents(data.completedEvents || [], "favorite-events");
        loadEvents(data.createdEvents || [], "events-created");

    } catch (error) {
        console.error("[fetchUserProfile] Error:", error);
        showDialog("Unable to load profile. Please try again later.");
    }
}

// Get user ID from token
async function getUserIdFromJwt() {
    const token = getToken();
    if (!token) return null;

    try {
        const res = await fetch(`${API_URL}/get-user-id-by-email`, {
            method: "GET",
            headers: { Authorization: `Bearer ${token}` }
        });

        if (!res.ok) return null;

        const data = await res.json();
        return data.userId;
    } catch (err) {
        console.error('[getUserIdFromJwt] Error:', err);
        return null;
    }
}

// Render enrolled events
function loadEnrolledEvents(events, sectionId) {
    const section = document.getElementById(sectionId);
    if (!section) return;

    section.innerHTML = "";

    if (events.length > 0) {
        events.forEach(event => {
            const li = document.createElement("li");
            li.classList.add("event-item");

            const formattedDate = event.eventDate
                ? new Date(event.eventDate).toLocaleString()
                : "N/A";

            const eventInfo = `
                <div>
                    <h5>${event.eventTitle}</h5>
                    <p><strong>Category:</strong> ${event.eventCategory}</p>
                    <p><strong>Date:</strong> ${formattedDate}</p>
                    <p><strong>Location:</strong> ${event.eventLocation}</p>
                    <p><strong>Ticket Type:</strong> ${event.ticketType}</p>
                </div>
            `;
            li.innerHTML = eventInfo;
            section.appendChild(li);
        });
    } else {
        section.innerHTML = "<li>No enrolled events available</li>";
    }
}

// Fetch enrolled events
async function fetchUserEnrolledEvents() {
    const userId = await getUserIdFromJwt();
    if (!userId) return;

    try {
        const res = await fetch(`${EVENTS_API_URL}/user-enrollments/${userId}`, {
            headers: { Authorization: `Bearer ${getToken()}` }
        });

        const data = await res.json();

        const events = data.map(event => ({
            eventTitle: event.eventTitle,
            eventCategory: event.eventCategory,
            eventDate: event.eventDate,
            eventLocation: event.eventLocation,
            ticketType: event.ticketType,
        }));

        loadEnrolledEvents(events, "enrolled-events");
    } catch (err) {
        console.error("[fetchUserEnrolledEvents] Error:", err);
    }
}

// 🔥 Render favorite events + Enroll button
function loadFavoriteEvents(events, sectionId) {
    const section = document.getElementById(sectionId);
    if (!section) return;

    section.innerHTML = "";

    if (events.length > 0) {
        events.forEach(event => {
            const li = document.createElement("li");
            li.classList.add("event-item");

            const formattedDate = event.eventDate
                ? new Date(event.eventDate).toLocaleString()
                : "N/A";

            const eventInfo = `
                <div>
                    <h5>${event.eventTitle}</h5>
                    <p><strong>Category:</strong> ${event.eventCategory}</p>
                    <p><strong>Date:</strong> ${formattedDate}</p>
                    <p><strong>Location:</strong> ${event.eventLocation}</p>
                    <p><strong>Ticket Type:</strong> ${event.ticketType}</p>
                    <button class="btn btn-success enroll-btn" data-event-id="${event.id}">Enroll</button>
                </div>
            `;

            li.innerHTML = eventInfo;
            section.appendChild(li);
        });

        section.querySelectorAll(".enroll-btn").forEach(btn => {
            btn.addEventListener("click", async () => {
                const eventId = btn.getAttribute("data-event-id");
                await enrollUser(eventId);
            });
        });

    } else {
        section.innerHTML = "<li>No favorite events available</li>";
    }
}

// 🔄 Fetch favorite events
async function fetchUserFavoriteEvents() {
    const userId = await getUserIdFromJwt();
    if (!userId) return;

    try {
        const res = await fetch(`${EVENTS_API_URL}/favorites/${userId}`, {
            headers: { Authorization: `Bearer ${getToken()}` }
        });

        const data = await res.json();

        const events = data.map(event => ({
            id: event.publishEventId,
            eventTitle: event.eventTitle,
            eventCategory: event.eventCategory,
            eventDate: event.eventDate,
            eventLocation: event.eventLocation,
            ticketType: event.ticketType
        }));

        loadFavoriteEvents(events, "favorite-events");
    } catch (err) {
        console.error("[fetchUserFavoriteEvents] Error:", err);
    }
}

// Enroll user in an event 🔥
async function enrollUser(eventId) {
    const userId = await getUserIdFromJwt();
    if (!userId) return showDialog("Please login to enroll.");

    try {
        const res = await fetch(`${EVENTS_API_URL}/enroll`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                userId: parseInt(userId),
                publishEventId: parseInt(eventId)
            })
        });

        if (!res.ok) {
            const errorText = await res.text();
            if (errorText.includes("already enrolled")) {
                // Show a specific message if the user is already enrolled
                showDialog("You are already enrolled in this event!", false, "info");
                return;
            }
            throw new Error(errorText);
        }
        
        // Success message after enrollment
        showDialog("You have successfully enrolled in the event!", true, "success");
    } catch (err) {
        console.error("[enrollUser] Error:", err);
        showDialog("An error occurred. Please try again.", false, "warning");
    }
}

// Updated showDialog to accept icon as a parameter
function showDialog(message, isSuccess = false, iconType = "success") {
    Swal.fire({
        title: isSuccess ? "Success!" : "Error!",
        text: message,
        icon: iconType, // iconType now controls the icon
        confirmButtonText: "OK",
        confirmButtonColor: isSuccess ? "#28a745" : "#dc3545",
    });
}


// Updated showDialog to accept icon as a parameter
function showDialog(message, isSuccess = false, iconType = "success") {
    Swal.fire({
        title: isSuccess ? "Success!" : "Error!",
        text: message,
        icon: iconType, // iconType now controls the icon
        confirmButtonText: "OK",
        confirmButtonColor: isSuccess ? "#28a745" : "#dc3545",
    });
}

// Created events
async function fetchCreatedEvents() {
    const token = getToken();
    if (!isTokenValid()) {
        showDialog("Session expired. Please log in again.");
        return setTimeout(() => window.location.href = "/login.html", 2000);
    }

    try {
        const response = await fetch("http://localhost:5215/api/created-events/my", {
            method: "GET",
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || "Failed to fetch created events");
        }

        const data = await response.json();
        loadEvents(data, "events-created");
    } catch (error) {
        console.error("[fetchCreatedEvents] Error:", error);
        console.log("Unable to load created events. Please try again later.");
    }
}

function proceedToPayment(eventId) {
    showDialog("Redirecting to payment page...", true);
    setTimeout(() => {
        window.location.href = `/payment.html?eventId=${eventId}`;
    }, 2000);
}

async function updateUserProfile() {
    const token = getToken();
    if (!isTokenValid()) {
        showDialog("Session expired. Please log in again.");
        return setTimeout(() => window.location.href = "/login.html", 2000);
    }

    const formData = new FormData(document.getElementById("profileForm"));

    try {
        showLoading("Updating profile...");
        const response = await fetch(`${API_URL}/update`, {
            method: "PUT",
            headers: { Authorization: `Bearer ${token}` },
            body: formData,
        });

        closeLoading();

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || "Failed to update profile");
        }

        showDialog("Profile updated successfully!", true);
        await fetchUserProfile();
    } catch (error) {
        closeLoading();
        console.error("[updateUserProfile] Error:", error);
        showDialog("Profile update failed. Please try again later.");
    }
}

// Init
window.addEventListener("DOMContentLoaded", () => {
    fetchUserProfile();
    fetchCreatedEvents();
    fetchUserEnrolledEvents();
    fetchUserFavoriteEvents();

    const updateButton = document.getElementById("updateProfileBtn");
    if (updateButton) {
        updateButton.addEventListener("click", updateUserProfile);
    }
});
