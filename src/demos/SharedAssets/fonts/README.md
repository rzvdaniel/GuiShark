# Multilingual sample fonts

Noto Sans JP, Noto Sans Arabic and Noto Sans Devanagari were downloaded from the [Google Fonts repository](https://github.com/google/fonts/tree/main/ofl) on 2026-10-01. Upstream paths:

- `ofl/notosansjp/NotoSansJP[wght].ttf`
- `ofl/notosansarabic/NotoSansArabic[wdth,wght].ttf`
- `ofl/notosansdevanagari/NotoSansDevanagari[wdth,wght].ttf`

These assets are static instances at `wght=400`, `wdth=100` where present, generated using fontTools 4.60.1 `instantiateVariableFont`. This avoids the Japanese variable font's default weight of 100; GuiShark currently does not expose variable font axes. No glyph subsetting was performed. Keep each `*-OFL.txt` license beside its font. The reserved name Source is not used for these modified assets.

Fonts and licenses are linked into Controls Gallery and Text Lab's `Assets/fonts` at build time. They require no installation, download or fontTools dependency when building or running GuiShark. To reproduce, load the upstream TTF with `TTFont`, pass a dictionary pinning every axis to its default except `wght=400`, and save the result of `instantiateVariableFont(font, axes, inplace=True)`.
