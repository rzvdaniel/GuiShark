# Third-party notices

GuiShark source and the original `src/demos/GuiShark.Demo/Assets/shark.png` artwork are distributed under this repository's MIT license. The artwork is an original shark-fin mark, not an Apple asset. The demo's visual styling is original.

The five woodland PNG assets in `src/demos/GuiShark.Balloon/Assets/art/` were generated for this project using the built-in image generation tool and are distributed with the project under its MIT license. Three are used by the current skin; two earlier studies are retained as optional artwork. No World of Warcraft or other Blizzard artwork is bundled. See [asset paths and generation prompts](docs/woodland-artwork.md).

The demo bundles Lato Regular and Bold from the [Google Fonts Lato directory](https://github.com/google/fonts/tree/main/ofl/lato). Lato is Copyright (c) 2010–2014 by tyPoland Lukasz Dziedzic, with Reserved Font Name "Lato", and is licensed under the SIL Open Font License 1.1. The complete license is in `src/demos/GuiShark.Demo/Assets/fonts/OFL.txt` and copied with the application assets.

NuGet dependencies retain their own licenses and notices:

- [AngleSharp](https://github.com/AngleSharp/AngleSharp): MIT.
- [OpenTK](https://github.com/opentk/opentk): MIT; its GLFW native dependency uses the zlib/libpng license.
- [SkiaSharp](https://github.com/mono/SkiaSharp): MIT, with Skia and other native third-party notices distributed by its packages.


Keep the font license and review native dependency notices when redistributing an application.

Controls Gallery and Text Lab bundle regular-weight static instances of Noto Sans JP, Noto Sans Arabic and Noto Sans Devanagari under SIL OFL 1.1. Their individual licenses are retained beside the fonts and copied into application assets. See [font provenance](src/demos/SharedAssets/fonts/README.md).

- [SkiaSharp.HarfBuzz / HarfBuzzSharp](https://github.com/mono/SkiaSharp): MIT; native [HarfBuzz](https://github.com/harfbuzz/harfbuzz) uses MIT-style licenses listed in [COPYING](licenses/HarfBuzz-MIT.txt), copied with applications.
- [Silk.NET.SDL](https://github.com/dotnet/Silk.NET): MIT; Ultz.Native.SDL bundles SDL 2.32.10. [SDL's zlib license](licenses/SDL-zlib.txt) is retained and copied with Controls Gallery.

The former standalone FreeType and MSDF backends and their generated assets/tools have been removed. Their earlier source remains available in Git history.
