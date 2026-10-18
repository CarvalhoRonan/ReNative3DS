# v0.2.0 Experimental: motor manager

- Added fully navigable Engines page and drop-down selector in main workflow.
- Runtime status, target platform and clear restrictions shown for each selected engine, translated into PT/EN/ES/RU.
- Official Pocket Native retains the existing Docker workflow and stable-release updates.
- Added explicit **disabled** ReNative Optimized placeholder: no optimizer has been written or tested yet.
- Added user-selected local Pocket Native-compatible fork adapter (read-only source, separate Docker scratch volume, `.3ds/.cci` only).
- Added user-selected Windows `3dsrecomp` command adapter (`build ROM`) for Zakuro, **not** a R36S output.
- About window: original author credits, links, source/license identification, non-affiliation notices.
- Expanded third-party notices, upstream Zakuro MIT license text, documentation and warnings against ROM redistribution.
- Windows WPF build and game builds not performed in this environment; integration must be tested.
