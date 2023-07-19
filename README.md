# Newscast WebAPI Application

## Requirements

Using ASP.NET Core, implement a RESTful API to retrieve the details of the first n "best stories" from the Hacker News API, where n is specified by the caller to the API.
The Hacker News API is documented here: https://github.com/HackerNews/API .
The IDs for the "best stories" can be retrieved from this URI: https://hacker-news.firebaseio.com/v0/beststories.json .
The details for an individual story ID can be retrieved from this URI: https://hacker-news.firebaseio.com/v0/item/21233041.json (in this case for the story with ID
21233041 )

The API should return an array of the first n "best stories" as returned by the Hacker News API, sorted by their score in a descending order, in the form:

	[
	{
	"title": "A uBlock Origin update was rejected from the Chrome Web Store",
	"uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
	"postedBy": "ismaildonmez",
	"time": "2019-10-12T13:43:01+00:00",
	"score": 1716,
	"commentCount": 572
	},
	{ ... },
	{ ... },
	{ ... },
	...
	]

In addition to the above, your API should be able to efficiently service large numbers of requests without risking overloading of the Hacker News API.
You should share a public repository with us, that should include a README.md file which describes how to run the application, any assumptions you have made, and
any enhancements or changes you would make, given the time.

## Solution

The solution presented here is called "Newscast API" and is a .NET 7.0 Solution that hosts a WebAPI 
providing a single endpoint. This endpoint returns the best N stories from the Hacker News API :

     'http://localhost:5118/api/stories/' +  the number of stories to be returned
	 e.g. 'http://localhost:5118/api/stories/20 - returns the best 20 stories in descending order of score
	 
## Managing the Impact of Requests on the Hacker News API

Having fetched the list of the bets 200 story ids, the 200 individual stories are then retrieved.
This requires 200 requests to the Hacker New API to retieve each of the best stories as the score is
contained within the story. Only with these scores can the stories be sorted into descending score order.

A balance is required between having too many requests impacting the Hacker News API at once and between waiting
for too many requests one after the other that would slow down the Newscast API. 
This balance is achieved by 

 1) Breaking the 200 story requests into configurable batches of requests and then awaiting each batch
 2) Caching stories in a concurrent dictionary. An assumption is made here that a story never changes.
 3) Polly Jitter Policy
 
## Story Fetch Batches

When retrieving the stories behnd the retrieved list of 200 best stories the 200 stories are split into batches.
This is to try and avoid overwhelmng the Hacker News API with too many requests at once.

The batch size is configurable and is set within the appsettings file. 
Within the project it is currently set to a batch size of 40. 

## Caching

A ConcurrentDictionary keyed by story identifier provides the cache. A concurrent dictionary is used to 
perform atomic operations given that multiple requests could be using the cache at any one time.

The ConcurrentDictionary is added to with an AddOrUpdate() method. As the assumption has been made that 
stories never change, why not just use an Add() method? The resaon is that between checking to see 
if a story exists within the cache and adding it to the cache a separate request may already have 
updated it. Hence the AddOrUpdate() instead of just Add().

Important Note 1: The cache is contained in the StoryService class. Therfore it is important
that it is added to the dependency container as a Singleton.

Important Note 2: The cache does not remove any stories. This has been left as a 'to do' but is obviously required
for production. Various strategies could be used such as timestamps and removal after a time span expires.

Improvement Suggestion: The cache could be warmed up at startup.

## Polly Jitter Retry Policy

If the Hacker News API becomes overwhelmed with requests and becomes unresponsive or faults and is 
overwhelmed by requests as it comes back online then the server will use a Polly Jitter retry and backoff 
statregy to avoid contributing to overloading the Hacker News API. The Jitter part of the policy adds 
randomness to the backoff time before retrying a request.

## HttpClientFactory

The HttpClientFactory is used for accessing HttpClient objects to provide a central location for the
Polly Retry Policy configuration and to avoid socket exhaustion.

## Swagger

A swagger UI is hosted on  http://localhost:5118/index.html .

## Running the Newscast API with Swagger

Clone the C# Solution 

    git clone https://github.com/wsteven101/Newscast-API.git

Change to the Solution directory

  cd Newscast-API/

On the command line type:

   dotnet run
   
Open a web browser and enter http://localhost:5118/index.html

This will bring up a swagger UI. Select "GET", then "Try Out"

Enter the number of Best Stories to be returned.

Select "Execute" and the results will be returned within a window in the browser.

## Running the Newscast API with Swagger

Clone the C# Solution 

    git clone https://github.com/wsteven101/Newscast-API.git

Change to the Solution directory

  cd Newscast-API/

On the command line type:

   dotnet run
   
Open a web browser and enter http://localhost:5118/api/stories/10 to retrieve the top 10 stories.

