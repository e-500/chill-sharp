#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
csproj_files=("$script_dir"/*.csproj)
if [[ ${#csproj_files[@]} -eq 0 || ! -f "${csproj_files[0]}" ]]; then
  printf "Error: Could not find a .csproj file in '%s'.\n" "$script_dir" >&2
  exit 1
elif [[ ${#csproj_files[@]} -gt 1 ]]; then
  printf "Error: Found multiple .csproj files in '%s': %s.\n" "$script_dir" "${csproj_files[*]}" >&2
  exit 1
fi
project_path="${csproj_files[0]}"
version="${1:-latest}"

if [[ -z "${version//[[:space:]]/}" || "$version" == 'latest' ]]; then
  dotnet add "$project_path" package ChillSharp
  printf 'Updated ChillSharp to the latest stable version from the configured NuGet sources.\n'
else
  dotnet add "$project_path" package ChillSharp --version "$version"
  printf 'Updated ChillSharp to %s from the configured NuGet sources.\n' "$version"
fi
