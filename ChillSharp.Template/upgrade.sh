#!/usr/bin/env bash
set -Eeuo pipefail

upgrade_script_version=1
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

extract_chillsharp_documentation() {
  local nupkg_path="$1"
  local target_folder="$2"

  python3 -c '
import os, shutil, sys, zipfile

nupkg = sys.argv[1]
target = sys.argv[2]
documentation_dir = os.path.join(target, "doc")

if os.path.exists(documentation_dir):
    shutil.rmtree(documentation_dir)
os.makedirs(documentation_dir, exist_ok=True)

documentation_root = os.path.abspath(documentation_dir)
with zipfile.ZipFile(nupkg, "r") as archive:
    for entry in archive.infolist():
        if not entry.filename.startswith("doc/"):
            continue

        relative_path = entry.filename[len("doc/"):]
        if not relative_path:
            continue

        destination = os.path.abspath(os.path.join(documentation_dir, relative_path))
        if os.path.commonpath((documentation_root, destination)) != documentation_root:
            raise RuntimeError(f"Refusing to extract documentation outside '{documentation_root}'.")

        if entry.is_dir():
            os.makedirs(destination, exist_ok=True)
            continue

        os.makedirs(os.path.dirname(destination), exist_ok=True)
        with archive.open(entry) as source, open(destination, "wb") as output:
            shutil.copyfileobj(source, output)
' "$nupkg_path" "$target_folder"
}

update_upgrade_script_if_newer() {
  local nupkg_path="$1"
  local script_path="$2"

  python3 -c '
import os, re, stat, sys, tempfile, zipfile

nupkg_path, script_path, current_version = sys.argv[1], sys.argv[2], int(sys.argv[3])
entry_name = "template-customization/upgrade.sh.template"

with zipfile.ZipFile(nupkg_path, "r") as archive:
    try:
        entry = archive.getinfo(entry_name)
    except KeyError:
        sys.exit(0)

    contents = archive.read(entry).decode("utf-8")

match = re.search(r"(?m)^\\s*upgrade_script_version\\s*=\\s*(\\d+)\\s*$", contents)
if match is None:
    raise RuntimeError("The packaged upgrade script does not define a valid upgrade_script_version.")

packaged_version = int(match.group(1))
if packaged_version <= current_version:
    sys.exit(0)

script_dir = os.path.dirname(os.path.abspath(script_path))
original_mode = stat.S_IMODE(os.stat(script_path).st_mode)
fd, temporary_script_path = tempfile.mkstemp(prefix=".upgrade-", dir=script_dir)
try:
    with os.fdopen(fd, "w", encoding="utf-8", newline="") as file:
        file.write(contents)
    os.chmod(temporary_script_path, original_mode)
    os.replace(temporary_script_path, script_path)
finally:
    if os.path.exists(temporary_script_path):
        os.unlink(temporary_script_path)

print(f"Updated upgrade.sh from internal version {current_version} to {packaged_version}. Rerun the script to continue the package upgrade.")
sys.exit(10)
' "$nupkg_path" "$script_path" "$upgrade_script_version"
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
if update_upgrade_script_if_newer "$destination_archive_path" "${BASH_SOURCE[0]}"; then
  :
else
  update_status=$?
  if [[ "$update_status" -eq 10 ]]; then
    exit 0
  fi
  die 'Could not update upgrade.sh from the ChillSharp package.'
fi
set_chillsharp_package_reference_version "$template_project_path" "$package_version"
extract_chillsharp_skills "$destination_archive_path" "$script_dir"
extract_chillsharp_documentation "$destination_archive_path" "$script_dir"

removed_global_cache_path="$(remove_chillsharp_global_package_cache "$package_version" || true)"
mapfile -t removed_restore_state_paths < <(remove_stale_restore_state "$restore_state_folder")

printf "Copied ChillSharp %s from '%s' to '%s'.\n" "$package_version" "$source_archive_path" "$destination_archive_path"
printf "Updated %s to ChillSharp %s.\n" "$template_project_name" "$package_version"
printf "Extracted and updated agent skills in '.agents/skills/'.\n"
printf "Extracted and updated documentation in 'doc/'.\n"
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
