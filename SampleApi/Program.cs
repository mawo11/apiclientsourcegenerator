using Microsoft.AspNetCore.Mvc;
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
				Date = DateTime.Now.AddDays(index),
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


app.MapPost("/array-test", (int[] id) =>
{
	return TypedResults.Text("o");
});

app.MapPost("/body-json-test", ([FromBody] WeatherForecast weatherForecast) =>
{
	TestData[] items = [
		 new TestData {
			 Id = 1,
			 Name = $"we: {weatherForecast.Date} -> {weatherForecast.TemperatureC} {weatherForecast.Summary} ",
			  DateTime = DateTime.Now,
			   Ids = [1,2,2,3]
		 },
		 new TestData {
			 Id = 2,
			 Name = "second item",
			  DateTime = DateTime.Now,
			   Ids = [21,22,22,23]
		 }

		];
	return TypedResults.Ok(items);
});

app.MapPost("/api/magic-test", ([FromBody] WeatherForecast weatherForecast) =>
{
	TestData[] items = [
		 new TestData {
			 Id = 1,
			 Name = $"we: {weatherForecast.Date} -> {weatherForecast.TemperatureC} {weatherForecast.Summary} ",
			  DateTime = DateTime.Now,
			   Ids = [1,2,2,3]
		 },
		 new TestData {
			 Id = 2,
			 Name = "second item",
			  DateTime = DateTime.Now,
			   Ids = [21,22,22,23]
		 }

		];
	return TypedResults.Ok(items);
});

app.MapSwagger();

app.Run();


