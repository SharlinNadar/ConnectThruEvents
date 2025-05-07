using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ConnectThruEventsBackend.Data;
using ConnectThruEventsBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System;

namespace ConnectThruEventsBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TaskController> _logger;

        public TaskController(AppDbContext context, ILogger<TaskController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Helper method to get the Event Manager ID from JWT token
        private Guid GetEventManagerIdFromJwt()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                _logger.LogWarning("User identity is null.");
                return Guid.Empty;
            }

            var eventManagerIdClaim = identity.FindFirst("EventManagerId");
            if (eventManagerIdClaim == null)
            {
                _logger.LogWarning("Event Manager ID not found in JWT token.");
                return Guid.Empty;
            }

            if (!Guid.TryParse(eventManagerIdClaim.Value, out var eventManagerId))
            {
                _logger.LogWarning($"Invalid Event Manager ID format in JWT token: {eventManagerIdClaim.Value}");
                return Guid.Empty;
            }

            _logger.LogInformation($"Successfully retrieved Event Manager ID from JWT token: {eventManagerId}");
            return eventManagerId;
        }

        // Get tasks for the event manager
        [HttpGet("event-manager/tasks")]
        [Authorize]
        public async Task<ActionResult<object>> GetTasksByEventManager()
        {
            try
            {
                var eventManagerId = GetEventManagerIdFromJwt();
                if (eventManagerId == Guid.Empty)
                {
                    return Unauthorized(new { status = "error", message = "Event Manager not found for the logged-in user" });
                }

                var tasks = await _context.Tasks
                    .Where(t => t.EventManagerId == eventManagerId)
                    .ToListAsync();

                if (!tasks.Any())
                {
                    return NotFound(new { status = "error", message = "No tasks found for the Event Manager" });
                }

                return Ok(new { status = "success", message = "Tasks retrieved successfully", tasks });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving tasks for Event Manager.");
                return StatusCode(500, new { status = "error", message = "Failed to retrieve tasks", error = ex.Message });
            }
        }

        // Add a new task
        [HttpPost("event-manager/tasks")]
        [Authorize]
        public async Task<ActionResult<object>> AddTask([FromBody] TaskItem newTask)
        {
            if (newTask == null || string.IsNullOrWhiteSpace(newTask.Name) || string.IsNullOrWhiteSpace(newTask.Description))
            {
                return BadRequest(new { status = "error", message = "Invalid task data. Task name and description are required." });
            }

            try
            {
                var eventManagerId = GetEventManagerIdFromJwt();
                if (eventManagerId == Guid.Empty)
                {
                    return Unauthorized(new { status = "error", message = "Event Manager not found for the logged-in user" });
                }

                newTask.EventManagerId = eventManagerId;
                newTask.CreatedAt = DateTime.UtcNow;
                newTask.UpdatedAt = DateTime.UtcNow;

                _context.Tasks.Add(newTask);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTasksByEventManager), new { id = newTask.TaskId }, new { status = "success", message = "Task added successfully", task = newTask });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding the task.");
                return StatusCode(500, new { status = "error", message = "An error occurred while adding the task", error = ex.Message });
            }
        }

        // Mark task as complete
[HttpPut("event-manager/tasks/{id}/complete")]
[Authorize]
public async Task<ActionResult<object>> CompleteTask(int id)  // Changed from Guid to int
{
    try
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound(new { status = "error", message = "Task not found" });
        }

        task.IsCompleted = true;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { status = "success", message = "Task marked as complete" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred while completing the task.");
        return StatusCode(500, new { status = "error", message = "An error occurred while completing the task", error = ex.Message });
    }
}

// Delete a task
[HttpDelete("event-manager/tasks/{id}")]
[Authorize]
public async Task<ActionResult<object>> DeleteTask(int id)
{
    try
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound(new { status = "error", message = "Task not found" });
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        var eventManagerId = GetEventManagerIdFromJwt();
        var updatedTasks = await _context.Tasks
            .Where(t => t.EventManagerId == eventManagerId)
            .ToListAsync();

        return Ok(new
        {
            status = "success",
            message = "Task deleted successfully",
            tasks = updatedTasks
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred while deleting the task.");
        return StatusCode(500, new { status = "error", message = "An error occurred while deleting the task", error = ex.Message });
    }
}

// Get count of pending tasks for the event manager
[HttpGet("event-manager/tasks/pending-count")]
[Authorize]
public async Task<ActionResult<object>> GetPendingTasksCount()
{
    try
    {
        var eventManagerId = GetEventManagerIdFromJwt();
        if (eventManagerId == Guid.Empty)
        {
            return Unauthorized(new { status = "error", message = "Event Manager not found for the logged-in user" });
        }

        var pendingTasksCount = await _context.Tasks
            .Where(t => t.EventManagerId == eventManagerId && !t.IsCompleted)
            .CountAsync();

        return Ok(new { status = "success", message = "Pending tasks count retrieved successfully", count = pendingTasksCount });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred while retrieving pending tasks count.");
        return StatusCode(500, new { status = "error", message = "Failed to retrieve pending tasks count", error = ex.Message });
    }
}


    }
}
