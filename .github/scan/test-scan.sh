#!/usr/bin/env bash
# Proves the scan still refuses what it should once allowed.txt exempts a string, and still lets that
# string through where it stands alone. Each case is one line handed to scan.sh as an extra file.
#
# The forbidden host is built at run time, because this file is scanned like any other and a literal
# here would fail the tree scan.
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
host="condux"".ai"
addr="security@$host"
work="$(mktemp -d)"
trap 'rm -r "$work"' EXIT

passes=(
  "Report to $addr."
  "$addr"
  "($addr)"
  "<$addr>"
  "Sweden, $addr</Copyright>"
  "mail $addr, or"
  "\"$addr\""
)
fails=(
  "see https://$host"
  "$addr and $host"
  "x$addr"
  "$addr.evil.com"
  "${addr}x"
  "$addr$addr"
  "https://$addr/admin"
  "$addr:8443/internal"
  "foo@$addr"
  "$addr.x"
  "set CONDUX""_KEY next to $addr"
)

failed=0
check() {
  printf '%s\n' "$2" > "$work/line"
  if bash "$here/scan.sh" "$work/line" > /dev/null; then got=pass; else got=fail; fi
  if [ "$got" != "$1" ]; then
    echo "expected $1, got $got: $2"
    failed=1
  fi
}
for line in "${passes[@]}"; do check pass "$line"; done
for line in "${fails[@]}"; do check fail "$line"; done

# A run that checked nothing would print nothing, which reads as success.
echo "checked ${#passes[@]} lines that must pass and ${#fails[@]} that must fail"
exit "$failed"
