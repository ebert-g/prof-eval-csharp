# Documentação Completa e Detalhada do Código Refatorado
## Sistema de Avaliação de Professores - UCSAL

Esta documentação fornece uma explicação exaustiva e de baixo nível sobre cada classe, componente, método e folha de estilo que foram refatorados ou adicionados ao projeto. O objetivo é assegurar a completa legibilidade e compreensibilidade da arquitetura unificada adotada.

---

## 📑 Sumário da Arquitetura
O projeto adota o padrão de **Arquitetura Unificada (Monólito Modular Pragmático)** executado sob o **.NET 8**. O servidor atua fornecendo simultaneamente:
1.  **Páginas Dinâmicas (Pre-rendered Pages)**: Razor Pages (`Index.cshtml` e `Index.cshtml.cs`) que servem o HTML inicial da aplicação de página única (SPA).
2.  **API RESTful**: Controladores do ASP.NET Core (`AuthController`, `ProfessorsController`, `EvaluationsController`) para comunicação assíncrona baseada em JSON.
3.  **Frontend Estático (SPA)**: Scripts JavaScript puros estruturados com o padrão **Component Pattern** e estilização via **Vanilla CSS** puro com variáveis globais do ecossistema UCSAL.

---

## 💾 1. Backend: Banco de Dados e Modelagem de Dados

### 📂 Arquivo: `ProfEval.Api/Domain/Entities/Evaluation.cs`
Este arquivo define o modelo de entidade `Evaluation` correspondente à tabela `evaluations` no PostgreSQL através do Entity Framework Core.

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProfEval.Api.Domain.Entities;

[Table("evaluations")]
public class Evaluation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("student_id")]
    public int? StudentId { get; set; }

    [Required]
    [Column("professor_id")]
    public int ProfessorId { get; set; }

    [Column("score")]
    public decimal Score { get; set; }

    [Column("comment")]
    [StringLength(1000)]
    public string? Comment { get; set; }

    [Column("anonymous_token")]
    [StringLength(100)]
    public string? AnonymousToken { get; set; }

    [Column("evaluation_date")]
    public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }

    [ForeignKey("ProfessorId")]
    public virtual Professor? Professor { get; set; }
}
```

#### Explicação Linha por Linha:
*   **Linha 1-2**: Importações dos namespaces de anotações de dados (`DataAnnotations`) do C# para validação física e mapeamento de colunas de banco de dados (`Schema`).
*   **Linha 6**: O atributo `[Table("evaluations")]` instrui o EF Core a mapear esta classe C# para a tabela física chamada `evaluations` no banco.
*   **Linha 9-11**: Define a propriedade `Id`. O atributo `[Key]` marca este campo como a Chave Primária. `[Column("id")]` especifica que o nome da coluna no PostgreSQL será `id` em letras minúsculas.
*   **Linha 13-14**: Define a propriedade `StudentId` como um tipo de dados nulo (`int?`). **Esta é a alteração arquitetural central do anonimato**: antes ela era `int` (obrigatória), o que impedia que a avaliação fosse salva sem vincular o ID do estudante. Agora, ao ser nula, o banco de dados desvincula completamente a chave estrangeira do estudante ao salvar o voto.
*   **Linha 16-18**: Propriedade `ProfessorId` obrigatória (`[Required]`), mapeada para a coluna `professor_id`, estabelecendo a relação de dependência com a tabela de professores.
*   **Linha 20-21**: A nota quantitativa `Score`, que armazena a avaliação de 1 a 5 dada pelo aluno.
*   **Linha 23-25**: Propriedade de comentário de texto (`Comment`). O atributo `[StringLength(1000)]` define o limite rígido no banco de dados para evitar estouro de armazenamento e injeção de grandes blocos de texto.
*   **Linha 27-29**: Propriedade `AnonymousToken` mapeada para a coluna `anonymous_token` com limite de 100 caracteres. Ela guardará o hash `SHA-256` correspondente à assinatura única do estudante para aquele professor específico, agindo como validador de duplicidade de voto de forma anônima.
*   **Linha 31-38**: Propriedades de auditoria temporal (`EvaluationDate`, `CreatedAt`, `UpdatedAt`) para rastreamento de criação e edições das avaliações.
*   **Linha 40-44**: Declaração das propriedades virtuais de navegação (`Student` e `Professor`). Os atributos `[ForeignKey]` apontam quais propriedades inteiras atuam como chaves estrangeiras físicas na tabela. Note que `Student` agora é marcado como opcional/nullable (`Student?`).

---

## ⚙️ 2. Backend: Configuração do DbContext

### 📂 Arquivo: `ProfEval.Api/Infrastructure/Persistence/ProfEvalDbContext.cs` (Trecho de Configuração de Modelos)
Este arquivo configura o pipeline de mapeamento ORM do Entity Framework Core.

```csharp
// Configuração da Entidade Evaluation
modelBuilder.Entity<Evaluation>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Score).HasPrecision(5, 2);
    entity.Property(e => e.Comment).HasMaxLength(1000);
    entity.Property(e => e.AnonymousToken).HasMaxLength(100);
    entity.HasIndex(e => e.AnonymousToken).IsUnique();
    entity.HasIndex(e => new { e.StudentId, e.ProfessorId });
    entity.HasOne(e => e.Student)
        .WithMany(s => s.Evaluations)
        .HasForeignKey(e => e.StudentId)
        .IsRequired(false)
        .OnDelete(DeleteBehavior.SetNull);
    entity.HasOne(e => e.Professor)
        .WithMany(p => p.Evaluations)
        .HasForeignKey(e => e.ProfessorId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

#### Explicação Linha por Linha das Regras de Negócio do Banco:
*   **Linha 3**: Define que a chave primária da entidade `Evaluation` é a propriedade `Id`.
*   **Linha 4**: Configura a propriedade `Score` no banco de dados para usar a precisão `decimal(5,2)`. Isso permite notas com decimais (ex: `4.50`) limitando a 5 dígitos no total e 2 dígitos após a vírgula.
*   **Linha 5-6**: Define os limites máximos de strings nas colunas do banco de dados PostgreSQL.
*   **Linha 7**: Cria um índice único no banco de dados PostgreSQL para a coluna `AnonymousToken` (`IsUnique()`). **Isso é crucial para a integridade de dados**: o banco de dados rejeitará fisicamente qualquer tentativa de inserir mais de um registro contendo a mesma assinatura hash de estudante-professor, prevenindo duplicidade de votos na camada de persistência.
*   **Linha 8**: Cria um índice composto clássico com `StudentId` e `ProfessorId` para otimização de consultas de busca locais.
*   **Linha 9-13**: Configura o relacionamento um-para-muitos da avaliação com o estudante:
    *   `IsRequired(false)`: Define formalmente a chave estrangeira como opcional na tabela física.
    *   `OnDelete(DeleteBehavior.SetNull)`: **Regra de integridade referencial muito importante**: Se um estudante decidir deletar seu cadastro institucional no futuro, todas as avaliações feitas por ele não serão excluídas do sistema. Em vez disso, o campo `student_id` correspondente será alterado para `null`, preservando as estatísticas e notas agregadas dos professores de forma completamente anônima.
*   **Linha 14-17**: Configura a relação com o professor. `OnDelete(DeleteBehavior.Cascade)` garante que se um professor for excluído do cadastro da instituição, todos os votos associados a ele serão deletados em cascata automaticamente.

---

## ⚡ 3. Backend: Controladores de API

### 📂 Arquivo: `ProfEval.Api/Controllers/EvaluationsController.cs`
Este controlador lida com o tráfego RESTful das avaliações e as regras criptográficas do anonimato.

```csharp
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
```

#### Explicação Linha por Linha:
*   **Linha 8-9**: Atributo `[ApiController]` habilita o comportamento de vinculação de modelo automática da API e tratamentos padrão de erro HTTP. `[Route("api/[controller]")]` estabelece que a URL base para este controlador será `/api/evaluations`.
*   **Linha 14-19**: Construtor do controlador injetando o repositório genérico da entidade `Evaluation` e o contexto do banco de dados `ProfEvalDbContext`.
*   **Linha 21-30**: **Método utilitário `ComputeAnonymousToken`**:
    *   **Linha 23**: Inicializa a classe hash criptográfica `SHA256` nativa do C#. O escopo de bloco `using` garante a liberação de recursos de memória ao finalizar.
    *   **Linha 25**: Cria a string a ser criptografada concatenando o `StudentId`, o `ProfessorId` e uma chave de segurança secreta salt (`_UcsalSecureEvaluationSalt123!`). O salt é obrigatório para evitar ataques de força bruta baseados em dicionários de hash comuns (tabelas Rainbow).
    *   **Linha 26**: Codifica a string de entrada para uma matriz de bytes usando o padrão de codificação UTF-8.
    *   **Linha 27**: Computa a hash de bytes baseada no SHA-256.
    *   **Linha 28**: Converte a matriz de bytes em uma string hexadecimal tradicional e muda para minúscula para consistência na busca.
*   **Linha 32-48**: **Método Endpoint `CheckVote` (Verificação de Voto Prévio)**:
    *   **Linha 32**: Mapeia requisições HTTP GET na rota `/api/evaluations/check-vote`. Os parâmetros `studentId` e `professorId` são capturados direto da string de consulta da URL (`[FromQuery]`).
    *   **Linha 35**: Computa o hash de token anônimo com base nos parâmetros recebidos.
    *   **Linha 36-37**: Consulta de forma assíncrona o banco para verificar se existe um registro correspondente na tabela de avaliações cujo `AnonymousToken` bata com a hash calculada.
    *   **Linha 39-40**: Se for nulo, significa que este estudante ainda não avaliou esse professor. Retorna um objeto JSON informando `hasEvaluated: false`.
    *   **Linha 42-46**: Caso contrário, retorna `hasEvaluated: true` repassando a nota (`Score`) e o comentário (`Comment`) previamente gravados no banco para o frontend preencher no formulário para edição.

---

### CONTINUAÇÃO: `EvaluationsController.cs` (Métodos de Salvar e Listar)

```csharp
    [HttpPost]
    public async Task<ActionResult<Evaluation>> Create(Evaluation evaluation)
    {
        if (evaluation.StudentId == null && string.IsNullOrEmpty(evaluation.AnonymousToken))
        {
            return BadRequest("Estudante não identificado.");
        }

        int studentId = evaluation.StudentId ?? 0;
        if (studentId > 0)
        {
            var token = ComputeAnonymousToken(studentId, evaluation.ProfessorId);
            
            var existing = await _context.Evaluations
                .FirstOrDefaultAsync(e => e.AnonymousToken == token);

            if (existing != null)
            {
                existing.Score = evaluation.Score;
                existing.Comment = evaluation.Comment;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.StudentId = null; // Garantia física de desvinculação

                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            else
            {
                var newEval = new Evaluation
                {
                    ProfessorId = evaluation.ProfessorId,
                    Score = evaluation.Score,
                    Comment = evaluation.Comment,
                    AnonymousToken = token,
                    StudentId = null, // Desvinculação estrita de identidade
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
```

#### Explicação Linha por Linha:
*   **Linha 1**: Associa requisições HTTP POST para `/api/evaluations` a este método.
*   **Linha 4-7**: Se o corpo da requisição JSON não fornecer o `StudentId` do estudante ativo, bloqueia e retorna erro HTTP 400 (`BadRequest`).
*   **Linha 9**: Extrai e valida o `StudentId` mapeado temporariamente na transferência.
*   **Linha 12**: Computa a assinatura criptográfica SHA-256 única do estudante.
*   **Linha 14-15**: Verifica de forma assíncrona se existe alguma avaliação contendo este token no banco.
*   **Linha 17-27**: **Fluxo de Edição de Voto (Se o token já existir no banco)**:
    *   **Linha 19-20**: Sobrescreve as propriedades `Score` e `Comment` da entidade localizada com as novas informações fornecidas pelo formulário do estudante.
    *   **Linha 21**: Define a data de atualização do registro para o horário UTC atual.
    *   **Linha 22**: **Segurança Arquitetural**: Força explicitamente `StudentId = null` para garantir que mesmo em um fluxo de atualização de nota, a identidade física do estudante continue limpa e desvinculada na tabela de banco.
    *   **Linha 24**: Salva as alterações de forma assíncrona no PostgreSQL.
    *   **Linha 25**: Retorna o objeto atualizado com HTTP 200 (`Ok`).
*   **Linha 28-48**: **Fluxo de Novo Voto (Se for a primeira vez que o estudante vota neste professor)**:
    *   **Linha 30-39**: Instancia um novo objeto `Evaluation`. O campo `ProfessorId`, a nota `Score` e o comentário são copiados do request. O `AnonymousToken` calculado é associado. E a propriedade `StudentId` **é definida como `null`**. A identidade é desvinculada antes mesmo do comando SQL `INSERT` ser executado.
    *   **Linha 41**: Adiciona o objeto mapeado ao conjunto de dados no contexto de memória do EF Core.
    *   **Linha 42**: Salva o novo registro assincronamente no banco físico.
    *   **Linha 43**: Retorna a resposta HTTP 201 (`Created`) com o localizador URI do novo recurso.

---

### CONTINUAÇÃO: `EvaluationsController.cs` (Consultas Anônimas)

```csharp
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
```

#### Explicação Linha por Linha das Listagens Anônimas:
*   **Linha 1-13**: **Método Endpoint `GetByStudent` (Contagem das avaliações do estudante logado)**:
    *   **Linha 1**: Atribui a rota `/api/evaluations/student/{studentId}` a este endpoint.
    *   **Linha 4**: Busca de forma otimizada os IDs de todos os professores cadastrados no banco de dados.
    *   **Linha 5**: Gera dinamicamente, em memória no servidor, a lista de hashes `AnonymousToken` possíveis que este estudante específico teria para cada professor da lista.
    *   **Linha 7-10**: Consulta a tabela de avaliações procurando registros cujo `AnonymousToken` esteja contido na lista gerada no servidor (`tokens.Contains(e.AnonymousToken)`).
    *   **Segurança de Dados**: Esta é a única forma de obter as avaliações feitas por um determinado estudante. O banco de dados continua com o `StudentId` nulo, mas o servidor consegue deduzir a contagem e as informações do professor calculando as chaves. Nenhum dado de identidade do estudante é trafegado para o frontend, e o relacionamento físico na tabela de avaliações não existe.
    *   **Linha 9**: Junta as informações da tabela de professores utilizando um `Inner Join` com o comando `.Include(e => e.Professor)`. Note que não há o comando `.Include(e => e.Student)`, o que previne qualquer vazamento de e-mail ou nome dos avaliadores.
*   **Linha 15-25**: **Método Endpoint `GetByProfessor` (Comentários e notas de um docente)**:
    *   Busca as avaliações associadas àquele professor para alimentar a lista de comentários. Os estudantes aparecem puramente como nulos, impedindo o rastreamento dos votos.

---

### 📂 Arquivo: `ProfEval.Api/Controllers/ProfessorsController.cs` (Endpoint GetAll Otimizado)

```csharp
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
```

#### Explicação Linha por Linha da Otimização da Listagem Lateral:
*   **Linha 3**: Inicia a consulta na tabela de professores.
*   **Linha 4**: Adiciona o carregamento da lista de avaliações de cada professor (`Evaluations`).
*   **Linha 5-16**: **Projeção de DTO Dinâmica (Select anônimo)**:
    *   Para evitar carregar todo o histórico de logs de avaliações na listagem lateral (o que pesaria na rede), o Entity Framework Core projeta as propriedades de forma direta em um tipo anônimo em SQL.
    *   **Linha 13**: `AverageRating` calcula a nota média em tempo real no banco: se houver avaliações registradas (`p.Evaluations.Any()`), calcula a média simples (`p.Evaluations.Average(...)`) das notas (`Score`) e arredonda a uma casa decimal (`Math.Round(..., 1)`). Caso contrário, define como `0.0`.
    *   **Linha 14**: `EvaluationCount` faz uma contagem simples (`Count`) do volume de votos do docente no banco de dados.
    *   A projeção compila em uma única query SQL otimizada contendo a função agregada `AVG` e `COUNT` agrupada por ID de professor.

---

## ✉️ 4. Backend: Serviços de Comunicação por E-mail (OTP)

### 📂 Arquivo: `ProfEval.Api/Infrastructure/Services/EmailService.cs`
Este serviço lida com a conexão SMTP física e envia os e-mails com códigos OTP HTML ou gera logs em console quando em modo de desenvolvimento.

```csharp
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProfEval.Api.Application.Interfaces;

namespace ProfEval.Api.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(string email, string code)
    {
        var smtpSection = _configuration.GetSection("SmtpSettings");
        var server = smtpSection["Server"];

        if (string.IsNullOrWhiteSpace(server))
        {
            _logger.LogWarning($"[MOCK EMAIL] Para: {email} | Código OTP Gerado: {code}");
            return;
        }
```

#### Explicação Linha por Linha:
*   **Linha 14-19**: Construtor injetando as chaves de configuração do arquivo `appsettings.json` através do `IConfiguration` e o provedor de logs `ILogger`.
*   **Linha 21**: Declaração do método assíncrono `SendVerificationCodeAsync` que recebe o e-mail de destino e o código de acesso gerado.
*   **Linha 23-24**: Acessa a chave `"SmtpSettings"` no arquivo de propriedades JSON e lê o valor de `"Server"`.
*   **Linha 26-30**: **Mecanismo Fallback de Desenvolvimento (Mock)**:
    *   Se a chave de servidor SMTP estiver vazia ou com espaços, o sistema presume que está rodando em ambiente local/teste.
    *   Ele grava um aviso especial no console da aplicação (`_logger.LogWarning`) contendo o código gerado.
    *   Em seguida, encerra o fluxo do método (`return`). Isso garante que o sistema execute perfeitamente sem exigir configuração de SMTP local para testes de desenvolvedor.

---

### CONTINUAÇÃO: `EmailService.cs` (Envio Físico de SMTP)

```csharp
        try
        {
            var port = int.Parse(smtpSection["Port"] ?? "587");
            var senderName = smtpSection["SenderName"] ?? "Avaliação UCSAL";
            var senderEmail = smtpSection["SenderEmail"] ?? "noreply@ucsal.edu.br";
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];
            var enableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true");

            using (var client = new SmtpClient(server, port))
            {
                client.EnableSsl = enableSsl;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(username, password);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = "Código de Acesso - Avaliação UCSAL",
                    Body = $@"
                        <div style='font-family: sans-serif; padding: 24px; max-width: 500px; border: 1px solid #e2e8f0; border-radius: 12px; margin: 0 auto;'>
                            <div style='text-align: center; margin-bottom: 20px;'>
                                <h1 style='color: #002d62; font-size: 24px; margin: 0;'>Avaliação UCSAL</h1>
                                <span style='font-size: 12px; color: #64748b;'>Universidade Católica do Salvador</span>
                            </div>
                            <p style='color: #1e293b; font-size: 15px;'>Olá,</p>
                            <p style='color: #1e293b; font-size: 15px;'>Use o código de acesso abaixo para confirmar o seu login no Sistema de Avaliação de Professores:</p>
                            <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; padding: 18px; border-radius: 8px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #002d62; margin: 24px 0;'>
                                {code}
                            </div>
                            <p style='color: #64748b; font-size: 13px; margin-top: 24px;'>Este código é válido por 15 minutos. Não compartilhe este código com ninguém.</p>
                        </div>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"E-mail de verificação OTP enviado com sucesso para {email}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[SMTP ERROR] Falha ao enviar e-mail OTP para {email}. Código temporário logado: {code}");
        }
    }
}
```

#### Explicação Linha por Linha:
*   **Linha 1**: Inicia o tratamento de erros para capturar falhas de rede, portas bloqueadas ou credenciais SMTP inválidas.
*   **Linha 3-8**: Lê e mapeia as variáveis SMTP de `"SmtpSettings"`. Se alguma chave opcional for nula, define valores padrão seguros (ex: porta `587` e SSL ativo).
*   **Linha 10**: Cria uma instância do objeto de rede de envio de e-mails (`SmtpClient`) conectando ao servidor e à porta configurados. O bloco `using` garante o fechamento da conexão de socket física de rede imediatamente após o uso.
*   **Linha 12**: Define `client.EnableSsl = enableSsl`. Crucial para conexão segura com serviços modernos como Gmail, Outlook, AWS SES ou SendGrid.
*   **Linha 13**: Desativa o uso de credenciais padrão do sistema operacional.
*   **Linha 14**: Associa o usuário e senha SMTP configurados via `NetworkCredential`.
*   **Linha 16-39**: Instancia a mensagem de e-mail (`MailMessage`):
    *   `From`: Endereço e nome de exibição do remetente.
    *   `Subject`: Título da mensagem de e-mail.
    *   `Body`: Estrutura do corpo do e-mail. Utiliza interpolação de strings do C# (`$@"..."`) para escrever um código de marcação HTML estilizado com uma tabela visual limpa centralizada no código de 6 dígitos (`{code}`).
    *   `IsBodyHtml = true`: Permite que o cliente de e-mail (Gmail, Outlook, etc.) interprete e renderize as tags HTML e estilizações CSS em vez de texto puro.
*   **Linha 41**: Adiciona o endereço de e-mail do destinatário (`mailMessage.To.Add(email)`).
*   **Linha 43**: Efetua a entrega assíncrona do e-mail de forma direta (`client.SendMailAsync`).
*   **Linha 47-50**: Tratador de Erro (`catch`). Em caso de erro físico de SMTP, o sistema captura a exceção de rede e registra o erro nos logs internos do servidor, evitando travar a tela ou exibir mensagens brutas de erro de rede C# para o aluno.

---

## 🎨 5. Frontend: Interface de Usuário e Estilização (CSS)

### 📂 Arquivo: `ProfEval.Api/wwwroot/css/styles.css` (Principais Regras do Layout)
Esta folha de estilo gerencia as regras visuais acadêmicas baseadas estritamente na identidade visual da UCSAL.

```css
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');

:root {
  --primary-color: #002d62; /* Azul Institucional UCSAL */
  --primary-hover: #001e43;
  --accent-color: #ffb300; /* Dourado/Amarelo das Estrelas */
  --bg-light: #f8fafc;
  --bg-white: #ffffff;
  --text-primary: #1e293b;
  --text-secondary: #64748b;
  --border-color: #e2e8f0;
  --success-color: #10b981;
  --danger-color: #ef4444;
  --border-radius: 12px;
  --shadow-sm: 0 1px 3px rgba(0, 0, 0, 0.05);
  --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -1px rgba(0, 0, 0, 0.02);
  --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.08), 0 4px 6px -2px rgba(0, 0, 0, 0.04);
  --transition-smooth: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
}
```

#### Explicação Linha por Linha:
*   **Linha 1**: Importa a fonte premium "Inter" diretamente da API do Google Fonts para a folha de estilo.
*   **Linha 3-18**: Declaração do escopo global `:root` que define as variáveis CSS globais:
    *   `--primary-color` define o azul institucional escuro do padrão UCSAL.
    *   `--accent-color` define o dourado/amarelo luminoso das estrelas.
    *   `--text-secondary` e `--border-color` mapeiam os tons de cinzas para textos descritivos secundários e linhas de borda divisória.
    *   `--transition-smooth` define uma curva de interpolação Bézier cúbica para suavizar todas as transições de cor, foco e hover na interface, garantindo um visual de alta qualidade.

---

### CONTINUAÇÃO: `styles.css` (Grid de 2 Colunas e Sidebar Rolável)

```css
/* Layout SPA: 2 Colunas */
.layout-grid {
  display: grid;
  grid-template-columns: 350px 1fr;
  gap: 32px;
  align-items: start;
}

/* Sidebar Rolável */
.sidebar {
  background: var(--bg-white);
  border-radius: var(--border-radius);
  border: 1px solid var(--border-color);
  box-shadow: var(--shadow-sm);
  padding: 20px;
  max-height: calc(100vh - 120px);
  display: flex;
  flex-direction: column;
}

.professor-list {
  overflow-y: auto;
  flex: 1;
  padding-right: 4px;
}
```

#### Explicação Linha por Linha:
*   **Linha 1-6**: Configura o grid de layout principal da SPA:
    *   `display: grid`: Ativa o CSS Grid.
    *   `grid-template-columns: 350px 1fr`: **Layout em 2 colunas**: A coluna da esquerda (sidebar) possui largura fixa de 350 pixels para abrigar confortavelmente os nomes dos docentes. A coluna central/direita (`1fr`) assume de forma flexível todo o espaço de visualização restante na tela.
    *   `gap: 32px`: Adiciona um distanciamento físico de 32 pixels entre as duas colunas para criar áreas de respiro visual.
*   **Linha 8-18**: Configura a barra lateral esquerda:
    *   `max-height: calc(100vh - 120px)`: Rígido limitador de altura vertical com base na altura total da janela de visualização do navegador (`100vh`) descontando a área de altura do cabeçalho fixo (`120px`). Isso impede que a página inteira ganhe rolagem geral.
*   **Linha 20-24**: Barra interna de rolagem da lista de professores:
    *   `overflow-y: auto`: Habilita a barra de rolagem vertical apenas se o conteúdo de cards de professores exceder a área de altura física calculada da barra lateral, mantendo o cabeçalho fixo no topo.

---

### CONTINUAÇÃO: `styles.css` (Seleção de Estrelas Reversas no CSS)

```css
/* Container de Estrelas */
.interactive-stars-container {
  display: flex;
  flex-direction: row-reverse;
  gap: 8px;
  margin-top: 8px;
  justify-content: center;
}

/* Estilo Base dos Botões de Estrela */
.star-interactive-btn {
  font-size: 38px;
  color: #cbd5e1;
  background: none;
  border: none;
  cursor: pointer;
  transition: var(--transition-smooth);
}

.star-interactive-btn:hover {
  transform: scale(1.15);
}

/* Comportamento de Destaque por Irmão Geral */
.star-interactive-btn.active,
.star-interactive-btn.active ~ .star-interactive-btn {
  color: var(--accent-color);
}

/* Comportamento de Hover por Irmão Geral */
.interactive-stars-container:hover .star-interactive-btn {
  color: #cbd5e1 !important;
}

.interactive-stars-container:hover .star-interactive-btn:hover,
.interactive-stars-container:hover .star-interactive-btn:hover ~ .star-interactive-btn {
  color: var(--accent-color) !important;
}
```

#### Explicação Detalhada do Funcionamento Visuo-Espacial das Estrelas:
*   **Linha 3**: `flex-direction: row-reverse`: Este é o segredo do funcionamento puramente via CSS. A propriedade inverte a renderização dos botões: o botão 5 é o primeiro a ser processado no DOM, mas visualmente é desenhado no canto direito da tela. O botão 1 é o último no DOM, mas aparece na extrema esquerda da tela.
*   **Linha 9-16**: Formatação padrão dos botões transparentes sem bordas, com tamanho de fonte aumentado (`38px`) e cor inicial de estrela inativa cinza claro (`#cbd5e1`).
*   **Linha 18**: Adiciona um efeito de micro-animação física de aumento de escala (`scale(1.15)`) ao passar o mouse.
*   **Linha 22-25**: **Ativação por Seleção**:
    *   Quando um botão de estrela é selecionado (ex: Estrela 3), ele recebe a classe `.active` no JavaScript.
    *   O seletor `.star-interactive-btn.active` muda a cor do botão 3 para dourado.
    *   O seletor `.star-interactive-btn.active ~ .star-interactive-btn` localiza e muda a cor de todos os botões de estrelas que aparecem **depois** da Estrela 3 na ordem física do DOM. Como a ordem física é inversa, os botões que vêm após a Estrela 3 no DOM são as Estrelas 2 e 1.
    *   Resultado visual na tela: as estrelas 1, 2 e 3 ficam acesas em dourado, enquanto as estrelas 4 e 5 permanecem cinzas.
*   **Linha 28-35**: **Visualização Temporária por Hover**:
    *   **Linha 28**: Quando o mouse entra no container das estrelas, todas as estrelas do bloco têm a cor forçada para cinza inativo (`#cbd5e1 !important`). Isso desliga visualmente qualquer nota prévia selecionada enquanto o usuário escolhe a nota com o mouse.
    *   **Linha 32**: Quando o mouse está posicionado sobre uma estrela (ex: Estrela 4), ela acende em dourado (`.star-interactive-btn:hover`).
    *   **Linha 33**: Graças ao combinador de irmão geral (`~`), todas as estrelas subsequentes no DOM à Estrela 4 (que são visualmente as Estrelas 3, 2 e 1 na esquerda da tela) também acendem de forma síncrona.
    *   Resultado visual: o usuário vê a avaliação de 1 a 4 acender suavemente ao posicionar o mouse na estrela 4.

---

## 🛠️ 6. Frontend: Orquestrador da Lógica de Autenticação (JavaScript)

### 📂 Arquivo: `ProfEval.Api/wwwroot/js/components/authComponent.js`
Este script coordena a exibição do estado de login, processa e valida os e-mails e coordena o modal de duas etapas (OTP).

```javascript
class AuthComponent {
  constructor() {
    this.currentStudent = null;
    this.pendingEmail = null;
    this.init();
  }

  init() {
    this.loadStoredStudent();
    this.updateAuthUI();
  }

  loadStoredStudent() {
    const stored = localStorage.getItem("currentStudent");
    if (stored) {
      this.currentStudent = JSON.parse(stored);
    }
  }

  showLoginModal() {
    const modal = document.getElementById("loginModal");
    if (modal) {
      modal.style.display = "flex";
      this.showEmailStep();
    }
  }

  hideLoginModal() {
    const modal = document.getElementById("loginModal");
    if (modal) {
      modal.style.display = "none";
    }
  }
```

#### Explicação Linha por Linha:
*   **Linha 6**: Inicialização da propriedade de controle do estudante ativo (`this.currentStudent`) e da memória temporária do e-mail do fluxo de autenticação (`this.pendingEmail`).
*   **Linha 11-14**: Pipeline de inicialização: lê a sessão armazenada no navegador e renderiza o HTML correspondente no cabeçalho.
*   **Linha 16-21**: Busca o JSON serializado do estudante persistido na memória cache de chave-valor do navegador (`localStorage`) para manter o usuário logado entre recarregamentos de página.
*   **Linha 23-29**: Abre o modal alterando a propriedade de estilo CSS para `flex`, centralizando a janela de diálogo na tela e forçando o carregamento do Passo 1 (E-mail).
*   **Linha 31-36**: Fecha a janela do modal na tela definindo o display CSS como `none`.

---

### CONTINUAÇÃO: `authComponent.js` (Modal OTP e Solicitação de Código)

```javascript
  showEmailStep() {
    const emailStep = document.getElementById("loginEmailStep");
    const codeStep = document.getElementById("loginCodeStep");
    if (emailStep && codeStep) {
      emailStep.style.display = "block";
      codeStep.style.display = "none";
    }
    const emailInput = document.getElementById("studentEmail");
    if (emailInput) {
      emailInput.value = this.pendingEmail || "";
      emailInput.focus();
    }
  }

  showCodeStep() {
    const emailStep = document.getElementById("loginEmailStep");
    const codeStep = document.getElementById("loginCodeStep");
    const emailPreview = document.getElementById("emailPreview");
    if (emailStep && codeStep) {
      emailStep.style.display = "none";
      codeStep.style.display = "block";
    }
    if (emailPreview) {
      emailPreview.textContent = this.pendingEmail;
    }
    const codeInput = document.getElementById("verificationCode");
    if (codeInput) {
      codeInput.value = "";
      codeInput.focus();
    }
  }

  deriveNameFromEmail(email) {
    const prefix = email.split('@')[0];
    const parts = prefix.split(/[\._\-]/).filter(p => p.length > 0);
    return parts.map(p => p.charAt(0).toUpperCase() + p.slice(1).toLowerCase()).join(" ");
  }
```

#### Explicação Linha por Linha:
*   **Linha 1-13**: **Navegação do Modal - Passo 1**:
    *   Exibe o bloco div do formulário de e-mail (`display = "block"`) e esconde o bloco div de entrada de código OTP (`display = "none"`).
    *   Foca o cursor automaticamente no campo de e-mail (`emailInput.focus()`).
*   **Linha 15-32**: **Navegação do Modal - Passo 2**:
    *   Inverte os displays oculares escondendo o campo de e-mail e exibindo o campo de digitação do código OTP.
    *   `emailPreview.textContent = this.pendingEmail` imprime na tela o e-mail de destino para que o estudante possa conferir para onde o código foi disparado.
    *   Limpa o input de código e foca o cursor nele.
*   **Linha 34-38**: **Método `deriveNameFromEmail` (Dedução Inteligente de Nome)**:
    *   **Linha 35**: Divide a string do e-mail no caractere `@` e captura apenas a primeira metade (o prefixo de usuário).
    *   **Linha 36**: Divide o prefixo em blocos usando uma expressão regular que busca pontos (`.`), traços (`-`) ou underscores (`_`) (ex: `joao.silva` vira a matriz `["joao", "silva"]`).
    *   **Linha 37**: Mapeia cada palavra da matriz capitalizando a primeira letra (`toUpperCase()`) e convertendo o resto para minúsculas (`toLowerCase()`). Por fim, junta as palavras com um espaço em branco.
    *   Resultado: o e-mail `marcos.de-souza@ucsal.edu.br` resulta no nome limpo `"Marcos De Souza"`, que é registrado na tabela de estudantes na primeira verificação do aluno.

---

### CONTINUAÇÃO: `authComponent.js` (Pipeline de Envio e Validação de OTP)

```javascript
  async handleRequestCode(event) {
    if (event) event.preventDefault();
    const email = document.getElementById("studentEmail")?.value.trim();

    if (!email) {
      showError("Por favor, informe seu e-mail.");
      return;
    }

    if (!email.toLowerCase().endsWith("@ucsal.edu.br")) {
      showError("Acesso exclusivo para e-mails institucionais (@ucsal.edu.br).");
      return;
    }

    const derivedName = this.deriveNameFromEmail(email);

    try {
      showLoading(true);
      const response = await apiService.requestVerificationCode({
        email,
        name: derivedName
      });

      if (response.success) {
        this.pendingEmail = email;
        this.showCodeStep();
        
        const match = response.message.match(/\[CÓDIGO:\s*(\d+)\]/);
        const codeValue = match ? match[1] : null;

        showNotification("Código enviado! Verifique seu console do navegador.");
        console.log("🔐 CÓDIGO DE VERIFICAÇÃO (DEV):", codeValue || response.message);
        
        if (codeValue) {
          const hint = document.querySelector("#loginCodeStep .form-hint");
          if (hint) {
            hint.innerHTML = `Código de teste (Dev): <strong style="color: var(--success-color); font-size: 15px; background: rgba(16, 185, 129, 0.1); padding: 2px 6px; border-radius: 4px;">${codeValue}</strong>`;
          }
        }
      } else {
        showError(response.message || "Erro ao solicitar o código.");
      }
    } catch (error) {
      showError("Erro na conexão com o servidor. Tente novamente.");
      console.error(error);
    } finally {
      showLoading(false);
    }
  }
```

#### Explicação Linha por Linha:
*   **Linha 2**: Previne o envio padrão do formulário do HTML, o que causaria recarregamento forçado do navegador (`preventDefault()`).
*   **Linha 10-13**: **Validação no Cliente do Domínio UCSAL**: Verifica se a string digitada termina estritamente com `@ucsal.edu.br`. Rejeita qualquer outro domínio de e-mail comercial (como gmail, hotmail, etc.) diretamente na interface para poupar processamento da API.
*   **Linha 15**: Deduze o nome a partir do e-mail.
*   **Linha 19-22**: Dispara a chamada HTTP POST assíncrona para a API da rota `/api/auth/request-code`.
*   **Linha 24-27**: Em caso de sucesso, armazena o e-mail solicitado e transiciona o formulário do modal para o Passo 2 de inserção de código OTP.
*   **Linha 29-30**: **Extração de código de desenvolvimento**: Executa uma verificação Regex buscando um padrão `[CÓDIGO: XXXXXX]` dentro do texto da mensagem enviada pelo servidor. Se encontrar, captura o número.
*   **Linha 32-33**: Grava nos logs do console do desenvolvedor do navegador o código capturado (`console.log`).
*   **Linha 35-40**: **Injeção Dinâmica na Interface**: Se o código de desenvolvimento for extraído com sucesso, localiza o elemento de dica HTML (`.form-hint`) dentro do bloco do formulário de código e insere um componente visual de destaque exibindo o código de 6 dígitos. Isso permite validar e entrar no sistema sem precisar abrir o console F12 ou olhar o terminal.
*   **Linha 41-48**: Em caso de erro na API ou falha de rede física, exibe a mensagem de aviso na janela de diálogo.

---

### CONTINUAÇÃO: `authComponent.js` (Confirmação e Conclusão de Login)

```javascript
  async handleVerifyCode(event) {
    if (event) event.preventDefault();
    const code = document.getElementById("verificationCode")?.value.trim();

    if (!code || code.length !== 6) {
      showError("O código de verificação deve ter 6 dígitos.");
      return;
    }

    if (!this.pendingEmail) {
      showError("E-mail pendente não encontrado. Digite o e-mail novamente.");
      this.showEmailStep();
      return;
    }

    try {
      showLoading(true);
      const response = await apiService.verifyCode({
        email: this.pendingEmail,
        code: code
      });

      if (response.success && response.student) {
        this.currentStudent = response.student;
        localStorage.setItem("currentStudent", JSON.stringify(this.currentStudent));

        // Dispara evento para outros componentes reagirem à mudança de login
        window.dispatchEvent(new CustomEvent("authChanged", { detail: this.currentStudent }));

        this.hideLoginModal();
        this.updateAuthUI();
        showNotification("Login efetuado com sucesso!");
      } else {
        showError(response.message || "Código inválido ou expirado.");
      }
    } catch (error) {
      showError("Erro na verificação. Tente novamente.");
      console.error(error);
    } finally {
      showLoading(false);
    }
  }

  handleLogout() {
    localStorage.removeItem("currentStudent");
    this.currentStudent = null;
    this.pendingEmail = null;

    window.dispatchEvent(new CustomEvent("authChanged", { detail: null }));
    this.updateAuthUI();
    showNotification("Você saiu da sessão.");
  }
```

#### Explicação Linha por Linha:
*   **Linha 2-8**: Captura o código digitado e valida se contém exatamente 6 dígitos numéricos.
*   **Linha 16-19**: Dispara chamada HTTP POST assíncrona enviando o e-mail armazenado no passo 1 e o código OTP digitado no passo 2 para a API `/api/auth/verify-code`.
*   **Linha 21-23**: Caso a verificação seja confirmada no banco:
    *   Armazena o objeto de dados do estudante retornado pela API em `this.currentStudent`.
    *   Converte o objeto do estudante em uma string JSON e salva no cache local (`localStorage.setItem`) sob a chave `"currentStudent"`.
*   **Linha 26**: **Arquitetura de Eventos do Frontend (Event-Driven SPA)**:
    *   Dispara um evento customizado global de JavaScript chamado `"authChanged"` repassando as informações do estudante recém-logado em `detail`.
    *   **Isso substitui recarregamentos manuais de página**: O componente de avaliação (`evaluationComponent.js`) escuta este evento global de forma passiva e redesenha automaticamente o módulo de avaliação interativo liberando o controle de estrelas para o aluno votar sem precisar atualizar a SPA.
*   **Linha 28-30**: Fecha o modal, atualiza o layout visual do cabeçalho da SPA e gera a notificação toast verde de sucesso.
*   **Linha 40-49**: **Método `handleLogout`**:
    *   Limpa todas as referências na memória cache do navegador (`localStorage.removeItem`) e redefine as propriedades internas como nulas.
    *   Dispara o mesmo evento global `"authChanged"` contendo `detail: null`. O componente de avaliação capta o logout em tempo real e substitui imediatamente as estrelas de avaliação interativa pela caixa amarela de bloqueio *"Para avaliar realize o login"*.

---

## 👨‍🏫 7. Frontend: Renderização dos Docentes e Notas Médias

### 📂 Arquivo: `ProfEval.Api/wwwroot/js/components/professorsComponent.js`
Este script gerencia os cards de lista dos professores na barra lateral esquerda e calcula cores consistentes baseadas em algoritmo hash.

```javascript
class ProfessorsComponent {
  constructor() {
    this.professors = [];
    this.selectedProfessor = null;
    this.filteredProfessors = [];
    this.init();
  }

  async init() {
    this.setupEventListeners();
    await this.loadProfessors();
  }

  setupEventListeners() {
    const searchInput = document.getElementById("searchProfessor");
    searchInput?.addEventListener("input", (e) => this.handleSearch(e));

    window.addEventListener("evaluationSubmitted", () => this.loadProfessors(false));
  }
```

#### Explicação Linha por Linha:
*   **Linha 11-14**: Pipeline de inicialização que cadastra eventos no DOM e puxa a listagem inicial de dados da API.
*   **Linha 16-21**: **Registro de Eventos Reactivos**:
    *   Adiciona ouvinte de entrada de dados no campo de pesquisa (`handleSearch`).
    *   Adiciona um ouvinte para o evento customizado `"evaluationSubmitted"`. Sempre que o aluno envia ou edita uma nota com sucesso na parte central da tela, a barra lateral recarrega os professores de forma assíncrona (`this.loadProfessors(false)`) sem exibir a tela de carregamento inteira, atualizando a nota média desenhada no card do docente na barra lateral em tempo real.

---

### CONTINUAÇÃO: `professorsComponent.js` (Geração Consistente de Cores e Iniciais)

```javascript
  getInitials(name) {
    if (!name) return "?";
    const cleanName = name.replace(/^(Prof\.\s+|Profa\.\s+)/i, "").trim();
    const parts = cleanName.split(/\s+/);
    if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
    return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
  }

  getColorForName(name) {
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    // Tons de azul e cinza institucionais baseados no nome do professor
    const hue = Math.abs(hash % 40) + 200; // Hue entre 200 (azul) e 240 (azul escuro)
    const saturation = Math.abs(hash % 20) + 45; // Saturação entre 45% e 65%
    const lightness = Math.abs(hash % 15) + 35; // Lightness entre 35% e 50%
    return `hsl(${hue}, ${saturation}%, ${lightness}%)`;
  }
```

#### Explicação Linha por Linha:
*   **Linha 1-7**: **Método `getInitials` (Extração de Iniciais de Título)**:
    *   **Linha 3**: Remove os prefixos acadêmicos de tratamento como `"Prof. "` ou `"Profa. "` usando expressões regulares insensíveis a maiúsculas/minúsculas.
    *   **Linha 4**: Divide a string de nome limpa em matriz de palavras.
    *   **Linha 5**: Se for um nome único, captura a primeira letra em maiúscula.
    *   **Linha 6**: Caso contrário, concatena a primeira letra do primeiro nome com a primeira letra do último sobrenome (ex: `"Ana Beatriz"` resulta em `"AB"`, `"Sandra Souza"` resulta em `"SS"`).
*   **Linha 9-19**: **Método `getColorForName` (Hashing de Cor Dinâmica)**:
    *   Este método serve para criar avatares coloridos de forma automática sem precisar de imagens salvas. A cor precisa ser consistente (o mesmo professor sempre exibe a mesma cor do círculo, mesmo reiniciando o site).
    *   **Linha 10-13**: Loop sobre cada caractere da string do nome do professor. Converte cada caractere para seu valor numérico ASCII (`charCodeAt`) e acumula na variável inteira `hash` usando operações bit-a-bit para embaralhar a assinatura numérica.
    *   **Linha 15**: Calcula o tom (Hue) no modelo de cor HSL. Limita o resto da divisão por 40 e soma 200. Isso garante um espectro de cor azulado e sóbrio (entre 200 e 240 graus no círculo de cores), combinando com as cores institucionais da UCSAL.
    *   **Linha 16-17**: Calcula saturação moderada e brilho sutilmente escurecido (lightness de 35% a 50%) para assegurar que a inicial desenhada em fonte branca por cima tenha contraste adequado e legibilidade (Acessibilidade WCAG).

---

### CONTINUAÇÃO: `professorsComponent.js` (Renderização de Cards HTML)

```javascript
  render() {
    const container = document.getElementById("professorList");
    if (!container) return;

    if (this.filteredProfessors.length === 0) {
      container.innerHTML =
        '<div class="loading-state">Nenhum docente encontrado</div>';
      return;
    }

    container.innerHTML = this.filteredProfessors
      .map(
        (prof) => {
          const initials = this.getInitials(prof.name);
          const avatarColor = this.getColorForName(prof.name);
          const ratingText = prof.averageRating > 0 ? prof.averageRating.toFixed(1) : "—";
          const hasRating = prof.averageRating > 0;

          return `
            <div class="professor-card ${this.selectedProfessor?.id === prof.id ? "active" : ""}" 
                 data-id="${prof.id}"
                 onclick="professorsComponent.selectProfessor(${prof.id})">
                <div class="prof-avatar-circle" style="background-color: ${avatarColor}">
                    ${initials}
                </div>
                <div class="prof-info-block">
                    <div class="prof-name-text">${prof.name}</div>
                    <div class="prof-dept-text">${prof.department || "Docente"}</div>
                </div>
                <div class="prof-rating-badge ${hasRating ? 'has-rating' : 'no-rating'}">
                    <span class="star-badge-icon">★</span>
                    <span class="star-badge-value">${ratingText}</span>
                </div>
            </div>
          `;
        }
      )
      .join("");
  }
```

#### Explicação Linha por Linha:
*   **Linha 1-8**: Validações de segurança do DOM. Se a busca de filtragem retornar vazia, insere a tela com a mensagem explicativa de docente não localizado.
*   **Linha 10-34**: **Mapeamento de Lista em Templates Literários**:
    *   Varre a matriz de professores filtrados (`filteredProfessors.map`).
    *   **Linha 13-14**: Extrai as iniciais de avatar e calcula a cor HSL consistente para o card.
    *   **Linha 15**: Formata a nota decimal retornada pela API agrupada a uma casa decimal (ex: `4.5`), ou desenha um traço longo (`"—"`) caso o professor ainda possua contagem zero de votos no sistema.
    *   **Linha 20-21**: Atribui a classe `.active` no card se o professor selecionado no estado corresponder ao ID do card iterado, colorindo a borda esquerda com o azul institucional da UCSAL.
    *   **Linha 23-25**: Define a tag div circular de avatar aplicando a cor gerada via estilo CSS dinâmico (`style="background-color: ${avatarColor}"`) e inserindo a string de iniciais.
    *   **Linha 30-33**: Insere a tag div com a nota e estrela no canto direito do card. Dependendo de `hasRating`, adiciona uma classe CSS para colorir o fundo do badge de dourado ou cinza neutro.

---

## 🌟 8. Frontend: Painel Central e Avaliação Interativa

### 📂 Arquivo: `ProfEval.Api/wwwroot/js/components/evaluationComponent.js`
Este script lida com a montagem do perfil detalhado do professor na área central e controla a lógica do formulário de envio de notas.

```javascript
class EvaluationComponent {
  constructor() {
    this.selectedProfessor = null;
    this.currentStudent = null;
    this.selectedRating = 0;
    this.hasEvaluated = false;
    this.previousRating = 0;
    this.previousComment = "";
    this.evaluations = [];
    this.init();
  }

  init() {
    this.setupEventListeners();
    this.loadStoredStudent();
  }

  setupEventListeners() {
    window.addEventListener("professorSelected", (e) => this.onProfessorSelected(e));
    window.addEventListener("authChanged", (e) => this.onAuthChanged(e));
  }
```

#### Explicação Linha por Linha:
*   **Linha 3-9**: Estado de gerenciamento do componente:
    *   `selectedProfessor`: Dados do professor selecionado ativo.
    *   `selectedRating`: Nota numérica temporária (1 a 5) selecionada no controle interativo de estrelas.
    *   `hasEvaluated`: Flag binária indicando se o estudante logado já possui uma avaliação gravada no banco para este docente.
*   **Linha 18-21**: **Configuração dos Ouvintes Globais**:
    *   `professorSelected`: Sempre que o card de um professor é clicado na lista da esquerda, este evento é disparado, fazendo com que o painel central recarregue as notas médias do professor.
    *   `authChanged`: Disparado sempre que há login ou logout. Recarrega as informações do painel central para destravar o formulário interativo de estrelas ou escondê-lo imediatamente sob o aviso amigável de login.

---

### CONTINUAÇÃO: `evaluationComponent.js` (Validação de Voto Existente)

```javascript
  async loadProfessorEvaluationData() {
    if (!this.selectedProfessor) return;

    try {
      showLoading(true);
      // Carrega todas as avaliações deste professor para as estatísticas
      this.evaluations = await apiService.getEvaluationsByProfessor(this.selectedProfessor.id);
      
      // Se estiver logado, verifica se o aluno já avaliou este professor
      if (this.currentStudent) {
        const check = await apiService.checkVote(this.currentStudent.id, this.selectedProfessor.id);
        if (check.hasEvaluated) {
          this.hasEvaluated = true;
          this.previousRating = check.score;
          this.previousComment = check.comment || "";
          
          this.selectedRating = check.score;
        }
      }
      this.render();
    } catch (error) {
      console.error("Erro ao carregar dados de avaliação:", error);
      this.evaluations = [];
      this.render();
    } finally {
      showLoading(false);
    }
  }
```

#### Explicação Linha por Linha:
*   **Linha 6**: Faz uma requisição assíncrona para a API `/api/evaluations/professor/{id}` buscando todas as avaliações anônimas deste professor para listar comentários de estudantes na parte inferior do perfil.
*   **Linha 9-19**: **Fluxo Reativo de Edição de Voto**:
    *   **Linha 9**: Se o estudante estiver autenticado, inicia a rotina de validação de duplicidade.
    *   **Linha 10**: Faz a chamada assíncrona para o endpoint do controlador `apiService.checkVote` passando o ID do aluno logado e o ID do professor em exibição.
    *   **Linha 11-13**: Se o retorno contiver `hasEvaluated: true`, significa que já existe um voto dele no banco de dados. O componente armazena os valores anteriores (`score` e `comment`) na sua memória de estado e define a flag `this.hasEvaluated` como verdadeira.
    *   **Linha 16**: Pré-define as estrelas interativas de entrada na nota anterior dada pelo aluno, proporcionando uma experiência de edição sem fricção.
*   **Linha 20**: Redesenha o HTML do painel central.

---

### CONTINUAÇÃO: `evaluationComponent.js` (Processamento de Envio da Nota e Comentário)

```javascript
  async submitEvaluation() {
    if (!this.currentStudent) {
      showError("Você precisa fazer login para avaliar.");
      return;
    }

    if (!this.selectedProfessor) {
      showError("Selecione um professor.");
      return;
    }

    if (this.selectedRating === 0) {
      showError("Selecione uma nota clicando nas estrelas.");
      return;
    }

    const comment = document.getElementById("evaluationComment")?.value.trim() || "";

    try {
      showLoading(true);

      const evaluation = {
        studentId: this.currentStudent.id,
        professorId: this.selectedProfessor.id,
        score: this.selectedRating,
        comment: comment
      };

      await apiService.createEvaluation(evaluation);

      // Dispara evento para atualizar a lista lateral de professores (e suas médias)
      window.dispatchEvent(new CustomEvent("evaluationSubmitted"));

      // Recarrega os dados locais atualizados
      await this.loadProfessorEvaluationData();
      
      showNotification(this.hasEvaluated ? "Avaliação atualizada com sucesso!" : "Avaliação enviada com sucesso!");
    } catch (error) {
      showError("Erro ao enviar avaliação: " + error.message);
      console.error(error);
    } finally {
      showLoading(false);
    }
  }
```

#### Explicação Linha por Linha:
*   **Linha 2-15**: Validações de segurança antes do envio. Rejeita o clique se não houver nota numérica marcada nas estrelas (`selectedRating === 0`).
*   **Linha 17**: Captura o comentário escrito no campo de texto limpo de espaços laterais vazios (`trim()`).
*   **Linha 22-27**: Monta o objeto JSON `evaluation` estruturado contendo a nota quantitativa das estrelas interativas, o comentário de texto e as credenciais.
*   **Linha 29**: Faz a chamada HTTP POST assíncrona enviando o JSON. No backend, a API intercepta, desvincula o `studentId` do registro na hora de persistir na tabela de banco, mantendo apenas a chave de token hash criptográfico SHA-256 e protegendo o anonimato do eleitor.
*   **Linha 32**: Dispara o evento global customizado `"evaluationSubmitted"`. A barra lateral escuta este sinal e recarrega os dados atualizados de nota média agregada sem mexer na tela ativa do usuário.
*   **Linha 35**: Recarrega as notas e comentários do professor atual para desenhar o comentário recém-enviado de forma instantânea.
*   **Linha 37**: Exibe o toast informando se a operação foi uma edição de voto anterior ou uma nova avaliação.

---

### CONTINUAÇÃO: `evaluationComponent.js` (Renderização de Estrelas Estáticas e Interativas no HTML)

```javascript
  renderStars(rating, isReadOnly = true) {
    const stars = [];
    const roundedRating = Math.round(rating);
    
    if (isReadOnly) {
      for (let i = 1; i <= 5; i++) {
        stars.push(`<span class="star-icon-read ${i <= roundedRating ? "filled" : "empty"}">★</span>`);
      }
    } else {
      // Loop reverso de 5 para 1 para habilitar o truque de hover/active do CSS
      for (let i = 5; i >= 1; i--) {
        stars.push(`
          <button type="button" 
                  class="star-interactive-btn ${i === this.selectedRating ? "active" : ""}" 
                  data-rating="${i}"
                  onclick="evaluationComponent.setRating(${i}); return false;">
              ★
          </button>
        `);
      }
    }
    return stars.join("");
  }
```

#### Explicação Detalhada da Renderização das Estrelas:
*   **Linha 5-9**: **Caso seja apenas leitura (`isReadOnly = true`)**:
    *   Loop progressivo clássico do 1 ao 5.
    *   Determina se a posição da estrela é menor ou igual à nota média arredondada (`i <= roundedRating`). Se for, adiciona a classe CSS `.filled` (que pinta de amarelo dourado), caso contrário adiciona `.empty` (que desenha a borda cinza).
*   **Linha 10-23**: **Caso seja interativo para votação (`isReadOnly = false`)**:
    *   **Linha 12**: **Otimização do Loop**: O loop itera de trás para frente, iniciando em `5` e finalizando em `1` (`i >= 1; i--`).
    *   **Linha 15**: O botão ganha a classe CSS `.active` apenas se o índice da estrela corresponder exatamente ao valor numérico fixado no estado do componente (`i === this.selectedRating`).
    *   **O Truque de Cascata**: O CSS é o encarregado de propagar a cor dourada para todas as estrelas à esquerda da estrela ativa através do combinador de irmãos gerais (`~`).
    *   `return false;` no manipulador do clique previne o comportamento padrão do botão no navegador, evitando submissões inadequadas de formulário.

---

## 🏁 Conclusão
O sistema opera combinando validações na camada de interface (JS/CSS) e na camada de banco de dados (Índices únicos criptográficos e colunas anuladas), garantindo que as avaliações sejam processadas de forma rápida, interativa e estritamente anônimas sob o ecossistema UCSAL.
