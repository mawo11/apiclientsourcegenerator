using SampleApi.Client.Contracts;

namespace SampleConsole;

[ApiClientGenerator(NetCore = true, ConnectionTooLongWarn = 50)]
public partial class SampleApiClientCore
{
	public SampleApiClientCore(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	[Post("/parameters-test/{id}/test/{val}")]
	public partial Task<string> GetItemAsync(int id, string val, string val2, [AliasAs("mode")] int val4, [Fmt("yyyy-MM")] DateTime from, [Header("X-TEST: val")] string headerValue, [Header("X-ID:")] int headerId);

	[Post("/body-json-test/")]
	public partial Task<string> PostBodyAsync([Body] WeatherForecast body);

	[Serialization(Serialization.Newtonsoft)]
	[Get("/")]
	public partial Task<string> GetHelloAsync();

	[Get("/weatherforecast")]
	public partial Task<WeatherForecast[]> GetWeatherForecastsAsync();

	[Get("/hello")]
	public partial Task NotExistingActionAsync();

	[Get("/ping")]
	public partial Task<bool> PingAsync();

	[Get("/ping")]
	public partial Task PingAsync(CancellationToken cancellationToken);

	[Get("/ping-bad")]
	public partial Task<bool> PingBadAsync();

	[Get("/ping-bad")]
	[ThrowsExceptions]
	public partial Task<bool> PingBad2Async();

	private partial void LogError(string methodName, string path, Exception e)
	{
		Console.WriteLine($"{methodName} => {path}: {e}");
	}

	private partial void LogConnectionTooLongWarning(string methodName, string path, long connectionDuration)
	{
		Console.WriteLine($"connection to long => {methodName} => {path}: time: {connectionDuration}ms");
	}
}

