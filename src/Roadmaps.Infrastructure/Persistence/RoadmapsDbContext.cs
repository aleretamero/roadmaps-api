using Microsoft.EntityFrameworkCore;

namespace Roadmaps.Infrastructure.Persistence;

public class RoadmapsDbContext(DbContextOptions<RoadmapsDbContext> options)
    : DbContext(options)
{
}
