#!/usr/bin/env bash
# `set -u` only. See the note in secret-read-guard.sh: pipefail turns a short-circuiting filter
# into a silent failure.
#
# WHY IT IS SCOPED TO ONE FILE AND DOES NOT CALL `make format`:
# `make format` runs `dotnet format` over the whole solution. That is right for a human before a
# commit and wrong here: it takes tens of seconds, it rewrites files the agent never touched, and
# it fires on EVERY edit. Scoped with --include it is a couple of seconds and the diff stays
# honest. This is the one place in this repo where a Makefile target exists and is deliberately
# not used — do not "fix" it.
#
# THE --include PATH IS RELATIVE TO THE DIRECTORY THIS SCRIPT RUNS IN, which is the repo root:
# the script cd's there before calling dotnet format. Do NOT "correct" it to be relative to the
# solution's directory — with a solution at src/TheOffice.sln that produces
# src/src/Application/... , which matches nothing, and dotnet format then exits 0 having done
# nothing at all.
#
# WHY IT LOADS THE SOLUTION ANYWAY: --include narrows which files get written, not which project
# gets loaded, so the MSBuild workspace still has to come up. That workspace is what makes style
# rules — unused usings, sorting — evaluable at all. `dotnet format whitespace --folder` skips it
# and looks much faster, but it silently ignores those rules and resolves --include relative to
# the folder, so repo-root paths match nothing and it exits 0 having done nothing.
#
# WHY IT NEVER BLOCKS: PostToolUse runs after the tool succeeded; there is nothing left to stop.
# The common failure is real and boring: the file does not compile yet because the agent is
# mid-refactor. Blocking on that would fight the agent instead of helping it.
set -u

DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
. "$DIR/_lib.sh"

hook_read_stdin

FILE="$(json_path file_path)"
[ -z "$FILE" ] && exit 0

# C# only. `dotnet format` has nothing to say about .md, .json or .csproj, and calling it on them
# pays the MSBuild startup cost for no change.
case "$FILE" in
  *.cs) ;;
  *) exit 0 ;;
esac

# Resolve BOTH paths physically before comparing them. `git rev-parse` and the payload can spell
# the same directory differently — on macOS the payload says /tmp and git says /private/tmp — and
# a textual strip then leaves REL unchanged, so the hook exits 0 having formatted nothing. A hook
# that silently does nothing passes every check in Phase 5.
ROOT="$(cd "$(repo_root)" 2>/dev/null && pwd -P)" || exit 0
FDIR="$(cd "$(dirname "$FILE")" 2>/dev/null && pwd -P)" || exit 0   # also covers a deleted file
FILE="$FDIR/$(basename "$FILE")"

cd "$ROOT" || exit 0

# Quoting "$ROOT" inside ${...#} turns off pattern matching, so a repo path containing [ ] * or ?
# is stripped literally instead of being read as a glob.
case "$FILE" in
  "$ROOT"/*) REL="${FILE#"$ROOT"/}" ;;
  *) exit 0 ;;                    # outside this repo; not ours to format
esac

[ -f "$REL" ] || exit 0

OUT="$(dotnet format "src/TheOffice.sln" --include "$REL" --no-restore --verbosity quiet 2>&1)"
STATUS=$?

if [ $STATUS -ne 0 ]; then
  # stderr on a PostToolUse hook is shown, not acted on. Keep it to a few lines: the whole point
  # is that somebody notices the formatter is broken, not that they read an MSBuild log here.
  printf 'format-on-edit: dotnet format exited %s for %s\n' "$STATUS" "$REL" >&2
  printf '%s\n' "$OUT" | head -n 5 >&2
fi

exit 0
