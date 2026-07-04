using MongoDB.Driver;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var mongoUrl = new MongoUrl(builder.Configuration.GetConnectionString("Mongo")
    ?? throw new InvalidOperationException("Missing connection string 'Mongo'"));
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoUrl));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName ?? "rockpaperscissors"));

builder.Services.AddSingleton(NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Missing connection string 'Postgres'")));

var app = builder.Build();

app.MapControllers();

app.Run();
