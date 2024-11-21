namespace Iceshrimp.Shared.Schemas.Web;

public class InstanceResponse
{
    public required string AccountDomain { get; set; }
    public required string WebDomain     { get; set; }
    
    // TODO: Add more instance metadata
}

public class StaffResponse
{
    public required List<UserResponse> Admins     { get; set; }
    public required List<UserResponse> Moderators { get; set; }
}