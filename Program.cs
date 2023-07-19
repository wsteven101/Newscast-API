global using NewscastApi.Interfaces;
global using NewscastApi.Endpoints;
global using NewscastApi.Services;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;

// build and run the Newscast WebAPI host

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddSingleton<IStoryService,StoryService>();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("NewscastAPI", client =>
{
    client.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
})
.AddPolicyHandler( RetryPolicy() ); 

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

app.MapStoryEndpoints();

app.Run();


// Return the Polly Jitter Retry Policy
IAsyncPolicy<HttpResponseMessage> RetryPolicy()
{
    var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);

    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(delay);

    return retryPolicy;
}