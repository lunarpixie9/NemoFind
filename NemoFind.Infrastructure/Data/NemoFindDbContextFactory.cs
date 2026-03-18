using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NemoFind.Infrastructure.Data;

public class NemoFindDbContextFactory : IDesignTimeDbContextFactory<NemoFindDbContext>
{
    public NemoFindDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NemoFindDbContext>();
        optionsBuilder.UseSqlite("Data Source=nemofind.db");

        return new NemoFindDbContext(optionsBuilder.Options);
    }
}