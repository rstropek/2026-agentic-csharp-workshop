#!/usr/bin/env bash

set -Eeuo pipefail

usage() {
  cat <<'EOF'
Usage: ./claude-via-mitmproxy.sh [PROMPT]

Runs one Claude Code print-mode prompt through an ephemeral mitmproxy
container. PROMPT defaults to "Hi".

Optional environment variables:
  CLAUDE_MITM_MODEL   Claude model alias (default: haiku)
  MITMPROXY_IMAGE     Container image (default: mitmproxy/mitmproxy:latest)
EOF
}

if (( $# > 1 )); then
  usage >&2
  exit 64
fi

for required_command in claude docker; do
  if ! command -v "$required_command" >/dev/null 2>&1; then
    printf 'Required command not found: %s\n' "$required_command" >&2
    exit 127
  fi
done

readonly prompt="${1:-Hi}"
readonly model="${CLAUDE_MITM_MODEL:-haiku}"
readonly image="${MITMPROXY_IMAGE:-mitmproxy/mitmproxy:latest}"
readonly container_name="claude-mitmproxy-$$-${RANDOM}"
readonly temporary_directory="$(mktemp -d "${TMPDIR:-/tmp}/claude-mitmproxy.XXXXXX")"
readonly ca_file="${temporary_directory}/mitmproxy-ca-cert.pem"

container_id=''

cleanup() {
  local exit_code=$?

  trap - EXIT INT TERM
  if [[ -n "$container_id" ]]; then
    docker rm --force "$container_name" >/dev/null 2>&1 || true
  fi
  rm -f "$ca_file"
  rmdir "$temporary_directory" 2>/dev/null || true
  exit "$exit_code"
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

if ! docker info >/dev/null 2>&1; then
  printf 'Docker is installed, but its daemon is not available.\n' >&2
  exit 1
fi

# Docker chooses unused host ports. Publishing on 127.0.0.1 keeps both the
# proxy and its inspection UI inaccessible from other machines.
container_id="$(
  docker run --detach --rm \
    --name "$container_name" \
    --publish 127.0.0.1::8080 \
    --publish 127.0.0.1::8081 \
    "$image" \
    mitmweb \
    --listen-host 0.0.0.0 \
    --listen-port 8080 \
    --web-host 0.0.0.0 \
    --web-port 8081 \
    --no-web-open-browser \
    --set web_password=claude
)"

# mitmproxy creates a unique CA when its configuration directory is first
# initialized. Copy only the public certificate out of the container.
ca_ready=false
for _ in {1..100}; do
  if docker cp \
    "${container_name}:/home/mitmproxy/.mitmproxy/mitmproxy-ca-cert.pem" \
    "$ca_file" >/dev/null 2>&1; then
    ca_ready=true
    break
  fi

  if ! docker inspect "$container_name" >/dev/null 2>&1; then
    break
  fi
  sleep 0.1
done

if [[ "$ca_ready" != true ]]; then
  printf 'mitmproxy did not become ready. Container output:\n' >&2
  docker logs "$container_name" >&2 || true
  exit 1
fi

proxy_binding="$(docker port "$container_name" 8080/tcp)"
web_binding="$(docker port "$container_name" 8081/tcp)"
proxy_binding="${proxy_binding%%$'\n'*}"
web_binding="${web_binding%%$'\n'*}"
readonly proxy_port="${proxy_binding##*:}"
readonly web_port="${web_binding##*:}"
readonly proxy_url="http://127.0.0.1:${proxy_port}"
readonly web_url="http://127.0.0.1:${web_port}"

if [[ ! "$proxy_port" =~ ^[0-9]+$ || ! "$web_port" =~ ^[0-9]+$ ]]; then
  printf 'Could not determine the Docker-published ports.\n' >&2
  exit 1
fi

printf 'mitmproxy web UI: %s (password: claude)\n' "$web_url"
printf 'Running Claude model %q with prompt %q ...\n\n' "$model" "$prompt"

set +e
env \
  HTTP_PROXY="$proxy_url" \
  HTTPS_PROXY="$proxy_url" \
  NO_PROXY='localhost,127.0.0.1,::1' \
  http_proxy="$proxy_url" \
  https_proxy="$proxy_url" \
  no_proxy='localhost,127.0.0.1,::1' \
  NODE_EXTRA_CA_CERTS="$ca_file" \
  DISABLE_AUTOUPDATER=1 \
  claude \
    --safe-mode \
    --no-session-persistence \
    --model "$model" \
    -p \
    -- "$prompt"
claude_status=$?
set -e

printf '\nClaude exited with status %d.\n' "$claude_status"
if [[ -t 0 ]]; then
  printf 'Inspect the completed flow at %s, then press Enter to clean up. ' "$web_url"
  read -r _ || true
else
  printf 'Standard input is not a terminal; cleaning up immediately.\n'
fi

exit "$claude_status"
