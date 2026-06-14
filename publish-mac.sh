#!/bin/bash
set -e

# Detect Intel vs Apple Silicon
ARCH=$(uname -m)
if [ "$ARCH" = "arm64" ]; then
    RID="osx-arm64"
else
    RID="osx-x64"
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
MAC_PROJ="$SCRIPT_DIR/SoundSync.Mac/SoundSync.Mac.csproj"
PUBLISH_OUT="$SCRIPT_DIR/SoundSync.Mac/bin/publish/$RID"
APP_OUT="$SCRIPT_DIR/SoundSync.app"

echo "Building SoundSync for $RID..."

dotnet publish "$MAC_PROJ" \
    -r "$RID" \
    -c Release \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o "$PUBLISH_OUT"

echo "Creating SoundSync.app bundle..."

rm -rf "$APP_OUT"
mkdir -p "$APP_OUT/Contents/MacOS"
mkdir -p "$APP_OUT/Contents/Resources"

cp "$PUBLISH_OUT/SoundSync" "$APP_OUT/Contents/MacOS/SoundSync"
cp "$SCRIPT_DIR/SoundSync.Mac/Info.plist" "$APP_OUT/Contents/Info.plist"

chmod +x "$APP_OUT/Contents/MacOS/SoundSync"

echo ""
echo "Done! SoundSync.app is in: $SCRIPT_DIR"
echo ""
echo "To run:       open \"$APP_OUT\""
echo "To install:   drag SoundSync.app to /Applications"
echo ""
echo "First launch note: if macOS blocks it, right-click → Open to bypass Gatekeeper."
