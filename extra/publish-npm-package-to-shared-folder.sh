#!/usr/bin/env bash
set -Eeuo pipefail

package_directory=''
shared_folder="${CHILL_NPM_SHARED_FOLDER:-$HOME/source/npm-shared}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --package-directory|-p)
      package_directory="${2:-}"
      shift 2
      ;;
    --shared-folder|-s)
      shared_folder="${2:-}"
      shift 2
      ;;
    *)
      printf "Error: Unknown argument '%s'.\n" "$1" >&2
      exit 1
      ;;
  esac
done

if [[ -z "${package_directory//[[:space:]]/}" ]]; then
  printf 'Error: --package-directory is required.\n' >&2
  exit 1
fi

shared_folder="$(python3 -c 'import os, sys; print(os.path.abspath(sys.argv[1]))' "$shared_folder")"
package_directory="$(cd -- "$package_directory" && pwd -P)"
package_json_path="$package_directory/package.json"

if [[ ! -f "$package_json_path" ]]; then
  printf "Error: Could not find package.json at '%s'.\n" "$package_json_path" >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  printf "Error: Required command 'jq' was not found.\n" >&2
  exit 1
fi

package_name="$(jq -er '.name // empty' "$package_json_path")"
package_version="$(jq -er '.version // empty' "$package_json_path")"

if [[ -z "${package_name//[[:space:]]/}" ]]; then
  printf "Error: Package name is missing from '%s'.\n" "$package_json_path" >&2
  exit 1
fi

if [[ -z "${package_version//[[:space:]]/}" ]]; then
  printf "Error: Package version is missing from '%s'.\n" "$package_json_path" >&2
  exit 1
fi

printf "Installing dependencies for %s in '%s'...\n" "$package_name" "$package_directory"
(cd "$package_directory" && npm install) || {
  printf "Error: npm install failed for '%s'.\n" "$package_name" >&2
  exit 1
}

printf 'Building %s %s...\n' "$package_name" "$package_version"
(cd "$package_directory" && npm run build) || {
  printf "Error: npm run build failed for '%s'.\n" "$package_name" >&2
  exit 1
}

mkdir -p -- "$shared_folder"

archive_package_name="${package_name#@}"
archive_package_name="${archive_package_name//\//-}"
archive_name="$archive_package_name-$package_version.tgz"
archive_path="$shared_folder/$archive_name"

printf "Packing %s into '%s'...\n" "$package_name" "$shared_folder"
(cd "$package_directory" && npm pack --ignore-scripts --pack-destination "$shared_folder") || {
  printf "Error: npm pack failed for '%s'.\n" "$package_name" >&2
  exit 1
}

if [[ ! -f "$archive_path" ]]; then
  printf "Error: npm pack completed for '%s', but '%s' was not found.\n" "$package_name" "$archive_path" >&2
  exit 1
fi

printf '\nPackage published to shared folder successfully: %s\n' "$archive_path"
