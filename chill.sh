#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
publish_script_path="$script_dir/extra/publish.sh"

if [[ ! -f "$publish_script_path" ]]; then
  printf "Error: Could not find publish script at '%s'.\n" "$publish_script_path" >&2
  exit 1
fi

exec "$publish_script_path" "$@"
