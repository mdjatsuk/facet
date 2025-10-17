using FacetApi.Data;
using FacetApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "facet.db");
builder.Services.AddDbContext<FacetDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "FACET API", Version = "v1" }); });

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed(_ => true)));

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddSingleton<ISensitiveDataScanner, SensitiveDataScanner>();

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FacetDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
