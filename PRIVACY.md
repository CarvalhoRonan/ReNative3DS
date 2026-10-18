# Privacy notes

The current ReNative3D36S frontend uses local ROM files and runs Docker/Podman locally to build its output. It creates diagnostics logs under `%LOCALAPPDATA%\ReNative3D36S\logs`, which may include local file paths and error messages. The project-local Windows publisher stores build logs under `logs/`.

First-time setup requires network requests to the official source/code/tool repositories and package services (GitHub, Microsoft/.NET/NuGet and container registries). This document does not imply a security audit or guarantee regarding external services.

ROMs and console AES keys should not be uploaded to public repositories or issue trackers. Review logs before publication. Donations are optional and use independently operated providers; no payment information is necessary to use the app.
