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
builder.Services.AddOpenApi();

// CORS configuration to allow requests from the frontend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // TODO: needs to be changed to the actual frontend URL in production
              .AllowAnyMethod()       
              .AllowAnyHeader();                   
    });
});

var app = builder.Build();

// For development.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
}
else // In production
{
    app.UseCors("FrontendPolicy");
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();