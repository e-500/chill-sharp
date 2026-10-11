namespace ChillSharp.Client.Dto
{
    /// <summary>
    /// Request and response DTO for executing an application-defined Chill command.
    /// </summary>
    public class ChillDtoCommand
    {
        /// <summary>
        /// Chill type identifying the concrete command implementation.
        /// </summary>
        public string ChillType { get; set; } = string.Empty;

        /// <summary>
        /// Named command inputs and outputs.
        /// </summary>
        public Dictionary<string, object?> Parameters { get; set; } = new();
    }
}