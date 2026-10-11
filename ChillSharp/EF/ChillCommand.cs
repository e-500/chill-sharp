namespace ChillSharp.EF
{
    /// <summary>
    /// Base class for application-defined commands that execute against a Chill context.
    /// Expose input and output parameters as properties marked with <see cref="ChillSharp.Annotations.ChillPropertyAttribute"/>.
    /// </summary>
    public abstract class ChillCommand : IChillCommand
    {
        /// <summary>
        /// Executes this command using the active Chill context.
        /// </summary>
        /// <param name="Context">The active Chill database context.</param>
        public abstract void OnCommand(IChillContext Context);
    }
}