using SampleApi.Client;
using SampleApi.Client.Contracts;

namespace SampleConsole;

[ApiClientGenerator(NetCore = true, ConnectionTooLongWarn = 50)]
public partial class SampleApiClientCore
{
	public SampleApiClientCore(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}


	[Post("/magic-test")]
	public partial Task<TestData[]> PostBodyAsync([Body] WeatherForecast body);

	private partial void LogError(string methodName, string path, Exception e)
	{
		Console.WriteLine($"{methodName} => {path}: {e}");
	}

	private partial void LogError(string methodName, string path, string message)
	{
		Console.WriteLine($"{methodName} => {path}: {message}");
	}

	private partial void LogConnectionTooLongWarning(string methodName, string path, long connectionDuration)
	{
		Console.WriteLine($"connection to long => {methodName} => {path}: time: {connectionDuration}ms");
	}
}

