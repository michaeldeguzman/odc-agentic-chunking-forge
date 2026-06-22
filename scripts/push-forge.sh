#!/usr/bin/env bash
# Syncs the public Forge repository from the current main branch.
# Run from the repo root: bash scripts/push-forge.sh
set -euo pipefail

FORGE_REMOTE="forge"
SOURCE_BRANCH="main"
TEMP_BRANCH="forge-release-$$"

# Files that exist in the dev repo but must not appear in the public Forge repo.
INTERNAL_FILES=(
    "CLAUDE.md"
    "scripts/push-forge.sh"
)

echo "Building Forge release from $SOURCE_BRANCH..."
git checkout -b "$TEMP_BRANCH" "$SOURCE_BRANCH"

# Remove internal files from tracking on the temp branch.
for f in "${INTERNAL_FILES[@]}"; do
    if git ls-files --error-unmatch "$f" &>/dev/null; then
        git rm --cached "$f" --quiet
        echo "  Excluded: $f"
    fi
done

# Persist the exclusions in .gitignore so they cannot be re-tracked.
for f in "${INTERNAL_FILES[@]}"; do
    grep -qxF "$f" .gitignore 2>/dev/null || echo "$f" >> .gitignore
done
git add .gitignore

# Commit only if there are staged changes.
if ! git diff --cached --quiet; then
    git commit -m "forge: exclude internal development files" --quiet
fi

echo "Pushing to $FORGE_REMOTE..."
git push "$FORGE_REMOTE" "$TEMP_BRANCH:$SOURCE_BRANCH" --force

git checkout "$SOURCE_BRANCH"
git branch -D "$TEMP_BRANCH"

echo "Done. odc-agentic-chunking-forge is up to date."
