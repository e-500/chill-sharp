#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
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

publish_script_paths=(
  "$script_dir/chill-sharp-ts-client/publish-to-shared-folder.sh"
  "$script_dir/chill-sharp-ng-client/publish-to-shared-folder.sh"
  "$script_dir/chill-sharp-ui-core/publish-to-shared-folder.sh"
  "$script_dir/chill-sharp-react-client/publish-to-shared-folder.sh"
  "$script_dir/chill-sharp-vue-client/publish-to-shared-folder.sh"
)

for publish_script_path in "${publish_script_paths[@]}"; do
  if [[ ! -f "$publish_script_path" ]]; then
    printf "Error: Could not find publish script at '%s'.\n" "$publish_script_path" >&2
    exit 1
  fi

  "$publish_script_path" --shared-folder "$shared_folder" || {
    printf "Error: Publish script failed: '%s'.\n" "$publish_script_path" >&2
    exit 1
  }
done

printf '\nextra packages published to shared folder successfully.\n'
