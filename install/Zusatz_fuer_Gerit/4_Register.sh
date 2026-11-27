#!/bin/bash
# So bei der Erstinstallation tatsaechlich ausgefuehrt (Stand 22.11.2026).
# Massgeblich ist install/Entwicklungsumgebung_Linux.md.
# Abweichung: ANDROID_HOME wurde hier nicht gesetzt (die Anleitung setzt es
# in Schritt 4). Ob es noetig ist, ist nicht geprueft; ohne lief es.

cat >> ~/.profile <<'EOF'

# .NET und Java fuer Avalonia/Android
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"
export JAVA_HOME="/usr/lib/jvm/java-21-openjdk-amd64"
EOF
