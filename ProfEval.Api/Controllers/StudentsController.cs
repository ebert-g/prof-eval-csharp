using Microsoft.AspNetCore.Mvc;
using ProfEval.Api.Domain.Entities;
using ProfEval.Api.Domain.Interfaces;

namespace ProfEval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IRepository<Student> _repository;

    public StudentsController(IRepository<Student> repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetAll()
    {
        var students = await _repository.GetAllAsync();
        return Ok(students);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetById(int id)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null)
            return NotFound();

        return Ok(student);
    }

    [HttpPost]
    public async Task<ActionResult<Student>> Create(Student student)
    {
        var createdStudent = await _repository.AddAsync(student);
        return CreatedAtAction(nameof(GetById), new { id = createdStudent.Id }, createdStudent);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Student>> Update(int id, Student student)
    {
        var existingStudent = await _repository.GetByIdAsync(id);
        if (existingStudent == null)
            return NotFound();

        existingStudent.Name = student.Name;
        existingStudent.Email = student.Email;
        existingStudent.Registration = student.Registration;
        existingStudent.EmailVerified = student.EmailVerified;
        existingStudent.VerifiedAt = student.VerifiedAt;
        existingStudent.UpdatedAt = DateTime.UtcNow;

        var updatedStudent = await _repository.UpdateAsync(existingStudent);
        return Ok(updatedStudent);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
