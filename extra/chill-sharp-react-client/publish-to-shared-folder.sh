#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
helper_script_path="$(cd -- "$script_dir/.." && pwd -P)/publish-npm-package-to-shared-folder.sh"
shared_folder="${CHILL_NPM_SHARED_FOLDER:-$HOME/source/npm-shared}"

if [[ "${1:-}" == "--shared-folder" || "${1:-}" == "-s" ]]; then
  shared_folder="${2:-}"
  shift 2
fi

exec "$helper_script_path" --package-directory "$script_dir" --shared-folder "$shared_folder" "$@"
