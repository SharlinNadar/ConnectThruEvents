// Function to decode JWT and extract user email
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

// Function to fetch tasks by Event Manager ID
async function fetchTasksByEventManager() {
    try {
        const eventManagerId = await getEventManagerIdFromJwt();
        if (!eventManagerId) {
            Swal.fire('Error', 'Unable to retrieve Event Manager ID.', 'error');
            return;
        }

        const response = await fetch(`http://localhost:5215/api/task/event-manager/tasks`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Content-Type': 'application/json'
            }
        });

        const data = await response.json();
        console.log("Fetched tasks data:", data);

        if (data && Array.isArray(data.tasks)) {
            displayTasks(data.tasks);
        } else {
            console.log('Error', 'Unexpected response format. Tasks data is not an array.', 'error');
            console.error('Unexpected response format:', data);
        }
    } catch (error) {
        console.error('Error fetching tasks:', error);
    }
}

// Function to display tasks in the UI
function displayTasks(tasks) {
    const taskListElement = document.getElementById('task-list');
    taskListElement.innerHTML = '';

    tasks.forEach(task => {
        const taskElement = document.createElement('li');
        taskElement.classList.add('task-item');
        
        // Apply strikethrough style if the task is completed
        const completedClass = task.isCompleted ? 'completed' : '';

        taskElement.innerHTML = `
            <div class="task-name ${completedClass}">${task.name}</div>
            <div class="task-description">${task.description}</div>
            <div class="task-status">${task.isCompleted ? 'Completed' : 'Pending'}</div>
            <button class="complete-btn" onclick="markTaskAsComplete(${task.taskId})">Mark as Complete</button>
            <button class="delete-btn" onclick="deleteTask(${task.taskId})">Delete Task</button>
        `;
        taskListElement.appendChild(taskElement);
    });
}

// Function to mark a task as complete
async function markTaskAsComplete(taskId) {
    try {
        const response = await fetch(`http://localhost:5215/api/task/event-manager/tasks/${taskId}/complete`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            const errorText = await response.text();
            Swal.fire('Error', `Failed to mark task as complete: ${errorText}`, 'error');
            return;
        }

        Swal.fire('Success', 'Task marked as complete', 'success');
        fetchTasksByEventManager();
    } catch (error) {
        console.error('Error marking task as complete:', error);
    }
}

// Function to delete a task
async function deleteTask(taskId) {
    try {
        const response = await fetch(`http://localhost:5215/api/task/event-manager/tasks/${taskId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            const errorText = await response.text();
            Swal.fire('Error', `Failed to delete task: ${errorText}`, 'error');
            return;
        }

        // Parse the response JSON
        const result = await response.json();

        if (result.status === "success") {
            Swal.fire('Success', result.message, 'success');
            // Assuming `result.tasks` is the updated tasks array
            updateTaskList(result.tasks);
        } else {
            Swal.fire('Error', 'Failed to delete task', 'error');
        }
    } catch (error) {
        console.error('Error deleting task:', error);
        Swal.fire('Error', 'An unexpected error occurred while deleting the task', 'error');
    }
}

// Function to update the task list UI
function updateTaskList(tasks) {
    // Clear the current task list
    const taskListElement = document.getElementById('task-list');
    taskListElement.innerHTML = '';

    // Loop through the updated tasks and append to the list
    tasks.forEach(task => {
        const taskItem = document.createElement('li');
        taskItem.textContent = task.name; // Customize this to show task details
        taskListElement.appendChild(taskItem);
    });
}

// Function to add a new task
async function addTask(taskName, taskDescription) {
    try {
        const response = await fetch(`http://localhost:5215/api/task/event-manager/tasks`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ name: taskName, description: taskDescription })
        });

        if (!response.ok) {
            const errorText = await response.text();
            Swal.fire('Error', `Failed to add task: ${errorText}`, 'error');
            return;
        }

        Swal.fire('Success', 'Task added successfully', 'success');
        fetchTasksByEventManager();
    } catch (error) {
        console.error('Error adding task:', error);
    }
}

// Function to show the 'Add New Task' dialog
function showAddTaskDialog() {
    Swal.fire({
        title: 'Add New Task',
        html: `
            <input type="text" id="task-name" class="swal2-input" placeholder="Task Name">
            <textarea id="task-description" class="swal2-textarea" placeholder="Task Description"></textarea>
        `,
        confirmButtonText: 'Add Task',
        preConfirm: () => {
            const taskName = document.getElementById('task-name').value;
            const taskDescription = document.getElementById('task-description').value;
            if (!taskName || !taskDescription) {
                Swal.showValidationMessage('Both task name and description are required');
                return;
            }
            addTask(taskName, taskDescription);
        }
    });
}

// Initial fetch of tasks when the page loads
document.addEventListener('DOMContentLoaded', fetchTasksByEventManager);
