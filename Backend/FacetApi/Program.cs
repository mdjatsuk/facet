using FacetApi.Data;
using FacetApi.Data.Repos;
using FacetApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<FacetDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
    
builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
{
    builder
    .SetIsOriginAllowed(_ => true)
    .AllowCredentials()
    .AllowAnyMethod()
    .AllowAnyHeader();
}));


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "FACET API", Version = "v1" }); });

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed(_ => true)));

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddSingleton<ISensitiveDataScanner, SensitiveDataScanner>();
builder.Services.AddScoped<UsersRepo>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("MyPolicy");

app.UseStaticFiles();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FacetDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
