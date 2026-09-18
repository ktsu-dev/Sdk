// ASP.NET Core web application (net10.0, OutputType=Exe).
using Web;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/healthz", () => Results.Ok(new HealthStatus("ok")));
app.Run();
