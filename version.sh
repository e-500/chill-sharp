#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
props_path="$script_dir/Directory.Build.props"

current_version="$(perl -ne 'print $1 if /<ChillSharpVersion>(\d+\.\d+\.\d+)<\/ChillSharpVersion>/' "$props_path")"
if [[ -z "$current_version" ]]; then
  echo "Could not find a three-part ChillSharpVersion in $props_path." >&2
  exit 1
fi

part="${1:-}"
if [[ -z "$part" ]]; then
  read -r -p 'Increase which version part? (major, minor, build): ' part
fi

IFS='.' read -r major minor build <<< "$current_version"
case "${part,,}" in
  major) major=$((major + 1)); minor=0; build=0 ;;
  minor) minor=$((minor + 1)); build=0 ;;
  build) build=$((build + 1)) ;;
  *) echo 'Choose major, minor, or build.' >&2; exit 1 ;;
esac

new_version="$major.$minor.$build"
CHILLSHARP_VERSION="$new_version" perl -0pi -e \
  's#<ChillSharpVersion>\d+\.\d+\.\d+</ChillSharpVersion>#<ChillSharpVersion>$ENV{CHILLSHARP_VERSION}</ChillSharpVersion>#' \
  "$props_path"

package_json_paths=(
  "$script_dir/extra/chill-sharp-ts-client/package.json"
  "$script_dir/extra/chill-sharp-ng-client/package.json"
  "$script_dir/extra/chill-sharp-react-client/package.json"
  "$script_dir/extra/chill-sharp-vue-client/package.json"
  "$script_dir/extra/chill-sharp-ui-core/package.json"
  "$script_dir/extra/chill-sharp-ui-template/package.json"
)

for manifest_path in "${package_json_paths[@]}"; do
  [[ -f "$manifest_path" ]] || continue
  CHILLSHARP_VERSION="$new_version" perl -0pi -e '
    s/("version"\s*:\s*")\d+\.\d+\.\d+("\s*,?)/$1$ENV{CHILLSHARP_VERSION}$2/;
    s/("\@chill-sharp\/(?:ts-client|ng-client|ui-core)"\s*:\s*"\^)\d+\.\d+\.\d+/$1$ENV{CHILLSHARP_VERSION}/g;
    s#(file:\./packages/chill-sharp-(?:ts-client|ng-client|ui-core)-)\d+\.\d+\.\d+(\.tgz)#$1$ENV{CHILLSHARP_VERSION}$2#g;
  ' "$manifest_path"
done

package_lock_paths=(
  "$script_dir/extra/chill-sharp-ts-client/package-lock.json"
  "$script_dir/extra/chill-sharp-ng-client/package-lock.json"
  "$script_dir/extra/chill-sharp-react-client/package-lock.json"
  "$script_dir/extra/chill-sharp-vue-client/package-lock.json"
  "$script_dir/extra/chill-sharp-ui-template/package-lock.json"
)

for lock_path in "${package_lock_paths[@]}"; do
  [[ -f "$lock_path" ]] || continue
  lock_version="$(perl -0777 -ne 'print $1 if /^\s*\{.*?"version"\s*:\s*"(\d+\.\d+\.\d+)"/s' "$lock_path")"
  [[ -n "$lock_version" ]] || continue
  CHILLSHARP_OLD_VERSION="$lock_version" CHILLSHARP_VERSION="$new_version" perl -0pi -e \
    's/\Q$ENV{CHILLSHARP_OLD_VERSION}\E/$ENV{CHILLSHARP_VERSION}/g' \
    "$lock_path"
done

python_project_path="$script_dir/extra/chill-sharp-py-client/pyproject.toml"
if [[ -f "$python_project_path" ]]; then
  CHILLSHARP_VERSION="$new_version" perl -0pi -e \
    's/^(version\s*=\s*")\d+\.\d+\.\d+(")/$1$ENV{CHILLSHARP_VERSION}$2/m' \
    "$python_project_path"
fi

echo "ChillSharp version: $current_version -> $new_version"
echo 'Updated .NET projects, JavaScript package manifests and locks, and the Python package manifest.'
echo 'Run: dotnet test ./ChillSharp.Test/ChillSharp.Test.csproj'

read -r -p "Create Git commit 'Switched to $new_version'? (y/N): " create_commit
case "${create_commit,,}" in
  y|yes)
    if ! git -C "$script_dir" diff --cached --quiet; then
      echo 'The Git index already contains staged changes. No commit was created.' >&2
      exit 0
    fi

    release_files=(
      'Directory.Build.props'
      'extra/chill-sharp-ts-client/package.json'
      'extra/chill-sharp-ng-client/package.json'
      'extra/chill-sharp-react-client/package.json'
      'extra/chill-sharp-vue-client/package.json'
      'extra/chill-sharp-ui-core/package.json'
      'extra/chill-sharp-ui-template/package.json'
      'extra/chill-sharp-ts-client/package-lock.json'
      'extra/chill-sharp-ng-client/package-lock.json'
      'extra/chill-sharp-react-client/package-lock.json'
      'extra/chill-sharp-vue-client/package-lock.json'
      'extra/chill-sharp-ui-template/package-lock.json'
      'extra/chill-sharp-py-client/pyproject.toml'
    )
    git -C "$script_dir" add -- "${release_files[@]}"
    if ! git -C "$script_dir" diff --cached --quiet; then
      git -C "$script_dir" commit -m "Switched to $new_version"
    else
      echo 'No version file changes were available to commit.'
    fi
    ;;
esac
