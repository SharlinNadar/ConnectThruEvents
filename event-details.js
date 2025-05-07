// Event Details JS

const apiUrl = 'http://localhost:5215/api/eventdetail';

// Get JWT token from local storage
function getToken() {
    return localStorage.getItem('token');
}

// Show success or error messages
function showMessage(type, message) {
    Swal.fire({
        icon: type,
        title: message,
        timer: 1500,
        showConfirmButton: false
    });
}

// Decode JWT to extract payload and EventManagerId
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

// Get EventManagerId from JWT
function getEventManagerIdFromJwt() {
    const token = getToken();
    if (!token) {
        console.error('Token not found');
        return null;
    }

    const decodedJwt = decodeJwt(token);
    return decodedJwt?.EventManagerId;
}

// Fetch event details
async function fetchEvents() {
    try {
        const response = await fetch(`${apiUrl}/event-manager/event-details`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${getToken()}`
            }
        });
        const result = await response.json();

        if (result.status === 'success') {
            displayEvents(result.eventDetails);
        } else {
            showMessage('error', result.message);
        }
    } catch (error) {
        console.error('Error fetching events:', error);
        showMessage('error', 'Failed to fetch events');
    }
}

// Display events on the page
function displayEvents(events) {
    const eventList = document.getElementById('event-list');
    eventList.innerHTML = '';

    events.forEach(event => {
        const listItem = document.createElement('li');
        listItem.classList.add('event-item');
        listItem.innerHTML = `
            <h4>${event.title}</h4>
            <p>${event.description}</p>
            <p><strong>Location:</strong> ${event.location}</p>
            <p><strong>Date:</strong> ${event.date}</p>
            <p><strong>Status:</strong> ${event.status}</p>
            <button onclick="markEventCompleted(${event.eventDetailId})">Complete</button>
            <button onclick="deleteEvent(${event.eventDetailId})">Delete</button>
            <button onclick="viewEventDetails(${event.eventDetailId})">View</button>
        `;
        eventList.appendChild(listItem);
    });
}

// View event details
function viewEventDetails(eventId) {
    // Redirect to view-event.html and pass the event ID in the URL
    window.location.href = `view-event.html?eventId=${eventId}`;
}

// Add a new event
document.getElementById('event-form').addEventListener('submit', async function (e) {
    e.preventDefault();
    const title = document.getElementById('title').value;
    const location = document.getElementById('location').value;
    const date = document.getElementById('date').value;
    const description = document.getElementById('description').value;

    try {
        const response = await fetch(`${apiUrl}/event-manager/event-details`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getToken()}`
            },
            body: JSON.stringify({ title, location, date, description })
        });
        const result = await response.json();

        if (result.status === 'success') {
            showMessage('success', result.message);
            fetchEvents();
            document.getElementById('event-form').reset();
        } else {
            showMessage('error', result.message);
        }
    } catch (error) {
        console.error('Error adding event:', error);
        showMessage('error', 'Failed to add event');
    }
});

// Mark event as completed
async function markEventCompleted(id) {
    try {
        const response = await fetch(`${apiUrl}/event-manager/event-details/${id}/complete`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${getToken()}`
            }
        });
        const result = await response.json();

        if (result.status === 'success') {
            showMessage('success', result.message);
            fetchEvents();
        } else {
            showMessage('error', result.message);
        }
    } catch (error) {
        console.error('Error marking event as completed:', error);
        showMessage('error', 'Failed to mark event as completed');
    }
}

// Delete an event
async function deleteEvent(id) {
    try {
        const response = await fetch(`${apiUrl}/event-manager/event-details/${id}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${getToken()}`
            }
        });
        const result = await response.json();

        if (result.status === 'success') {
            showMessage('success', result.message);
            fetchEvents();
        } else {
            showMessage('error', result.message);
        }
    } catch (error) {
        console.error('Error deleting event:', error);
        showMessage('error', 'Failed to delete event');
    }
}

// Fetch accepted events using EventManagerId from JWT
async function getAcceptedEvents() {
    try {
        // Get EventManagerId from JWT
        const eventManagerId = getEventManagerIdFromJwt();
        if (!eventManagerId) {
            console.error('EventManagerId not found');
            showMessage('error', 'Unable to fetch accepted events');
            return;
        }

        // Fetch accepted events from API
        const token = getToken();
        const response = await fetch(`http://localhost:5215/api/created-events/accepted/${eventManagerId}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }

        const events = await response.json();

        // Display accepted events
        const eventsList = document.getElementById('accepted-events');
        eventsList.innerHTML = '';

        events.forEach(event => {
            const listItem = document.createElement('li');
            listItem.innerHTML = `
                <h3>${event.eventTitle}</h3>
                <p><strong>Date:</strong> ${new Date(event.eventDate).toLocaleString()}</p>
                <p><strong>Budget:</strong> $${event.budget.toFixed(2)}</p>
                <p><strong>Status:</strong> ${event.status}</p>
                <p><strong>Created At:</strong> ${new Date(event.createdAt).toLocaleString()}</p>
                
            `;
            eventsList.appendChild(listItem);
        });

    } catch (error) {
        console.error('Error fetching accepted events:', error);
    }
}

// Call the function when the page loads
window.onload = getAcceptedEvents;

// Initial load of events
fetchEvents();
