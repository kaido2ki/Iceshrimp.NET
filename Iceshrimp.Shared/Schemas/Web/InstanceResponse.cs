namespace Iceshrimp.Shared.Schemas.Web;

// TODO: Instance Response for /api/iceshrimp/instance
public class InstanceResponse { }

public class StaffResponse
{
    public required List<UserResponse> Admins     { get; set; }
    public required List<UserResponse> Moderators { get; set; }
}