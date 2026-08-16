#!/bin/bash
set -euo pipefail

# Wait for Hub images from tagged-releases.txt, then deploy db → api → web → dev-web.
# Each line is: <component> <x.y.z>
#
# Requires: docker (logged in), gh (GH_TOKEN that can dispatch beseler-private).

REPO="${DEPLOY_REPO:-abeseler/beseler-private}"
WORKFLOW="${DEPLOY_WORKFLOW:-deploy.yml}"
RELEASES_FILE="${1:-tagged-releases.txt}"
ORDER=(beseler-net-dbdeploy beseler-net-api beseler-net-web beseler-dev-web)

if [[ ! -s "$RELEASES_FILE" ]]
then
    echo "No tagged releases in $RELEASES_FILE"
    exit 0
fi

declare -A TAGS
while read -r name tag
do
    [[ -z "${name:-}" || -z "${tag:-}" ]] && continue
    TAGS["$name"]="$tag"
done < "$RELEASES_FILE"

wait_for_image() {
    local image="$1"
    echo "Waiting for $image"
    local i
    for i in $(seq 1 60)
    do
        if docker buildx imagetools inspect "$image" >/dev/null 2>&1
        then
            echo "Found $image"
            return 0
        fi
        echo "Not on Hub yet ($i/60), retrying in 10s"
        sleep 10
    done
    echo "Timed out waiting for $image"
    return 1
}

deploy_one() {
    local name="$1"
    local tag="$2"
    local image="abeseler/${name}:${tag}"
    local title="Deploy ${name}:${tag}"

    wait_for_image "$image"

    echo "Dispatching $title"
    local before
    before=$(date -u +%Y-%m-%dT%H:%M:%SZ)
    gh workflow run "$WORKFLOW" --repo "$REPO" -f "name=$name" -f "tag=$tag"

    local run_id=""
    local i
    for i in $(seq 1 30)
    do
        run_id=$(gh run list --repo "$REPO" --workflow "$WORKFLOW" --limit 20 \
            --json databaseId,displayTitle,createdAt \
            | jq -r --arg title "$title" --arg since "$before" \
                '[.[] | select(.displayTitle == $title and .createdAt >= $since)][0].databaseId // empty')
        if [[ -n "$run_id" ]]
        then
            break
        fi
        sleep 2
    done

    if [[ -z "$run_id" ]]
    then
        echo "Could not find $WORKFLOW run for $title"
        exit 1
    fi

    echo "Watching $title (run $run_id)"
    gh run watch "$run_id" --repo "$REPO" --exit-status
}

for name in "${ORDER[@]}"
do
    tag="${TAGS[$name]:-}"
    if [[ -n "$tag" ]]
    then
        deploy_one "$name" "$tag"
    fi
done
