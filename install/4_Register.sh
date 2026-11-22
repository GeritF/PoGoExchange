cat >> ~/.profile <<'EOF'

# .NET und Java fuer Avalonia/Android
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"
export JAVA_HOME="/usr/lib/jvm/java-21-openjdk-amd64"
EOF
