using ProfEval.Api.Infrastructure.Persistence;
using ProfEval.Api.Domain.Entities;
using ProfEval.Api.Domain.Interfaces;
using ProfEval.Api.Infrastructure.Repositories;
using ProfEval.Api.Application.Interfaces;
using ProfEval.Api.Application.Services;
using ProfEval.Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add CORS policy for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", builder =>
    {
        builder.WithOrigins("http://localhost:8000", "http://localhost:3000", "http://127.0.0.1:8000")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

/// Configurando o banco de dados para usar PostgreSQL (Npgsql)
builder.Services.AddDbContext<ProfEvalDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddLogging();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowLocalhost");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapControllers();
app.MapRazorPages();

// Semente de Dados (Seed Data) - Executa ao iniciar o App
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProfEvalDbContext>();
    
    // Se houver menos de 5 professores ou nenhuma avaliação, adiciona uma carga de teste rica para visualização
    if (context.Professors.Count() < 5 || !context.Evaluations.Any())
    {
        // Remove os antigos para evitar duplicação ou dados incompletos
        context.Professors.RemoveRange(context.Professors);
        context.SaveChanges();

        context.Professors.AddRange(
            new Professor { Name = "Prof. Dr. Carlos Augusto", Email = "carlos.augusto@ucsal.edu.br", Department = "Engenharia de Software", Specialization = "Inteligência Artificial & Machine Learning", CreatedAt = DateTime.UtcNow },
            new Professor { Name = "Profa. Dra. Ana Beatriz", Email = "ana.beatriz@ucsal.edu.br", Department = "Ciência da Computação", Specialization = "Banco de Dados & Big Data", CreatedAt = DateTime.UtcNow },
            new Professor { Name = "Prof. Me. Marcos Vinícius", Email = "marcos.vinicius@ucsal.edu.br", Department = "Sistemas de Informação", Specialization = "Segurança da Informação & Defesa Cibernética", CreatedAt = DateTime.UtcNow },
            new Professor { Name = "Profa. Dra. Sandra Souza", Email = "sandra.souza@ucsal.edu.br", Department = "Redes de Computadores", Specialization = "Sistemas Distribuídos & Cloud Computing", CreatedAt = DateTime.UtcNow },
            new Professor { Name = "Prof. Dr. Roberto Mendes", Email = "roberto.mendes@ucsal.edu.br", Department = "Engenharia de Software", Specialization = "Arquitetura & Engenharia de Requisitos", CreatedAt = DateTime.UtcNow },
            new Professor { Name = "Profa. Me. Patricia Lima", Email = "patricia.lima@ucsal.edu.br", Department = "Design Digital", Specialization = "Interação Humano-Computador & UX Design", CreatedAt = DateTime.UtcNow },
            new Professor { Name = "Prof. Dr. Fernando Costa", Email = "fernando.costa@ucsal.edu.br", Department = "Matemática Aplicada", Specialization = "Algoritmos Avançados & Complexidade", CreatedAt = DateTime.UtcNow },
            new Professor { Name = "Profa. Dra. Letícia Ramos", Email = "leticia.ramos@ucsal.edu.br", Department = "Governança de TI", Specialization = "Gestão de Projetos & Métodos Ágeis", CreatedAt = DateTime.UtcNow }
        );
        context.SaveChanges();

        // Adiciona avaliações de teste anônimas para cada um dos professores
        var savedProfessors = context.Professors.ToList();
        var random = new Random();
        var comments = new[]
        {
            "Excelente didática, o professor domina completamente o assunto e traz exemplos práticos reais.",
            "Professor muito atencioso, tira dúvidas sempre que solicitado e as aulas são bem dinâmicas.",
            "O conteúdo é bastante denso, mas a forma de ensinar facilita o aprendizado. Recomendo!",
            "Muito bom! Um dos melhores docentes do curso.",
            "As explicações são claras, mas exige bastante dedicação nos projetos práticos. Excelente profissional.",
            "Aulas muito bem estruturadas e organizadas. Os materiais disponibilizados são excelentes.",
            "Avaliação justa e condizente com o que é ministrado em sala. Muito prestativo.",
            "Consegue prender a atenção da turma do início ao fim da aula. Didática excelente!"
        };

        foreach (var prof in savedProfessors)
        {
            // Adiciona de 2 a 4 avaliações para cada professor
            int numEvaluations = random.Next(2, 5); 
            for (int i = 0; i < numEvaluations; i++)
            {
                var score = random.Next(3, 6); // Notas 3, 4 ou 5
                decimal finalScore = score == 5 ? 5.0m : score + (random.Next(0, 2) == 1 ? 0.5m : 0.0m);
                var comment = random.Next(0, 4) > 0 ? comments[random.Next(comments.Length)] : null; // 75% de chance de ter comentário

                // Gera um token anônimo de teste usando IDs fictícios de alunos (100 + i + prof.Id * 10)
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var input = $"{(100 + i + prof.Id * 10)}_{prof.Id}_UcsalSecureEvaluationSalt123!";
                    var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                    var hash = sha256.ComputeHash(bytes);
                    var token = Convert.ToHexString(hash).ToLower();

                    context.Evaluations.Add(new Evaluation
                    {
                        ProfessorId = prof.Id,
                        Score = finalScore,
                        Comment = comment,
                        AnonymousToken = token,
                        StudentId = null, // Estritamente anônimo
                        EvaluationDate = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
        context.SaveChanges();
    }
}

app.Run();