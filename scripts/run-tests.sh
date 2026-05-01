#!/usr/bin/env bash
# Local test runner. Filters integration tests in/out depending on Docker availability.
set -euo pipefail

cd "$(dirname "$0")/.."

SOLUTION="src/backend/HomeChefPro.slnx"
FILTER="${1:-auto}"

case "$FILTER" in
    unit)        FILTER_EXPR='Category!=Integration' ;;
    integration) FILTER_EXPR='Category=Integration' ;;
    all)         FILTER_EXPR='' ;;
    auto)
        if docker info >/dev/null 2>&1; then
            echo "[run-tests] Docker available — running ALL tests"
            FILTER_EXPR=''
        else
            echo "[run-tests] Docker not running — skipping integration tests"
            FILTER_EXPR='Category!=Integration'
        fi
        ;;
    *)
        echo "Usage: $0 [unit|integration|all|auto]"
        exit 1
        ;;
esac

if [[ -n "$FILTER_EXPR" ]]; then
    dotnet test "$SOLUTION" --filter "$FILTER_EXPR" --nologo
else
    dotnet test "$SOLUTION" --nologo
fi
