using Serilog;
using StudioB2B.Web.Extensions;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

var app = builder.Build();
app.ConfigurePipeline();
app.Run();
