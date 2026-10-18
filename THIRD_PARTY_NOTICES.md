# Attribution and third-party notices

## Project code and licenses

**ReNative3D36S Windows GUI**: Overclock Informática, offered under the [MIT license](LICENSE).

**Pocket Native**: https://github.com/whovian64/pocket-native, © 2026 fearkov and © 2026 whovian, under the upstream [MIT license](licenses/POCKET-NATIVE-MIT.txt). ReNative3D36S retrieves upstream source for local compilation; it does not claim authorship of Pocket Native.

Pocket Native is based on **Zakuro** (MIT) and **3dsrecomp** by fearkov and acknowledges WINDROID-EMU/3dsrecomp. The project includes game profiles, an emulation engine, and an ahead-of-time recompiler maintained by their respective authors.

## Additional licenses in generated Pocket Native builds

Pocket Native's [CREDITS.md](https://github.com/whovian64/pocket-native/blob/master/CREDITS.md) documents:

- The M PLUS Rounded 1c menu font: **SIL Open Font License 1.1**.
- An open-source shared-system-font replacement obtained during upstream builds from Azahar's archived files: may incorporate materials under **GPL-2.0, GPL-2.0-or-later with GNU font embedding exception**, and **SIL OFL 1.1**.
- Rust dependencies licensed under multiple terms, including MIT, Apache-2.0 and MPL-2.0 (Symphonia).

The upstream build places `LICENSE`, `CREDITS.md`, `OFL.txt`, and `THIRD_PARTY_LICENSES.txt` in the generated Pocket Native output. Preserve them when sharing generated output; review additional license obligations before distribution.

**Important:** Choosing MIT for our own frontend does **not** override or replace these third-party licenses or rights in game files, fonts, emulators, compilers or proprietary dependencies. No game ROMs, keys or copyrighted console firmware are redistributed by this repository.

No affiliation with Nintendo or endorsement by the Pocket Native maintainers is implied.

## Selectable engines and referenced projects (v0.2.0)

- **Pocket Native Official** (https://github.com/whovian64/pocket-native): © 2026 fearkov and © 2026 whovian, MIT. Downloaded separately from the official release when selected. The upstream credits remain in game builds.
- **Pocket Native-compatible local fork**: user-supplied and user-selected source tree, not copied into or redistributed by ReNative3D36S. **License obligations depend on the chosen fork**, which the user must verify. The app does not edit or auto-update local forks. The fork should preserve original notices, and the GUI isolates its scratch volume from the official engine.
- **ReNative Optimized**: planned only, not currently shipped as a functioning engine. No performance benefits are claimed.
- **Zakuro** (https://github.com/fearkov/zakuro): by fearkov, MIT; full upstream text copied in [licenses/ZAKURO-MIT.txt](licenses/ZAKURO-MIT.txt) for reference. **3dsrecomp** (https://github.com/fearkov/3dsrecomp), by fearkov, is a separate experimental static recompiler designed to run with Zakuro. The source repository's README explains commands and redistribution restrictions; no 3dsrecomp binary or source is distributed in this GUI. ReNative only calls a user-selected executable. The generated game-code libraries may contain content subject to rights of the game owner and **must not be published in this repository**.
- **ScarletNX** (https://github.com/scarletnx/ScarletNX): GPLv3 code by its contributors. It has an early 3DS backend but no claimed first game frame or gameplay. Research reference only; no integration, linking, download, source or binary redistribution.
- **Azahar** (https://github.com/azahar-emu/azahar): 3DS emulator by the Azahar community, GPLv3. Not a static 3DS recompiler; research reference only, not integrated or redistributed.

All names are used descriptively to identify their corresponding original projects. ReNative3D36S is independent, unaffiliated with and not endorsed by the maintainers or Nintendo. User must review the actual dependency/license terms before redistributing binaries; a GPL-licensed runtime cannot simply be relabeled under the GUI MIT license.
