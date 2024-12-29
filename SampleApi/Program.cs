using SampleApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
var summaries = new[]
{
				"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
			};

app.MapGet("/weatherforecast", (HttpContext httpContext) =>
{
	var forecast = Enumerable.Range(1, 5)
		.Select(index =>
			new WeatherForecast
			{
				Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
				TemperatureC = Random.Shared.Next(-20, 55),
				Summary = summaries[Random.Shared.Next(summaries.Length)]
			})
		.ToArray();
	return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/", () => TypedResults.Text("Hello World from api!"));

app.MapGet("/ping", () => TypedResults.NoContent());
app.MapGet("/ping-bad", () => TypedResults.BadRequest());
app.MapPost("/parameters-test/{id}/test/{val}", (int id, string val, string? val2, int? mode, DateTime from, HttpContext httpContext) =>
{
	StringBuilder stringBuilder = new();
	stringBuilder.AppendLine($"Router: id[{id}]] val[{val}] ");
	stringBuilder.AppendLine($"Query: val2[{val2}] mode[{mode}] from[{from}]");

	if (httpContext.Request.Headers.TryGetValue("X-TEST", out var value))
	{
		stringBuilder.AppendLine($"Header[X-TEST]: {value.ToString()}");
	}

	if (httpContext.Request.Headers.TryGetValue("X-ID", out value))
	{
		stringBuilder.AppendLine($"Header[X-ID]: {value.ToString()}");
	}


	return TypedResults.Text(stringBuilder.ToString());

});
app.MapSwagger();

app.Run();


