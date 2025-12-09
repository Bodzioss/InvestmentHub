using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Contracts.Users;

public class UpdateUserRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
}
