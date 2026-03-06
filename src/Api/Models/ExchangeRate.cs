using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("ExchangeRates")]
public class ExchangeRate
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string FromCurrency { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string ToCurrency { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Rate { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;
}
