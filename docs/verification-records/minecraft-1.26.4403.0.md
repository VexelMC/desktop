# Minecraft build detection record — 1.26.4403.0

Verified date: 2026-08-27

## Identity

| Field | Value |
| --- | --- |
| Distribution | Microsoft Store / GDK package |
| Package family | `Microsoft.MinecraftUWP_8wekyb3d8bbwe` |
| File version | `1.26.4403.0` |
| Architecture | x64 |
| Loaded module size | `310935552` bytes |
| Loaded-image SHA-256 | `3F8F6A78416E4CF9A9DA7D1CCF5D345379FCF8C315EBAD5880F37B5FFDE919D2` |
| Fingerprint source | Read-only process-module image |

The Store-protected file cannot be opened directly by the standard user token,
so this is a loaded-image fingerprint rather than an on-disk file hash. The
process identity and loaded module were read without writing to Minecraft.

## Patch verification

| Patch | Status | Reason |
| --- | --- | --- |
| Item Delay Fix | Unverified | No build-specific reverse engineering yet |
| No Camera Reset | Unverified | No build-specific reverse engineering yet |
| AutoSprint | Unverified | No approved implementation strategy yet |
| No Hurt Cam | Unverified | No build-specific reverse engineering yet |
| GUI Scale | Unverified | No build-specific reverse engineering yet |

This document records detection only. It does not authorize an internal runtime
attach or any memory patch for this build.
