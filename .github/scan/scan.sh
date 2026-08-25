#!/usr/bin/env bash
# Fails if any forbidden string appears in the repository, or in the extra files passed to it.
#
# Only files under version control are searched. That set is exactly what publishes, and it leaves out
# build output and installed dependencies, which are not part of the repository and whose own contents
# would otherwise produce noise loud enough that somebody turns the gate off.
#
# Binary files are searched as text rather than skipped, because a captured fixture is binary and a
# leaked path inside one would be invisible otherwise.
#
# The workflow passes the commit messages of a push or a pull request as an extra file. A message is
# public and permanent in the same way the tree is, and it is the likelier place for a stray path.
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(git -C "$here" rev-parse --show-toplevel)"
extra=("$@")

# A run that finds nothing to look at would report success, which is the worst answer a gate can give.
if [ -z "$(git -C "$root" ls-files | head -1)" ]; then
  echo "there are no tracked files to scan, so this run would pass by looking at nothing"
  exit 1
fi

# Same reasoning for a file that was asked for and is not there. grep would say so on stderr and the
# run would still end in success, having quietly skipped whatever the caller cared most about.
#
# The count is checked first because expanding an empty array under `set -u` is an error in bash 3.2,
# which is what macOS ships, and this script is meant to be run by hand as well as by the workflow.
if [ ${#extra[@]} -gt 0 ]; then
  for file in "${extra[@]}"; do
    if [ ! -f "$file" ]; then
      echo "asked to scan $file, which does not exist"
      exit 1
    fi
  done
fi

status=0
while IFS= read -r pattern; do
  [ -z "$pattern" ] && continue
  case "$pattern" in \#*) continue ;; esac

  hits="$(git -C "$root" grep -n --text --fixed-strings -e "$pattern" \
    -- . ':!.github/scan/forbidden.txt' || true)"

  if [ ${#extra[@]} -gt 0 ]; then
    hits="$hits$(grep -nH --text --fixed-strings -e "$pattern" "${extra[@]}" || true)"
  fi

  if [ -n "$hits" ]; then
    echo "forbidden string: $pattern"
    echo "$hits" | cut -c1-200 | sed 's/^/  /'
    status=1
  fi
done < "$here/forbidden.txt"

if [ "$status" -eq 0 ]; then
  echo "clean: no forbidden strings in the tracked tree${extra[*]:+ or ${extra[*]}}"
fi
exit "$status"
