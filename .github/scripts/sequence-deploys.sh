#!/bin/bash
set -euo pipefail

# Deploy db → api → web → dev-web from tagged-releases.txt.
# Each line is: <component> <x.y.z>
#
# Images are built and pushed before this script runs. A short Hub inspect
# covers tag visibility lag; a missing image fails fast instead of waiting
# for a tag-triggered rebuild that will never come.
#
# Requires: docker (logged in), gh (GH_TOKEN that can dispatch beseler-private).
# Does not use `gh run watch` — fine-grained PATs cannot read Checks annotations.

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

echo "Release order:"
for name in "${ORDER[@]}"
do
    if [[ -n "${TAGS[$name]:-}" ]]
    then
        echo "  $name ${TAGS[$name]}"
    fi
done

wait_for_image() {
    local image="$1"
    echo "Waiting for $image"
    local i
    for i in $(seq 1 12)
    do
        if docker buildx imagetools inspect "$image" >/dev/null 2>&1
        then
            echo "  ready"
            return 0
        fi
        echo "  not on Hub yet (${i}/12)"
        sleep 5
    done
    echo "Timed out waiting for $image"
    echo "Images are built before tagging; a missing tag means that build did not push."
    return 1
}

wait_for_run() {
    local run_id="$1"
    local title="$2"
    echo "Waiting for $title (run $run_id)"
    local i status conclusion
    for i in $(seq 1 90)
    do
        status=$(gh run view "$run_id" --repo "$REPO" --json status --jq .status)
        if [[ "$status" == "completed" ]]
        then
            conclusion=$(gh run view "$run_id" --repo "$REPO" --json conclusion --jq .conclusion)
            if [[ "$conclusion" == "success" ]]
            then
                echo "  $title succeeded"
                return 0
            fi
            echo "  $title failed ($conclusion)"
            echo "  https://github.com/${REPO}/actions/runs/${run_id}"
            return 1
        fi
        if (( i == 1 || i % 6 == 0 ))
        then
            echo "  $status (${i}/90)"
        fi
        sleep 5
    done
    echo "Timed out waiting for $title"
    echo "  https://github.com/${REPO}/actions/runs/${run_id}"
    return 1
}

deploy_one() {
    local name="$1"
    local tag="$2"
    local image="abeseler/${name}:${tag}"
    local title="Deploy ${name}:${tag}"

    echo
    echo "==> $title"
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

    wait_for_run "$run_id" "$title"
}

for name in "${ORDER[@]}"
do
    tag="${TAGS[$name]:-}"
    if [[ -n "$tag" ]]
    then
        deploy_one "$name" "$tag"
    fi
done

echo
echo "All sequenced deploys finished."
