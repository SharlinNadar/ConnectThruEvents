// Decode JWT token
function decodeJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const decoded = JSON.parse(atob(base64));
        console.log('[decodeJwt] Token payload:', decoded);
        return decoded;
    } catch (error) {
        console.error('[decodeJwt] Failed to decode token:', error);
        return null;
    }
}

// Check if the JWT token has expired
function isTokenExpired(token) {
    const decoded = decodeJwt(token);
    if (!decoded) return true;
    const current = Math.floor(Date.now() / 1000);
    const expired = decoded.exp <= current;
    console.log(`[isTokenExpired] Token exp: ${decoded.exp}, Now: ${current}, Expired: ${expired}`);
    return expired;
}

// Get user ID from JWT token via backend
async function getUserIdFromJwt() {
    const token = localStorage.getItem('token');
    if (!token || isTokenExpired(token)) {
        console.warn('[getUserIdFromJwt] Token missing or expired');
        return null;
    }

    try {
        const res = await fetch('http://localhost:5215/api/user/get-user-id-by-email', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!res.ok) {
            const msg = await res.text();
            console.warn('[getUserIdFromJwt] Backend rejected request:', msg);
            return null;
        }

        const data = await res.json();
        console.log('[getUserIdFromJwt] Got user ID:', data.userId);
        return data.userId;
    } catch (err) {
        console.error('[getUserIdFromJwt] Error:', err);
        return null;
    }
}

// Fetch events and render them into the DOM
async function fetchEvents() {
    try {
        const res = await fetch('http://localhost:5215/api/publishevents/all');
        const events = await res.json();
        renderEvents(events);
    } catch (err) {
        console.error('[fetchEvents] Failed to load events:', err);
    }
}

// Render the list of events into the DOM
function renderEvents(events) {
    const container = document.getElementById('events-container');
    container.innerHTML = ''; // Clear existing events

    events.forEach(event => {
        const card = createEventCard(event);
        container.appendChild(card);
    });
}

// Create a card for a single event
function createEventCard(event) {
    const card = document.createElement('div');
    card.className = 'event-card';

    // Add validation for image URL
    const imageUrl = 
        event.eventImageUrl && 
        event.eventImageUrl !== 'null' && 
        event.eventImageUrl.trim() !== '' && 
        event.eventImageUrl.startsWith('http')
        ? event.eventImageUrl 
        : 'img/default.jpg';  // Fallback to default if image URL is invalid

    card.innerHTML = `
        <img src="${imageUrl}" alt="${event.eventTitle}" onerror="this.onerror=null;this.src='img/default.jpg';">
        <div class="card-content">
            <h3>${event.eventTitle}</h3>
            <p><strong>Date:</strong> ${new Date(event.eventDate).toLocaleDateString()}</p>
            <p><strong>Location:</strong> ${event.eventLocation}</p>
            <p><strong>Price:</strong> ${getPriceLabel(event.ticketType)}</p>
            <p>${event.description?.slice(0, 100)}...</p>
            <div class="btn-group">
                <button class="btn enroll-btn">Enroll</button>
                <button class="btn view-btn">View Details</button>
                <button class="btn fav-btn">❤️</button>
            </div>
        </div>
    `;

    card.querySelector('.enroll-btn').addEventListener('click', () => enrollUser(event.publishEventId));
    card.querySelector('.fav-btn').addEventListener('click', () => addToFavorites(event.publishEventId));
    card.querySelector('.view-btn').addEventListener('click', () => showDetails(event));

    return card;
}


// View event details in a modal
function showDetails(event) {
    const imageUrl = event.eventImageUrl && event.eventImageUrl !== 'null' && event.eventImageUrl.trim() !== ''
        ? event.eventImageUrl
        : 'img/default.jpg';

    Swal.fire({
        title: event.eventTitle,
        html: `
            <img src="${imageUrl}" style="width:100%;margin-bottom:15px;" />
            <p><strong>Date:</strong> ${new Date(event.eventDate).toLocaleDateString()}</p>
            <p><strong>Location:</strong> ${event.eventLocation}</p>
            <p><strong>Price:</strong> ${getPriceLabel(event.ticketType)}</p>
            <p>${event.description}</p>
        `,
        width: 600
    });
}

// Helper to show price label safely
function getPriceLabel(ticketType) {
    if (!ticketType) return 'N/A';
    const type = ticketType.toLowerCase();

    switch (type) {
        case 'free': return '🟢 Free';
        case 'paid': return '🟡 Paid';
        case 'vip': return '🔥 VIP';
        default: return ticketType;
    }
}



// Enroll a user in an event (simplified)
async function enrollUser(eventId) {
    const userId = await getUserIdFromJwt();
    if (!userId) {
        Swal.fire('Error', 'You must be logged in to enroll!', 'error');
        return;
    }

    try {
        const res = await fetch('http://localhost:5215/api/publishevents/enroll', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                userId: parseInt(userId),
                publishEventId: parseInt(eventId)
            })
        });

        if (!res.ok) throw new Error(await res.text());
        Swal.fire('Success', 'You have enrolled in the event!', 'success');
    } catch (err) {
        console.error('[enrollUser] Failed to enroll:', err);
        Swal.fire('Error', 'Could not enroll. Please try again.', 'error');
    }
}

// Add an event to the user's favorites (simplified)
async function addToFavorites(eventId) {
    const userId = await getUserIdFromJwt();
    if (!userId) {
        Swal.fire('Error', 'You must be logged in to add favorites!', 'error');
        return;
    }

    try {
        const res = await fetch('http://localhost:5215/api/publishevents/favorite', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                userId: parseInt(userId),
                publishEventId: parseInt(eventId)
            })
        });

        if (!res.ok) throw new Error(await res.text());
        const message = await res.text();
        Swal.fire('Success', message, 'success');
    } catch (err) {
        console.error('[addToFavorites] Failed to add to favorites:', err);
        Swal.fire('Error', 'Could not add to favorites. Please try again.', 'error');
    }
}

// On load, fetch and display events
document.addEventListener('DOMContentLoaded', fetchEvents);
