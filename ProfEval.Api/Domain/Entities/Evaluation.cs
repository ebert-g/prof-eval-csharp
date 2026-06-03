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
