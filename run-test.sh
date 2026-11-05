#!/usr/bin/env bash
set -euo pipefail

configuration="${1:-Release}"
repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
test_project="$repository_root/ChillSharp.Test/ChillSharp.Test.csproj"
results_directory="$repository_root/TestResults"
coverage_prefix="$results_directory/coverage"

mkdir -p "$results_directory"

dotnet test "$test_project" \
    --configuration "$configuration" \
    --results-directory "$results_directory" \
    --logger "trx;LogFileName=tests.trx" \
    "-p:CollectCoverage=true" \
    "-p:CoverletOutput=$coverage_prefix" \
    "-p:CoverletOutputFormat=json%2ccobertura"

coverage_report="$coverage_prefix.cobertura.xml"
if [[ ! -f "$coverage_report" ]]; then
    printf 'Coverage report was not generated: %s\n' "$coverage_report" >&2
    exit 1
fi

awk '
function attr(line, name, token, start, tail, end) {
    token = name "=\""
    start = index(line, token)
    if (!start) return ""
    tail = substr(line, start + length(token))
    end = index(tail, "\"")
    return end ? substr(tail, 1, end - 1) : ""
}
function percent(rate) {
    value = sprintf("%.2f", int(rate * 10000) / 100)
    sub(/0+$/, "", value)
    sub(/\.$/, "", value)
    return value "%"
}
function border() {
    printf "+--------------------------------+--------+--------+--------+\n"
}
function finish_package(    line_rate, branch_rate, method_rate) {
    line_rate = package_lines ? package_covered_lines / package_lines : 1
    branch_rate = package_branches ? package_covered_branches / package_branches : 1
    method_rate = package_methods ? package_covered_methods / package_methods : 1
    count++
    names[count] = package_name
    lines[count] = line_rate
    branches[count] = branch_rate
    methods[count] = method_rate
    average_line += int(line_rate * 10000) / 10000
    average_branch += int(branch_rate * 10000) / 10000
    average_method += int(method_rate * 10000) / 10000
    total_lines += package_lines
    total_covered_lines += package_covered_lines
    total_branches += package_branches
    total_covered_branches += package_covered_branches
    total_methods += package_methods
    total_covered_methods += package_covered_methods
}
BEGIN {
    count = 0
    average_line = 0
    average_branch = 0
    average_method = 0
    total_lines = 0
    total_covered_lines = 0
    total_branches = 0
    total_covered_branches = 0
    total_methods = 0
    total_covered_methods = 0
}
/<package / {
    package_name = attr($0, "name")
    package_lines = 0
    package_covered_lines = 0
    package_branches = 0
    package_covered_branches = 0
    package_methods = 0
    package_covered_methods = 0
}
/<class / {
    class_file = attr($0, "filename")
}
/<line / {
    line_number = attr($0, "number")
    line_hits = attr($0, "hits") + 0
    line_key = class_file SUBSEP line_number
    if (!(line_key in seen_lines)) {
        seen_lines[line_key] = 1
        package_lines++
        if (line_hits > 0) {
            covered_lines[line_key] = 1
            package_covered_lines++
        }
    } else if (line_hits > 0 && !covered_lines[line_key]) {
        covered_lines[line_key] = 1
        package_covered_lines++
    }

    condition = attr($0, "condition-coverage")
    if (condition != "" && match(condition, /\([0-9]+\/[0-9]+\)/)) {
        counts = substr(condition, RSTART + 1, RLENGTH - 2)
        split(counts, parts, "/")
        package_branches += parts[2]
        package_covered_branches += parts[1]
    }
}
/<method / {
    package_methods++
    if ((attr($0, "line-rate") + 0) > 0) package_covered_methods++
}
/<\/package>/ {
    finish_package()
}
END {
    border()
    printf "| %-30s | %6s | %6s | %6s |\n", "Module", "Line", "Branch", "Method"
    border()
    for (i = 1; i <= count; i++) {
        printf "| %-30s | %6s | %6s | %6s |\n", names[i], percent(lines[i]), percent(branches[i]), percent(methods[i])
        border()
    }
    if (count) {
        total_line_rate = total_lines ? total_covered_lines / total_lines : 1
        total_branch_rate = total_branches ? total_covered_branches / total_branches : 1
        print ""
        printf "| %-30s | %6s | %6s | %6s |\n", "", "Line", "Branch", "Method"
        border()
        total_method_rate = total_methods ? total_covered_methods / total_methods : 1
        printf "| %-30s | %6s | %6s | %6s |\n", "Total", percent(total_line_rate), percent(total_branch_rate), percent(total_method_rate)
        border()
        printf "| %-30s | %6s | %6s | %6s |\n", "Average", percent(average_line / count), percent(average_branch / count), percent(average_method / count)
        border()
    }
}' "$coverage_report"

printf 'Test results: %s/tests.trx\n' "$results_directory"
printf 'Coverage reports: %s.json and %s.cobertura.xml\n' "$coverage_prefix" "$coverage_prefix"
