#!/bin/bash
# build_and_run.sh — pull latest, build, bundle, launch SoundSync.app
# Usage:
#   ./build_and_run.sh           # build for native arch (arm64 on Apple Silicon, x64 on Intel)
#   ./build_and_run.sh x64       # force Intel
#   ./build_and_run.sh arm64     # force Apple Silicon
#   ./build_and_run.sh watch     # rebuild + relaunch every time source files change (requires fswatch)

set -euo pipefail
cd "$(dirname "$0")"

ARCH="${1:-}"
WATCH=false

if [[ "$ARCH" == "watch" ]]; then
    WATCH=true
    ARCH=""
fi

# Auto-detect architecture
if [[ -z "$ARCH" ]]; then
    ARCH=$(uname -m | sed 's/x86_64/x64/;s/arm64/arm64/')
fi

RID="osx-$ARCH"
CONFIG=Release
PROJECT="SoundSync.Mac/SoundSync.Mac.csproj"
PUBLISH_DIR="./publish/$ARCH"
APP="./SoundSync.app"

build_and_launch() {
    echo ""
    echo "=== Pulling latest from git ==="
    git pull --ff-only || echo "(git pull skipped — not a git repo or nothing to pull)"

    echo ""
    echo "=== Building SoundSync for $RID ==="
    dotnet publish "$PROJECT" \
        -r "$RID" \
        -c "$CONFIG" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -o "$PUBLISH_DIR"

    echo ""
    echo "=== Assembling SoundSync.app ==="
    rm -rf "$APP"
    mkdir -p "$APP/Contents/MacOS"
    mkdir -p "$APP/Contents/Resources"

    cp "$PUBLISH_DIR/SoundSync"                     "$APP/Contents/MacOS/SoundSync"
    chmod +x "$APP/Contents/MacOS/SoundSync"
    cp "SoundSync.Mac/Assets/AppIcon.icns"          "$APP/Contents/Resources/AppIcon.icns"
    cp "SoundSync.Mac/Assets/Info.plist"            "$APP/Contents/Info.plist"

    # Kill any running instance before relaunching
    pkill -x SoundSync 2>/dev/null || true
    sleep 0.3

    echo ""
    echo "=== Launching SoundSync.app ==="
    open "$APP"
    echo "Done."
}

if [[ "$WATCH" == true ]]; then
    if ! command -v fswatch &>/dev/null; then
        echo "ERROR: 'fswatch' not found. Install it with: brew install fswatch"
        exit 1
    fi
    echo "Watching for changes in SoundSync.Mac/ ... (Ctrl-C to stop)"
    build_and_launch
    fswatch -o SoundSync.Mac/ SoundSync.Core/ | while read -r _; do
        echo ""
        echo ">>> Change detected, rebuilding..."
        build_and_launch
    done
else
    build_and_launch
fi
