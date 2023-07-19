using NewscastApi.Models;

namespace NewscastApi.Interfaces
{
    internal interface IStoryService
    {
        public Task<IEnumerable<Story>> GetBestStories(int noOfStories);
    }
}
