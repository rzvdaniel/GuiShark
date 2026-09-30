# Third-party notices

GuiShark source and the original `src/demos/GuiShark.Demo/Assets/shark.png` artwork are distributed under this repository's MIT license. The artwork is an original shark-fin mark, not an Apple asset. The demo's visual styling is original.

The five woodland PNG assets in `src/demos/GuiShark.Balloon/Assets/art/` were generated for this project using the built-in image generation tool and are distributed with the project under its MIT license. Three are used by the current skin; two earlier studies are retained as optional artwork. No World of Warcraft or other Blizzard artwork is bundled. See [asset paths and generation prompts](docs/woodland-artwork.md).

The demo bundles Lato Regular and Bold from the [Google Fonts Lato directory](https://github.com/google/fonts/tree/main/ofl/lato). Lato is Copyright (c) 2010–2014 by tyPoland Lukasz Dziedzic, with Reserved Font Name "Lato", and is licensed under the SIL Open Font License 1.1. The complete license is in `src/demos/GuiShark.Demo/Assets/fonts/OFL.txt` and copied with the application assets.

NuGet dependencies retain their own licenses and notices:

- [AngleSharp](https://github.com/AngleSharp/AngleSharp): MIT.
- [OpenTK](https://github.com/opentk/opentk): MIT; its GLFW native dependency uses the zlib/libpng license.
- [SkiaSharp](https://github.com/mono/SkiaSharp): MIT, with Skia and other native third-party notices distributed by its packages.
- [FreeTypeSharp 3.1.0](https://github.com/ryancheung/FreeTypeSharp): MIT, Copyright 2024 ryancheung. Its patched FreeType 2.13.2 native binaries use the FreeType License. Portions of this software are copyright © 2023 The FreeType Project (www.freetype.org). All rights reserved. The [FreeType license](licenses/FreeType-FTL.txt) is copied beside application binaries.
- [MSDFgen 1.13](https://github.com/Chlumsky/msdfgen) and [msdf-atlas-gen 1.4](https://github.com/Chlumsky/msdf-atlas-gen): MIT, Copyright (c) 2014–2025 Viktor Chlumsky. The MSDF shader follows the upstream reconstruction formula; [MSDFgen's license](licenses/MSDFgen-MIT.txt) is copied with application binaries. The [atlas generator license](licenses/MSDF-atlas-gen-MIT.txt) is retained for tool provenance; no generator executable is bundled.

The Text Lab's regular and bold MSDF PNG/JSON atlases were generated from the bundled Lato fonts. They follow the font's SIL OFL license. The font files and OFL notice are copied into every demo's `Assets/fonts` directory. Reproduction instructions and the exact charset are in [text-rendering notes](docs/text-rendering.md).

Keep the font license and review native dependency notices when redistributing an application.
