using Microsoft.Azure.Devices;
using IoTDeviceApi.Controllers;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettingsdev.json", optional: true, reloadOnChange: true);

// Add services to the container
builder.Services.AddControllers()
    .AddApplicationPart(typeof(IoTDeviceApi.Controllers.DeviceController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register RegistryManager using IoT Hub connection string from appsettings.json
builder.Services.AddSingleton<RegistryManager>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetSection("AzureIOTHub")["IoTHubConnectionString"];
    Console.WriteLine($" IoT Hub Connection String: {connectionString}");
    return RegistryManager.CreateFromConnectionString(connectionString);
});

builder.Services.AddSingleton<string>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var deviceConnectionString = config.GetSection("AzureIOTHub")["DeviceConnectionString"];
    
    if (string.IsNullOrWhiteSpace(deviceConnectionString))
    {
        throw new InvalidOperationException("DeviceConnectionString is missing or empty in configuration.");
    }

    Console.WriteLine($"Device Connection String: {deviceConnectionString}");
    return deviceConnectionString!;
});


var app = builder.Build();

// Enable Swagger in all environments (optional: restrict to Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "IoT Device API V1");
    c.RoutePrefix = "swagger"; // Ensures Swagger UI is served at /swagger
});

app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
