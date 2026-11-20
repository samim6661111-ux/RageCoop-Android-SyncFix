RAGECOOP ANDROID SYNC FIX — GITHUB BUILD KIT
=============================================

GitHub repository name:
  RageCoop-Android-SyncFix

গুরুত্বপূর্ণ:
- ZIP-এর বাইরে আলাদা project wrapper folder নেই।
- কিন্তু ZIP-এর ভেতরের .github/, source/, build_refs/, tests/ folder অবশ্যই রাখতে হবে।
  এগুলো ছাড়া source compile হবে না।
- Final DLL ZIP flat হবে: কোনো subfolder থাকবে না।
- Repository PRIVATE রাখবে; এতে source ও binary reference files আছে।

GitHub-এ build:
1) ZIP extract করো।
2) GitHub-এ PRIVATE repository তৈরি করো: RageCoop-Android-SyncFix
3) Extract করা folder-এর ভেতরের সব content repository root-এ upload করো।
   ZIP file নিজে upload করবে না।
4) .github/workflows/build.yml path ঠিক আছে নিশ্চিত করো।
5) main branch-এ commit করো।
6) Actions tab -> Build RageCoop Android Sync Fix -> Run workflow চাপতে পারো।
   main-এ push করলেও workflow নিজে চলবে।
7) Build success হলে Actions run-এর artifact:
   RageCoop-Android-SyncFix
   download করো।
8) Artifact-এর ভেতরের RageCoop-Android-SyncFix-ClientDLL.zip নাও।

Build workflow কী করে:
- Python source checks চালায়।
- Windows runner-এ .NET 8 Roslyn compiler দিয়ে শুধু RageCoop.Client.dll compile করে।
- flat ZIP বানায়; এর root-এ DLL, INSTALL.txt, SHA256SUMS.txt থাকবে।
- compiler log-ও artifact-এ রাখে।

Android-এ:
- আগে পুরোনো RageCoop.Client.dll backup করো।
- Flat ZIP extract করে শুধু RageCoop.Client.dll কপি করো:
  /storage/emulated/0/games/gtav/scripts/RageCoop/RageCoop.Client.dll
- RageCoop.Core.dll, APK, libgtavbridge.so, libmonohost.so, GtavHost.dll
  বা Mono runtime replace করবে না।
- দুইটি client দিয়ে multiplayer test না করা পর্যন্ত sync পুরোপুরি ঠিক হয়েছে ধরে নেবে না।

Build failure হলে:
- Actions artifact থেকে build_logs/RageCoop.Client.log পাঠাও।
- Compilation success runtime sync success-এর প্রমাণ নয়।

আরও বিস্তারিত: GITHUB_BUILD_GUIDE.md
