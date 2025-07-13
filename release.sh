#!/bin/bash

# ✅ Använd rätt .NET SDK och verktyg
export DOTNET_ROOT="$HOME/.dotnet8"
export PATH="$DOTNET_ROOT:$PATH"
export JAVA_HOME="/usr/lib/jvm/java-21-openjdk"
export PATH="$JAVA_HOME/bin:$PATH"
export ANDROID_HOME="/opt/android-sdk"
export PATH="$ANDROID_HOME/platform-tools:$PATH"

# 📁 Projektinställningar
PROJ_NAME="NecFjärr"
ANDROID_PROJ="${PROJ_NAME}.Android"
CSProjPath="${ANDROID_PROJ}/${ANDROID_PROJ}.csproj"
PACKAGE_NAME="se.nichlasek.necfjarr"
RUNTIME_ID="android-arm64"
TARGET_FRAMEWORK="net8.0-android"
RELEASE_DIR="$HOME/releases"
ZIP_NAME="${PROJ_NAME}-$(date +%Y%m%d-%H%M).zip"

# 🏁 Info
echo "🔧 Bygger Android-projektet: $ANDROID_PROJ"
echo "📦 Dotnet-version: $("$DOTNET_ROOT/dotnet" --version)"

# 🚀 Kör restore och publicering
"$DOTNET_ROOT/dotnet" restore "$CSProjPath"
"$DOTNET_ROOT/dotnet" publish "$CSProjPath" \
  -c Release \
  -f "$TARGET_FRAMEWORK" \
  -r "$RUNTIME_ID" \
  --self-contained true \
  -p:AndroidPackageFormat=apk \
  -p:AndroidSdkDirectory="$ANDROID_HOME" \
  -p:PublishTrimmed=true

# 🔍 Leta efter APK
APK_PATH=$(find "$ANDROID_PROJ/bin" "$ANDROID_PROJ/publish" -iname "*.apk" | head -n 1)

if [ ! -f "$APK_PATH" ]; then
  echo "❌ Ingen .apk hittades i bin/ eller publish/"
  exit 1
fi

echo "✅ APK hittad: $APK_PATH"

# 📲 Installera APK
echo "📲 Installerar APK till emulator/enhet: $APK_PATH"
adb install -r "$APK_PATH"
if [ $? -ne 0 ]; then
  echo "❌ Installationen av APK misslyckades"
  exit 1
fi

# 🚀 Starta appen
echo "🚀 Startar appen: $PACKAGE_NAME"
adb shell monkey -p "$PACKAGE_NAME" -c android.intent.category.LAUNCHER 1

# 💾 Packa backup zip
mkdir -p "$RELEASE_DIR"
cd "$(dirname "$APK_PATH")" || exit
zip -r "$RELEASE_DIR/$ZIP_NAME" ./*

echo "✅ Klar!"
echo "📦 Backup zip: $RELEASE_DIR/$ZIP_NAME"
