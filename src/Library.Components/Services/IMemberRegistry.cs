namespace Library.Components.Services
{
    using System;
    using System.Threading.Tasks;


    public interface IMemberRegistry
    {
        Task<bool> IsMemberValid(Guid memberId);
    }
}