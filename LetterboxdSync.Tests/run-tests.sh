#!/bin/bash

echo "=== Exécution des tests unitaires pour LetterboxdSync ==="
echo ""

# Vérifier les installations de .NET
echo "📍 Recherche des installations .NET..."

# Chemins possibles pour .NET
DOTNET_PATHS=(
    "/opt/homebrew/opt/dotnet@9/libexec/dotnet" # Homebrew .NET 9 spécifique
#    "/opt/homebrew/opt/dotnet@6/libexec/dotnet" # Homebrew .NET 6 spécifique
    "/usr/local/share/dotnet/dotnet"  # Installation Microsoft standard
    "/opt/homebrew/opt/dotnet/libexec/dotnet"  # Homebrew ARM64
    "/usr/local/opt/dotnet/libexec/dotnet"  # Homebrew Intel
    "dotnet"  # PATH par défaut
)

DOTNET_CMD=""
HAS_NET9=false

for cmd in "${DOTNET_PATHS[@]}"; do
    if [ -x "$cmd" ] || command -v "$cmd" &> /dev/null; then
        echo "  Trouvé: $cmd"
        if $cmd --list-sdks 2>/dev/null | grep -q "9.0"; then
            echo "    ✅ .NET9.0 SDK détecté avec ce binaire"
            DOTNET_CMD="$cmd"
            HAS_NET9=true
            break
        else
            echo "    ⚠️  Pas de .NET9.0 SDK avec ce binaire"
        fi
    fi
done

if [ -z "$DOTNET_CMD" ] || [ "$HAS_NET9" = false ]; then
    echo ""
    echo "⚠️  .NET9.0 SDK n'a pas été trouvé."
    echo ""
    echo "   Si vous avez installé .NET 6 via les binaires Microsoft:"
    echo "   export PATH=\"/usr/local/share/dotnet:\$PATH\""
    echo ""
    echo "   Pour installer .NET9.0 sur macOS:"
    echo "   • Via les binaires Microsoft: https://dotnet.microsoft.com/en-us/download/dotnet9.0"
    echo "   • Via Homebrew: brew install --cask dotnet-sdk6"
    echo ""
    echo "   Vous pouvez aussi modifier temporairement le projet pour utiliser .NET 9:"
    echo "   Éditez LetterboxdSync.Tests.csproj et changez <TargetFramework>ne9.0</TargetFramework>"
    echo "   en <TargetFramework>net9.0</TargetFramework>"
    exit 1
fi

echo ""
echo "🚀 Utilisation de: $DOTNET_CMD"
echo ""

# Restaurer les packages
echo "📦 Restauration des packages..."
$DOTNET_CMD restore

# Compiler le projet
echo "🔨 Compilation..."
$DOTNET_CMD build --no-restore

# Exécuter les tests unitaires
echo "🧪 Exécution des tests unitaires..."
$DOTNET_CMD test --no-build --verbosity normal --filter "FullyQualifiedName!~IntegrationTests"

# Pour exécuter aussi les tests d'intégration (nécessite internet et credentials)
# Décommentez la ligne suivante:
# echo "🌐 Exécution des tests d'intégration..."
# $DOTNET_CMD test --no-build --verbosity normal --filter "FullyQualifiedName~IntegrationTests"

echo ""
echo "✅ Tests terminés!"