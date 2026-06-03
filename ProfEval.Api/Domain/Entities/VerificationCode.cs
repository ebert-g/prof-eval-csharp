using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProfEval.Api.Domain.Entities;

[Table("verification_codes")]
public class VerificationCode
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("email")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("code")]
    [StringLength(6)]
    public string Code { get; set; } = string.Empty;

    [Column("is_used")]
    public bool IsUsed { get; set; } = false;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }
}
