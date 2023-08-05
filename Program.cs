using LogFetcher.Services.Implementation;
using LogFetcher.Services.Interface;
using LogFetcher.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin() // Replace with your client's domain
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});
builder.Services.AddControllers();
builder.Services.AddScoped<ILogFetcherService, LogFetcherService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors();
app.UseDeveloperExceptionPage();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
