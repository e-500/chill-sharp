#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
csproj_files=("$script_dir"/*.csproj)
if [ ${#csproj_files[@]} -eq 0 ] || [ ! -f "${csproj_files[0]}" ]; then
  die "Could not find a .csproj file in '$script_dir'."
elif [ ${#csproj_files[@]} -gt 1 ]; then
  die "Found multiple .csproj files in '$script_dir': ${csproj_files[*]}."
fi
template_project_path="${csproj_files[0]}"
template_project_name="$(basename -- "$template_project_path")"
local_package_folder="$script_dir/nupkgs"
restore_state_folder="$script_dir/obj"
shared_folder="${CHILL_NUGET_SHARED_FOLDER:-$HOME/source/nuget-shared}"
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

nuget_global_packages_folder() {
  if [[ -n "${NUGET_PACKAGES:-}" ]]; then
    normalize_path "$NUGET_PACKAGES"
    return 0
  fi

  normalize_path "$HOME/.nuget/packages"
}

resolve_confirmed_shared_folder() {
  local selected_path="$shared_folder"
  local entered_path confirmation

  if [[ "$skip_confirmation" == false ]]; then
    printf 'NuGet shared folder suggestion: %s\n' "$selected_path" >&2
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
  [[ -d "$selected_path" ]] || die "NuGet shared folder '$selected_path' was not found."

  export CHILL_NUGET_SHARED_FOLDER="$selected_path"
  persist_user_env_var CHILL_NUGET_SHARED_FOLDER "$selected_path"

  resolved_shared_folder="$selected_path"
}

latest_shared_package_line() {
  local folder_path="$1"

  find "$folder_path" -maxdepth 1 -type f -name 'ChillSharp.*.nupkg' -printf '%f\t%p\n' |
    sed -n 's/^ChillSharp\.\([0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*[-+0-9A-Za-z.]*\)\.nupkg\t/\1\t/p' |
    sort -V -r |
    head -n 1
}

set_chillsharp_package_reference_version() {
  local project_path="$1"
  local package_version="$2"

  grep -Eq '<PackageReference Include="ChillSharp" Version="[^"]+"[[:space:]]*/>' "$project_path" ||
    die "Could not find the ChillSharp package reference in '$project_path'."

  CHILL_PACKAGE_VERSION="$package_version" perl -0pi -e '
    my $version = $ENV{CHILL_PACKAGE_VERSION};
    s{<PackageReference Include="ChillSharp" Version="[^"]+"\s*/>}{<PackageReference Include="ChillSharp" Version="$version" />}s
  ' "$project_path"
}

remove_chillsharp_global_package_cache() {
  local package_version="$1"
  local global_packages_folder cached_package_path

  global_packages_folder="$(nuget_global_packages_folder)"
  cached_package_path="$global_packages_folder/chillsharp/$package_version"

  if [[ ! -d "$cached_package_path" ]]; then
    return 0
  fi

  rm -rf -- "$cached_package_path"
  printf '%s\n' "$cached_package_path"
}

remove_stale_restore_state() {
  local restore_state_folder_path="$1"
  local removed_paths=()
  local item

  [[ -d "$restore_state_folder_path" ]] || return 0

  while IFS= read -r -d '' item; do
    rm -f -- "$item"
    removed_paths+=("$item")
  done < <(
    find "$restore_state_folder_path" -maxdepth 1 -type f \( \
      -name 'project.assets.json' -o \
      -name 'project.nuget.cache' -o \
      -name '*.nuget.g.props' -o \
      -name '*.nuget.g.targets' \
    \) -print0
  )

  if [[ "${#removed_paths[@]}" -gt 0 ]]; then
    printf '%s\n' "${removed_paths[@]}"
  fi
}

extract_chillsharp_skills() {
  local nupkg_path="$1"
  local target_folder="$2"

  python3 -c '
import zipfile, os, sys
nupkg = sys.argv[1]
target = sys.argv[2]
agents_dir = os.path.join(target, ".agents")
skills_dir = os.path.join(agents_dir, "skills")

if os.path.exists(skills_dir):
    import shutil
    shutil.rmtree(skills_dir)
os.makedirs(skills_dir, exist_ok=True)

with zipfile.ZipFile(nupkg, "r") as z:
    for name in z.namelist():
        if name.startswith(".agents/skills/") and not name.endswith("/"):
            rel_path = name[len(".agents/skills/"):]
            dest = os.path.join(skills_dir, rel_path)
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            with open(dest, "wb") as f_out:
                f_out.write(z.read(name))
' "$nupkg_path" "$target_folder"
}

if ! command -v python3 >/dev/null 2>&1; then
  die "Required command 'python3' was not found."
fi

[[ -f "$template_project_path" ]] || die "Could not find template project at '$template_project_path'."
mkdir -p -- "$local_package_folder"

resolve_confirmed_shared_folder
latest_package_line="$(latest_shared_package_line "$resolved_shared_folder")"
[[ -n "$latest_package_line" ]] ||
  die "Could not find a ChillSharp.<version>.nupkg archive in '$resolved_shared_folder'."

IFS=$'\t' read -r package_version source_archive_path <<<"$latest_package_line"
destination_archive_path="$local_package_folder/$(basename -- "$source_archive_path")"

while IFS= read -r -d '' existing_package; do
  if [[ "$(normalize_path "$existing_package")" != "$(normalize_path "$destination_archive_path")" ]]; then
    rm -f -- "$existing_package"
  fi
done < <(find "$local_package_folder" -maxdepth 1 -type f -name 'ChillSharp.*.nupkg' -print0)

cp -f -- "$source_archive_path" "$destination_archive_path"
set_chillsharp_package_reference_version "$template_project_path" "$package_version"
extract_chillsharp_skills "$destination_archive_path" "$script_dir"

removed_global_cache_path="$(remove_chillsharp_global_package_cache "$package_version" || true)"
mapfile -t removed_restore_state_paths < <(remove_stale_restore_state "$restore_state_folder")

printf "Copied ChillSharp %s from '%s' to '%s'.\n" "$package_version" "$source_archive_path" "$destination_archive_path"
printf "Updated %s to ChillSharp %s.\n" "$template_project_name" "$package_version"
printf "Extracted and updated agent skills in '.agents/skills/'.\n"
printf "Saved CHILL_NUGET_SHARED_FOLDER to '%s'.\n" "$env_file"

if [[ -n "${removed_global_cache_path//[[:space:]]/}" ]]; then
  printf "Removed cached global package '%s' so NuGet will re-extract ChillSharp %s.\n" \
    "$removed_global_cache_path" \
    "$package_version"
fi

if [[ "${#removed_restore_state_paths[@]}" -gt 0 ]]; then
  printf 'Removed stale restore state:\n'
  printf ' - %s\n' "${removed_restore_state_paths[@]}"
fi
