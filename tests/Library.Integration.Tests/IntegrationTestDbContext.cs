namespace Library.Integration.Tests
{
    using System.Collections.Generic;
    using MassTransit.EntityFrameworkCoreIntegration;
    using MassTransit.EntityFrameworkCoreIntegration.Mappings;
    using Microsoft.EntityFrameworkCore;


    public class IntegrationTestDbContext :
        SagaDbContext
    {
        public IntegrationTestDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override IEnumerable<ISagaClassMap> Configurations => new[] {new ThankYouClassMap()};
    }
}