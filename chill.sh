#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
publish_script_path="$script_dir/extra/publish.sh"
core_test_project_path="$script_dir/ChillSharp.Test/ChillSharp.Test.csproj"
ui_core_path="$script_dir/extra/chill-sharp-ui-core"

pause_for_user() { read -r -p 'Press Enter to continue' _; }
read_menu_choice() { local prompt="$1"; shift; local choice; while true; do read -r -p "$prompt: " choice; for valid_choice in "$@"; do [[ "$choice" == "$valid_choice" ]] && { printf '%s\n' "$choice"; return; }; done; printf 'Invalid choice. Valid options: %s\n' "$*" >&2; done; }

show_test_menu() {
  while true; do
    clear || true
    printf 'Test Menu\n=========\n\n1. Test ChillSharp core (C#)\n2. Test ChillSharp Ui Core (Angular)\n0. Back\n\n'
    case "$(read_menu_choice 'Select an option' 1 2 0)" in
      1) dotnet test "$core_test_project_path"; pause_for_user ;;
      2) (cd "$ui_core_path" && npm test); pause_for_user ;;
      0) return ;;
    esac
  done
}

while true; do
  clear || true
  printf 'ChillSharp Menu\n===============\n\n1. Publish\n2. Test\n0. Exit\n\n'
  case "$(read_menu_choice 'Select an option' 1 2 0)" in
    1) "$publish_script_path" ;;
    2) show_test_menu ;;
    0) exit 0 ;;
  esac
done
