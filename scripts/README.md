# scripts/

Manually-run operational scripts for the fork. This branch (`chore/scripts`) is a
long-lived dumping ground for one-off / maintenance scripts — it is **not** tied to
an issue number and is not auto-merged; check it out when you need to run something.

## create-issues.sh

Recreates the RV-NNNN backlog issues (RV-0001 … RV-0027) in a GitHub repo from the
captured markdown under `issues/` (one file per issue: title on line 1, blank line,
then the body). Useful to restore the backlog or port it to another repo.

```bash
# default: recreate in the fork
./scripts/create-issues.sh

# see what it would do without creating anything
DRY_RUN=1 ./scripts/create-issues.sh

# port the backlog to upstream
REPO=vese/Recaptcha.Verify.Net ./scripts/create-issues.sh

# Windows (gh not on PATH)
GH="/c/Program Files/GitHub CLI/gh.exe" ./scripts/create-issues.sh
```

Caveats: GitHub assigns fresh numbers (originals #11..#40 cannot be reproduced); the
script is not idempotent (each run creates a new batch); labels / project Priority are
not applied here.
