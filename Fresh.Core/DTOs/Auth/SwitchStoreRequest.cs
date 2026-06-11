using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Auth;

public class SwitchStoreRequest
{
    [Required]
    public int StoreId { get; set; }
}
