/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core 
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Reflection;

namespace ChillSharp;

/// <summary>
/// Discovers the set of assemblies that may contribute Chill entities and queries for a runtime context.
/// </summary>
public static class ChillAssemblyDiscovery
{
    public static IReadOnlyList<Assembly> GetCandidateAssemblies(Assembly rootAssembly)
    {
        ArgumentNullException.ThrowIfNull(rootAssembly);

        var discoveredAssemblies = new List<Assembly>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<Assembly>();

        queue.Enqueue(rootAssembly);

        while (queue.Count > 0)
        {
            var currentAssembly = queue.Dequeue();
            var identity = currentAssembly.FullName ?? currentAssembly.GetName().Name ?? currentAssembly.ToString();
            if (!visited.Add(identity))
            {
                continue;
            }

            discoveredAssemblies.Add(currentAssembly);

            foreach (var reference in currentAssembly.GetReferencedAssemblies())
            {
                try
                {
                    queue.Enqueue(Assembly.Load(reference));
                }
                catch
                {
                }
            }
        }

        return discoveredAssemblies;
    }

    public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null)!;
        }
    }
}
