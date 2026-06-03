using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProfEval.Api.Domain.Entities;

[Table("students")]
public class Student
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("registration")]
    [StringLength(50)]
    public string? Registration { get; set; }

    [Column("email_verified")]
    public bool EmailVerified { get; set; } = false;

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore]
    public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
}
