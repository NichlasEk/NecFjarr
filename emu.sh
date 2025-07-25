#!/bin/bash

# 🛠 Miljövariabler – använder rätt installerad SDK
export ANDROID_SDK_ROOT="/opt/android-sdk"
export ANDROID_HOME="$ANDROID_SDK_ROOT"
export JAVA_HOME="/usr/lib/jvm/java-21-openjdk"

# Lägg till SDK-verktyg i PATH
export PATH="$ANDROID_HOME/emulator:$ANDROID_HOME/platform-tools:$ANDROID_HOME/cmdline-tools/latest/bin:$JAVA_HOME/bin:$PATH"

# 📱 Namn på AVD
AVD_NAME="pixel_arm64"

# 🔌 Döda ev. gammal adb-server
adb kill-server

# 🧼 Döda eventuell gammal emulatorprocess (tyst)
pkill -f "emulator.*$AVD_NAME" 2>/dev/null

# 🚀 Starta emulatorn i bakgrunden med loggning
echo "🚀 Startar emulatorn '$AVD_NAME' (arm64)..."
nohup emulator -avd "$AVD_NAME" \
  -gpu swiftshader_indirect \
  -no-accel \
  -verbose > ~/.emulator-$AVD_NAME.log 2>&1 &

# ⏳ Vänta tills emulatorn är redo
echo "⏳ Väntar på att emulatorn ska starta..."
adb wait-for-device

# ✅ Klar
echo "✅ Emulatorn '$AVD_NAME' är redo!"
adb devices
