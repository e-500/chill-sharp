using System.Reflection;
using ChillSharp.Schema.Contracts;

namespace ChillSharp.Schema;

/// <summary>
/// Provides the minimal runtime metadata and schema-building hooks required by schema persistence services.
/// </summary>
public interface IChillSchemaRuntimeContext
{
    Assembly ModelAssembly { get; }
    string ChillTypePrefix { get; }
    string DefaultUserCultureName { get; }
    string RuntimeContextKey { get; }
    IChillDtoSchema BuildSchema(object activatedType, string chillViewCode, string cultureName);
}
