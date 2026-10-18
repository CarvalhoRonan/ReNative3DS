# Security / Segurança

- Do not put console AES keys, ROMs, firmware dumps, bank account data, passwords, personal paths, or payment credentials in public issues or pull requests.
- Report possible security issues privately to the repository maintainer through GitHub private vulnerability reporting **if the repository owner enables it**. Otherwise contact the maintainer outside public issue threads; no private security contact is currently configured.
- The app downloads build tools and source from external services (Microsoft, NuGet, GitHub, Docker/Podman dependencies). Verify the original publishers and use current supported versions.
- Recompilation logs are stored locally under `%LOCALAPPDATA%\ReNative3D36S\logs`. Review and redact them before sharing.
- Do not run untrusted game files or unknown scripts with administrator privileges. Windows may require elevation only for authorized system setup steps.

Security fixes should be validated before publishing a general release.
