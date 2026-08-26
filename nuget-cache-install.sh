#!/usr/bin/env bash
# Builds KludgeBox from the local sources and puts it into the NuGet global packages folder in place of
# the nuget.org package of the same version (the one in KludgeBox.csproj). Projects that reference this
# version then build against the local code, nothing gets published. nuget-cache-clear.sh undoes it.
set -euo pipefail

project="$(dirname "${BASH_SOURCE[0]}")/KludgeBox/KludgeBox.csproj"

packages="$(dotnet nuget locals global-packages --list --force-english-output)"
packages="${packages#global-packages: }"
packages="${packages%/}"
[[ -d "$packages" ]] || { echo "NuGet global packages folder not found: $packages" >&2; exit 1; }

version="$(dotnet msbuild "$project" -getProperty:PackageVersion)"
target="$packages/kludgebox/$(echo "$version" | tr '[:upper:]' '[:lower:]')"

out="$(mktemp -d)"
trap 'rm -rf "$out"' EXIT

# The same flags as the nuget.org release in .github/workflows/publish.yml.
dotnet pack "$project" --output "$out" -p:IncludeSymbols=false -p:IncludeSource=false

# A push into a folder laid out like the cache extracts the package the same way restore does
# (lib/, .nupkg.metadata, .sha512). It fails when the version is already there, so the old copy goes first.
rm -rf "$target"
dotnet nuget push "$out/KludgeBox.$version.nupkg" --source "$packages"

[[ -f "$target/.nupkg.metadata" ]] || { echo "KludgeBox $version was not extracted into $target" >&2; exit 1; }
echo "KludgeBox $version from the local sources is in $target"
