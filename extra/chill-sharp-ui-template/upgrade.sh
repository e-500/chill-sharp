#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
version="${1:-latest}"

cd "$script_dir"
npm install \
  "@chill-sharp/ui-core@$version" \
  "@chill-sharp/ng-client@$version" \
  "@chill-sharp/ts-client@$version"

printf "Published ChillSharp UI dependencies were upgraded to '%s'.\n" "$version"
