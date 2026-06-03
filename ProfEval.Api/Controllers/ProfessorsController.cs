using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProfEval.Api.Domain.Entities;
using ProfEval.Api.Domain.Interfaces;
using ProfEval.Api.Infrastructure.Persistence;

namespace ProfEval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfessorsController : ControllerBase
{
    private readonly IRepository<Professor> _repository;
    private readonly ProfEvalDbContext _context;

    public ProfessorsController(IRepository<Professor> repository, ProfEvalDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var professors = await _context.Professors
            .Include(p => p.Evaluations)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Email,
                p.Department,
                p.Specialization,
                p.CreatedAt,
                AverageRating = p.Evaluations.Any() ? Math.Round((double)p.Evaluations.Average(e => e.Score), 1) : 0.0,
                EvaluationCount = p.Evaluations.Count
            })
            .ToListAsync();
        return Ok(professors);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Professor>> GetById(int id)
    {
        var professor = await _repository.GetByIdAsync(id);
        if (professor == null)
            return NotFound();

        return Ok(professor);
    }

    [HttpPost]
    public async Task<ActionResult<Professor>> Create(Professor professor)
    {
        var createdProfessor = await _repository.AddAsync(professor);
        return CreatedAtAction(nameof(GetById), new { id = createdProfessor.Id }, createdProfessor);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Professor>> Update(int id, Professor professor)
    {
        var existingProfessor = await _repository.GetByIdAsync(id);
        if (existingProfessor == null)
            return NotFound();

        existingProfessor.Name = professor.Name;
        existingProfessor.Email = professor.Email;
        existingProfessor.Department = professor.Department;
        existingProfessor.Specialization = professor.Specialization;
        existingProfessor.UpdatedAt = DateTime.UtcNow;

        var updatedProfessor = await _repository.UpdateAsync(existingProfessor);
        return Ok(updatedProfessor);
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
