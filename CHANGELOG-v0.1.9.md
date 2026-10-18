# ReNative3D36S v0.1.9 (test source)

- Rebuilt the WPF main window with an approved three-column dark-blue layout, left navigation, a clear brand header, and right engine panels.
- Omitted all stylized Nintendo 3DS and R36S console artwork from the banner.
- Created a **typographic R3D icon** in verified PNG and standard Windows ICO formats.
- Added functional navigation for Recompile, Updates, Library, and Settings; the existing About/Support window remains accessible.
- Added live output-structure preview, ROM selected state, engine version details, last checked time, real elapsed build time, log shortcuts, and library folder scanning.
- Added new translation keys in English, Brazilian Portuguese, Spanish, and Russian; no ungrounded ETA is displayed. Windows language is used by default.
- Preserved existing Pocket Native integration, stable-release check, independent profile update, Docker preparation, per-game packaging and CI script.
- Updated Portuguese and English guides with Docker Desktop startup, folder access permissions and official upstream links.

**Test status:** XAML parses as XML; resources and event handlers pass static checks. Neither WPF compilation nor real R36S execution could be performed in this Linux container. Build with `COMPILAR-UM-CLIQUE.cmd` on Windows x64 and share publication logs if errors arise.
