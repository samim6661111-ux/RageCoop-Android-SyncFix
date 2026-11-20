from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CLIENT = ROOT / "source" / "RageCoop.Client"
CORE = ROOT / "source" / "RageCoop.Core"


def normalize_relative(value: str) -> str:
    if not value or not value.strip():
        raise ValueError("empty path")
    portable = value.replace("\\", "/")
    if portable.startswith("/"):
        raise ValueError("rooted path")
    parts = portable.split("/")
    if any(part in ("", ".", "..") or ":" in part or "\0" in part for part in parts):
        raise ValueError("unsafe path segment")
    return "/".join(parts)


def main() -> None:
    # The path reported by the user's log was created by a Windows-style separator.
    assert normalize_relative("RageCoop.Resources.Management.Client.res") == "RageCoop.Resources.Management.Client.res"
    assert normalize_relative(r"resources\resource.res") == "resources/resource.res"
    assert normalize_relative(r"51.38.205.167.31274\resource.res") == "51.38.205.167.31274/resource.res"

    for bad in ["", "../outside.res", r"server\..\outside.res", "/etc/passwd", r"C:\Windows\evil.res", "a//b.res", "a/./b.res"]:
        try:
            normalize_relative(bad)
        except ValueError:
            pass
        else:
            raise AssertionError(f"unsafe path accepted: {bad!r}")

    paths = (CLIENT / "Android" / "AndroidPaths.cs").read_text(encoding="utf-8")
    download = (CLIENT / "Networking" / "DownloadManager.cs").read_text(encoding="utf-8")
    resources = (CLIENT / "Scripting" / "Resources.cs").read_text(encoding="utf-8")
    send = (CLIENT / "Networking" / "Send.cs").read_text(encoding="utf-8")
    receive = (CLIENT / "Networking" / "Receive.cs").read_text(encoding="utf-8")

    assert "NormalizeRelativePath" in paths and "CombineUnder" in paths
    assert "relativePath.Replace('\\\\', '/')" in paths
    assert "parts[i] == \"..\"" in paths
    assert "AndroidPaths.CombineUnder(ResourceFolder, name)" in download
    assert "AndroidPaths.CombineUnder(ResourceFolder, f.FileName)" in download
    assert "FileAlreadyExists(path, length)" in download
    assert 'var path = $"{ResourceFolder}\\\\{name}"' not in download
    assert "AndroidPaths.CombineUnder(path, zip)" in resources
    assert "p.VehicleID = 0;" in send and "p.Seat = 0;" in send
    assert "ResolveVehicleForPedSync(ped, p.Speed)" in send
    assert "0x814FA8BE5449445D" in send and "0x9A9112A0FE9A4713" in send
    assert "veh == null || veh.ID == 0" in send and "p.Speed = 0;" in send
    assert 'Main.Logger.Error("Invalid vehicle")' not in send
    assert "packet.VehicleID != 0" in receive
    assert "c.VehicleID = 0;" in receive and "c.Seat = 0;" in receive

    print("PASS: 3 valid path cases; 7 unsafe path cases rejected")
    print("PASS: resource download/load paths use AndroidPaths.CombineUnder")
    print("PASS: send/receive PedSync guards, reset, and native vehicle-handle fallback are present")
    print("NOTE: source checks are not a C# compile or Android runtime test")


if __name__ == "__main__":
    main()
