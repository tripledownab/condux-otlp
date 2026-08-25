#!/usr/bin/env bash
# Fails if the built package declares a dependency.
#
# An empty dependency list is the whole claim this package makes, so it is checked rather than asserted,
# on every change and again before a release. A NuGet dependency can arrive without anyone deciding to
# add one, through a transitive reference or a framework change, and the first person to notice would
# otherwise be whoever installed the version that shipped it.
set -euo pipefail

# Every package given, not just the first. A caller passes a glob, and a glob that grows is exactly how a
# gate ends up reporting success over a package it never opened.
if [ "$#" -eq 0 ]; then
  echo "usage: check-no-dependencies.sh <path-to-nupkg>..."
  exit 1
fi

status=0
for package in "$@"; do
  if [ ! -f "$package" ]; then
    echo "no such package: $package"
    exit 1
  fi

  nuspec="$(mktemp)"
  unzip -p "$package" '*.nuspec' > "$nuspec"

  if grep -q "<dependency " "$nuspec"; then
    echo "$(basename "$package") declares a dependency, which is the one thing it must not do:"
    grep "<dependency " "$nuspec"
    status=1
  else
    echo "$(basename "$package") declares no dependencies"
  fi
done

exit "$status"
