# Windows IME checks

Windows verification is enough for this development milestone. Linux/macOS native interaction remains explicitly unverified; cross-publish package inspection is recorded separately.

## Current evidence

- Native Windows Controls Gallery launches and responds to mouse interaction.
- Multilingual tab, Preview kana, Convert and Commit Tokyo were exercised through native window automation. Kana and converted preedit are underlined; committing removes the underline and logs a committed-value change.
- Physical keyboard: the user confirmed that Ctrl+Z undoes the Tokyo commit and Tab moves focus to the notes field. Injected shortcuts produced no visible changes, so they were not used as evidence of a host failure.
- Mouse cancellation and switching focus to the notes field both restore the empty committed Japanese field without inserting preedit into the notes. Scroll/resize during composition remains a manual checklist item.
- The queried Windows input-language profile contains English (Ireland) only. Actual Japanese conversion and candidate-window placement have not been verified.
- Managed composition regression tests already cover undo, cancellation, modal Escape, focus changes, password masking, wrapped preedit and offsets. These are SDK checks, not an OS IME test.

## Run

```powershell
dotnet run --project src/demos/GuiShark.ControlsDemo -- --page=multilingual
```

## Without an installed IME

1. Click Preview kana, then Convert. The field should show underlined 東京 without a committed-value event.
2. Click Commit Tokyo. The underline should disappear. Click the field and press Ctrl+Z: one undo should restore its original value. Ctrl+Y should redo.
3. Preview again and press Escape. The original committed value should return; focus should stay in the field.
4. Preview again, then click the notes field or press Tab. Preedit should be canceled without inserting it into either field.
5. Preview, then scroll the page or resize the window. The underline and caret should follow the field and remain clipped to it.

## With Japanese Microsoft IME enabled

Enable/select Japanese input using your Windows language controls, then switch the IME to Hiragana. The SDK exercise buttons do not perform native conversion.

1. Click the Japanese field and type `toukyou` with physical keys. Confirm that preedit is underlined.
2. Press Space to convert. Confirm that the native candidate window appears near the visible caret and selecting 東京 updates the preedit.
3. Press Enter to commit once. Confirm that preedit ends and the committed value changes. Ctrl+Z should undo the whole commit.
4. Start another composition and cancel with Escape. Confirm that no committed text changes.
5. During composition, switch fields, scroll, resize and move focus outside the app. Confirm there are no stale commits into another field and no detached candidate window.
6. Try different Windows display scales when available. Verify candidate placement against the visible caret; simulated Text Lab density does not test native display DPI.
7. In the gallery's password dialog, composing text must remain masked. Do not use a real password; copy/cut must remain disabled.

Record the OS version, IME mode and display scale with results. Mark unsupported or untested behavior explicitly. No Windows language settings or optional language features were changed by the automated check.
