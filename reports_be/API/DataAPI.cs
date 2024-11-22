using Microsoft.EntityFrameworkCore;
using reports_be.Context;
using reports_be.Models;

namespace reports_be.API;

public class DataAPI //Used to retrieve data
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/data", async (AppDbContext context) => //Used to retrieve data
        {
            try
            {
                var status = await context.BackupStatus.ToListAsync();
                return Results.Ok(status);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        });
    }
}