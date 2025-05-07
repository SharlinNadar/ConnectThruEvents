let assignedEmployees = [];
let assignedTasks = [];
let employeeMap = {}; // key: displayName, value: employeeAssignmentId
let eventProgress = 0;  // Initial progress

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

async function getEventManagerIdFromJwt() {
    const token = localStorage.getItem('token');
    if (!token) {
        console.error('Token not found');
        return null;
    }

    const decodedJwt = decodeJwt(token);
    if (!decodedJwt) {
        console.error('Failed to decode JWT');
        return null;
    }

    const eventManagerId = decodedJwt.EventManagerId;
    if (!eventManagerId) {
        console.error('Event Manager ID not found in JWT');
        return null;
    }

    return eventManagerId;
}
async function saveEventNotes(eventId) {
    const token = localStorage.getItem('token');
    const notes = document.getElementById('event-notes').value.trim();

    if (!notes) {
        Swal.fire('Validation Error', 'Notes field cannot be empty.', 'warning');
        return;
    }

    try {
        const response = await fetch(`http://localhost:5215/api/eventdetail/event-manager/event-details/${eventId}/notes`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ notes })
        });

        if (!response.ok) throw new Error('Failed to update notes.');

        document.getElementById('saved-notes-content').textContent = notes;
        Swal.fire('Success', 'Notes updated successfully!', 'success');
    } catch (error) {
        console.error('Error updating notes:', error);
        Swal.fire('Error', error.message || 'Could not update notes.', 'error');
    }
}

// Fetch event details
async function fetchEventDetails(eventId) {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            Swal.fire('Error', 'You need to be logged in to view the event details.', 'error');
            return;
        }

        const response = await fetch(`http://localhost:5215/api/eventdetail/event-manager/event-details/${eventId}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) throw new Error(`Failed to fetch event details for ID ${eventId}`);
        const data = await response.json();

        console.log('Fetched Event Details:', data); // Added log

        if (data && data.eventDetail) {
            document.getElementById('event-title').textContent = data.eventDetail.title || 'No title';
            document.getElementById('event-location').textContent = data.eventDetail.location || 'No location';
            document.getElementById('event-date').textContent = new Date(data.eventDetail.date).toLocaleDateString();
            document.getElementById('event-description').textContent = data.eventDetail.description || 'No description';
            document.getElementById('event-status').textContent = data.eventDetail.status || 'No status';
            document.getElementById('event-notes').value = data.eventDetail.notes || '';

            if (data.eventDetail.rating !== undefined) {
                eventProgress = data.eventDetail.rating;
                updateProgress();
            }
        } else {
            throw new Error('Event details not found.');
        }
    } catch (error) {
        console.error('Error fetching event details:', error);
        Swal.fire('Error', error.message, 'error');
    }
}

function updateProgress() {
    document.getElementById('event-progress-bar').value = eventProgress;
    document.getElementById('event-progress-text').textContent = `${eventProgress}%`;
}

// Assign employee to event
async function assignEmployee(eventId, employeeName, employeeRole) {
    const token = localStorage.getItem('token');
    if (!employeeName || !employeeRole) {
        Swal.fire('Validation Error', 'Employee name and role are required.', 'warning');
        return;
    }

    try {
        const eventManagerId = await getEventManagerIdFromJwt();
        if (!eventManagerId) {
            Swal.fire('Error', 'Event Manager ID not found.', 'error');
            return;
        }

        console.log(`Assigning employee: ${employeeName}, Role: ${employeeRole}`); // Added log

        const response = await fetch(`http://localhost:5215/api/event/assign-employee/${eventManagerId}/event/${eventId}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ employeeName, role: employeeRole })
        });

        if (!response.ok) throw new Error('Failed to assign employee.');

        assignedEmployees.push({ name: employeeName, role: employeeRole });
        updateAssignedEmployees();
        await fetchAndPopulateEmployees(eventId);
        Swal.fire('Success', 'Employee assigned successfully!', 'success');
    } catch (error) {
        console.error('Error assigning employee:', error);
        Swal.fire('Error', error.message, 'error');
    }
}

function updateAssignedEmployees() {
    const employeeList = document.getElementById('assigned-employees-list');
    employeeList.innerHTML = '';  // Clear the list before updating

    assignedEmployees.forEach(employee => {
        const li = document.createElement('li');
        li.textContent = `${employee.name} - ${employee.role}`;
        employeeList.appendChild(li);
    });
}

// Fetch employees for task assignment dropdown
async function fetchAndPopulateEmployees(eventId) {
    const token = localStorage.getItem('token');
    try {
        const response = await fetch(`http://localhost:5215/api/event/dropdown-employees/${eventId}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) throw new Error('Failed to fetch employees');

        const employees = await response.json();
        console.log('Fetched Employees:', employees); // Added log

        const dropdown = document.getElementById('task-employee');
        dropdown.innerHTML = `<option value="" disabled selected>Assign to Employee</option>`;
        employeeMap = {};

        employees.forEach(emp => {
            const label = `${emp.employeeName} (${emp.role})`;
            employeeMap[label] = emp.employeeAssignmentId;

            const option = document.createElement('option');
            option.value = label;
            option.textContent = label;
            dropdown.appendChild(option);
        });
    } catch (err) {
        console.error('Error populating employee dropdown:', err);
        Swal.fire('Error', err.message, 'error');
    }
}

// Assign task to selected employee
async function assignTask(eventId, name, cost, status, priority, selectedName) {
    const token = localStorage.getItem('token');
    const employeeId = employeeMap[selectedName];

    if (!employeeId) {
        Swal.fire('Error', 'Invalid employee selection.', 'error');
        return;
    }

    const eventManagerId = await getEventManagerIdFromJwt();
    if (!eventManagerId) {
        Swal.fire('Error', 'Event Manager ID not found.', 'error');
        return;
    }

    const parsedCost = parseFloat(cost);
    if (isNaN(parsedCost)) {
        Swal.fire('Validation Error', 'Cost must be a valid number.', 'warning');
        return;
    }

    console.log(`Assigning task: ${name}, Cost: ${parsedCost}, Priority: ${priority}, Employee: ${selectedName}`);

    try {
        const response = await fetch(`http://localhost:5215/api/event/assign-task/${eventManagerId}/event/${eventId}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                taskName: name,
                taskCost: parsedCost, // ✅ Use correct key expected by backend
                status,
                priority,
                employeeAssignmentId: employeeId
            })
        });

        if (!response.ok) throw new Error('Failed to assign task.');

        const newTask = await response.json();

        assignedTasks.push({
            taskName: newTask.taskName,
            status: newTask.status,
            priority: newTask.priority,
            employeeName: selectedName,
            taskCost: newTask.taskCost
        });

        updateAssignedTasks();
        Swal.fire('Success', 'Task assigned successfully!', 'success');
    } catch (error) {
        console.error('Error assigning task:', error);
        Swal.fire('Error', error.message || 'Could not assign task.', 'error');
    }
}

async function fetchTasks(eventId) {
    const token = localStorage.getItem('token');
    try {
        const response = await fetch(`http://localhost:5215/api/event/tasks-for-event/${eventId}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) throw new Error('Failed to fetch tasks.');

        const data = await response.json();
        console.log('Fetched Tasks:', data);

        // ✅ Make sure taskAssignmentId (or taskId) is included
        assignedTasks = data.map(task => ({
            taskId: task.taskAssignmentId || task.taskId, // adapt to your API
            taskName: task.taskName,
            status: task.status,
            priority: task.priority,
            employeeName: task.employeeName,
            taskCost: task.taskCost
        }));

        updateAssignedTasks(); // ✅ Do not pass managerId or eventDetailId here
    } catch (error) {
        console.error('Error fetching tasks:', error);
        Swal.fire('Error', error.message, 'error');
    }
}


// Assuming the code for rendering the task list is already there as you provided
function updateAssignedTasks() {
    const taskList = document.getElementById('assigned-tasks-list');
    taskList.innerHTML = ''; // Clear the task list

    if (assignedTasks.length === 0) {
        taskList.innerHTML = '<p>No tasks assigned.</p>';
        return;
    }

    assignedTasks.forEach(task => {
        const div = document.createElement('div');
        div.classList.add('task-item');

        div.innerHTML = `
            <h4>${task.taskName}</h4>
            <p>Status: ${task.status}</p>
            <p>Priority: ${task.priority}</p>
            <p>Assigned to: ${task.employeeName || ''}</p>
            <p>Cost: ${task.taskCost}</p>
            <button class="delete-task-btn" data-task-id="${task.taskId}">Delete</button>
            <button class="update-status-btn" data-task-id="${task.taskId}">Update Status</button>
        `;

        taskList.appendChild(div);

        div.querySelector('.delete-task-btn').addEventListener('click', () => {
            Swal.fire({
                title: 'Are you sure?',
                text: 'This will permanently delete the task.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Yes, delete it!',
            }).then(result => {
                if (result.isConfirmed) {
                    handleDeleteTask(task.taskId);
                }
            });
        });

        div.querySelector('.update-status-btn').addEventListener('click', async () => {
            const { value: newStatus } = await Swal.fire({
                title: 'Update Task Status',
                input: 'text',
                inputLabel: 'New Status',
                inputPlaceholder: 'e.g., In Progress, Completed',
                showCancelButton: true,
            });

            if (newStatus) {
                handleUpdateTaskStatus(task.taskId, newStatus);
            }
        });
    });
}


// Delete Task (refactored to auto-fetch managerId and eventId)
async function handleDeleteTask(taskAssignmentId) {
    console.log('Starting task delete operation:', { taskAssignmentId });

    // Fetch managerId and eventDetailId from JWT and URL parameters
    const managerId = await getEventManagerIdFromJwt();
    const urlParams = new URLSearchParams(window.location.search);
    const eventDetailId = urlParams.get('eventId');

    console.log('Fetched managerId:', managerId);
    console.log('Fetched eventDetailId:', eventDetailId);

    // Check if managerId or eventDetailId is missing
    if (!managerId || !eventDetailId) {
        Swal.fire('Error', 'Missing manager ID or event ID.', 'error');
        return;
    }

    // 🔍 Log the request details before making the DELETE request
    const requestUrl = `http://localhost:5215/api/event/delete-task/${managerId}/event/${eventDetailId}/task/${taskAssignmentId}`;
    console.log('Deleting Task - Request Details:', {
        managerId,
        eventDetailId,
        taskAssignmentId,
        url: requestUrl,
        token: localStorage.getItem('token'),
    });

    try {
        // Make the DELETE request
        const response = await fetch(requestUrl, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
            },
        });

        // 👀 Log the response status and raw data
        console.log('Response Status:', response.status);

        // Check if the response status is ok (200)
        if (!response.ok) {
            const errorData = await response.json();
            console.error('Error Response Data:', errorData);
            Swal.fire('Error', errorData.message || 'Could not delete task.', 'error');
            return;
        }

        const data = await response.json();
        console.log('Response Data:', data);

        // Handle success based on response data
        if (data.message === "Task deleted successfully.") {
            Swal.fire('Deleted!', 'Task deleted successfully.', 'success');
            fetchTasks(eventDetailId); // Refresh tasks
        } else {
            Swal.fire('Error', data.message || 'Could not delete task.', 'error');
        }
    } catch (error) {
        // Handle any errors during the fetch operation
        console.error('Fetch Error:', error);
        Swal.fire('Error', error.message, 'error');
    }
}




// Handle Task Status Update
// Update Task Status (refactored to auto-fetch managerId and eventId)
async function handleUpdateTaskStatus(taskAssignmentId, newStatus) {
    console.log("Handling task update:", { taskAssignmentId, newStatus });

    // Fetch managerId and eventDetailId
    const managerId = await getEventManagerIdFromJwt();
    const urlParams = new URLSearchParams(window.location.search);
    const eventDetailId = urlParams.get('eventId');

    console.log("Fetched managerId:", managerId);
    console.log("Fetched eventDetailId:", eventDetailId);

    // Check if managerId or eventDetailId is missing
    if (!managerId || !eventDetailId) {
        Swal.fire('Error', 'Missing manager ID or event ID.', 'error');
        return;
    }

    try {
        // Retrieve the token from localStorage
        const token = localStorage.getItem('token');
        console.log("Authorization token:", token);

        // Log the full request URL
        const requestUrl = `http://localhost:5215/api/event/update-task-status/${managerId}/event/${eventDetailId}/task/${taskAssignmentId}`;
        console.log("Making PUT request to:", requestUrl);

        // Ensure the body is correctly formatted with 'newStatus' being a string
        const body = JSON.stringify(newStatus); // Send only the 'newStatus' string

        // Perform the PUT request
        const response = await fetch(requestUrl, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
            },
            body: body, // Send the 'newStatus' string as the request body
        });

        console.log("Response status:", response.status);

        // Parse the response data
        const data = await response.json();
        console.log("Response data:", data);

        // Handle response based on success or error
        if (response.status === 200 && data.message === "Task status updated successfully.") {
            Swal.fire('Success', 'Task status updated successfully.', 'success');
            fetchTasks(eventDetailId); // Refresh tasks
        } else {
            Swal.fire('Error', data.message || 'Could not update task.', 'error');
            console.error("Error in response message:", data.message);
        }
    } catch (error) {
        // Handle any errors that occur during the fetch operation
        console.error("Error occurred during task status update:", error);
        Swal.fire('Error', error.message, 'error');
    }
}





// Fetch and display employee details (name, role) for the event
async function fetchAndDisplayEmployeeDetails(eventId) {
    const token = localStorage.getItem('token');
    try {
        const response = await fetch(`http://localhost:5215/api/event/employees-for-event/${eventId}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) throw new Error('Failed to fetch employee details');

        const employees = await response.json();
        console.log('Fetched Employees:', employees); // Debug log

        // Assuming employees is an array of employee objects
        const employeeList = document.getElementById('assigned-employees-list'); // This is the container where employee details will be displayed
        employeeList.innerHTML = ''; // Clear existing list before adding new ones

        employees.forEach(emp => {
            const li = document.createElement('li');
            li.textContent = `${emp.employeeName} - ${emp.role}`;
            employeeList.appendChild(li); // Add each employee to the list
        });

    } catch (err) {
        console.error('Error fetching employee details:', err);
        Swal.fire('Error', err.message, 'error');
    }
}

// Ensure to call this function on page load or after fetching event details
async function fetchEventDetailsAndEmployees(eventId) {
    await fetchEventDetails(eventId);
    await fetchAndDisplayEmployeeDetails(eventId);
}

window.onload = function () {
    const urlParams = new URLSearchParams(window.location.search);
    const eventId = urlParams.get('eventId');

    if (eventId) {
        console.log(`Event ID: ${eventId}`); // Added log for debugging
        fetchEventDetailsAndEmployees(eventId);
        fetchTasks(eventId);
        fetchAndPopulateEmployees(eventId);

        // Event listener setup for saving notes and assigning employees/tasks
        document.getElementById('save-notes-btn').addEventListener('click', () => {
            saveEventNotes(eventId);
        });

        document.getElementById('assign-employee-btn').addEventListener('click', () => {
            const name = document.getElementById('employee-name').value.trim();
            const role = document.getElementById('employee-role').value;
            assignEmployee(eventId, name, role);
        });

        document.getElementById('assign-task-btn').addEventListener('click', () => {
            const name = document.getElementById('task-name').value.trim();
            const cost = document.getElementById('task-cost').value.trim();
            const status = document.getElementById('task-status').value;
            const priority = document.getElementById('task-priority').value;
            const employee = document.getElementById('task-employee').value;

            if (!name || !cost || !status || !priority || !employee) {
                Swal.fire('Validation Error', 'All task fields are required.', 'warning');
                return;
            }

            assignTask(eventId, name, cost, status, priority, employee);
        });
    } else {
        Swal.fire('Error', 'Event ID not found in URL.', 'error');
    }
};





