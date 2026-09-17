namespace ChillSharp.Client;

public sealed class ChillSharpClientOptions
{
    public const string DefaultApiBasePath = "api/";

    public string ApiBasePath { get; set; } = DefaultApiBasePath;
}
