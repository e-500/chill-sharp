#!/usr/bin/env bash
set -Eeuo pipefail

script_path="${BASH_SOURCE[0]}"
script_dir="$(cd -- "$(dirname -- "$script_path")" && pwd -P)"
extra_dir="$script_dir"
repository_root="$(cd -- "$script_dir/.." && pwd -P)"

default_shared_folder="${CHILL_NPM_SHARED_FOLDER:-$HOME/source/npm-shared}"
default_nuget_shared_folder="${CHILL_NUGET_SHARED_FOLDER:-$HOME/source/nuget-shared}"
ui_template_path="$extra_dir/chill-sharp-ui-template"
api_template_path="$repository_root/ChillSharp.Template"
nuget_shared_folder="$default_nuget_shared_folder"
env_file="${CHILL_ENV_FILE:-$HOME/.profile}"

package_keys=(ts-client ng-client react-client vue-client ui-core)
package_labels=(
  "@chill-sharp/ts-client"
  "@chill-sharp/ng-client"
  "@chill-sharp/react-client"
  "@chill-sharp/vue-client"
  "@chill-sharp/ui-core"
)
package_dirs=(
  "$extra_dir/chill-sharp-ts-client"
  "$extra_dir/chill-sharp-ng-client"
  "$extra_dir/chill-sharp-react-client"
  "$extra_dir/chill-sharp-vue-client"
  "$extra_dir/chill-sharp-ui-core"
)
package_modes=(shared-folder shared-folder shared-folder shared-folder shared-folder)
package_shared_folders=(
  "$default_shared_folder"
  "$default_shared_folder"
  "$default_shared_folder"
  "$default_shared_folder"
  "$default_shared_folder"
)

nuget_labels=(ChillSharp ChillSharp.Client)
nuget_project_paths=(
  "$repository_root/ChillSharp/ChillSharp.csproj"
  "$repository_root/ChillSharp.Client/ChillSharp.Client.csproj"
)

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
  {
    printf 'export %s=%s\n' "$name" "$quoted_value"
  } >> "$temp_file"

  mv -- "$temp_file" "$env_file"
}

read_shared_folder_setting() {
  local prompt="$1"
  local current_value="$2"
  local input_value

  read -r -p "$prompt [$current_value]: " input_value
  if [[ -z "${input_value//[[:space:]]/}" ]]; then
    input_value="$current_value"
  fi

  normalize_path "$input_value"
}

configure_shared_folder_environment() {
  printf 'Shared folder configuration\n'
  printf '===========================\n\n'
  printf "Values are stored in '%s' for future shell sessions.\n\n" "$env_file"

  default_shared_folder="$(read_shared_folder_setting \
    'CHILL_NPM_SHARED_FOLDER' \
    "$default_shared_folder")"
  default_nuget_shared_folder="$(read_shared_folder_setting \
    'CHILL_NUGET_SHARED_FOLDER' \
    "$default_nuget_shared_folder")"

  export CHILL_NPM_SHARED_FOLDER="$default_shared_folder"
  export CHILL_NUGET_SHARED_FOLDER="$default_nuget_shared_folder"
  persist_user_env_var CHILL_NPM_SHARED_FOLDER "$default_shared_folder"
  persist_user_env_var CHILL_NUGET_SHARED_FOLDER "$default_nuget_shared_folder"

  local index
  for index in "${!package_shared_folders[@]}"; do
    package_shared_folders[$index]="$default_shared_folder"
  done
  nuget_shared_folder="$default_nuget_shared_folder"

  printf '\nSaved shared folder configuration.\n'
  printf 'NPM: %s\n' "$CHILL_NPM_SHARED_FOLDER"
  printf 'NuGet: %s\n\n' "$CHILL_NUGET_SHARED_FOLDER"
}

pause_for_user() {
  printf '\n'
  read -r -p 'Press Enter to continue' _
}

trim_slash() {
  local value="$1"
  value="${value%/}"
  printf '%s\n' "$value"
}

normalize_path() {
  local path="$1"
  mkdir -p -- "$(dirname -- "$path")" >/dev/null 2>&1 || true
  python3 -c 'import os, sys; print(os.path.abspath(sys.argv[1]))' "$path"
}

mode_label() {
  local mode="$1"
  local shared_folder="${2:-}"

  case "$mode" in
    shared-folder) printf 'Shared npm folder (%s)\n' "$shared_folder" ;;
    private-registry) printf 'Private npm registry (FUTURE IMPLEMENTATION)\n' ;;
    public-npm) printf 'Public npm (FUTURE IMPLEMENTATION)\n' ;;
    *) printf '%s\n' "$mode" ;;
  esac
}

read_menu_choice() {
  local prompt="$1"
  shift
  local valid_choices=("$@")
  local choice valid

  while true; do
    read -r -p "$prompt: " choice
    for valid in "${valid_choices[@]}"; do
      if [[ "$choice" == "$valid" ]]; then
        printf '%s\n' "$choice"
        return 0
      fi
    done

    printf 'Invalid choice. Valid options: %s\n' "${valid_choices[*]}" >&2
  done
}

show_package_table() {
  printf '\nPublishable packages:\n'

  local index
  for index in "${!package_labels[@]}"; do
    printf '%d. %s [%s]\n' \
      "$((index + 1))" \
      "${package_labels[$index]}" \
      "$(mode_label "${package_modes[$index]}" "${package_shared_folders[$index]}")"
  done

  printf 'A. All packages\n'
}

select_packages() {
  show_package_table >&2
  printf '\n' >&2

  local valid_choices=(A)
  local index
  for index in "${!package_labels[@]}"; do
    valid_choices+=("$((index + 1))")
  done

  local choice
  choice="$(read_menu_choice 'Select package number or A for all' "${valid_choices[@]}")"
  if [[ "$choice" == A ]]; then
    printf '%s\n' "${!package_labels[@]}"
    return 0
  fi

  printf '%s\n' "$((choice - 1))"
}

set_publish_mode() {
  printf '\nPublish targets:\n'
  printf '1. extra npm packages\n'
  printf '2. ChillSharp NuGet packages\n\n'

  local target_choice
  target_choice="$(read_menu_choice 'Select publish target' 1 2)"

  case "$target_choice" in
    1)
      mapfile -t selected_indexes < <(select_packages)

      printf '\nPublish modes:\n'
      printf '1. Shared npm folder\n'
      printf '2. Private npm registry (FUTURE IMPLEMENTATION)\n'
      printf '3. Public npm (FUTURE IMPLEMENTATION)\n\n'

      local mode_choice
      mode_choice="$(read_menu_choice 'Select publish mode' 1 2 3)"

      case "$mode_choice" in
        1)
          local first_index current_folder shared_folder
          first_index="${selected_indexes[0]}"
          current_folder="${package_shared_folders[$first_index]}"
          read -r -p "Shared folder path [$current_folder]: " shared_folder
          if [[ -z "${shared_folder//[[:space:]]/}" ]]; then
            shared_folder="$current_folder"
          fi
          shared_folder="$(normalize_path "$shared_folder")"

          local index
          for index in "${selected_indexes[@]}"; do
            package_modes[$index]=shared-folder
            package_shared_folders[$index]="$shared_folder"
          done
          ;;
        2)
          local index
          for index in "${selected_indexes[@]}"; do
            package_modes[$index]=private-registry
          done
          ;;
        3)
          local index
          for index in "${selected_indexes[@]}"; do
            package_modes[$index]=public-npm
          done
          ;;
      esac

      printf '\nUpdated package configuration:\n'
      local index
      for index in "${selected_indexes[@]}"; do
        printf -- '- %s: %s\n' \
          "${package_labels[$index]}" \
          "$(mode_label "${package_modes[$index]}" "${package_shared_folders[$index]}")"
      done
      ;;
    2)
      local nuget_folder
      read -r -p "NuGet shared folder path [$nuget_shared_folder]: " nuget_folder
      if [[ -n "${nuget_folder//[[:space:]]/}" ]]; then
        nuget_shared_folder="$nuget_folder"
      fi

      printf '\nUpdated NuGet package output folder: %s\n' "$nuget_shared_folder"
      ;;
  esac
}

package_json_value() {
  local package_json_path="$1"
  local key="$2"

  require_command jq
  [[ -f "$package_json_path" ]] || die "Could not find package.json at '$package_json_path'."
  jq -er ".$key // empty" "$package_json_path"
}

normalized_npm_archive_name() {
  local package_name="$1"
  local package_version="$2"
  package_name="${package_name#@}"
  package_name="${package_name//\//-}"
  printf '%s-%s.tgz\n' "$package_name" "$package_version"
}

package_archive_metadata() {
  local package_index="$1"
  local package_json_path="${package_dirs[$package_index]}/package.json"
  local package_name package_version archive_name

  package_name="$(package_json_value "$package_json_path" name)"
  package_version="$(package_json_value "$package_json_path" version)"

  [[ -n "${package_name//[[:space:]]/}" ]] || die "Package name is missing from '$package_json_path'."
  [[ -n "${package_version//[[:space:]]/}" ]] || die "Package version is missing from '$package_json_path'."

  archive_name="$(normalized_npm_archive_name "$package_name" "$package_version")"
  printf '%s\t%s\t%s\n' "$package_name" "$package_version" "$archive_name"
}

run_npm_in_dir() {
  local directory="$1"
  shift

  (cd "$directory" && npm "$@")
}

publish_standard_npm_package() {
  local package_index="$1"
  local package_dir="${package_dirs[$package_index]}"
  local shared_folder
  local package_name package_version archive_name archive_path

  [[ -d "$package_dir" ]] || die "Could not find package directory at '$package_dir'."
  shared_folder="$(normalize_path "${package_shared_folders[$package_index]}")"
  IFS=$'\t' read -r package_name package_version archive_name < <(package_archive_metadata "$package_index")

  printf "Installing dependencies for %s in '%s'...\n" "$package_name" "$package_dir"
  run_npm_in_dir "$package_dir" install || die "npm install failed for '$package_name'."

  printf 'Building %s %s...\n' "$package_name" "$package_version"
  run_npm_in_dir "$package_dir" run build || die "npm run build failed for '$package_name'."

  mkdir -p -- "$shared_folder"
  archive_path="$shared_folder/$archive_name"

  printf "Packing %s into '%s'...\n" "$package_name" "$shared_folder"
  run_npm_in_dir "$package_dir" pack --ignore-scripts --pack-destination "$shared_folder" ||
    die "npm pack failed for '$package_name'."

  [[ -f "$archive_path" ]] || die "npm pack completed for '$package_name', but '$archive_path' was not found."

  printf '\nPackage published to shared folder successfully: %s\n' "$archive_path"
}

publish_ui_core_package() {
  local package_index="$1"
  local package_dir="${package_dirs[$package_index]}"
  local shared_folder
  local package_json_path="$package_dir/package.json"
  local dist_path="$package_dir/dist"
  local node_modules_path="$package_dir/node_modules"
  local ng_packagr_package_path="$node_modules_path/ng-packagr/package.json"
  local package_name package_version archive_name archive_path

  [[ -d "$package_dir" ]] || die "Could not find package directory at '$package_dir'."
  shared_folder="$(normalize_path "${package_shared_folders[$package_index]}")"
  IFS=$'\t' read -r package_name package_version archive_name < <(package_archive_metadata "$package_index")

  local dependency_index dependency_archive dependency_archive_path
  for dependency_index in 0 1; do
    IFS=$'\t' read -r _ _ dependency_archive < <(package_archive_metadata "$dependency_index")
    dependency_archive_path="$shared_folder/$dependency_archive"
    [[ -f "$dependency_archive_path" ]] ||
      die "Expected shared package archive '$dependency_archive_path' was not found."
  done

  printf "Building %s %s from '%s'...\n" "$package_name" "$package_version" "$package_dir"
  (
    cd "$package_dir"
    printf "Installing shared client package archives into '%s'...\n" "$package_dir"
    npm install --no-save "$shared_folder/$(package_archive_metadata 0 | cut -f3)" "$shared_folder/$(package_archive_metadata 1 | cut -f3)" ||
      die 'npm install for shared client packages failed.'

    if [[ ! -f "$ng_packagr_package_path" ]]; then
      printf "Build dependencies are missing in '%s'. Running npm install...\n" "$node_modules_path"
      npm install || die "npm install failed. Install dependencies in '$package_dir' and try again."
    fi

    npm run build || die 'npm run build failed.'
  )

  [[ -d "$dist_path" ]] || die "Build completed but dist folder was not found at '$dist_path'."

  mkdir -p -- "$shared_folder"
  archive_path="$shared_folder/$archive_name"

  printf "Packing built library from '%s' into '%s'...\n" "$dist_path" "$shared_folder"
  npm pack "$dist_path" --pack-destination "$shared_folder" || die 'npm pack failed.'

  [[ -f "$archive_path" ]] || die "npm pack completed, but the file was not found at '$archive_path'."

  printf '\nPackage published to shared folder successfully.\n'
  printf 'Archive: %s\n' "$archive_path"
  printf 'Install with: npm install %s\n' "$archive_path"
}

publish_package() {
  local package_index="$1"
  local mode="${package_modes[$package_index]}"

  case "$mode" in
    shared-folder)
      printf "\nPublishing %s to shared folder '%s'...\n" \
        "${package_labels[$package_index]}" \
        "${package_shared_folders[$package_index]}"

      if [[ "${package_keys[$package_index]}" == ui-core ]]; then
        publish_ui_core_package "$package_index"
      else
        publish_standard_npm_package "$package_index"
      fi
      ;;
    private-registry)
      warn "Publishing ${package_labels[$package_index]} to a private npm registry is FUTURE IMPLEMENTATION."
      ;;
    public-npm)
      warn "Publishing ${package_labels[$package_index]} to public npm is FUTURE IMPLEMENTATION."
      ;;
    *)
      die "Unsupported publish mode '$mode' for '${package_labels[$package_index]}'."
      ;;
  esac
}

show_nuget_package_table() {
  printf '\nPublishable ChillSharp NuGet packages:\n'

  local index
  for index in "${!nuget_labels[@]}"; do
    printf '%d. %s\n' "$((index + 1))" "${nuget_labels[$index]}"
  done

  printf 'A. All NuGet packages\n'
}

select_nuget_packages() {
  show_nuget_package_table >&2
  printf '\n' >&2

  local valid_choices=(A)
  local index
  for index in "${!nuget_labels[@]}"; do
    valid_choices+=("$((index + 1))")
  done

  local choice
  choice="$(read_menu_choice 'Select package number or A for all' "${valid_choices[@]}")"
  if [[ "$choice" == A ]]; then
    printf '%s\n' "${!nuget_labels[@]}"
    return 0
  fi

  printf '%s\n' "$((choice - 1))"
}

publish_nuget_packages() {
  mapfile -t selected_indexes < <(select_nuget_packages)
  mkdir -p -- "$nuget_shared_folder"

  local index project_path
  for index in "${selected_indexes[@]}"; do
    project_path="${nuget_project_paths[$index]}"
    [[ -f "$project_path" ]] || die "Could not find project file at '$project_path'."

    printf "\nPacking %s into '%s'...\n" "${nuget_labels[$index]}" "$nuget_shared_folder"
    dotnet pack "$project_path" -c Release -o "$nuget_shared_folder" ||
      die "dotnet pack failed for '${nuget_labels[$index]}'."
  done

  printf "\nNuGet package publication completed to '%s'.\n" "$nuget_shared_folder"
}

publish_packages() {
  printf '\nPublish targets:\n'
  printf '1. extra npm packages\n'
  printf '2. ChillSharp NuGet packages\n\n'

  local target_choice
  target_choice="$(read_menu_choice 'Select publish target' 1 2)"

  case "$target_choice" in
    1)
      mapfile -t selected_indexes < <(select_packages)
      local index
      for index in "${selected_indexes[@]}"; do
        publish_package "$index"
      done
      ;;
    2)
      publish_nuget_packages
      ;;
  esac

  printf '\nPublish action completed.\n'
}

copy_filtered_item() {
  local source_path="$1"
  local destination_parent_path="$2"
  shift 2
  local excluded_names=("$@")
  local item_name
  item_name="$(basename -- "$source_path")"

  local excluded
  for excluded in "${excluded_names[@]}"; do
    if [[ "$item_name" == "$excluded" ]]; then
      return 0
    fi
  done

  local destination_path="$destination_parent_path/$item_name"
  if [[ -d "$source_path" ]]; then
    mkdir -p -- "$destination_path"
    local child
    while IFS= read -r -d '' child; do
      copy_filtered_item "$child" "$destination_path" "${excluded_names[@]}"
    done < <(find "$source_path" -mindepth 1 -maxdepth 1 -print0)
    return 0
  fi

  cp -f -- "$source_path" "$destination_path"
}

copy_template_project() {
  local template_path="$1"
  local destination_prompt="$2"
  local template_label="$3"
  shift 3
  local excluded_names=("$@")

  [[ -d "$template_path" ]] || die "Could not find $template_label template at '$template_path'."

  printf '\n'
  local destination_input
  read -r -p "$destination_prompt: " destination_input
  [[ -n "${destination_input//[[:space:]]/}" ]] || die 'Destination folder is required.'

  local destination_path
  destination_path="$(normalize_path "$destination_input")"

  if [[ -e "$destination_path" ]]; then
    if find "$destination_path" -mindepth 1 -maxdepth 1 -print -quit | grep -q .; then
      die "Destination folder '$destination_path' already exists and is not empty."
    fi
  else
    mkdir -p -- "$destination_path"
  fi

  local item
  while IFS= read -r -d '' item; do
    copy_filtered_item "$item" "$destination_path" "${excluded_names[@]}"
  done < <(find "$template_path" -mindepth 1 -maxdepth 1 -print0)

  printf "\n%s template copied to '%s'.\n" "$template_label" "$destination_path"
  printf '%s\n' "$destination_path"
}

package_index_by_key() {
  local key="$1"
  local index

  for index in "${!package_keys[@]}"; do
    if [[ "${package_keys[$index]}" == "$key" ]]; then
      printf '%s\n' "$index"
      return 0
    fi
  done

  die "Could not find package configuration for key '$key'."
}

file_dependency_spec() {
  local path="$1"
  local full_path
  full_path="$(normalize_path "$path")"
  python3 -c 'from pathlib import Path; import sys; print(Path(sys.argv[1]).as_uri())' "$full_path"
}

relative_file_dependency_spec() {
  local base_path="$1"
  local path="$2"
  local relative_path

  relative_path="$(python3 -c 'import os, sys; print(os.path.relpath(os.path.abspath(sys.argv[2]), os.path.abspath(sys.argv[1])).replace(os.sep, "/"))' "$base_path" "$path")"
  if [[ "$relative_path" != .* ]]; then
    relative_path="./$relative_path"
  fi

  printf 'file:%s\n' "$relative_path"
}

chillsharp_package_version() {
  local project_path="$repository_root/ChillSharp/ChillSharp.csproj"
  [[ -f "$project_path" ]] || die "Could not find ChillSharp project at '$project_path'."

  local version
  version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$project_path" | head -n 1)"
  [[ -n "${version//[[:space:]]/}" ]] || die "Could not determine ChillSharp package version from '$project_path'."

  printf '%s\n' "$version"
}

chillsharp_package_output_folder() {
  local project_path="$repository_root/ChillSharp/ChillSharp.csproj"
  [[ -f "$project_path" ]] || die "Could not find ChillSharp project at '$project_path'."

  local output_path
  output_path="$(sed -n 's:.*<PackageOutputPath>\(.*\)</PackageOutputPath>.*:\1:p' "$project_path" | head -n 1)"
  [[ -n "${output_path//[[:space:]]/}" ]] || return 0

  normalize_path "$output_path"
}

chillsharp_package_archive_path() {
  local package_version="$1"
  local archive_name="ChillSharp.$package_version.nupkg"
  local package_output_folder
  package_output_folder="$(chillsharp_package_output_folder || true)"

  local candidate_paths=(
    "$nuget_shared_folder/$archive_name"
  )
  if [[ -n "${package_output_folder//[[:space:]]/}" ]]; then
    candidate_paths+=("$package_output_folder/$archive_name")
  fi
  candidate_paths+=("$api_template_path/nupkgs/$archive_name")

  local candidate_path
  for candidate_path in "${candidate_paths[@]}"; do
    if [[ -f "$candidate_path" ]]; then
      normalize_path "$candidate_path"
      return 0
    fi
  done

  die "Could not find '$archive_name'. Pack or publish ChillSharp $package_version first."
}

sync_chillsharp_package_to_local_folder() {
  local destination_path="$1"
  local package_version="$2"
  local package_folder_path="$destination_path/nupkgs"
  local archive_name="ChillSharp.$package_version.nupkg"
  local destination_archive_path="$package_folder_path/$archive_name"
  local source_archive_path

  mkdir -p -- "$package_folder_path"
  source_archive_path="$(chillsharp_package_archive_path "$package_version")"

  local existing_package
  while IFS= read -r -d '' existing_package; do
    if [[ "$(normalize_path "$existing_package")" != "$(normalize_path "$destination_archive_path")" ]]; then
      rm -f -- "$existing_package"
    fi
  done < <(find "$package_folder_path" -maxdepth 1 -type f -name 'ChillSharp.*.nupkg' -print0)

  if [[ "$(normalize_path "$source_archive_path")" != "$(normalize_path "$destination_archive_path")" ]]; then
    cp -f -- "$source_archive_path" "$destination_archive_path"
  fi

  printf '%s\n' "$destination_archive_path"
}

shared_archive_dependency_spec() {
  local package_index="$1"
  [[ "${package_modes[$package_index]}" == shared-folder ]] ||
    die "Package '${package_labels[$package_index]}' is configured for unsupported publish mode '${package_modes[$package_index]}'."

  local package_name package_version archive_name archive_path dependency_spec
  IFS=$'\t' read -r package_name package_version archive_name < <(package_archive_metadata "$package_index")
  archive_path="${package_shared_folders[$package_index]}/$archive_name"
  [[ -f "$archive_path" ]] ||
    die "Expected shared package archive '$archive_path' was not found for '${package_labels[$package_index]}'. Publish the package first."

  dependency_spec="$(file_dependency_spec "$archive_path")"
  printf '%s\t%s\t%s\t%s\n' "$package_name" "$archive_name" "$dependency_spec" "$archive_path"
}

set_ui_template_package_source() {
  local destination_path="$1"
  local template_package_json_path="$destination_path/package.json"
  local embedded_packages_path="$destination_path/packages"
  local required_keys=(ui-core ng-client ts-client)

  require_command jq

  local key index
  for key in "${required_keys[@]}"; do
    index="$(package_index_by_key "$key")"
    if [[ "${package_modes[$index]}" != shared-folder ]]; then
      warn "UI template package source update is skipped because ${package_labels[$index]} publish mode is '${package_modes[$index]}'."
      return 0
    fi
  done

  [[ -f "$template_package_json_path" ]] || die "Could not find template package.json at '$template_package_json_path'."
  jq -e '.dependencies | type == "object"' "$template_package_json_path" >/dev/null ||
    die "Template package.json at '$template_package_json_path' does not define a dependencies object."

  mkdir -p -- "$embedded_packages_path"

  local configured_lines=()
  for key in "${required_keys[@]}"; do
    index="$(package_index_by_key "$key")"

    local package_name package_version archive_name embedded_archive_path source_archive_path
    IFS=$'\t' read -r package_name package_version archive_name < <(package_archive_metadata "$index")
    embedded_archive_path="$embedded_packages_path/$archive_name"

    if shared_archive_line="$(shared_archive_dependency_spec "$index" 2>/dev/null)"; then
      IFS=$'\t' read -r _ _ _ source_archive_path <<<"$shared_archive_line"
      cp -f -- "$source_archive_path" "$embedded_archive_path"
    else
      [[ -f "$embedded_archive_path" ]] || shared_archive_dependency_spec "$index" >/dev/null
      source_archive_path="$embedded_archive_path"
    fi

    local dependency_spec temp_json
    dependency_spec="$(relative_file_dependency_spec "$destination_path" "$embedded_archive_path")"
    temp_json="$(mktemp)"
    jq --arg name "$package_name" --arg spec "$dependency_spec" \
      '.dependencies[$name] = $spec' \
      "$template_package_json_path" > "$temp_json"
    mv -- "$temp_json" "$template_package_json_path"

    configured_lines+=("Embedded $package_name into '$embedded_archive_path' from '$source_archive_path'.")
  done

  printf '%s\n' "${configured_lines[@]}"
}

set_api_template_package_source() {
  local destination_path="$1"
  local template_project_path="$destination_path/ChillSharp.Template.csproj"
  local package_version

  [[ -f "$template_project_path" ]] || die "Could not find template project at '$template_project_path'."

  package_version="$(chillsharp_package_version)"

  CHILL_PACKAGE_VERSION="$package_version" perl -0pi -e '
    our $matched;
    my $replacement = qq{<PackageReference Include="ChillSharp" Version="$ENV{CHILL_PACKAGE_VERSION}" />};
    if (!$matched && s{<ProjectReference Include="\.\.\\ChillSharp\.AspNetCore\\ChillSharp\.AspNetCore\.csproj"\s*/>}{$replacement}s) {
      $matched = 1;
    }
    if (!$matched && s{<PackageReference Include="ChillSharp" Version="[^"]+"\s*/>}{$replacement}s) {
      $matched = 1;
    }
    END { exit($matched ? 0 : 2); }
  ' "$template_project_path" || die "Could not update the ChillSharp package reference in '$template_project_path'."

  local nuget_config_path="$destination_path/NuGet.Config"
  printf '%s\n' \
    '<?xml version="1.0" encoding="utf-8"?>' \
    '<configuration>' \
    '  <packageSources>' \
    '    <clear />' \
    '    <add key="local-chillsharp" value="./nupkgs" />' \
    '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />' \
    '  </packageSources>' \
    '</configuration>' > "$nuget_config_path"

  local local_archive_path
  local_archive_path="$(sync_chillsharp_package_to_local_folder "$destination_path" "$package_version")"

  printf "Configured API template to restore ChillSharp %s from '%s'.\n" "$package_version" "$local_archive_path"
}

create_ui_from_template() {
  local destination_path
  destination_path="$(copy_template_project "$ui_template_path" 'Destination folder for the new UI project' 'UI' node_modules dist .angular | tail -n 1)"
  set_ui_template_package_source "$destination_path"
}

create_api_from_template() {
  local package_version destination_path
  package_version="$(chillsharp_package_version)"
  sync_chillsharp_package_to_local_folder "$api_template_path" "$package_version" >/dev/null

  destination_path="$(copy_template_project "$api_template_path" 'Destination folder for the new API project' 'API' bin obj .vs | tail -n 1)"
  set_api_template_package_source "$destination_path"
}

is_within_root() {
  local path root normalized_path normalized_root
  path="$1"
  root="$2"
  normalized_path="$(trim_slash "$(normalize_path "$path")")"
  normalized_root="$(trim_slash "$(normalize_path "$root")")"

  [[ "$normalized_path" == "$normalized_root" || "$normalized_path" == "$normalized_root/"* ]]
}

is_same_or_child_path() {
  local path parent normalized_path normalized_parent
  path="$1"
  parent="$2"
  normalized_path="$(trim_slash "$(normalize_path "$path")")"
  normalized_parent="$(trim_slash "$(normalize_path "$parent")")"

  [[ "$normalized_path" == "$normalized_parent" || "$normalized_path" == "$normalized_parent/"* ]]
}

cleanup_workspace() {
  local cleanup_targets=()
  local match

  while IFS= read -r -d '' match; do
    cleanup_targets+=("$match")
  done < <(find "$extra_dir" -type d \( -name node_modules -o -name .angular -o -name __pycache__ \) -print0)

  local py_client_dir="$extra_dir/chill-sharp-py-client"
  if [[ -d "$py_client_dir" ]]; then
    while IFS= read -r -d '' match; do
      cleanup_targets+=("$match")
    done < <(find "$py_client_dir" -mindepth 1 -maxdepth 1 -type d \( -name build -o -name '*.egg-info' \) -print0)
  fi

  [[ -d "$repository_root/.tmp-npm-shared" ]] && cleanup_targets+=("$repository_root/.tmp-npm-shared")
  [[ -d "$repository_root/build-logs" ]] && cleanup_targets+=("$repository_root/build-logs")

  if [[ "${#cleanup_targets[@]}" -eq 0 ]]; then
    printf '\nNo cleanup targets were found.\n'
    return 0
  fi

  mapfile -t unique_targets < <(printf '%s\n' "${cleanup_targets[@]}" | sort -u)
  local filtered_targets=()
  local target existing_target is_covered

  for target in "${unique_targets[@]}"; do
    is_within_root "$target" "$repository_root" || continue
    is_covered=false

    for existing_target in "${filtered_targets[@]}"; do
      if is_same_or_child_path "$target" "$existing_target"; then
        is_covered=true
        break
      fi
    done

    if [[ "$is_covered" == false ]]; then
      filtered_targets+=("$target")
    fi
  done

  if [[ "${#filtered_targets[@]}" -eq 0 ]]; then
    printf '\nNo cleanup targets were found.\n'
    return 0
  fi

  printf '\nCleanup will remove:\n'
  printf -- '- %s\n' "${filtered_targets[@]}"

  printf '\n'
  local confirmation
  read -r -p 'Type YES to continue: ' confirmation
  if [[ "$confirmation" != YES ]]; then
    printf 'Cleanup cancelled.\n'
    return 0
  fi

  for target in "${filtered_targets[@]}"; do
    [[ -e "$target" ]] && rm -rf -- "$target"
  done

  printf '\nCleanup completed.\n'
}

show_main_menu() {
  while true; do
    clear || true
    printf 'Extra Publish Menu\n'
    printf '==================\n\n'
    printf '1. Select publish mode\n'
    printf '2. Publish package\n'
    printf '3. Create UI from template\n'
    printf '4. Create API from template\n'
    printf '5. Cleanup\n'
    printf '0. Exit\n\n'

    local choice
    choice="$(read_menu_choice 'Select an option' 1 2 3 4 5 0)"

    case "$choice" in
      1) set_publish_mode; pause_for_user ;;
      2) publish_packages; pause_for_user ;;
      3) create_ui_from_template; pause_for_user ;;
      4) create_api_from_template; pause_for_user ;;
      5) cleanup_workspace; pause_for_user ;;
      0) return 0 ;;
    esac
  done
}

if [[ ! -d "$extra_dir" ]]; then
  die "Could not find extra directory at '$extra_dir'."
fi

configure_shared_folder_environment
show_main_menu
