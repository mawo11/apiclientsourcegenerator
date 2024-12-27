using SampleApi.Client.Contracts;

namespace SampleApi.Client;

[ApiClientGenerator]
public partial class SampleApiClient
{
	public SampleApiClient(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	[Get("/")]
	public partial Task<string> GetHelloAsync();

	[Get("/weatherforecast")]
	public partial Task<WeatherForecast[]> GetWeatherForecastsAsync();

	[Get("/hello")]
	public partial Task NotExistingActionAsync();

	[Get("/ping")]
	public partial Task<bool> PingAsync();

	[Get("/ping")]
	public partial Task PingAsync(System.Threading.CancellationToken cancellationToken);

	[Get("/ping-bad")]
	public partial Task<bool> PingBadAsync();

	[Get("/ping-bad")]
	[ThrowsExceptions]
	public partial Task<bool> PingBad2Async();

	private partial void LogError(string path, Exception e)
	{
		Console.WriteLine($"{path}: {e}");
	}
}

