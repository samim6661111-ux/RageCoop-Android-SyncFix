# GitHub Actions build — RageCoop Android Sync Fix

## Repository name
`RageCoop-Android-SyncFix`

**Keep the repository PRIVATE.** This package contains uploaded source and binary reference files. Do not publish it publicly unless you have confirmed that you have the rights to redistribute every included file.

## What this repository does
- Runs the source-level Android path and PedSync checks.
- Uses a Windows GitHub-hosted runner and the .NET 8 SDK's Roslyn compiler.
- Compiles only `RageCoop.Client.dll`, using the exact compile-time references included in `build_refs/`.
- Produces a flat ZIP whose root contains only:
  - `RageCoop.Client.dll`
  - `INSTALL.txt`
  - `SHA256SUMS.txt`
- Uploads the DLL, flat ZIP, and compiler log as a GitHub Actions artifact.

## Upload the project
1. On GitHub, create a **Private** repository named `RageCoop-Android-SyncFix`.
2. Extract this ZIP on your device/computer.
3. Upload the **contents** of the extracted folder to the repository root. Do not upload the ZIP itself.
4. Preserve the `.github/workflows/build.yml` path exactly. The `.github` folder is hidden on some file managers; make sure it is included.
5. Commit the files to the `main` branch.

The ZIP has no extra outer project folder. Its internal folders (`.github`, `source`, `build_refs`, `tests`, etc.) are required and must stay in place for the build.

## Run the build
- A push to `main` starts the workflow automatically.
- Or open the repository's **Actions** tab, select **Build RageCoop Android Sync Fix**, and choose **Run workflow**.
- Wait for the job to finish. A green check means the source checks and C# compilation passed.
- Open the completed run and download the artifact named `RageCoop-Android-SyncFix`.
- Inside it, download `RageCoop-Android-SyncFix-ClientDLL.zip`. That ZIP is flat (no subfolders).

## Android installation
1. Back up the existing `RageCoop.Client.dll`.
2. Extract the flat ClientDLL ZIP.
3. Copy only `RageCoop.Client.dll` to:
   `/storage/emulated/0/games/gtav/scripts/RageCoop/RageCoop.Client.dll`
4. Do not replace `RageCoop.Core.dll`, the APK, the native bridge, GtavHost, or Mono runtime.
5. Test with two clients. A successful compile does not prove multiplayer sync works at runtime. Keep the old DLL so you can roll back.

## If the build fails
Download `build_logs/RageCoop.Client.log` from the artifact (the workflow uploads it even when compilation fails), then share that log. The workflow may reveal API/reference mismatches that cannot be confirmed by source-only tests.

## Build details
The workflow uses GitHub's current supported Actions patterns for checkout, .NET setup, Python setup, and artifact upload. See:
- GitHub .NET Actions guide: https://docs.github.com/en/actions/tutorials/build-and-test-code/net
- checkout: https://github.com/actions/checkout
- setup-dotnet: https://github.com/actions/setup-dotnet
- setup-python: https://github.com/actions/setup-python
- upload-artifact: https://github.com/actions/upload-artifact
