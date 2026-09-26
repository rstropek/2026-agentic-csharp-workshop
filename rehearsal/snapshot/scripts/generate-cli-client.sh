#!/usr/bin/env bash
# Regenerates the Kiota API client of the CLI (GraderCli/ApiClient) from the committed OpenAPI spec.
# See tech-docs/cli.md and tech-docs/api-client.md.
#
# Usage: scripts/generate-cli-client.sh [--check]
#
# --check: generates into a temporary folder and fails if the committed client differs from it, ignoring
#          doc comments (description-only API changes). Used by scripts/qa.sh; changes nothing.
# Exit code: 0 on success (--check: client is up to date), 1 otherwise.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SPEC="GraderApi/openapi/GraderApi.json"
CLIENT_DIR="GraderCli/ApiClient"

CHECK=false
for arg in "$@"; do
  case "$arg" in
    --check) CHECK=true ;;
    -h | --help)
      sed -n '2,9p' "$0" | sed 's/^# \{0,1\}//'
      exit 0
      ;;
    *)
      echo "Unknown argument: $arg (see --help)" >&2
      exit 2
      ;;
  esac
done

cd "$ROOT"
dotnet tool restore >/dev/null

# generate <output folder>
generate() {
  dotnet kiota generate --openapi "$SPEC" --language CSharp --class-name GraderApiClient \
    --namespace-name GraderCli.ApiClient --output "$1" --exclude-backward-compatible --clean-output \
    --log-level Error
  rm -f "$1/.kiota.log"
}

if [ "$CHECK" = false ]; then
  generate "$CLIENT_DIR"
  echo "Regenerated $CLIENT_DIR from $SPEC."
  exit 0
fi

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT
generate "$TMP_DIR/ApiClient"
# The lock file contains the relative path to the spec, which differs for the temporary folder.
if ! diff -r -u -I '^[[:space:]]*///' -x kiota-lock.json "$CLIENT_DIR" "$TMP_DIR/ApiClient"; then
  echo
  echo "The CLI API client ($CLIENT_DIR) is out of date with $SPEC. Run scripts/generate-cli-client.sh."
  exit 1
fi
echo "The CLI API client is up to date."
