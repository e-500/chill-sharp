using ChillSharp.EF;

namespace ChillSharp.Dto
{
    /// <summary>
    /// Serializable request and response DTO for executing an application-defined command.
    /// </summary>
    public class ChillDtoCommand : IDtoChillable
    {
        /// <summary>
        /// Chill type identifying the concrete <see cref="IChillCommand"/> implementation.
        /// </summary>
        public string ChillType { get; set; } = string.Empty;

        /// <summary>
        /// Input and output values mapped to the command's Chill properties.
        /// </summary>
        public Dictionary<string, object?> Parameters { get; set; } = new();

        /// <summary>
        /// Copies annotated command properties into this DTO.
        /// </summary>
        public void FromCommand(IChillContext Context, IChillCommand Command)
        {
            ChillType = ChillTypeResolver.NormalizeChillType(Command.GetType(), Context.GetChillTypePrefix());
            Parameters = ChillDtoObjectMapper.BuildProperties(
                Context,
                Command,
                ChillType,
                ChillDtoTypeMetadataCache.Get(Command.GetType()).ChillProperties);
        }

        /// <summary>
        /// Applies this DTO's parameters to an instance of the identified command type.
        /// </summary>
        public void ToCommand(IChillContext Context, IChillCommand Command)
        {
            var commandChillType = ChillTypeResolver.NormalizeChillType(Command.GetType(), Context.GetChillTypePrefix());
            if (ChillType != commandChillType)
                throw new ChillException($"Command ChillType ({commandChillType}) differs from Dto ChillType ({ChillType})");

            ChillDtoObjectMapper.ApplyProperties(
                Context,
                Command,
                ChillType,
                Parameters,
                ChillDtoTypeMetadataCache.Get(Command.GetType()).ChillProperties,
                "command",
                loadTrackedCollections: false);
        }
    }
}