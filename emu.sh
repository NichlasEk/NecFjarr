#!/bin/bash

export ANDROID_SDK_ROOT=$HOME/Android
export ANDROID_HOME=$ANDROID_SDK_ROOT
export PATH=$ANDROID_SDK_ROOT/cmdline-tools/latest/bin:$PATH

emulator -avd pixel_avd
echo "✅ Android emulator startad"
