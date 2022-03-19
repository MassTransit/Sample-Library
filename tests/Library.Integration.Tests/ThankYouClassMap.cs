namespace Library.Integration.Tests
{
    using Components.StateMachines;
    using MassTransit;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;


    public class ThankYouClassMap :
        SagaClassMap<ThankYou>
    {
        protected override void Configure(EntityTypeBuilder<ThankYou> entity, ModelBuilder model)
        {
            entity.HasIndex(x => new
            {
                x.BookId,
                x.MemberId
            }).IsUnique();
        }
    }
}