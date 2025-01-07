using SampleApi.Client;
using SampleApi.Client.Contracts;

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
Console.WriteLine();

var pingBadAsyncResult = await apiClient.PingBadAsync();
Console.WriteLine($"Bad ping result: {pingBadAsyncResult}");
Console.WriteLine();

Console.WriteLine("Aplication json test");
var getWeatherForecastsResult = await apiClient.GetWeatherForecastsAsync();
foreach (var forecast in getWeatherForecastsResult)
{
	Console.WriteLine($"Date: {forecast.Date}, Temperature: {forecast.TemperatureC}, Summary: {forecast.Summary}");
}
Console.WriteLine();

Console.WriteLine("Query, route, header parameter test");
var getItemResult = await apiClient.GetItemAsync(1, "val22", "xxxa2", 3434, DateTime.Now, "header test", 2);
Console.WriteLine(getItemResult);
Console.WriteLine();

Console.WriteLine("json upload test");

Console.WriteLine("form field test");
var postResult = await apiClient.PostBodyAsync(new WeatherForecast()
{
	Date = DateTime.Now,
	Summary = "test upload xxxx",
	TemperatureC = 32,
});
Console.WriteLine($"form field result: {postResult}");


Console.WriteLine("conection too long warn");
