# Third-party notices

## ForgeUI

This project may reuse the pure C# enemy-bar state/projection code (`EnemyBarBook`, `ClipTransform`, related pure helpers and tests) from ForgeUI 0.1.0.

- Source: https://github.com/NAinfini/GTFO-Forge-Mods
- Snapshot commit used for research: `3083cab72e93f2d934bafdfef20f5ee96d59c181`
- License: MIT

```
MIT License

Copyright (c) 2026 NAinfini

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

Nothing in this file grants rights to third-party mods whose license is unknown.

## BFV feedback assets (external, not in this repository)

The `EnemyHealthBar.BFVFeedback` runtime code is original: it uses the game's hit/death
indicator events and a read-only damage prefix, and does not copy KillFeed source.
The optional audio/icon pack was extracted from HawChow/KillFeed 1.2.19:

- Source: https://thunderstore.io/c/gtfo/p/HawChow/KillFeed/
- Package SHA256: `F3EB59372468CB8BAC3AEEA88EFAD02ADBF576635658CC744513B67B20C2AD70`
- License: no explicit LICENSE; the audio/icon assets are Battlefield-derived
  (EA/DICE) and are **not committed to this repository**.
- Private/local use only. Before public distribution, replace the pack with CC0 or
  self-made assets, or obtain permission. The DLL works without the pack and falls
  back to vanilla feedback when assets are absent.

## DamageNumbers / FloatingTextAPI

The damage-number sub-mod was designed after the Harmony hook/parameter flow and
the armor/weakspot/back-bonus classification used by GTFO-Modding/DamageNumbers
and its FloatingTextAPI companion. No FloatingTextAPI runtime dependency or TMP
asset bundle is shipped; the IMGUI renderer is original.

- Source: https://github.com/GTFO-Modding/DamageNumbers
- License: MIT, Copyright (c) 2022 Dak

```
MIT License

Copyright (c) 2022 Dak

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

