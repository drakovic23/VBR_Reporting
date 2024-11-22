using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using reports_be.API;
using reports_be.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//Db context
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("Data Source=data.db");
    options.EnableSensitiveDataLogging(true);
});
builder.Services.AddCors();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IngestionAPI>();

var app = builder.Build();
app.UseCors(builder => builder.AllowAnyOrigin());

app.UseStaticFiles();
app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    db.Database.ExecuteSqlRaw(@"
    CREATE VIEW IF NOT EXISTS BackupStatus AS
        WITH LatestDates AS (
            SELECT
                BHostId,
                MAX(Date) AS LatestDate
            FROM
                RestorePoints
            GROUP BY
                BHostId
        )
        SELECT
            V.VbrHostName,
            B.BHostName,
            R.LatestDate,
            RP.ParentJob,
            CASE
                WHEN (julianday('now','start of day') - julianday(R.LatestDate, 'start of day')) > 1 THEN 'Error'
                ELSE 'OK'
                END AS Status
        FROM
            VbrHost V
                INNER JOIN BackedUpHosts B ON V.Id = B.VbrId
                INNER JOIN LatestDates R ON B.Id = R.BHostId
                INNER JOIN RestorePoints RP ON RP.BHostId = R.BHostId AND RP.Date = R.LatestDate;
    ");
}

// Configure the HTTP request pipeline.
/*if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}*/

//app.UseHttpsRedirection();
app.Services.GetRequiredService<IngestionAPI>().Map(app);
DataAPI.Map(app);

app.Urls.Add("http://0.0.0.0:80");
//app.Urls.Add("https://0.0.0.0:443");
app.Run();
