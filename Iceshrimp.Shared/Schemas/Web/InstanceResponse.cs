namespace Iceshrimp.Shared.Schemas.Web;

public class InstanceResponse
{
    public required string        AccountDomain { get; set; }
    public required string        WebDomain     { get; set; }
    public required Registrations Registration  { get; set; } 
    public required string?       Name          { get; set; }
}

public class StaffResponse
{
    public required List<UserResponse> Admins     { get; set; }
    public required List<UserResponse> Moderators { get; set; }
}

public enum Registrations
{
    Closed = 0,
    Invite = 1,
    Open   = 2
}