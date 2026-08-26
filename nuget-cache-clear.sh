#!/usr/bin/env bash
# Replaces the local builds that nuget-cache-install.sh put into the NuGet global packages folder with the
# nuget.org packages of the same versions. They are downloaded right away rather than on the next restore:
# Rider's incremental build copies the DLL straight from this folder and does not run restore.
set -euo pipefail
shopt -s nullglob

packages="$(dotnet nuget locals global-packages --list --force-english-output)"
packages="${packages#global-packages: }"
packages="${packages%/}"
[[ -d "$packages" ]] || { echo "NuGet global packages folder not found: $packages" >&2; exit 1; }

# nuget-cache-install.sh pushes straight into the folder, so its .nupkg.metadata has no "source",
# unlike every package that came through restore.
versions=()
for metadata in "$packages"/kludgebox/*/.nupkg.metadata; do
    grep -q '"source"' "$metadata" || versions+=("$(basename "$(dirname "$metadata")")")
done
if (( ${#versions[@]} == 0 )); then
    echo "No local builds of KludgeBox in $packages/kludgebox"
    exit 0
fi

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

# PackageDownload fetches the package alone, without dependencies. Any target framework will do,
# the one this SDK ships with needs no targeting pack download.
cat > "$tmp/Download.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net$(BundledNETCoreAppTargetFrameworkVersion)</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <PackageDownload Include="KludgeBox" Version="[$(KludgeBoxVersion)]" />
    </ItemGroup>
</Project>
EOF

failed=0
for version in "${versions[@]}"; do
    dir="$packages/kludgebox/$version"
    # Kept aside until the download succeeds, so a failed one leaves the local build in place.
    mv "$dir" "$tmp/$version"
    # --no-http-cache: the cached version list may be too old to have a version published minutes ago.
    if dotnet restore "$tmp/Download.csproj" -p:KludgeBoxVersion="$version" \
        --source https://api.nuget.org/v3/index.json --no-http-cache --verbosity quiet; then
        echo "KludgeBox $version: the local build is replaced with the package from nuget.org"
    else
        mv "$tmp/$version" "$dir"
        echo "KludgeBox $version: not downloaded from nuget.org, the local build stays (rm -rf $dir removes it)" >&2
        failed=1
    fi
done
exit "$failed"
