using Microsoft.AspNetCore.Mvc.RazorPages;
using ProfEval.Api.Domain.Entities;
using ProfEval.Api.Domain.Interfaces;

namespace ProfEval.Api.Pages;

public class IndexModel : PageModel
{
    private readonly IRepository<Professor> _professorRepository;

    public IndexModel(IRepository<Professor> professorRepository)
    {
        _professorRepository = professorRepository;
    }

    public IEnumerable<Professor> Professors { get; private set; } = Array.Empty<Professor>();

    public async Task OnGetAsync()
    {
        Professors = await _professorRepository.GetAllAsync();
    }
}
