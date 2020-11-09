namespace Library.Components.StateMachines
{
    using System;


    public interface CheckOutSettings
    {
        /// <summary>
        /// The length of time a book is checked out for by default
        /// </summary>
        TimeSpan CheckOutDuration { get; }
    }
}