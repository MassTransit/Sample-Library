namespace Library.Integration.Tests
{
    using System.Reflection;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;


    public class IntegrationTestSagaDbContextFactory :
        IDesignTimeDbContextFactory<IntegrationTestDbContext>
    {
        public IntegrationTestDbContext CreateDbContext(params string[] args)
        {
            var builder = new DbContextOptionsBuilder();

            Apply(builder);

            return new IntegrationTestDbContext(builder.Options);
        }

        public static void Apply(DbContextOptionsBuilder builder)
        {
            builder.UseNpgsql("host=localhost;user id=postgres;password=Password12!;database=LibraryIntegrationTests;", m =>
            {
                m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                m.MigrationsHistoryTable($"__{nameof(IntegrationTestDbContext)}");
            });
        }

        public IntegrationTestDbContext CreateDbContext(DbContextOptionsBuilder optionsBuilder)
        {
            return new IntegrationTestDbContext(optionsBuilder.Options);
        }
    }
}