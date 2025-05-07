document.addEventListener('DOMContentLoaded', function () {
    const API_BASE = 'http://localhost:5215/api/event-manager';
    const eventForm = document.getElementById('eventForm');
    const budgetOption = document.getElementById('budgetOption');
    const calculationResults = document.getElementById('calculationResults');
    const previewSection = document.getElementById('eventPreview');
    const previewTitle = document.getElementById('previewTitle');
    const previewDate = document.getElementById('previewDate');
    const previewManagerName = document.getElementById('previewManagerName');
    const previewManagerEmail = document.getElementById('previewManagerEmail');
    const previewBudget = document.getElementById('previewBudget');
    const previewServiceCharge = document.getElementById('previewServiceCharge');
    const previewTotalPrice = document.getElementById('previewTotalPrice');
    const previewServiceChargePercentage = document.getElementById('previewServiceChargePercentage');
    const submitButton = document.getElementById('submitEvent');

    const assignedManagerIdField = document.getElementById('eventManagerId');
    const assignedManagerNameField = document.getElementById('manager-name');
    const assignedManagerEmailField = document.getElementById('manager-email');

    let allManagers = [];

    // Check if the user is logged in by checking the token in localStorage
    function checkLoginStatus() {
        const token = localStorage.getItem('token');
        if (!token) {
            // If the token is missing, redirect to login page
            Swal.fire({
                icon: 'error',
                title: 'Not Logged In',
                text: 'You need to log in to access this page.',
                willClose: () => {
                    window.location.href = 'login.html';
                }
            });
        }
    }

    // Fetch event managers
    function fetchAllEventManagers() {
        fetch(`${API_BASE}/all`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        })
            .then(response => {
                if (response.status === 401) {
                    Swal.fire({
                        icon: 'question',
                        title: 'Oops! Log In First',
                        text: 'You need to be logged in to create an event. You\'ll be redirected to the login page shortly.',
                        willClose: () => {
                            window.location.href = 'login.html';
                        }
                    });
                    
                    throw new Error('Unauthorized');
                } else if (!response.ok) {
                    throw new Error('Failed to fetch event managers.');
                }
                return response.json();
            })
            .then(data => {
                allManagers = data;
                renderManagers(allManagers);
            })
            .catch(error => console.error('Error fetching event managers:', error));
    }

    // Render event managers in cards
    function renderManagers(managers) {
        const container = document.getElementById('managers-container');
        container.innerHTML = '';

        if (managers.length === 0) {
            container.innerHTML = '<p>No event managers available.</p>';
            return;
        }

        managers.forEach(manager => {
            const card = document.createElement('div');
            card.classList.add('manager-card');
            card.innerHTML = `
                <h3>${manager.user.fullName}</h3>
                <p>Email: ${manager.user.email}</p>
                <p>Organization: ${manager.organization}</p>
                <p>Experience: ${manager.experienceYears} years</p>
                <p>Completed Events: ${manager.eventsCompleted}</p>
                <p>Price per Event: $${manager.pricePerEvent}</p>
                <button type="button" class="assign-manager-btn">Assign Manager</button>
            `;

            const button = card.querySelector('.assign-manager-btn');
            button.addEventListener('click', () => assignManager(manager));
            container.appendChild(card);
        });
    }

    // Assign manager to form and preview
    function assignManager(manager) {
        assignedManagerIdField.value = manager.eventManagerId;
        assignedManagerNameField.value = manager.user.fullName;
        assignedManagerEmailField.value = manager.user.email;

        previewManagerName.textContent = manager.user.fullName;
        previewManagerEmail.textContent = manager.user.email;

        Swal.fire({
            icon: 'success',
            title: 'Manager Assigned',
            text: `Manager "${manager.user.fullName}" has been successfully assigned.`,
            timer: 2000,
            showConfirmButton: false
        });
    }

    // Budget calculation preview (dynamic service charge percentage)
    budgetOption.addEventListener('change', function () {
        const budget = parseFloat(budgetOption.value);
        let serviceChargePercentage = 5;

        if (budget > 10000 && budget <= 20000) {
            serviceChargePercentage = 3;
        } else if (budget > 20000 && budget <= 50000) {
            serviceChargePercentage = 10;
        } else if (budget > 50000) {
            serviceChargePercentage = 2;
        }

        const serviceCharge = (budget * serviceChargePercentage) / 100;
        const totalPrice = budget + serviceCharge;

        document.getElementById('calculatedBudget').textContent = budget.toFixed(2);
        document.getElementById('calculatedServiceCharge').textContent = serviceCharge.toFixed(2);
        document.getElementById('calculatedTotalPrice').textContent = totalPrice.toFixed(2);

        previewServiceChargePercentage.textContent = serviceChargePercentage;
        calculationResults.style.display = 'block';
    });

    // Live preview for event form fields
    eventForm.addEventListener('input', function () {
        previewTitle.textContent = document.getElementById('eventTitle').value;
        previewDate.textContent = document.getElementById('eventDate').value;
        previewBudget.textContent = document.getElementById('calculatedBudget').textContent;
        previewServiceCharge.textContent = document.getElementById('calculatedServiceCharge').textContent;
        previewTotalPrice.textContent = document.getElementById('calculatedTotalPrice').textContent;

        previewSection.style.display = 'block';
    });

    // Submit event to backend
    submitButton.addEventListener('click', async function (e) {
        e.preventDefault();

        const managerId = assignedManagerIdField.value;
        if (!managerId) {
            Swal.fire({
                icon: 'warning',
                title: 'No Manager Assigned',
                text: 'Please assign a manager before submitting.'
            });
            return;
        }

        const eventData = {
            eventTitle: document.getElementById('eventTitle').value,
            eventDate: document.getElementById('eventDate').value,
            eventManagerId: managerId,
            budget: parseFloat(budgetOption.value)
        };

        try {
            const response = await fetch('http://localhost:5215/api/created-events/create', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                },
                body: JSON.stringify(eventData),
            });

            const data = await response.json();
            if (response.ok) {
                Swal.fire({
                    icon: 'success',
                    title: 'Event Created!',
                    text: 'Your event has been successfully created.',
                    timer: 2000,
                    showConfirmButton: false
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Creation Failed',
                    text: `Failed to create event: ${data.message || 'Unknown error'}`
                });
            }
        } catch (error) {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: `Error creating event: ${error.message}`
            });
        }
    });

    // Initialize the event manager fetching
    checkLoginStatus();  // Add the login status check
    fetchAllEventManagers();
});
