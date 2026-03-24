namespace Fresh.Core.DTOs.Permission;

public class UpdateUserPermissionsRequest
{
    public Dictionary<string, bool> Pages { get; set; } = new();
}
