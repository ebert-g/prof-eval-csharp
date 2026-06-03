using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProfEval.Api.Infrastructure.Persistence;
using ProfEval.Api.Domain.Entities;
using ProfEval.Api.Domain.Interfaces;

namespace ProfEval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EvaluationsController : ControllerBase
{
    private readonly IRepository<Evaluation> _repository;
    private readonly ProfEvalDbContext _context;

    public EvaluationsController(IRepository<Evaluation> repository, ProfEvalDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    private string ComputeAnonymousToken(int studentId, int professorId)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var input = $"{studentId}_{professorId}_UcsalSecureEvaluationSalt123!";
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }
    }

    [HttpGet("check-vote")]
    public async Task<ActionResult> CheckVote([FromQuery] int studentId, [FromQuery] int professorId)
    {
        var token = ComputeAnonymousToken(studentId, professorId);
        var evaluation = await _context.Evaluations
            .FirstOrDefaultAsync(e => e.AnonymousToken == token);

        if (evaluation == null)
            return Ok(new { hasEvaluated = false });

        return Ok(new { 
            hasEvaluated = true, 
            score = evaluation.Score, 
            comment = evaluation.Comment 
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Evaluation>>> GetAll()
    {
        var evaluations = await _context.Evaluations
            .Include(e => e.Professor)
            .ToListAsync();
        return Ok(evaluations);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Evaluation>> GetById(int id)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Professor)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evaluation == null)
            return NotFound();

        return Ok(evaluation);
    }

    [HttpPost]
    public async Task<ActionResult<Evaluation>> Create(Evaluation evaluation)
    {
        if (evaluation.StudentId == null && string.IsNullOrEmpty(evaluation.AnonymousToken))
        {
            // If studentId comes in but we want anonymity, we extract the ID
            return BadRequest("Estudante não identificado.");
        }

        // We use evaluation.StudentId (which is passed in the request body from front) 
        // to compute the token, but we set StudentId to null before saving to database.
        int studentId = evaluation.StudentId ?? 0;
        if (studentId > 0)
        {
            var token = ComputeAnonymousToken(studentId, evaluation.ProfessorId);
            
            // Check if already evaluated
            var existing = await _context.Evaluations
                .FirstOrDefaultAsync(e => e.AnonymousToken == token);

            if (existing != null)
            {
                // Update previous vote
                existing.Score = evaluation.Score;
                existing.Comment = evaluation.Comment;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.StudentId = null; // Uncoupled

                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            else
            {
                // Create new anonymous evaluation
                var newEval = new Evaluation
                {
                    ProfessorId = evaluation.ProfessorId,
                    Score = evaluation.Score,
                    Comment = evaluation.Comment,
                    AnonymousToken = token,
                    StudentId = null, // Uncoupled
                    EvaluationDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Evaluations.Add(newEval);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetById), new { id = newEval.Id }, newEval);
            }
        }

        return BadRequest("Estudante inválido.");
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Evaluation>> Update(int id, Evaluation evaluation)
    {
        var existingEvaluation = await _repository.GetByIdAsync(id);
        if (existingEvaluation == null)
            return NotFound();

        existingEvaluation.Score = evaluation.Score;
        existingEvaluation.Comment = evaluation.Comment;
        existingEvaluation.UpdatedAt = DateTime.UtcNow;
        existingEvaluation.StudentId = null; // Ensure it remains uncoupled

        var updatedEvaluation = await _repository.UpdateAsync(existingEvaluation);
        return Ok(updatedEvaluation);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<IEnumerable<Evaluation>>> GetByStudent(int studentId)
    {
        var professorIds = await _context.Professors.Select(p => p.Id).ToListAsync();
        var tokens = professorIds.Select(pId => ComputeAnonymousToken(studentId, pId)).ToList();

        var evaluations = await _context.Evaluations
            .Where(e => e.AnonymousToken != null && tokens.Contains(e.AnonymousToken))
            .Include(e => e.Professor)
            .ToListAsync();

        return Ok(evaluations);
    }

    [HttpGet("professor/{professorId}")]
    public async Task<ActionResult<IEnumerable<Evaluation>>> GetByProfessor(int professorId)
    {
        var evaluations = await _context.Evaluations
            .Where(e => e.ProfessorId == professorId)
            .Include(e => e.Professor)
            .ToListAsync();

        return Ok(evaluations);
    }
}
