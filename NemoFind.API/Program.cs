using Microsoft.EntityFrameworkCore;
using NemoFind.Core.Interfaces;
using NemoFind.Infrastructure.Data;
using NemoFind.Infrastructure.Services;
using NemoFind.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1.01", new() { Title = " NemoFind API", Version = "v1.01" });
});

// SQLite Database
builder.Services.AddDbContext<NemoFindDbContext>(options =>
    options.UseSqlite("Data Source=nemofind.db"));

// Register all services
builder.Services.AddScoped<ICrawlerService, CrawlerService>();
builder.Services.AddScoped<IIndexerService, IndexerService>();
builder.Services.AddScoped<ISearchService, SearchService>();

// Background file watcher
builder.Services.AddHostedService<FileWatcherService>();

// Allow webpage to call API (CORS)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Auto-create/migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NemoFindDbContext>();
    db.Database.Migrate();
}

// Serve static files (index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NemoFind v1"));
}

app.UseAuthorization();
app.MapControllers();

app.Run();