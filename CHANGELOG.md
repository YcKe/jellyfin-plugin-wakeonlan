# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow the
four-part scheme Jellyfin plugins use.

## [Unreleased]

## [1.0.0.2] - 2026-09-16

### Changed

- Plugin is now named "Wake-on-LAN (WOL)". The release archive is accordingly named `wake-on-lan-wol_<version>.zip`.

### Fixed

- Release workflow now updates `manifest.json` correctly after publishing.

## [1.0.0.1] - 2026-09-16

### Changed

- New plugin GUID. Remove any previously installed copy before installing this version.

## [1.0.0.0] - 2026-09-09

### Added

- Wake the configured storage server when a client session starts or playback begins.
- Optional TCP probe so no magic packet is sent when the server is already awake.
- Cooldown between wake attempts.
- Settings page with a server check and a test packet button.
- Administrator-only endpoints `POST /Wol/Test` and `GET /Wol/Status`.

[Unreleased]: https://github.com/YcKe/jellyfin-plugin-wakeonlan/compare/v1.0.0.2...HEAD
[1.0.0.2]: https://github.com/YcKe/jellyfin-plugin-wakeonlan/compare/v1.0.0.1...v1.0.0.2
[1.0.0.1]: https://github.com/YcKe/jellyfin-plugin-wakeonlan/compare/v1.0.0.0...v1.0.0.1
[1.0.0.0]: https://github.com/YcKe/jellyfin-plugin-wakeonlan/releases/tag/v1.0.0.0
