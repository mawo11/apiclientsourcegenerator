//namespace SampleApi.Client.ClientEx;

//[ApiClientGeneratorAttribute]
//public partial class SampleApiClientEx
//{
//	public SampleApiClientEx(HttpClient httpClient)
//	{
//		_httpClient = httpClient;
//	}

//	[Get("/")]
//	public partial Task<string> GetHelloAsync();


//	[Get("/ping")]
//	public partial Task PingAsync();


//	[Get("/ping")]
//	public partial Task<bool> Ping2Async();


//	[Get("/ping-bad")]
//	public partial Task<bool> PingBadAsync();

//	[Get("/ping-bad")]
//	[ThrowsExceptions]
//	public partial Task<bool> PingBad2Async();

//}

