#!/bin/bash
# So bei der Erstinstallation tatsaechlich ausgefuehrt (Stand 22.11.2026).
# Massgeblich ist install/Entwicklungsumgebung_Linux.md.

echo "=== System ==="
echo "Mint: $(grep '^PRETTY_NAME=' /etc/os-release | cut -d= -f2)"
ub=$(grep '^DISTRIB_DESCRIPTION=' /etc/upstream-release/lsb-release 2>/dev/null | cut -d= -f2)
echo "Ubuntu-Basis: ${ub:-nicht ermittelbar}"
echo "Architektur: $(uname -m)"
echo "Vorhandenes dotnet: $(command -v dotnet || echo 'nichts gefunden')"
echo "Vorhandenes java: $(command -v java || echo 'nichts gefunden')"
