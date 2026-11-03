#!/bin/bash
# This script manually runs the "Detect code changes" step from a GitHub Actions workflow
# to verify its behavior on various scenarios without network access.
# Usage: bash scripts/check-ci-changes-job.sh [workflow file]
# This script is meant to be run by hand as part of CONTRIBUTING.md section 6.

set -euo pipefail

# Create temporary work directory
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

# Default workflow file
wf="${1:-.github/workflows/ci.yml}"

# Check if workflow file exists
if [ ! -f "$wf" ]; then
    echo "Error: Workflow file '$wf' not found" >&2
    exit 2
fi

# Extract the "Detect code changes" step script
awk '
/name: Detect code changes$/ {
    found = 1
    next
}
found && /run: \|$/ {
    found2 = 1
    next
}
found2 {
    if (/^          /) {
        print substr($0, 11)
    } else {
        exit
    }
}
' <(tr -d '\r' < "$wf") > "$work/step.sh" || true

if [ ! -s "$work/step.sh" ]; then
    echo "Error: Could not extract the 'Detect code changes' step from $wf" >&2
    exit 2
fi

# Helper functions
new_repo() {
    local dir="$1"
    git -C "$dir" init -q
    git -C "$dir" config user.name check
    git -C "$dir" config user.email check@example.invalid
    git -C "$dir" config core.autocrlf false
}

commit_paths() {
    local dir="$1"
    shift
    for path in "$@"; do
        mkdir -p "$(dirname "$dir/$path")"
        echo "change" >> "$dir/$path"
    done
    git -C "$dir" add -A
    git -C "$dir" commit -qm change
    git -C "$dir" rev-parse HEAD
}

run_step() {
    local dir="$1"
    local event="$2"
    local action="$3"
    local before="$4"
    local base="$5"
    local head="$6"

    (
        cd "$dir"
        local tmpdir=$(mktemp -d "$work/run.XXXXXX")
        export EVENT_NAME="$event"
        export EVENT_ACTION="$action"
        export BEFORE_SHA="$before"
        export BASE_SHA="$base"
        export HEAD_SHA="$head"
        export RUNNER_TEMP="$tmpdir"
        export GITHUB_OUTPUT="$tmpdir/output"
        mkdir -p "$(dirname "$GITHUB_OUTPUT")"
        bash "$work/step.sh" >/dev/null
        grep -E "^code=" "$GITHUB_OUTPUT" | cut -d= -f2
    )
}

check() {
    local name="$1"
    local got="$2"
    local want="$3"
    if [ "$got" = "$want" ]; then
        echo "PASS $name"
    else
        echo "FAIL $name: got $got, want $want"
        failures=$((failures + 1))
    fi
}

# Initialize failure counter
failures=0

# Repository A setup
mkdir -p "$work/a"
repo_a="$work/a"
new_repo "$repo_a"
base_commit=$(commit_paths "$repo_a" seed.txt)
code_commit=$(commit_paths "$repo_a" src/a.cs)
docs_commit=$(commit_paths "$repo_a" docs/r.md)

# Case 1: documentation-only push to a code pull request
result=$(run_step "$repo_a" pull_request synchronize "$code_commit" "$base_commit" "$docs_commit")
check "documentation-only push to a code pull request" "$result" true

# Case 2: code push to a code pull request
result=$(run_step "$repo_a" pull_request synchronize "$base_commit" "$base_commit" "$code_commit")
check "code push to a code pull request" "$result" true

# Case 3: ready for review of a code pull request
result=$(run_step "$repo_a" pull_request ready_for_review "" "$base_commit" "$docs_commit")
check "ready for review of a code pull request" "$result" true

# Repository B setup
mkdir -p "$work/b"
repo_b="$work/b"
new_repo "$repo_b"
base_commit=$(commit_paths "$repo_b" seed.txt)
n1_commit=$(commit_paths "$repo_b" docs/a.md)
n2_commit=$(commit_paths "$repo_b" notes.md)

# Case 4: documentation-only push to a documentation-only pull request
result=$(run_step "$repo_b" pull_request synchronize "$n1_commit" "$base_commit" "$n2_commit")
check "documentation-only push to a documentation-only pull request" "$result" false

# Case 5: documentation-only pull request opened
result=$(run_step "$repo_b" pull_request opened "" "$base_commit" "$n2_commit")
check "documentation-only pull request opened" "$result" false

# Repository C setup
mkdir -p "$work/c"
repo_c="$work/c"
new_repo "$repo_c"
base_commit=$(commit_paths "$repo_c" seed.txt)
r_commit=$(commit_paths "$repo_c" README.md)

# Case 6: pull request changing only README.md
result=$(run_step "$repo_c" pull_request opened "" "$base_commit" "$r_commit")
check "pull request changing only README.md" "$result" true

# Case 7: manual run
result=$(run_step "$repo_c" workflow_dispatch "" "" "" "")
check "manual run" "$result" true

# Final result
echo "$failures of 7 cases failed"
exit $([ "$failures" -eq 0 ] && echo 0 || echo 1)
