using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProfEval.Api.Domain.Entities;

[Table("professors")]
public class Professor
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

    [Column("department")]
    [StringLength(255)]
    public string? Department { get; set; }

    [Column("specialization")]
    [StringLength(255)]
    public string? Specialization { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore]
    public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
}
