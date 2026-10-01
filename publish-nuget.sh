#!/usr/bin/env bash
# Sets the version in src/Jev.Sdk/Jev.Sdk.csproj, packs the project,
# and pushes Jev.Sdk to NuGet.
#
# The API key is read from NUGET_API_KEY, or from the .env file next to
# this script if the variable is not set.
#
# macOS and Linux, bash 3.2 or newer.

set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd -P)"
CSPROJ="$ROOT/src/Jev.Sdk/Jev.Sdk.csproj"
OUT="$ROOT/artifacts"
SOURCE_URL="https://api.nuget.org/v3/index.json"
VERSION=""
DRY_RUN=0

usage() {
    cat <<'USAGE'
Usage: publish-nuget.sh VERSION [--dry-run] [--source URL]

  VERSION       The new package version, e.g. 1.0.0 or 1.0.0-beta.1
  --dry-run     Set the version and pack, but do not push
  --source URL  NuGet feed to push to (default: nuget.org)
  -h, --help    This message

Requires NUGET_API_KEY (environment or .env) unless --dry-run is given.
USAGE
}

while [ $# -gt 0 ]; do
    case "$1" in
        --dry-run) DRY_RUN=1;       shift ;;
        --source)  SOURCE_URL="$2"; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        -*) echo "Unknown option: $1" >&2; usage >&2; exit 2 ;;
        *)
            [ -z "$VERSION" ] || { echo "Unexpected argument: $1" >&2; usage >&2; exit 2; }
            VERSION="$1"; shift ;;
    esac
done

say()  { printf '  %s\n' "$*"; }
warn() { printf 'warning: %s\n' "$*" >&2; }
die()  { printf 'error: %s\n' "$*" >&2; exit 1; }

[ -n "$VERSION" ] || { usage >&2; exit 2; }

echo "$VERSION" | grep -Eq '^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$' \
    || die "'$VERSION' is not a version like 1.0.0 or 1.0.0-beta.1"

command -v dotnet >/dev/null || die "dotnet is not on PATH"

# Load NUGET_API_KEY from .env if it is not already set in the environment.
ENV_FILE="$ROOT/.env"
if [ -z "${NUGET_API_KEY:-}" ] && [ -f "$ENV_FILE" ]; then
    NUGET_API_KEY="$(sed -n 's/^[[:space:]]*NUGET_API_KEY[[:space:]]*=[[:space:]]*//p' "$ENV_FILE" \
        | tail -n 1 | tr -d '\r' | sed -e 's/[[:space:]]*$//' -e 's/^["'\'']//' -e 's/["'\'']$//')"
    [ -z "$NUGET_API_KEY" ] || echo "Using NUGET_API_KEY from .env"
fi

[ "$DRY_RUN" = 1 ] || [ -n "${NUGET_API_KEY:-}" ] || die "NUGET_API_KEY is not set. Add it to .env or export it"

CURRENT="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$CSPROJ" | head -n 1)"
[ -n "$CURRENT" ] || die "no <Version> in $CSPROJ"

if [ -n "$(git -C "$ROOT" status --porcelain 2>/dev/null)" ]; then
    warn "the working tree has uncommitted changes; the package will point at commit $(git -C "$ROOT" rev-parse --short HEAD)"
fi

echo "Version $CURRENT -> $VERSION"
perl -pi -e "s|<Version>\Q$CURRENT\E</Version>|<Version>$VERSION</Version>|" "$CSPROJ"

echo "Packing"
rm -rf "$OUT"
dotnet pack "$CSPROJ" -c Release -o "$OUT" --nologo -v quiet -clp:ErrorsOnly
for pkg in "$OUT"/*.nupkg; do say "$(basename "$pkg")"; done

if [ "$DRY_RUN" = 1 ]; then
    echo "Dry run: not pushing. Package is in $OUT"
    exit 0
fi

echo "Pushing to $SOURCE_URL"
PKG="$(ls "$OUT"/*.nupkg | head -n 1)"
[ -n "$PKG" ] || die "no .nupkg found in $OUT"
dotnet nuget push "$PKG" --api-key "$NUGET_API_KEY" --source "$SOURCE_URL" --skip-duplicate

echo "Published $VERSION. Commit Jev.Sdk.csproj and tag the release:"
say "git commit -am 'Release $VERSION' && git tag v$VERSION"
