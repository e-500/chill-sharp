#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
package_json_path="$script_dir/package.json"
packages_folder="$script_dir/packages"
shared_folder="${CHILL_NPM_SHARED_FOLDER:-$HOME/source/npm-shared}"
env_file="${CHILL_ENV_FILE:-$HOME/.profile}"
skip_confirmation=false
resolved_shared_folder=''

while [[ $# -gt 0 ]]; do
  case "$1" in
    --shared-folder|-s)
      shared_folder="${2:-}"
      shift 2
      ;;
    --skip-confirmation|-y)
      skip_confirmation=true
      shift
      ;;
    *)
      printf "Error: Unknown argument '%s'.\n" "$1" >&2
      exit 1
      ;;
  esac
done

die() {
  printf 'Error: %s\n' "$*" >&2
  exit 1
}

warn() {
  printf 'Warning: %s\n' "$*" >&2
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || die "Required command '$1' was not found."
}

normalize_path() {
  python3 -c 'import os, sys; print(os.path.abspath(sys.argv[1]))' "$1"
}

shell_quote() {
  local value="$1"
  printf "'%s'" "${value//\'/\'\\\'\'}"
}

persist_user_env_var() {
  local name="$1"
  local value="$2"
  local quoted_value temp_file

  quoted_value="$(shell_quote "$value")"
  mkdir -p -- "$(dirname -- "$env_file")"
  touch "$env_file"

  temp_file="$(mktemp)"
  awk -v name="$name" '
    $0 ~ "^[[:space:]]*export[[:space:]]+" name "=" { next }
    $0 ~ "^[[:space:]]*" name "=" { next }
    { print }
  ' "$env_file" > "$temp_file"
  printf 'export %s=%s\n' "$name" "$quoted_value" >> "$temp_file"
  mv -- "$temp_file" "$env_file"
}

resolve_confirmed_shared_folder() {
  local selected_path="$shared_folder"
  local entered_path confirmation

  if [[ "$skip_confirmation" == false ]]; then
    printf 'npm shared folder suggestion: %s\n' "$selected_path" >&2
    read -r -p 'Press Enter to confirm, or type a different path: ' entered_path
    if [[ -n "${entered_path//[[:space:]]/}" ]]; then
      selected_path="$entered_path"
    fi

    read -r -p "Continue with '$selected_path'? [Y/n]: " confirmation
    if [[ -n "${confirmation//[[:space:]]/}" && ! "$confirmation" =~ ^[Yy]([Ee][Ss])?$ ]]; then
      printf 'Upgrade cancelled.\n' >&2
      exit 0
    fi
  fi

  selected_path="$(normalize_path "$selected_path")"
  [[ -d "$selected_path" ]] || die "npm shared folder '$selected_path' was not found."

  export CHILL_NPM_SHARED_FOLDER="$selected_path"
  persist_user_env_var CHILL_NPM_SHARED_FOLDER "$selected_path"

  resolved_shared_folder="$selected_path"
}

latest_archive_line() {
  local folder_path="$1"
  local archive_prefix="$2"

  find "$folder_path" -maxdepth 1 -type f -name "$archive_prefix-*.tgz" -printf '%f\t%p\n' |
    sed -n "s/^$archive_prefix-\([0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*[-+0-9A-Za-z.]*\)\.tgz\t/\1\t/p" |
    sort -V -r |
    head -n 1
}

set_package_dependency() {
  local package_name="$1"
  local dependency_spec="$2"
  local temp_json

  jq -e '.dependencies | type == "object"' "$package_json_path" >/dev/null ||
    die "package.json at '$package_json_path' does not define a dependencies object."

  temp_json="$(mktemp)"
  jq --arg name "$package_name" --arg spec "$dependency_spec" \
    '.dependencies[$name] = $spec' \
    "$package_json_path" > "$temp_json"
  mv -- "$temp_json" "$package_json_path"
}

copy_latest_package_archive() {
  local package_name="$1"
  local archive_prefix="$2"
  local latest_line package_version source_archive_path destination_archive_path

  latest_line="$(latest_archive_line "$resolved_shared_folder" "$archive_prefix")"
  [[ -n "$latest_line" ]] ||
    die "Could not find a '$archive_prefix-<version>.tgz' archive in '$resolved_shared_folder'."

  IFS=$'\t' read -r package_version source_archive_path <<<"$latest_line"
  destination_archive_path="$packages_folder/$(basename -- "$source_archive_path")"

  while IFS= read -r -d '' existing_archive; do
    if [[ "$(normalize_path "$existing_archive")" != "$(normalize_path "$destination_archive_path")" ]]; then
      rm -f -- "$existing_archive"
    fi
  done < <(find "$packages_folder" -maxdepth 1 -type f -name "$archive_prefix-*.tgz" -print0)

  cp -f -- "$source_archive_path" "$destination_archive_path"
  set_package_dependency "$package_name" "file:./packages/$(basename -- "$destination_archive_path")"

  printf "Copied %s %s to '%s'.\n" "$package_name" "$package_version" "$destination_archive_path"
}

require_command python3
require_command jq

[[ -f "$package_json_path" ]] || die "Could not find package.json at '$package_json_path'."
mkdir -p -- "$packages_folder"

resolve_confirmed_shared_folder

copy_latest_package_archive '@chill-sharp/ui-core' 'chill-sharp-ui-core'
copy_latest_package_archive '@chill-sharp/ng-client' 'chill-sharp-ng-client'
copy_latest_package_archive '@chill-sharp/ts-client' 'chill-sharp-ts-client'

printf "Saved CHILL_NPM_SHARED_FOLDER to '%s'.\n" "$env_file"

if command -v npm >/dev/null 2>&1; then
  printf 'Refreshing package-lock.json and local file dependencies with npm install --ignore-scripts...\n'
  if ! (cd "$script_dir" && npm install --ignore-scripts); then
    warn 'npm install --ignore-scripts did not complete successfully. Local archives were copied, but package-lock.json and node_modules may still need a manual refresh.'
  fi
else
  warn 'npm was not found on PATH. Local archives were copied, but package-lock.json was not refreshed.'
fi

printf 'UI template dependencies were upgraded from the shared npm folder.\n'
