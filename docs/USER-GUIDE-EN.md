# ReNative3D36S v0.1.8: build and user guide

This is an experimental Windows x64 GUI for recompiling your own 3DS game dumps for R36S handhelds using Pocket Native. The built Windows GUI is self-contained, but game recompilation still needs Docker Desktop/Podman.

## Build the standalone Windows app

1. Get the code from [GitHub](https://github.com/CarvalhoRonan/ReNative3DS) and extract it to a writable local folder, preferably outside OneDrive.
2. Double-click `COMPILAR-UM-CLIQUE.cmd`. It checks the latest stable upstream Pocket Native release, installs the .NET 10 SDK locally if needed, restores NuGet, and publishes the standalone app.
3. Open `dist/portable/ReNative3D36S.exe`. If publication fails, inspect the publisher logs under `logs/`.

## Start Docker Desktop and allow folder access

1. **Open Docker Desktop and wait until the Docker engine reports that it is running.** Simply launching the application window is not enough.
2. Complete any WSL2, virtualization, or Windows restart prompts during the first setup.
3. **When prompted, allow Docker to access the ROM input folder and the output destination.** This is important if your ROM or output is on a separate drive such as `E:`.
4. On versions that offer it, check **Docker Desktop → Settings → Resources → File Sharing**, add the required paths and apply. The setting may not appear under a WSL2 backend; ensure the engine is running and Windows/WSL file permissions permit access.
5. On errors like `Mounts denied`, `drive not shared`, `permission denied`, or bind-mount failures, revisit these settings and restart Docker.

Docker Windows help: https://docs.docker.com/desktop/setup/install/windows-install/

## Recompile a game

Select a decrypted `.3ds/.cci` dump of your own game or a supported `.cia` along with AES keys from your own console; select the output parent folder and click **Start recompilation**. The app checks the newest upstream stable release, downloads/prepares the source in its local cache, starts the Docker/Podman build, and reports milestone-based approximate progress.

Output example:

```text
Chosen folder/
  Game Name/
    pocket-native/
      run.sh
      pocket-native
      games/<title-id>/title.so
      games/<title-id>/game.3ds
  Game Name.sh
```

**Copy BOTH `Game Name/` AND its sibling `Game Name.sh`** to the SD card's `roms/ports` or `roms2/ports` directory. Leave the names unchanged, eject the card safely, then restart EmulationStation. The `.sh` belongs in the top-level Ports folder.

## Updates and limitations

Engine and title profiles can be updated independently of the Windows GUI. The previous source/profiles are backed up. Existing games are not silently rebuilt. **Upgrading the engine/profiles on the PC does not upgrade the previously compiled ARM64 game binary or `title.so`; recompilation may be needed.** Do not mistake progress percentage for predicted remaining time.

CIA conversion may fail for titlekey-layer encrypted CIA content, seeded NCCH, missing AES key entries, or incompatible dumps. Read the local logs and original docs. No ROMs or keys are supplied by this project.

## Consult the upstream documentation for errors

**If you run into problems, follow the original Pocket Native project's documentation in addition to this guide.** The upstream project is the authority for engine compatibility and supported firmware:

- Original README: https://github.com/whovian64/pocket-native
- Official START_HERE troubleshooting: https://github.com/whovian64/pocket-native/blob/master/START_HERE.md
- Games / profiles / known limitations: https://github.com/whovian64/pocket-native/blob/master/docs/games.md
- Upstream issues: https://github.com/whovian64/pocket-native/issues

Never upload game dumps, console secrets, or private paths when sharing logs. Overclock Informática frontend code is MIT licensed; third-party licenses remain separate.


## v0.1.9 interface

The Windows GUI has a new clean dark-blue layout (no illustrations of consoles in the header), with a typographic R3D identity, four functional sidebar pages for recompilation, updates, library and settings, and a separate engine/language/shortcut panel. The library lists individually generated game folders within the chosen destination. The output preview is only indicative until Pocket Native detects the official game name.

**Start Docker Desktop and wait for its engine to be ready** before recompiling games. If prompted, allow Docker access to the ROM and output folders. For conversion, compatibility and runtime errors also follow the official upstream instructions at https://github.com/whovian64/pocket-native/blob/master/START_HERE.md.
