#!/bin/sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
SRC="$ROOT/source/RageCoop.Client"
REF="$ROOT/build_refs"
OUT="$ROOT/install/games/gtav/scripts/RageCoop"
mkdir -p "$OUT" "$ROOT/build_logs"
command -v mcs >/dev/null 2>&1 || { echo 'mcs not found: install mono-devel first.' >&2; exit 2; }
for name in mscorlib.dll System.dll System.Core.dll System.Drawing.dll System.Xml.dll System.Xml.Linq.dll System.Windows.Forms.dll System.Runtime.Serialization.dll System.Runtime.Serialization.Formatters.Soap.dll System.Net.Http.dll System.IO.Compression.dll System.IO.Compression.FileSystem.dll System.Numerics.dll System.Configuration.dll System.ComponentModel.Composition.dll System.Data.dll System.Transactions.dll System.Runtime.dll netstandard.dll ScriptHookVDotNet.dll ScriptHookVDotNet3.dll LemonUI.SHVDN3.dll Lidgren.Network.dll Newtonsoft.Json.dll RageCoop.Core.dll; do
  test -f "$REF/$name" || { echo "Missing build reference: $REF/$name" >&2; exit 3; }
done
COMMON="-r:$REF/mscorlib.dll -r:$REF/System.dll -r:$REF/System.Core.dll -r:$REF/System.Drawing.dll -r:$REF/System.Xml.dll -r:$REF/System.Xml.Linq.dll -r:$REF/System.Windows.Forms.dll -r:$REF/System.Runtime.Serialization.dll -r:$REF/System.Runtime.Serialization.Formatters.Soap.dll -r:$REF/System.Net.Http.dll -r:$REF/System.IO.Compression.dll -r:$REF/System.IO.Compression.FileSystem.dll -r:$REF/System.Numerics.dll -r:$REF/System.Configuration.dll -r:$REF/System.ComponentModel.Composition.dll -r:$REF/System.Data.dll -r:$REF/System.Transactions.dll -r:$REF/System.Runtime.dll -r:$REF/netstandard.dll"
CLIENT="$OUT/RageCoop.Client.dll"
# shellcheck disable=SC2086
mcs -noconfig -nostdlib -target:library -unsafe -optimize+ -langversion:latest -out:"$CLIENT" $COMMON \
  -r:"$REF/ScriptHookVDotNet.dll" -r:"$REF/ScriptHookVDotNet3.dll" \
  -r:"$REF/LemonUI.SHVDN3.dll" -r:"$REF/Lidgren.Network.dll" \
  -r:"$REF/Newtonsoft.Json.dll" -r:"$REF/RageCoop.Core.dll" \
  $(find "$SRC" -type f -name '*.cs' | sort) > "$ROOT/build_logs/RageCoop.Client.log" 2>&1 || { cat "$ROOT/build_logs/RageCoop.Client.log"; exit 1; }
echo "BUILD COMPLETE: $CLIENT"
echo 'Copy only RageCoop.Client.dll. Keep the existing Core DLL and Android APK unchanged.'
