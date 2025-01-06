using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Shared.Schemas.Web;

public class RuleResponse : IIdentifiable
{
    public required string  Id          { get; set; }
    public required string  Text        { get; set; }
    public required string? Description { get; set; }
}
