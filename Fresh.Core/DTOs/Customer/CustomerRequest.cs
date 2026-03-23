using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Customer;

public class CustomerRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string DocumentNumber { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(150)]
    public string? ReferenceName { get; set; }

    [MaxLength(20)]
    public string? ReferencePhone { get; set; }

    [Required]
    public int CreatedById { get; set; }

    public bool IsActive { get; set; } = true;
}