#!/bin/bash

set -e

# 📦 Miljövariabler
export DOTNET_ROOT="$HOME/.dotnet8"
export ANDROID_SDK_ROOT="/opt/android-sdk"
export ANDROID_HOME="$ANDROID_SDK_ROOT"
export JAVA_HOME="/usr/lib/jvm/java-21-openjdk"

export PATH="$DOTNET_ROOT:$JAVA_HOME/bin:$ANDROID_HOME/emulator:$ANDROID_HOME/platform-tools:$ANDROID_HOME/cmdline-tools/latest/bin:$PATH"

# 📁 Projektinfo
PROJ_NAME="NecFjärr"
ANDROID_PROJ="${PROJ_NAME}.Android"
CSProjPath="${ANDROID_PROJ}/${ANDROID_PROJ}.csproj"
PACKAGE_NAME="se.nichlasek.necfjarr"
TARGET_FRAMEWORK="net8.0-android"
AVD_NAME="pixel_x86_64"

# 🛠️ Bygg (signeras automatiskt i Release)
echo "🛠️ Bygger APK med dotnet build för android-x86_64..."
dotnet build "$CSProjPath" -c Release -f "$TARGET_FRAMEWORK" /p:RuntimeIdentifier=android-x86_64

# 🔍 Leta efter signerad APK
APK_PATH=$(find "$ANDROID_PROJ/bin" -iname "*Signed.apk" | grep android-x86_64 | head -n 1)

if [[ ! -f "$APK_PATH" ]]; then
  echo "❌ Kunde inte hitta signerad .apk efter build."
  exit 1
fi

echo "✅ APK hittad: $APK_PATH"

# 🚀 Starta emulator
echo "📱 Startar emulatorn '$AVD_NAME'..."
nohup emulator -avd "$AVD_NAME" -gpu swiftshader_indirect -no-snapshot -no-audio > ~/.emulator-log 2>&1 &

# ⏳ Vänta tills emulatorn är redo
echo "⏳ Väntar på att emulatorn ska svara via ADB..."
adb wait-for-device

# 🧹 Försök avinstallera tidigare version
echo "🧹 Avinstallerar eventuell gammal version av $PACKAGE_NAME..."
adb uninstall "$PACKAGE_NAME" || echo "(Ingen tidigare version installerad)"

# 📲 Installera signerad APK
echo "📲 Installerar APK..."
adb install -r "$APK_PATH"

# 🚀 Starta appen
echo "🚀 Startar appen..."
adb shell monkey -p "$PACKAGE_NAME" -c android.intent.category.LAUNCHER 1

echo "✅ Klar!"
