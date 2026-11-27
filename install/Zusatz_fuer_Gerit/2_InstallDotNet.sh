#!/bin/bash
# So bei der Erstinstallation tatsaechlich ausgefuehrt (Stand 22.11.2026).
# Massgeblich ist install/Entwicklungsumgebung_Linux.md.

cd ~
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0
