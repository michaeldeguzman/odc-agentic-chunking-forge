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

# Remove internal files from git tracking on the temp branch.
for f in "${INTERNAL_FILES[@]}"; do
    if git ls-files --error-unmatch "$f" &>/dev/null; then
        git rm --cached "$f" --quiet
        echo "  Excluded: $f"
    fi
done

# Point the CI badge and release download links to the Forge repo, not the dev repo.
sed -i '' \
    -e 's|odc-agentic-chunking/actions/workflows|odc-agentic-chunking-forge/actions/workflows|g' \
    -e 's|odc-agentic-chunking/releases|odc-agentic-chunking-forge/releases|g' \
    README.md
git add README.md

# Commit only if there are staged changes.
if ! git diff --cached --quiet; then
    git commit -m "forge: exclude internal files, fix repo links" --quiet
fi

echo "Pushing to $FORGE_REMOTE..."
git push "$FORGE_REMOTE" "$TEMP_BRANCH:$SOURCE_BRANCH" --force

# Force checkout handles untracked files left behind by git rm --cached.
git checkout -f "$SOURCE_BRANCH"
git branch -D "$TEMP_BRANCH"

echo "Done. odc-agentic-chunking-forge is up to date."
