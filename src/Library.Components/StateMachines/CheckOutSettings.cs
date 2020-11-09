namespace Library.Components.StateMachines
{
    using System;


    public interface CheckOutSettings
    {
        /// <summary>
        /// The length of time a book is checked out for by default
        /// </summary>
        TimeSpan CheckOutDuration { get; }

        /// <summary>
        /// The maximum length of time a book can be checked out including renewals
        /// </summary>
        TimeSpan CheckOutDurationLimit { get; }
    }
}