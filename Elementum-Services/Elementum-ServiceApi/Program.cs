using Elementum.Infrastructure.Data;
using Elementum_ServiceApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["CONNECTION_STRING"]
    ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection or CONNECTION_STRING.");
builder.Services.AddElementumDbContext(connectionString);
builder.Services.AddScoped<ApiService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
