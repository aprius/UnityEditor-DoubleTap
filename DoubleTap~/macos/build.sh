#!/bin/bash
set -euo pipefail

DIR="$(cd "$(dirname "$0")" && pwd)"
BUNDLE="$DIR/../../DoubleTap/Plugins/DoubleTapHook.bundle"

rm -rf "$BUNDLE"
mkdir -p "$BUNDLE/Contents/MacOS"

clang -bundle -fobjc-arc -O2 \
    -arch arm64 -arch x86_64 \
    -mmacosx-version-min=11.0 \
    -framework Cocoa \
    -o "$BUNDLE/Contents/MacOS/DoubleTapHook" \
    "$DIR/DoubleTapHook.m"

cat > "$BUNDLE/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>DoubleTapHook</string>
    <key>CFBundleIdentifier</key>
    <string>com.worldengine.doubletaphook</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>DoubleTapHook</string>
    <key>CFBundlePackageType</key>
    <string>BNDL</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundleVersion</key>
    <string>1</string>
</dict>
</plist>
PLIST

echo "built: $BUNDLE"
lipo -archs "$BUNDLE/Contents/MacOS/DoubleTapHook"
