// Event Manager Profile Script
const apiUrl = 'http://localhost:5215/api/event-manager';
const profileForm = document.getElementById("profile-form");
const statusMessage = document.getElementById("status-message");
const recentEventsList = document.getElementById("recent-events-list");
const clearEventsBtn = document.getElementById("clear-events-btn");
const addEventBtn = document.getElementById("add-event-btn");

// Display SweetAlert message
function showAlert(title, text, icon) {
    Swal.fire({
        title: title,
        text: text,
        icon: icon,
        confirmButtonText: 'OK'
    });
}

// Fetch profile details from the backend
async function fetchProfile() {
    try {
        const response = await fetch(`${apiUrl}/dashboard`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (!response.ok) throw new Error('Failed to fetch profile data');

        const data = await response.json();
        console.log('Fetched Profile Data:', data);

        // ✅ Save event manager ID for use in update
        localStorage.setItem('eventManagerId', data.id);

        document.getElementById("manager-name").value = data.managerName || "Not Available";
        document.getElementById("email").value = data.email || "Not Available";
        document.getElementById("organization").value = data.organization || "N/A";
        document.getElementById("experience").value = data.experienceYears ?? 0;
        document.getElementById("certifications").value = data.certifications || "N/A";
        document.getElementById("events-completed").value = data.eventsCompleted ?? 0;
        document.getElementById("price").value = data.pricePerEvent ?? 0;

        showAlert("Success", "Profile loaded successfully!", "success");
    } catch (error) {
        showAlert("Error", `Failed to load profile: ${error.message}`, "error");
        console.error("Error fetching profile data:", error);
    }
}

// Update profile details
profileForm.addEventListener("submit", async function (event) {
    event.preventDefault();

    const eventManagerId = localStorage.getItem('eventManagerId');
    if (!eventManagerId) {
        showAlert("Error", "Event Manager ID not found. Please reload the page.", "warning");
        return;
    }

    const formData = new FormData();
    formData.append("organization", document.getElementById("organization").value);
    formData.append("experienceYears", parseInt(document.getElementById("experience").value) || 0);
    formData.append("certifications", document.getElementById("certifications").value);
    formData.append("pricePerEvent", parseFloat(document.getElementById("price").value) || 0);

    try {
        const response = await fetch(`${apiUrl}/update/${eventManagerId}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: formData
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Failed to update profile');
        }

        const data = await response.json();
        showAlert("Success", data.message || "Profile updated successfully!", "success");
    } catch (error) {
        showAlert("Error", `Failed to update profile: ${error.message}`, "error");
        console.error("Error updating profile:", error);
    }
});

// Render recent events from localStorage
function renderRecentEvents() {
    const recentEvents = JSON.parse(localStorage.getItem('recentEvents')) || [];
    recentEventsList.innerHTML = '';
    recentEvents.forEach(event => {
        const li = document.createElement("li");
        li.innerHTML = `<strong>${event.title}</strong> <br> <small>Date: ${event.date}, Location: ${event.location}</small>`;
        recentEventsList.appendChild(li);
    });
}

// Clear recent events
clearEventsBtn.addEventListener("click", () => {
    localStorage.removeItem('recentEvents');
    renderRecentEvents();
    showAlert("Success", "All recent events have been cleared!", "success");
});

// Redirect to add new event page
addEventBtn.addEventListener("click", () => {
    window.location.href = "event-history.html";
});

// Initialize
fetchProfile();
renderRecentEvents();
