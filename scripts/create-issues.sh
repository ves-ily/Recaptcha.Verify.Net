#!/usr/bin/env bash
#
# Recreates the RV-NNNN backlog issues in a GitHub repo from the markdown files
# in ./issues/. Each ./issues/RV-NNNN.md has the issue TITLE on its first line,
# a blank line, then the BODY (markdown).
#
# Usage:
#   ./create-issues.sh                                   # create in ves-ily/Recaptcha.Verify.Net
#   REPO=ves-ily/Recaptcha.Verify.Net ./create-issues.sh # explicit target (the fork)
#   REPO=vese/Recaptcha.Verify.Net     ./create-issues.sh # port the backlog to upstream
#   DRY_RUN=1 ./create-issues.sh                          # print what would be created, create nothing
#   GH="/c/Program Files/GitHub CLI/gh.exe" ./create-issues.sh   # override the gh binary (Windows)
#
# Notes:
#   - GitHub assigns fresh issue numbers; it CANNOT reproduce the originals (#11..#40).
#   - NOT idempotent — each run creates a brand-new batch of issues.
#   - Labels / project Priority are not applied here. The board's auto-add workflow
#     picks up new issues automatically; Priority is a Projects v2 field set separately.
set -euo pipefail

REPO="${REPO:-ves-ily/Recaptcha.Verify.Net}"
GH="${GH:-gh}"
DRY_RUN="${DRY_RUN:-0}"

dir="$(cd "$(dirname "$0")/issues" && pwd)"
shopt -s nullglob
files=("$dir"/RV-*.md)

if [ ${#files[@]} -eq 0 ]; then
    echo "No issue files found in $dir" >&2
    exit 1
fi

echo "Target repo:   $REPO"
echo "gh binary:     $GH"
echo "Issues to create: ${#files[@]}"
[ "$DRY_RUN" = "1" ] && echo "DRY RUN — nothing will be created"
echo

for f in "${files[@]}"; do
    title=$(head -n 1 "$f")
    body=$(tail -n +3 "$f")   # skip the title line and the blank separator
    echo "→ $title"
    if [ "$DRY_RUN" = "1" ]; then
        echo "    (dry-run; skipping 'gh issue create')"
    else
        "$GH" issue create --repo "$REPO" --title "$title" --body "$body"
    fi
done

echo
echo "Done."
