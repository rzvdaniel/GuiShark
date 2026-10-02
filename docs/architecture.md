# GuiShark architecture and extension boundaries

## Purpose and decision

GuiShark's foundation is an embeddable engine that interprets HTML/CSS, lays out a retained interface, and renders it into a host application's graphics surface. Its original use is game HUDs and menus described in HTML/CSS. The same foundation can also serve rich desktop applications.

**HTML/CSS rendering remains the core product. Higher-level extensions must be separate projects that embed and depend on GuiShark. The core must not depend on those extensions.**

Preserving the core means preserving this responsibility and its embedding contract, rather than freezing its implementation. We can enhance parsing, layout, styling, rendering and interaction toward familiar browser behavior. Desktop application features must not turn the core into an application framework or make a game adopt one.

This document records the architectural direction. The extension projects below are proposed boundaries, not implemented packages or a commitment to their final names. No project move or API migration is required by this decision.

## Existing foundation

| Project | Responsibility |
| --- | --- |
| `src/GuiShark` | HTML loading, supported CSS cascade, retained DOM/state, layout, text metrics contract, hit testing, focus, input and generic HTML control behavior. Independent of windows and graphics contexts. |
| `src/GuiShark.OpenGL` | OpenGL rendering, image/text resources, font loading and shaping, clipping, DPI-aware text rasterization and host graphics-state restoration. Depends on `GuiShark`. |
| `src/demos` | Executable hosts, input adapters, sample HTML/CSS and game/application behavior. Embed the SDK libraries. |
| `src/tests` | Regression verification of the supported contracts. Tests are consumers, not runtime dependencies. |

The two SDK libraries together provide the current rendering foundation. Keeping rendering implementation in `GuiShark.OpenGL` preserves the separation between document/layout behavior and graphics resources. Skia/HarfBuzz and OpenGL implementation details should remain outside the host-independent `GuiShark` project.

Current generic behaviors such as text editing, dropdowns, tabs, dialogs and tooltips remain supported in the foundation. Their presence does not authorize moving application workflows into it.

## Dependency direction

| Consumer | Depends on | Owns |
| --- | --- | --- |
| Proposed `GuiShark.Components` | `GuiShark` public APIs | Reusable tree/table/property-editor behavior, component state and HTML/CSS templates. It should not require OpenGL merely to describe or manage elements. |
| Proposed desktop host project | `GuiShark`, `GuiShark.OpenGL`, chosen platform/window libraries | Windows, graphics contexts, frame loops, native input/IME adapters, clipboard and native services. |
| Game or desktop application | Rendering foundation and whichever extensions it chooses | Application data, commands, persistence, networking and domain behavior. |

Components and desktop hosting are independent optional layers. A game can reuse a tree component with its existing host. A desktop application can use its own components with a desktop host. An application can also embed the rendering foundation directly, as the demos do today.

Neither SDK library may reference an application, a component collection or a desktop host. Additional rendering backends, if justified later, belong in separate renderer projects depending on the foundation. They are not part of this implementation plan.

## What belongs in the core

Core improvements express general HTML/CSS or document interaction capabilities that can benefit both games and applications:

- Parsing, selectors, cascade, inheritance and supported markup semantics.
- Box sizing, intrinsic sizing, text flow, resizing and layout algorithms.
- Images, transparency, borders, backgrounds, clipping and paint order.
- Text shaping, font fallback, bidirectional layout, alignment and editing geometry.
- DOM mutation/invalidation, event delivery, focus, pointer capture and generic control state.
- Generic semantic information and extension points needed by an accessibility adapter.

The long-term rendering direction is progressively closer browser behavior. GuiShark currently supports a bounded subset, with documented differences; it is not a full browser. New behavior should specify supported syntax, layout/paint/input effects, compatibility and failure behavior. See [the current HTML/CSS subset](css-subset.md).

Adding layout or paint capability does not require a JavaScript runtime, navigation engine, HTTP client or embedded browser. Application callbacks can remain C#. Any optional scripting layer or external asset provider should be a separate consumer with an explicit contract.

## What belongs in extensions

A reusable component coordinates ordinary elements into a higher-level interaction. A tree, for example, can use containers, rows, buttons, text and images. Its component owns node data, expansion, selection, indentation policy and tree-specific keyboard behavior; HTML/CSS controls its appearance. Loading a folder hierarchy belongs to the application or a service, not to the tree or renderer.

The same boundary applies to data grids, property inspectors, docking workspaces, command systems, application data binding and themes packaged for particular products. Native file dialogs, OS integration, window management and accessibility platform bridges belong in host/adapter projects.

An extension must use supported public APIs rather than reaching into internal layout or rendering state. If composition exposes a missing capability, such as safe dynamic child insertion, introduce a general DOM operation in the foundation with clear invalidation, focus and lifetime behavior. Do not add tree-specific fields or renderer branches as a shortcut. Public dynamic composition is an area to review, not a capability assumed to be complete today.

CSS and templates remain replaceable by the developer. Extensions should expose data and behavior contracts without forcing a fixed skin or a particular desktop host.

## Hosting, ownership and compatibility

The host owns its window, graphics context, framebuffer clearing, frame loop and buffer presentation. It forwards input and logical size changes, supplies assets, and chooses when to render. GuiShark must remain usable as a transparent layer over an existing game scene.

Keep ownership explicit: renderers/backends release resources they own, while borrowed views and font books remain the caller's responsibility. Existing GL-thread and disposal-order requirements continue to apply. Components must detach their event subscriptions when removed or disposed; hosts must stop native input sessions and release their platform resources. Background services marshal UI updates onto the host's UI/render thread. See [embedding and lifetime rules](embedding.md) and [IME integration](multilingual-input.md).

Preserve existing public contracts unless a change is deliberately reviewed and documented. Optional extension packages must not force dependencies or initialization steps onto direct SDK consumers. Share behavior through small classes with clear responsibilities; add abstractions when a real implementation or consumer needs them.

## Review and next work

Before implementing an extension, evaluate each requirement:

1. Is it a general HTML/CSS rendering or interaction capability? Improve the appropriate SDK library.
2. Does it coordinate primitives into a reusable widget or application convention? Put it in a separate component project.
3. Does it need a window, native service or OS API? Put it in a host/adapter project.
4. Does it express application data or a workflow? Keep it in the application.

The next review should inspect composition APIs, dynamic document updates, resizing, input/focus behavior, rendering limits and performance. A proposed explorer/editor demo with a navigation tree, document tabs and a properties panel can then expose concrete gaps. It belongs in a separate demo/application project and should consume the same foundation as the balloon HUD.

Prioritize core capabilities revealed by that review, then develop optional components and hosting independently. Do not create empty extension projects or a large control catalogue before their contracts are demonstrated. Both game HUDs and desktop applications should remain useful consumers of the same rendering engine.
