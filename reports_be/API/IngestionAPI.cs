using Microsoft.EntityFrameworkCore;
using reports_be.Context;
using reports_be.Models;

namespace reports_be.API;

public class IngestionAPI //Handles ingestion of backup data
{
    private readonly IServiceProvider _serviceProvider;

    public IngestionAPI(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public void Map(WebApplication app)
    {
        app.MapPost("/api/ingest", async (HttpContext httpContext, AppDbContext context, RestorePointDto[] rpDtos) =>
        {   
            Console.WriteLine("Received call to /api/data");
            
            //Each request is from a single VBR host
            string vbrHostName = rpDtos.Select(rp => rp.VbrHost).First();
            if (vbrHostName.Length < 2)
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsync("VBR Hostname must be provided");
                return;
            }
            
            //Check if VBR Host exists
            VbrHost? vbrHost = await context.VbrHost.FirstOrDefaultAsync(vbr => vbr.VbrHostName == vbrHostName);

            if (vbrHost is null)
            {
                vbrHost = new VbrHost { VbrHostName = vbrHostName };
                await context.VbrHost.AddAsync(vbrHost);
                await context.SaveChangesAsync();
            }
            
            
            //Get BHostNames from restorePointsDto
            var hostNames = rpDtos.Select(rp => rp.HostName)
                .Distinct()
                .ToList();
            
            //TODO: Fix this
            //Get existing BHosts from the db
            var existingHosts = await (from host in context.BackedUpHosts
                    join name in hostNames on host.BHostName equals name
                    where host.BHostName != null
                    select host)
                .ToListAsync();
            
            // Dictionary of existing host names for quick lookup
            var existingHostsDict = existingHosts.ToDictionary(h => h.BHostName);
            
            //Prepare list for new hosts
            //var newHosts = new List<BackedUpHost>();
            var newHosts = new List<BackedUpHost>();
            
            for (int i = 0; i < rpDtos.Length; i++)
            {
                //Check if BHost already exists in the dictionary otherwise create BHost
                if (!existingHostsDict.ContainsKey(rpDtos[i].HostName))
                {
                    var backedUpHost = new BackedUpHost() { BHostName = rpDtos[i].HostName, VbrId = vbrHost.Id};
                    newHosts.Add(backedUpHost);
                    
                    existingHostsDict[backedUpHost.BHostName] = backedUpHost;
                }
                
            }
            
            if (newHosts.Any())
            {
                context.BackedUpHosts.AddRange(newHosts);
                await context.SaveChangesAsync();
            }
            
            //Handle possible duplicates
            var newRestorePoints = new HashSet<RestorePoint>();
            for (int i = 0; i < rpDtos.Length; i++)
            {
                BackedUpHost host = existingHostsDict[rpDtos[i].HostName];
                RestorePoint rp = new RestorePoint { Date = rpDtos[i].Date, BHostId = host.Id, VbrId = vbrHost.Id
                    , ParentJob = rpDtos[i].ParentJob};
                
                newRestorePoints.Add(rp);
            }
            
            
            // Add posted restore points
            if (newRestorePoints.Any())
            {
                foreach (var rp in newRestorePoints)
                {
                    try
                    {
                        context.RestorePoints.Add(rp);
                        await context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        Console.WriteLine($"Skipping duplicate entry for VbrId {rp.VbrId}, BHostId {rp.BHostId}, Date {rp.Date}, ParentJob {rp.ParentJob}");
                        context.Entry(rp).State = EntityState.Detached;
                    }
                }
            }

            httpContext.Response.StatusCode = 200;
            await httpContext.Response.WriteAsync("Success");
            
            
            //TODO: Implement processing deleted hosts
            _ = Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await ProcessDeletedHosts(rpDtos, dbContext, vbrHost);
            });
            //return Results.Ok();
        });
    }

    
    // Checks for hosts that have been deleted
    // If a BHost name is not within the POST request, and is still referencing the VBR server then delete it
    static async Task ProcessDeletedHosts(ICollection<RestorePointDto> rpDtos, AppDbContext dbContext, VbrHost vbrHost)
    {
        if (rpDtos.Count < 1)
            return;
        // HashSet of (HostName, ParentJob)
        var dtoHostParentJobsSet = new HashSet<(string HostName, string? ParentJob)>(
            rpDtos.Select(dto => (dto.HostName, dto.ParentJob)),
            new HostNameParentJobComparer());

        // Get all RestorePoints associated to VbrHost
        var restorePointsInDb = await dbContext.RestorePoints
            .Include(rp => rp.BackedUpHost)
            .Where(rp => rp.VbrId == vbrHost.Id)
            .ToListAsync();

        // Filter which RPs we need to delete
        var restorePointsToDelete = restorePointsInDb
            .Where(rp => rp.BackedUpHost != null && 
                         !dtoHostParentJobsSet.Contains((rp.BackedUpHost.BHostName ?? "", rp.ParentJob)))
            .ToList();

        // Remove the RPs and save changes
        dbContext.RestorePoints.RemoveRange(restorePointsToDelete);
        await dbContext.SaveChangesAsync();
    }

    private class HostNameParentJobComparer : IEqualityComparer<(string HostName, string? ParentJob)>
    {
        private static readonly StringComparer StringComparer = StringComparer.OrdinalIgnoreCase;

        public bool Equals((string HostName, string? ParentJob) x, (string HostName, string? ParentJob) y)
        {
            return StringComparer.Equals(x.HostName, y.HostName) &&
                   StringComparer.Equals(x.ParentJob ?? "", y.ParentJob ?? "");
        }

        public int GetHashCode((string HostName, string? ParentJob) obj)
        {
            int hashHostName = StringComparer.GetHashCode(obj.HostName ?? "");
            int hashParentJob = StringComparer.GetHashCode(obj.ParentJob ?? "");

            return hashHostName ^ hashParentJob;
        }
    }
}