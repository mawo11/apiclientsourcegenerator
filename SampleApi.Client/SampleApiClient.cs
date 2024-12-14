using SampleApi.Client.Contracts;

namespace SampleApi.Client;

[ApiClientGenerator]
public partial class SampleApiClient
{
	public SampleApiClient(HttpClient httpClient)
	{
		//_httpClient = httpClient;
	}

	[Get("/")]
	public partial Task<string> GetHelloAsync();


	[Get("/ping")]
	public partial Task PingAsync();

	[Get("/weatherforecast")]
	public partial Task<WeatherForecast[]> GetWeatherForecastsAsync();


	[Get("/hello")]
	public partial Task HelloAsync(int param1, string param2, int[] params3);


	//[Get("/ping")]
	//public partial Task<bool> Ping2Async();


	//[Get("/ping-bad")]
	//public partial Task<bool> PingBadAsync();

	//[Get("/ping-bad")]
	//[ThrowsExceptions]
	//public partial Task<bool> PingBad2Async();

}

