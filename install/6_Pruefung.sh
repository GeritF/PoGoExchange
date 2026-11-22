echo "=== Pruefung ==="
echo "dotnet Pfad: $(command -v dotnet || echo 'nichts gefunden')"
sdks=$(dotnet --list-sdks 2>/dev/null)
echo "dotnet SDKs: ${sdks:-nichts gefunden}"
echo "DOTNET_ROOT: ${DOTNET_ROOT:-nicht gesetzt}"
echo "JAVA_HOME: ${JAVA_HOME:-nicht gesetzt}"
echo "JAVA_HOME existiert: $( [ -d "$JAVA_HOME" ] && echo True || echo False )"
if [ -x "$JAVA_HOME/bin/java" ]; then echo "JDK Version: $("$JAVA_HOME/bin/java" -version 2>&1 | head -n1)"; else echo "JDK Version: nichts gefunden"; fi
