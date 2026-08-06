using MongoDB.Driver;
using Npgsql;
using RockPaperScissors.Api.Data;
using RockPaperScissors.Api.HostedServices;
using RockPaperScissors.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var mongoUrl = new MongoUrl(builder.Configuration.GetConnectionString("Mongo")
    ?? throw new InvalidOperationException("Missing connection string 'Mongo'"));
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoUrl));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName ?? "rockpaperscissors"));

builder.Services.AddSingleton(NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Missing connection string 'Postgres'")));
builder.Services.AddSingleton<IPostgres, DapperPostgres>();
builder.Services.AddSingleton<IMatchEventRepository, MatchEventRepository>();
builder.Services.AddSingleton<IFeatureFlagRepository, FeatureFlagRepository>();
builder.Services.AddSingleton<IMatchesRepository, MongoMatchesRepository>();
builder.Services.AddHostedService<FeatureFlagStartupReporter>();


var app = builder.Build();

app.MapControllers();

app.Run();
