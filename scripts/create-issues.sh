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
#   FORCE=1 ./create-issues.sh                            # override the duplicate guard (DANGEROUS)
#
# Duplicate guard:
#   Before creating, the script lists existing issues in the target repo and extracts
#   their RV-NNNN keys. If ANY key about to be created already exists there, it ABORTS
#   (listing the conflicts) unless FORCE=1 is set. This prevents accidentally recreating
#   the whole backlog (e.g. running bare against the fork where it already lives).
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
FORCE="${FORCE:-0}"

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

# --- Duplicate guard: refuse to recreate issues that already exist in the target repo ---
want_keys=()
for f in "${files[@]}"; do
    want_keys+=( "$(head -n 1 "$f" | grep -oE 'RV-[0-9]+' | head -n 1)" )
done

existing_keys="$( "$GH" issue list --repo "$REPO" --state all --limit 500 --json title \
    --jq '[.[] | .title | scan("RV-[0-9]+")] | unique | join("\n")' 2>/dev/null || true )"

conflicts=()
for k in "${want_keys[@]}"; do
    [ -n "$k" ] || continue
    if printf '%s\n' "$existing_keys" | grep -Fxq -- "$k"; then
        conflicts+=("$k")
    fi
done

if [ ${#conflicts[@]} -gt 0 ]; then
    echo "WARNING: $REPO already has issues for ${#conflicts[@]}/${#want_keys[@]} backlog keys:" >&2
    printf '  %s\n' "${conflicts[@]}" >&2
    echo "Creating now would produce DUPLICATES. Aborting." >&2
    if [ "$FORCE" != "1" ]; then
        echo "Set FORCE=1 to override (duplicates WILL be created)." >&2
        exit 1
    fi
    echo "FORCE=1 set — proceeding anyway." >&2
fi
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
