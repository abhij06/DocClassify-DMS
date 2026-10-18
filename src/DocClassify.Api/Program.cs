using System.Text.Json.Serialization;
using DocClassify.Api.Data;
using DocClassify.Api.Services;
using Microsoft.EntityFrameworkCore;

var b = WebApplication.CreateBuilder(args);
b.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
b.Services.AddEndpointsApiExplorer();
b.Services.AddSwaggerGen();
b.Services.AddDbContext<AppDb>(o => o.UseSqlite(b.Configuration.GetConnectionString("Default")));
b.Services.Configure<LlmOptions>(b.Configuration.GetSection("Llm"));
b.Services.AddHttpClient<LlmService>(c => c.Timeout = TimeSpan.FromSeconds(60));
b.Services.AddScoped<EmbeddingService>();
b.Services.AddScoped<DocumentClassifier>();
b.Services.AddScoped<VectorSearch>();
b.Services.AddScoped<DocumentPipeline>();

var app = b.Build();
using (var s = app.Services.CreateScope())
    s.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreated();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
