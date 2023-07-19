using NewscastApi.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace NewscastApi.Services
{
    /// <summary>
    /// The StoryService class performs the work of retrieving the
    /// best stories from the Hackercast News API and of processing
    /// the stories
    /// </summary>  
    public class StoryService : IStoryService
    {
         // cache of already fetched Story items
        private ConcurrentDictionary<long,Story> _storiesCache = new ConcurrentDictionary<long,Story>();
        
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StoryService> _logger;
        private readonly IConfiguration _config;
        private readonly int _fetchBatchSize =  1;

        static JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public StoryService(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<StoryService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
            _fetchBatchSize = config.GetValue<int>("Story:FetchBatchSize");
        }
        
        public async Task<IEnumerable<Story>> GetBestStories(int noOfStories)
        {
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                requestUri: "beststories.json");

            var httpClient = _httpClientFactory.CreateClient("NewscastAPI");
            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            httpResponseMessage.EnsureSuccessStatusCode();

            using var contentStream =
                await httpResponseMessage.Content.ReadAsStreamAsync();

            var bestStories = await JsonSerializer.
                DeserializeAsync<long[]>(contentStream, _jsonOptions);
            
            if (bestStories == null)
            {
                return new List<Story>();
            }

            List<Story> stories = await GetStories(bestStories);

            return stories.OrderByDescending(s => s.Score).Take(noOfStories); ;
            
        }
        async Task<List<Story>> GetStories(long[]? storyIds)
        {
            
            List<Story> stories = new List<Story>();
            if (storyIds?.Any() ?? false)
            {
                int storyRequestNo = 1;
                var storyTasks = new List<Task<Story>>();

                foreach (var storyId in storyIds)
                {
                    storyTasks.Add(GetStory(storyId));
                    if (storyRequestNo++ % _fetchBatchSize == 0)
                    {
                        await Task.WhenAll(storyTasks);
                        storyTasks.ForEach(st => stories.Add(st.Result) );

                        _logger.LogInformation($"Sucessfully fetched batch of {storyTasks.Count} Storys");
                        storyTasks = new List<Task<Story>>();
                    }
                }
            }

            _logger.LogInformation($"Fetched {stories.Count} Stories in total");
            
             // logic safety check
            if (stories.GroupBy(s=>s.Id).Count() != stories.Count)
            {
                throw new Exception("Logic error! Detected duplicate Story Ids!");
            }

            return stories;
        }

        async Task<Story> GetStory(long id)
        {

            if (_storiesCache.TryGetValue(id,out Story? story))
            {
                return story;
            }

            var uri = $"item/{id}.json";

            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,    
                uri);

            var httpClient = _httpClientFactory.CreateClient("NewscastAPI");
            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            httpResponseMessage.EnsureSuccessStatusCode();

            using var contentStream =
                await httpResponseMessage.Content.ReadAsStreamAsync();

            story = await JsonSerializer.
                DeserializeAsync<Story>(contentStream, _jsonOptions);

            if (story == null )
            {
                throw new NullReferenceException($"Null Story retrieved for story '{id}' with uri {uri}");
            }

            _storiesCache.AddOrUpdate(id, story, (id,s)=>story);

            return story;
        }
    }
}
