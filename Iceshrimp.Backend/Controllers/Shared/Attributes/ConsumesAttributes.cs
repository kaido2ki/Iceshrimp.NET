using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers.Shared.Attributes;

public class ConsumesActivityStreamsPayload() : ConsumesAttribute("application/activity+json",
                                                                  "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"");