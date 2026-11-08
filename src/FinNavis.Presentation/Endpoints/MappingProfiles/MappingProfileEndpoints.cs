using FinNavis.Presentation.Endpoints.MappingProfiles.Confirm;
using FinNavis.Presentation.Endpoints.MappingProfiles.Inspect;

namespace FinNavis.Presentation.Endpoints.MappingProfiles;

public static class MappingProfileEndpoints
{
    public static IEndpointRouteBuilder MapMappingProfiles(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder mappingProfiles = endpoints
            .MapGroup("/api/v1/mapping-profiles")
            .WithTags("Mapping profiles");

        mappingProfiles.MapInspectFile();
        mappingProfiles.MapConfirmMappingProfile();

        return endpoints;
    }
}
