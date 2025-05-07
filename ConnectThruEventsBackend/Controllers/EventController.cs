using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectThruEventsBackend.Models;
using ConnectThruEventsBackend.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConnectThruEventsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "EventManager,Admin")]  // Ensure that only Event Managers and Admins can access this controller
    public class EventController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EventController(AppDbContext context)
        {
            _context = context;
        }

        // Get all events for a specific manager
        [HttpGet("events-by-manager/{managerId}")]
        public async Task<ActionResult<IEnumerable<EventDetail>>> GetEventsByManager(Guid managerId)
        {
            var events = await _context.EventDetails
                                        .Where(e => e.EventManagerId == managerId)
                                        .ToListAsync();

            if (events == null || events.Count == 0)
            {
                return NotFound(new { message = "No events found for this manager." });
            }

            return Ok(events);
        }

        // Get all tasks for a specific event
        [HttpGet("tasks-for-event/{eventDetailId}")]
        public async Task<ActionResult<IEnumerable<TaskAssignment>>> GetTasksForEvent(int eventDetailId)
        {
            var tasks = await _context.TaskAssignments
                                      .Where(t => t.EventDetailId == eventDetailId)
                                      .Include(t => t.EmployeeAssignment)
                                      .ToListAsync();

            if (tasks == null || tasks.Count == 0)
            {
                return NotFound(new { message = "No tasks found for this event." });
            }

            return Ok(tasks);
        }

        // Get all employees for a specific event
        [HttpGet("employees-for-event/{eventDetailId}")]
        public async Task<ActionResult<IEnumerable<EmployeeAssignment>>> GetEmployeesForEvent(int eventDetailId)
        {
            var employees = await _context.EmployeeAssignments
                                          .Where(e => e.EventDetailId == eventDetailId)
                                          .Include(e => e.EventDetail)
                                          .ToListAsync();

            if (employees == null || employees.Count == 0)
            {
                return NotFound(new { message = "No employees found for this event." });
            }

            return Ok(employees);
        }

        // POST: Assign an employee to an event for a specific manager
        [HttpPost("assign-employee/{managerId}/event/{eventDetailId}")]
        public async Task<ActionResult<EmployeeAssignment>> AssignEmployee(Guid managerId, int eventDetailId, [FromBody] EmployeeAssignment employee)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var eventDetail = await _context.EventDetails.FindAsync(eventDetailId);
            if (eventDetail == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (eventDetail.EventManagerId != managerId)
            {
                return BadRequest(new { message = "You are not authorized to assign employees to this event." });
            }

            employee.EventDetailId = eventDetailId; // Assign only EventDetailId
            _context.EmployeeAssignments.Add(employee);
            await _context.SaveChangesAsync();

            return Ok(employee);
        }

        // POST: Assign a task to an employee for a specific event and manager
        [HttpPost("assign-task/{managerId}/event/{eventDetailId}")]
        public async Task<ActionResult<TaskAssignment>> AssignTask(Guid managerId, int eventDetailId, [FromBody] TaskAssignment task)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Ensure the required fields are present
            if (string.IsNullOrWhiteSpace(task.TaskName))
            {
                return BadRequest(new { message = "Task name is required." });
            }

            // Fetch the event and its employee assignments
            var eventDetail = await _context.EventDetails
                .Include(e => e.EmployeeAssignments)
                .FirstOrDefaultAsync(e => e.EventDetailId == eventDetailId);

            if (eventDetail == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (eventDetail.EventManagerId != managerId)
            {
                return BadRequest(new { message = "You are not authorized to assign tasks to this event." });
            }

            // Check that the employee is part of this event
            var employee = await _context.EmployeeAssignments
                .FirstOrDefaultAsync(e => e.EmployeeAssignmentId == task.EmployeeAssignmentId && e.EventDetailId == eventDetailId);

            if (employee == null)
            {
                return BadRequest(new { message = "The employee is not assigned to this event." });
            }

            // Set foreign keys explicitly
            task.EmployeeAssignmentId = employee.EmployeeAssignmentId;
            task.EventDetailId = eventDetailId;

            // Set default values if necessary
            task.Status = string.IsNullOrWhiteSpace(task.Status) ? "Not Started" : task.Status;
            task.Priority = string.IsNullOrWhiteSpace(task.Priority) ? "Medium" : task.Priority;

            // Detach navigation properties to avoid tracking errors
            task.EmployeeAssignment = null;
            task.EventDetail = null;

            // Save the task
            _context.TaskAssignments.Add(task);
            await _context.SaveChangesAsync();

            return Ok(task);
        }

        // GET: All employees assigned to an event (to populate dropdown in task assignment)
        [HttpGet("dropdown-employees/{eventDetailId}")]
        public async Task<ActionResult<IEnumerable<EmployeeAssignment>>> GetDropdownEmployees(int eventDetailId)
        {
            var employees = await _context.EmployeeAssignments
                                          .Where(e => e.EventDetailId == eventDetailId)
                                          .ToListAsync();

            if (employees == null || employees.Count == 0)
            {
                return NotFound(new { message = "No employees found for this event." });
            }

            return Ok(employees);
        }
        // DELETE: Delete a task assignment for a specific event and manager
[HttpDelete("delete-task/{managerId}/event/{eventDetailId}/task/{taskAssignmentId}")]
public async Task<ActionResult> DeleteTask(Guid managerId, int eventDetailId, int taskAssignmentId)
{
    var task = await _context.TaskAssignments
                             .Include(t => t.EventDetail)
                             .FirstOrDefaultAsync(t => t.TaskAssignmentId == taskAssignmentId && t.EventDetailId == eventDetailId);

    if (task == null || task.EventDetail == null)
    {
        return NotFound(new { message = "Task or event detail not found for this task." });
    }

    if (task.EventDetail?.EventManagerId != managerId)
    {
        return BadRequest(new { message = "You are not authorized to delete this task." });
    }

    _context.TaskAssignments.Remove(task);
    await _context.SaveChangesAsync();

    return Ok(new { message = "Task deleted successfully." });
}

// PUT: Update the status of a task assignment for a specific event and manager
[HttpPut("update-task-status/{managerId}/event/{eventDetailId}/task/{taskAssignmentId}")]
public async Task<ActionResult> UpdateTaskStatus(Guid managerId, int eventDetailId, int taskAssignmentId, [FromBody] string newStatus)
{
    var task = await _context.TaskAssignments
                             .Include(t => t.EventDetail)
                             .FirstOrDefaultAsync(t => t.TaskAssignmentId == taskAssignmentId && t.EventDetailId == eventDetailId);

    if (task == null || task.EventDetail == null)
    {
        return NotFound(new { message = "Task or event detail not found for this task." });
    }

    if (task.EventDetail?.EventManagerId != managerId)
    {
        return BadRequest(new { message = "You are not authorized to update the status of this task." });
    }

    if (string.IsNullOrWhiteSpace(newStatus))
    {
        return BadRequest(new { message = "New status cannot be empty." });
    }

    task.Status = newStatus;

    _context.TaskAssignments.Update(task);
    await _context.SaveChangesAsync();

    return Ok(new { message = "Task status updated successfully." });
}

        
    }
}
