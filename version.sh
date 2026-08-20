#!/bin/bash
set -euo pipefail

# Source of truth is <Version> in each shipping csproj, or data/VERSION for dbdeploy.
# This script compares that number to the latest tag for that component.
# It does not bump the project. A commit that does not change Version is not a release.
#
# Usage:
#   ./version.sh plan                    # print (and optionally record) bumps; no git writes
#   ./version.sh tag                     # create tags locally, then push them together
#   ./version.sh                         # same as tag (all components)
#   ./version.sh [plan|tag] <component> <file>
#
# Tags look like beseler-net-api-v1.0.0 so each image can release independently.
# CI plans first, builds images, then tags only if every build succeeded.

SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)
CREATED_TAGS=()

usage() {
    echo "Usage: $0 [plan|tag] [<component> <version-file>]"
    echo "  version-file is a .csproj (<Version>) or a file whose first line is semver (data/VERSION)."
    exit 1
}

read_version() {
    local file="$1"
    if [[ "$file" == *.csproj ]]
    then
        sed -n 's/.*<Version>\([0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*\)<\/Version>.*/\1/p' "$file" | head -n 1
    else
        grep -oE '^[0-9]+\.[0-9]+\.[0-9]+$' "$file" | head -n 1
    fi
}

resolve_file() {
    local file="$1"
    if [[ "$file" = /* ]]
    then
        echo "$file"
    else
        echo "$SCRIPT_DIR/$file"
    fi
}

record_release() {
    local component="$1"
    local project_version="$2"
    if [ -n "${TAGGED_RELEASES_FILE:-}" ]
    then
        echo "$component $project_version" >> "$TAGGED_RELEASES_FILE"
    fi
}

consider_component() {
    local component="$1"
    local source
    source=$(resolve_file "$2")

    if [[ ! -f "$source" ]]
    then
        echo "Version file not found: $source"
        exit 1
    fi

    local project_version
    project_version=$(read_version "$source")
    if [[ ! $project_version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]
    then
        echo "Could not read a semver version from $source"
        exit 1
    fi

    local desired="$component-v$project_version"
    echo "[$component] Project VERSION: $project_version"

    local tag="$component-v0.0.0"
    if git describe --abbrev=0 --tags --match "$component-v*" >/dev/null 2>&1
    then
        tag=$(git describe --abbrev=0 --tags --match "$component-v*")
    fi

    if [[ $tag =~ ^"$component"-v[0-9]+\.[0-9]+\.[0-9]+$ ]]
    then
        echo "[$component] Latest tag: $tag"
    else
        echo "[$component] Invalid tag: $tag"
        exit 1
    fi

    local tag_version=${tag#"$component"-v}
    if [ "$project_version" = "$tag_version" ]
    then
        echo "[$component] Project version matches latest tag. Nothing to tag."
        return 0
    fi

    local lowest
    lowest=$(printf '%s\n%s\n' "$project_version" "$tag_version" | sort -V | head -n 1)
    if [ "$lowest" = "$project_version" ]
    then
        echo "[$component] Project version $project_version is behind latest tag $tag"
        exit 1
    fi

    if git rev-parse -q --verify "refs/tags/$desired" >/dev/null
    then
        echo "[$component] Tag $desired already exists; cannot reuse it for a new release"
        exit 1
    fi

    if [[ "$MODE" == "plan" ]]
    then
        echo "[$component] PLANNED: $desired"
        record_release "$component" "$project_version"
        return 0
    fi

    echo "[$component] NEW VERSION: $desired"
    git tag -a "$desired" -m "New version: $desired"
    echo "[$component] Tagged $desired locally"
    CREATED_TAGS+=("$desired")
    record_release "$component" "$project_version"
}

run_all() {
    consider_component beseler-net-api src/BeselerNet.Api/BeselerNet.Api.csproj
    consider_component beseler-net-web src/BeselerNet.Web/BeselerNet.Web.csproj
    consider_component beseler-dev-web src/BeselerDev.Web/BeselerDev.Web.csproj
    consider_component beseler-net-dbdeploy data/VERSION
}

push_created_tags() {
    if [[ "$MODE" != "tag" ]]
    then
        return 0
    fi
    if [[ ${#CREATED_TAGS[@]} -eq 0 ]]
    then
        echo "No new tags to push."
        return 0
    fi
    echo "Pushing tags: ${CREATED_TAGS[*]}"
    git push origin "${CREATED_TAGS[@]}"
}

MODE=tag
if [[ $# -ge 1 && ( "$1" == "plan" || "$1" == "tag" ) ]]
then
    MODE=$1
    shift
fi

if [ $# -eq 0 ]
then
    run_all
elif [ $# -eq 2 ]
then
    consider_component "$1" "$2"
else
    usage
fi

push_created_tags
