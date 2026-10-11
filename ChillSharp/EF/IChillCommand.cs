namespace ChillSharp.EF
{
    /// <summary>
    /// Defines an application command that executes against a Chill context.
    /// </summary>
    public interface IChillCommand : IChillable
    {
        /// <summary>
        /// Executes this command using the active Chill context.
        /// </summary>
        /// <param name="Context">The active Chill database context.</param>
        void OnCommand(IChillContext Context);
    }
}