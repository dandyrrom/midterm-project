# midterm-project

A Unity game project (`midterm-project`) built with **Unity 6000.4.11f1** using the
Universal Render Pipeline (URP). The pinned editor version lives in
`ProjectSettings/ProjectVersion.txt`; the main scene is `Assets/Scenes/SampleScene.unity`.

## Cursor Cloud specific instructions

### Editor location & why the update script guards the download
- The Unity editor is installed at `/opt/unity/Editor/Unity` (version `6000.4.11f1`,
  revision `b0a1d6caadd2`). It is a ~4 GB download / ~8 GB on disk and is **not** in the
  repo. The startup update script installs Linux system libs and only downloads/extracts
  the editor if `/opt/unity/Editor/Unity` is missing, so it is a fast no-op once the
  editor is present in the VM snapshot. Do not add the editor to git.
- Unity must always be run headlessly here: use `xvfb-run -a <cmd>` together with
  `-batchmode -nographics`. There is no display otherwise.

### Licensing is required before ANY editor operation (activate every session)
- Every editor action (project import, tests, builds — even `-quit`) fails with
  `No valid Unity Editor license found` until a license is activated. Activation is
  **not** persisted reliably across cloud pods (it is machine-bound), so treat it as a
  per-session step, not an update-script step. Never put credentials in the update script.
- Activate with a Unity account (Unity Personal works). Credentials come from secrets
  `UNITY_EMAIL` and `UNITY_PASSWORD` (add `UNITY_SERIAL` only for Plus/Pro seats):

  ```bash
  xvfb-run -a /opt/unity/Editor/Unity -batchmode -nographics -quit \
    -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD" \
    ${UNITY_SERIAL:+-serial "$UNITY_SERIAL"} \
    -logFile /tmp/unity_activate.log
  ```
  Confirm success with `grep -i "license" /tmp/unity_activate.log` (look for a granted
  entitlement / no `No valid Unity Editor license found`). Return the seat when done with
  `-returnlicense` if using a floating/Plus/Pro seat.

### Build / test / run commands
- **Compile + import project** (also surfaces C# compile errors — Unity has no separate
  linter; the C# compiler is the lint gate):
  ```bash
  xvfb-run -a /opt/unity/Editor/Unity -batchmode -nographics -quit \
    -projectPath /workspace -logFile /tmp/unity_import.log
  ```
- **Run tests** (Unity Test Framework is a package dependency; the project currently has
  no test assemblies, so this mainly validates the harness):
  ```bash
  xvfb-run -a /opt/unity/Editor/Unity -runTests -batchmode \
    -projectPath /workspace -testPlatform EditMode \
    -testResults /tmp/editmode-results.xml -logFile /tmp/unity_tests.log
  ```
- **Build a player (dev "run" target).** There is no built-in CLI flag to build a player;
  Unity needs a static `-executeMethod`. Add a small editor script under `Assets/Editor/`
  that calls `BuildPipeline.BuildPlayer` for `StandaloneLinux64`, then run it with
  `-buildTarget Linux64 -executeMethod <Class>.<Method>`. The built player can be launched
  under `xvfb-run` to confirm it boots.

### Notes
- The editor writes verbose progress to the `-logFile` path, not stdout; always pass
  `-logFile` and read that file to diagnose failures.
- First import after a fresh checkout is slow (package resolution + asset import); later
  runs reuse the generated `Library/` folder (git-ignored).
