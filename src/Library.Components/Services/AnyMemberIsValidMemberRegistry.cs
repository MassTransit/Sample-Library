namespace Library.Components.Services
{
    using System;
    using System.Threading.Tasks;


    public class AnyMemberIsValidMemberRegistry :
        IMemberRegistry
    {
        public Task<bool> IsMemberValid(Guid memberId)
        {
            return Task.FromResult(true);
        }
    }
}