using SampleApi.Client;

Console.WriteLine("Test sampple api client");

ISampleApiClient apiClient = new SampleApiClient(new HttpClient()
{
	BaseAddress = new Uri("http://localhost:5000")
});



var result = await apiClient.GetHelloAsync();
Console.WriteLine($"Hello test: {result}");
Console.WriteLine();

Console.WriteLine("Non existing API url test");
await apiClient.NotExistingActionAsync();
Console.WriteLine();


Console.WriteLine("Ping test...");
var pingResult = await apiClient.PingAsync();
Console.WriteLine($"Ping result: {pingResult}");
await apiClient.PingAsync(CancellationToken.None);

Console.WriteLine("Bad ping test...");
try
{
	await apiClient.PingBad2Async();
}
catch (Exception ex)
{
	Console.WriteLine($"Error: {ex.Message}");
}

var pingBadAsyncResult = await apiClient.PingBadAsync();
Console.WriteLine($"Bad ping result: {pingBadAsyncResult}");

var getWeatherForecastsResult = await apiClient.GetWeatherForecastsAsync();
foreach (var forecast in getWeatherForecastsResult)
{
	Console.WriteLine($"Date: {forecast.Date}, Temperature: {forecast.TemperatureC}, Summary: {forecast.Summary}");
}


Console.WriteLine("Query, route, header parameter test");
//
Console.WriteLine("Aplication json test");
//
Console.WriteLine("form field test");

Console.WriteLine("byte array test");
