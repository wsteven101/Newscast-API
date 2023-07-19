using NewscastApi.Models;
using System.Security.Claims;

namespace NewscastApi.Endpoints
{
    public static class StoryEndpoints
    {
        public static void MapStoryEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/stories");

            group.MapGet("/{noOfStories}", async Task<IEnumerable<Story>> (int noOfStories, IStoryService storyService) =>
            {
                return await storyService.GetBestStories(noOfStories);
            })
            .WithDescription("Fetch the N best stories in order")
            .WithOpenApi();

        }
    }
}
