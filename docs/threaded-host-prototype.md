# Threaded application host prototype

`GuiShark.ThreadedHost` is a separate demo project. The GuiShark libraries remain HTML/CSS rendering components and know nothing about hosting applications.

Run from the repository root after building the solution:

```powershell
dotnet run --no-build --project src/demos/GuiShark.ThreadedHost
```

The host has its own GuiShark document, view, renderer and visible OpenGL context. A second GuiShark application runs on a dedicated .NET thread with another view, renderer and hidden OpenGL context. OpenTK creates both GLFW windows on the host thread; the hidden context is released there and made current only on the app thread. The contexts share GPU textures. The app renders into framebuffer textures, finishes GPU writes, and publishes complete texture names through a three-slot exchange. The host blits the latest texture into its panel. Its texture matches the panel's physical framebuffer pixels, including display scaling, so the final blit is 1:1 and does not soften text. Panel resize recreates the app's targets on its GL thread. GuiShark layout and queued mouse coordinates remain in logical UI units. No rendered frame pixels travel through managed memory between the app and host; the app alone touches its view.

Try **Freeze app for 5 seconds**. The blue panel keeps its last frame while the host status and frame counter advance. The app then resumes. **Throw handled app error** exercises the app's command boundary: the exception is caught and shown in the status while both render loops continue. The button inside the blue panel runs a C# callback on the app thread. For an automated run:

```powershell
dotnet run --no-build --project src/demos/GuiShark.ThreadedHost -- --verify
```

This clicks the app's GuiShark button, verifies a host frame advance during the five-second app pause, then checks app resumption and continuation after a caught exception. It also resizes the host and checks that the app publishes textures at the new pixel size. Run with `--capture` instead to save a screenshot at `artifacts/threaded-host.png`. It does **not** prove crash isolation. An unhandled .NET exception, process termination, native crash, or GPU driver hang can still take down the entire host. A truly stuck thread cannot be killed safely; shutdown waits up to seven seconds and then leaves its context for process exit. Separate processes are necessary if those failure modes must be contained. Shared OpenGL contexts and window setup still need validation on Linux and macOS drivers.
