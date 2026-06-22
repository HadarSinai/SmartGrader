#!/usr/bin/env bash
set -euo pipefail

FILE="Api/appsettings.Development.json"

if [ -f "$FILE" ]; then
  # Replace any ApiKey value with an empty string across history.
  perl -0777 -i -pe 's/"ApiKey"\s*:\s*"[^"]*"/"ApiKey": ""/g' "$FILE"
fi
