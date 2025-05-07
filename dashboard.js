const API_URL = "http://localhost:5215/api";

// ✅ Check if token is expired
function isTokenExpired(token) {
    const payload = parseJwt(token);
    if (!payload || !payload.exp) {
        console.error("⚠️ Token does not have an expiration field!");
        return true;
    }

    const expiryTime = payload.exp * 1000;
    console.log("⏳ Token Expiry Time:", new Date(expiryTime));
    console.log("🕒 Current Time:", new Date());

    return expiryTime < Date.now();
}

// ✅ Parse JWT Token
function parseJwt(token) {
    try {
        const base64Url = token.split(".")[1];
        const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
        return JSON.parse(atob(base64));
    } catch (error) {
        console.error("❌ Error decoding JWT:", error);
        return null;
    }
}

document.addEventListener("DOMContentLoaded", async function () {
    console.log("🔍 Checking for Token at Page Load...");

    let token = getToken();

    if (!token) {
        console.warn("🚨 No valid token found. Redirecting to login.");
        window.location.href = "login.html";
        return;
    }

    console.log("✅ Token Found:", token);

    if (isTokenExpired(token)) {
        console.warn("🚨 Token has expired. Redirecting to login.");
        clearToken();
        window.location.href = "login.html";
        return;
    } 

    console.log("✅ User role is valid. Proceeding with Dashboard Load...");

    try {
        const data = await fetchDashboardData(token);
        if (!data) {
            console.error("❌ Failed to fetch dashboard data.");
            Swal.fire({ icon: "error", title: "Error", text: "Could not load dashboard. Please try again." });
            return;
        }

        console.log("🎉 Dashboard Data Loaded:", data);

        // Update text content for Event Manager dashboard sections
        safeUpdateTextContent("manager-name", data.managerName);
        safeUpdateTextContent("event-rating", `${data.averageRating}%`);

        // ✅ Load Upcoming Events Count
        await loadUpcomingEventsCount(token, data.eventManagerId);

        // ✅ Load Pending Tasks Count
        await loadPendingTasksCount(token, data.eventManagerId);

        // Conditionally load notifications if any exist
        if (Array.isArray(data.notifications) && data.notifications.length > 0) {
            loadNotifications(data.notifications);
        } else {
            console.warn("⚠️ No notifications found.");
        }

    } catch (error) {
        console.error("⚠️ Error loading dashboard:", error.message);
        Swal.fire({ icon: "error", title: "Error", text: "Failed to load dashboard data. Please try again." });
        clearToken();
        window.location.href = "login.html";
    }
});

/* ✅ Fetch Dashboard Data */
async function fetchDashboardData(token) {
    console.log("🔍 Fetching dashboard data with token:", token);

    try {
        const response = await fetch(`${API_URL}/event-manager/dashboard`, {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Content-Type": "application/json"
            }
        });

        console.log("📡 API Response Status:", response.status);

        if (!response.ok) {
            console.error("❌ API Error Response:", await response.json());
            return null;
        }

        return await response.json();
    } catch (error) {
        console.error("⚠️ Error fetching dashboard:", error.message);
        return null;
    }
}

/* ✅ Load Upcoming Events Count */
async function loadUpcomingEventsCount(token, eventManagerId) {
    try {
        const response = await fetch(`http://localhost:5215/api/eventdetail/event-manager/upcoming-events-count`, {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${token}`,
            },
        });

        if (!response.ok) {
            throw new Error("Failed to load upcoming events count");
        }

        const data = await response.json();
        const upcomingCount = data.count;

        // Display the upcoming events count on the dashboard
        safeUpdateTextContent("upcoming-events-count", upcomingCount);
        console.log("✅ Upcoming events count loaded successfully:", upcomingCount);
    } catch (error) {
        console.error("❌ Error loading upcoming events count:", error);
    }
}

/* ✅ Load Pending Tasks Count */
async function loadPendingTasksCount(token, eventManagerId) {
    try {
        const response = await fetch(`${API_URL}/task/event-manager/tasks/pending-count`, {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Content-Type": "application/json"
            }
        });

        if (!response.ok) throw new Error("Failed to load pending tasks count");

        const responseData = await response.json();
        const pendingTasksCount = responseData.count;

        // Get the count element from the dashboard
        const countElement = document.getElementById("pending-tasks-count");

        if (countElement) {
            countElement.textContent = pendingTasksCount > 0 ? pendingTasksCount : "No pending tasks";
            console.log(`✅ Updated pending tasks count to ${pendingTasksCount}`);
        }
    } catch (error) {
        console.error("Error loading pending tasks count:", error);
    }
}

/* ✅ Get Token from Storage */
function getToken() {
    return localStorage.getItem("token") || sessionStorage.getItem("token");
}

/* ✅ Safe Function to Update Text */
function safeUpdateTextContent(elementId, value) {
    const element = document.getElementById(elementId);
    if (element) {
        element.textContent = value;
        console.log(`✅ Updated ${elementId} with value:`, value);
    } else {
        console.warn(`⚠️ Element '${elementId}' not found in the DOM.`);
    }
}

/* ✅ Dynamically Fetch and Load Notifications */
async function loadNotifications() {
    const token = getToken();
    if (!token) return;

    try {
        const response = await fetch(`http://localhost:5215/api/created-events/dashboard`, {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Content-Type": "application/json"
            }
        });

        if (!response.ok) {
            throw new Error("Failed to fetch notifications");
        }

        const data = await response.json();
        const notifications = data.notifications;

        const notificationsList = document.getElementById("notifications-list");
        if (!notificationsList) return;
        notificationsList.innerHTML = "";

        if (!notifications || notifications.length === 0) {
            notificationsList.innerHTML = "<li>No new event requests</li>";
            return;
        }

        notifications.forEach(event => {
            const li = document.createElement("li");
            li.classList.add("notification-item");

            const eventInfo = `
                <strong>${event.eventTitle}</strong><br>
                📅 ${new Date(event.eventDate).toLocaleDateString()}<br>
                💰 Budget: $${event.budget} | Total: $${event.totalPrice}<br>
                📌 Status: ${event.status}<br>
                🕒 Created At: ${new Date(event.createdAt).toLocaleString()}
            `;
            li.innerHTML = eventInfo;

            // Accept Button
            const acceptBtn = document.createElement("button");
            acceptBtn.textContent = "✅ Accept";
            acceptBtn.classList.add("btn", "btn-success", "btn-sm", "me-2");
            acceptBtn.onclick = () => handleEventAction(event.id, "accept");

            // Deny Button
            const denyBtn = document.createElement("button");
            denyBtn.textContent = "❌ Deny";
            denyBtn.classList.add("btn", "btn-danger", "btn-sm");
            denyBtn.onclick = () => handleEventAction(event.id, "deny");

            li.appendChild(document.createElement("br"));
            li.appendChild(acceptBtn);
            li.appendChild(denyBtn);

            notificationsList.appendChild(li);
        });

    } catch (error) {
        console.error("❌ Error loading notifications:", error.message);
    }
}
async function handleEventAction(eventId, action) {
    const token = getToken();
    if (!token) return;

    const confirm = await Swal.fire({
        title: `${action === "accept" ? "Accept" : "Deny"} Event`,
        text: `Are you sure you want to ${action} this event?`,
        icon: "question",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: `Yes, ${action}`,
    });

    if (!confirm.isConfirmed) return;

    const endpoint = `http://localhost:5215/api/created-events/${action}/${eventId}`;

    try {
        const response = await fetch(endpoint, {
            method: "PUT",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Content-Type": "application/json"
            }
        });

        const result = await response.json();
        if (!response.ok) throw new Error(result.message);

        Swal.fire("Success", result.message, "success");
        setTimeout(() => window.location.reload(), 1000);
    } catch (error) {
        console.error(`❌ Failed to ${action} event:`, error);
        Swal.fire("Error", `Could not ${action} the event.`, "error");
    }
}

/* ✅ Clear Token from Storage */
function clearToken() {
    localStorage.removeItem("token");
    sessionStorage.removeItem("token");
    console.log("🗑️ Token cleared from storage.");
}

//calendar
// Function to decode JWT
function decodeJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        return JSON.parse(window.atob(base64));
    } catch (error) {
        console.error('Error decoding JWT:', error);
        return null;
    }
}

// Function to get the Event Manager ID from JWT
async function getEventManagerIdFromJwt() {
    const token = localStorage.getItem('token');
    if (!token) {
        console.error('Token not found');
        return null;
    }

    const decodedJwt = decodeJwt(token);
    const eventManagerId = decodedJwt?.EventManagerId;

    if (!eventManagerId) {
        console.error('Event Manager ID not found in JWT');
        return null;
    }

    console.log('Event Manager ID from JWT:', eventManagerId);
    return eventManagerId;
}

document.addEventListener('DOMContentLoaded', async function () {
    var calendarEl = document.getElementById('calendar');

    try {
        const managerId = await getEventManagerIdFromJwt();
        if (!managerId) {
            throw new Error('Unable to retrieve Event Manager ID');
        }

        var calendar = new FullCalendar.Calendar(calendarEl, {
            initialView: 'dayGridMonth',
            headerToolbar: {
                left: 'prev,next today',
                center: 'title',
                right: 'dayGridMonth,timeGridWeek,timeGridDay'
            },
            events: async function (fetchInfo, successCallback, failureCallback) {
                try {
                    const token = localStorage.getItem('token');

                    const response = await fetch(`http://localhost:5215/api/eventstatus/by-status/Upcoming/manager/${managerId}`, {
                        method: 'GET',
                        headers: {
                            'Content-Type': 'application/json',
                            'Authorization': `Bearer ${token}` // Include token for authentication
                        }
                    });
                    

                    if (!response.ok) {
                        throw new Error('Failed to fetch upcoming events for the manager');
                    }

                    const events = await response.json();
                    const eventList = events.map(event => ({
                        title: event.title,
                        start: event.date,
                        description: event.description,
                        status: event.status,
                        location: event.location,
                        backgroundColor: '#f39c12', // Orange highlight for upcoming events
                        borderColor: '#e67e22',    // Darker border color
                        textColor: '#fff',         // White text color for contrast
                    }));
                    successCallback(eventList);
                } catch (error) {
                    console.error(error);
                    failureCallback(error);
                }
            },
            eventClick: function (info) {
                Swal.fire({
                    title: info.event.title,
                    html: `
                        <strong>Status:</strong> ${info.event.extendedProps.status}<br>
                        <strong>Location:</strong> ${info.event.extendedProps.location}<br>
                        <strong>Description:</strong> ${info.event.extendedProps.description}
                    `,
                    icon: 'info',
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#3085d6',
                    background: '#f4f6f9',
                    color: '#333'
                });
            },
            eventDidMount: function (info) {
                // Add a glow effect to highlight upcoming events
                info.el.style.boxShadow = '0 4px 8px rgba(243, 156, 18, 0.6)';
                info.el.style.borderRadius = '8px';
            },
            height: 'auto',
            editable: false,
            selectable: true,
            dayMaxEventRows: true
        });

        calendar.render();
    } catch (error) {
        console.error(error);
        Swal.fire({
            title: 'Error',
            text: error.message,
            icon: 'error',
            confirmButtonText: 'OK',
            confirmButtonColor: '#d33'
        });
    }
});


