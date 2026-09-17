#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
repository_root="$(cd -- "$script_dir/../.." && pwd -P)"
shared_folder="${CHILL_NPM_SHARED_FOLDER:-$HOME/source/npm-shared}"

if [[ "${1:-}" == "--shared-folder" || "${1:-}" == "-s" ]]; then
  shared_folder="${2:-}"
  shift 2
fi

if [[ $# -gt 0 ]]; then
  printf "Error: Unknown argument '%s'.\n" "$1" >&2
  exit 1
fi

shared_folder="$(python3 -c 'import os, sys; print(os.path.abspath(sys.argv[1]))' "$shared_folder")"

if ! command -v jq >/dev/null 2>&1; then
  printf "Error: Required command 'jq' was not found.\n" >&2
  exit 1
fi

package_json_value() {
  jq -er ".$2 // empty" "$1"
}

archive_name_for_package_dir() {
  local package_dir="$1"
  local package_json_path="$package_dir/package.json"

  if [[ ! -f "$package_json_path" ]]; then
    printf "Error: Could not find package.json at '%s'.\n" "$package_json_path" >&2
    exit 1
  fi

  local package_name package_version
  package_name="$(package_json_value "$package_json_path" name)"
  package_version="$(package_json_value "$package_json_path" version)"

  if [[ -z "${package_name//[[:space:]]/}" ]]; then
    printf "Error: Package name is missing from '%s'.\n" "$package_json_path" >&2
    exit 1
  fi

  if [[ -z "${package_version//[[:space:]]/}" ]]; then
    printf "Error: Package version is missing from '%s'.\n" "$package_json_path" >&2
    exit 1
  fi

  package_name="${package_name#@}"
  package_name="${package_name//\//-}"
  printf '%s-%s.tgz\n' "$package_name" "$package_version"
}

package_json_path="$script_dir/package.json"
dist_path="$script_dir/dist"
node_modules_path="$script_dir/node_modules"
ng_packagr_package_path="$node_modules_path/ng-packagr/package.json"

if [[ ! -f "$package_json_path" ]]; then
  printf "Error: Could not find package.json at '%s'.\n" "$package_json_path" >&2
  exit 1
fi

package_name="$(package_json_value "$package_json_path" name)"
package_version="$(package_json_value "$package_json_path" version)"

if [[ -z "${package_name//[[:space:]]/}" ]]; then
  printf 'Error: Package name is missing from package.json.\n' >&2
  exit 1
fi

if [[ -z "${package_version//[[:space:]]/}" ]]; then
  printf 'Error: Package version is missing from package.json.\n' >&2
  exit 1
fi

shared_client_dirs=(
  "$repository_root/extra/chill-sharp-ts-client"
  "$repository_root/extra/chill-sharp-ng-client"
)

archive_paths=()
for package_dir in "${shared_client_dirs[@]}"; do
  archive_name="$(archive_name_for_package_dir "$package_dir")"
  archive_path="$shared_folder/$archive_name"
  if [[ ! -f "$archive_path" ]]; then
    printf "Error: Expected shared package archive '%s' was not found.\n" "$archive_path" >&2
    exit 1
  fi
  archive_paths+=("$archive_path")
done

printf "Building %s %s from '%s'...\n" "$package_name" "$package_version" "$script_dir"
(
  cd "$script_dir"
  printf "Installing shared client package archives into '%s'...\n" "$script_dir"
  npm install --no-save "${archive_paths[@]}" || {
    printf 'Error: npm install for shared client packages failed.\n' >&2
    exit 1
  }

  if [[ ! -f "$ng_packagr_package_path" ]]; then
    printf "Build dependencies are missing in '%s'. Running npm install...\n" "$node_modules_path"
    npm install || {
      printf "Error: npm install failed. Install dependencies in '%s' and try again.\n" "$script_dir" >&2
      exit 1
    }
  fi

  npm run build || {
    printf 'Error: npm run build failed.\n' >&2
    exit 1
  }
)

if [[ ! -d "$dist_path" ]]; then
  printf "Error: Build completed but dist folder was not found at '%s'.\n" "$dist_path" >&2
  exit 1
fi

mkdir -p -- "$shared_folder"
archive_package_name="${package_name#@}"
archive_package_name="${archive_package_name//\//-}"
archive_name="$archive_package_name-$package_version.tgz"
archive_path="$shared_folder/$archive_name"

printf "Packing built library from '%s' into '%s'...\n" "$dist_path" "$shared_folder"
npm pack "$dist_path" --pack-destination "$shared_folder" || {
  printf 'Error: npm pack failed.\n' >&2
  exit 1
}

if [[ ! -f "$archive_path" ]]; then
  printf "Error: npm pack completed, but the file was not found at '%s'.\n" "$archive_path" >&2
  exit 1
fi

printf '\nPackage published to shared folder successfully.\n'
printf 'Archive: %s\n' "$archive_path"
printf 'Install with: npm install %s\n' "$archive_path"
