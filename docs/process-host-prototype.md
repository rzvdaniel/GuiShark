# Separate-process GuiShark app prototype

GuiShark.ProcessHost is a small two-panel workspace prototype. It starts the independently built Aurora executable, forwards the host panel's logical size and input events over a named pipe, and displays Aurora's rendered frames. Aurora owns its UiView, HTML/CSS, application state, and OpenGL context. It renders into an offscreen framebuffer and sends paced PNG frames; the host decodes the latest frame and draws it in the panel. Frame delivery is bounded so slow pipe writes replace stale frames instead of building latency. The host has no project reference to Aurora. Shared message types live in src/hosting/GuiShark.AppProtocol; the HTML/CSS engine remains independent of this hosting model.

From the repository root, build and run:

```powershell
dotnet build Gui.Shark.sln -c Release
dotnet run --no-build -c Release --project src/demos/GuiShark.ProcessHost
```

The host loads Aurora from its build output by default. To exercise click, freeze, fatal crash, and restart automatically:

```powershell
dotnet run --no-build -c Release --project src/demos/GuiShark.ProcessHost -- --verify
```

Aurora can also run independently. Double-click `src/demos/GuiShark.AuroraProcess/bin/Release/net10.0/GuiShark.AuroraProcess.exe` on Windows, or launch it from the repository root:

```powershell
dotnet run --no-build -c Release --project src/demos/GuiShark.AuroraProcess
```

With no arguments it creates its own OpenGL window and renders the same HTML/CSS. On Windows it is built as a GUI executable, so double-clicking it does not open a console. Its button uses the same `AuroraCounter` behavior as the hosted process. `--verify-standalone` opens the window, clicks the button programmatically, and exits after checking the result.

For a portable local package directory, publish Aurora and point the host at its manifest:

```powershell
dotnet publish src/demos/GuiShark.AuroraProcess -c Release -o artifacts/aurora-package
./artifacts/aurora-package/GuiShark.AuroraProcess.exe
dotnet run --no-build -c Release --project src/demos/GuiShark.ProcessHost -- --app artifacts/aurora-package/app.json
```

Close any Aurora or host window using that package before publishing over the same directory. You can publish to a new directory while an older version remains open.

The package has an `app.json` with protocol version, app ID/title, managed entry assembly, assets directory, and HTML document. Its HTML, CSS, fonts, and executable are copied alongside the manifest. The Windows `.exe` is framework-dependent and needs .NET 10 and an OpenGL 3.3 driver. Build or publish on each target platform for its native launcher. A creator can build a separate project that references `GuiShark.AppProtocol` for hosted behavior and `GuiShark.OpenGL` for standalone rendering, publish it to a folder, and pass the manifest path with `--app`. Protocol v1 carries frame, ready, state, status, resize, pointer, key, and committed-text messages; freeze and crash are demo controls. Frame delivery is latest-only and capped at 16 million pixels. The host and child both run as the current OS user. This is process fault isolation, not a security sandbox for untrusted code or assets.

The host remains responsive and keeps displaying Aurora's last frame when Aurora freezes or exits. It routes mouse movement, button, wheel, key, committed text, and panel-resize events to the focused app. GuiShark handles controls and text editing in the child. IME composition, clipboard bridging, multiple simultaneous app instances, state persistence, app discovery, and an installation command remain future work. Frames currently travel as PNG over the local named pipe, which favors a simple cross-process contract over bandwidth and encode/decode cost; GPU texture sharing or a more efficient pixel transport can be evaluated separately. Child state is lost on restart; host failure currently closes the pipe and ends Aurora.

NuGet can distribute an eventual package or installer, but `dotnet tool install` installs CLI commands rather than defining a workspace app catalog. A future `guishark app install` could fetch a versioned package into a user app directory and validate its manifest. The plain-folder contract keeps local development independent of any registry or install service.
