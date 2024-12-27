using SampleApi;

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
app.MapSwagger();

app.Run();
