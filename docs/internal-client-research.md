# Internal-client research addendum

Date: 2026-08-26

## Requested direction

Vexel is being redirected from an external patch utility toward an internal
Minecraft: Bedrock client with an in-game interface. The requested target
versions are 1.16.0, 1.16.100, 1.16.201, 1.18.12, and the 1.20–1.26.40 range.

These are product targets, not current compatibility claims. No build in this
set is marked supported until its exact executable fingerprint and in-game
behaviour have been verified.

## Flarial Launcher findings

The reviewed source was `flarialmc/Flarial.Launcher` commit
`86504e4`. This public repository is a launcher/runtime library; it does not
contain Flarial's in-game client source, feature implementations, or render
hooks.

Its launcher code has these relevant concepts:

- a managed desktop launcher and a separate runtime assembly;
- Minecraft package/process lookup and launch lifecycle management;
- build/version registry retrieval;
- a distinct native client library loaded only after game launch;
- dialogs for unsupported versions and failed load operations.

The launcher uses a conventional remote-library loading sequence. Vexel will
not copy that code or permit arbitrary DLL paths. An internal runtime must be a
Vexel-owned, signed component, bound to the selected process instance and an
explicitly verified executable fingerprint.

The visual layout, assets, fonts, controls, and branding of Flarial are not
reused. Vexel can take only high-level product cues: clear launch state,
version selection, compact navigation, explicit error handling, and an
in-game-first module model.

## Vexel internal-client architecture

```text
Vexel.exe (WPF control center)
  ├── process/build detector
  ├── signed compatibility manifest resolver
  ├── launch and attach coordinator
  └── local IPC endpoint

Vexel.Runtime.dll (native in-game component)
  ├── verified bootstrap entry point
  ├── renderer/input integration for the in-game UI
  ├── feature modules with independent lifecycle
  ├── IPC client
  └── unload/cleanup handler

Vexel compatibility database
  ├── executable SHA-256 and PE metadata
  ├── per-build bootstrap and feature definitions
  ├── instruction-context expectations
  └── test/verification records
```

The WPF process is not the in-game client. It controls discovery, diagnostics,
preferences, verified component deployment, and attach lifecycle. The runtime
is responsible for UI and behaviour inside Minecraft.

## Safety gates for an internal runtime

1. The selected PID must still have the recorded start time and executable
   fingerprint immediately before attach.
2. The runtime payload must be Vexel-owned and integrity-checked. Users cannot
   provide an arbitrary DLL path.
3. The compatibility resolver must return a build-specific, verified bootstrap
   record. Unknown builds cannot attach.
4. A module may only become active after its own pattern/instruction validation
   succeeds. Runtime UI must show the actual result, not a preference.
5. Every memory write is recorded in a process-scoped session and is restored
   on disable/unload when its bytes still match the recorded replacement.
6. Runtime operations must not attempt to bypass server-side movement,
   authentication, or anti-cheat validation.

## Compatibility reality

The requested versions span substantially different Bedrock executable
families. A version range cannot safely share one hook, offset, or pattern.
The initial compatibility ledger therefore contains no claimed support:

| Requested family | Current status | Requirement before support |
| --- | --- | --- |
| 1.16.0 | Unverified | Original executable, fingerprint, reverse engineering, offline test |
| 1.16.100 | Unverified | Original executable, fingerprint, reverse engineering, offline test |
| 1.16.201 | Unverified | Original executable, fingerprint, reverse engineering, offline test |
| 1.18.12 | Unverified | Original executable, fingerprint, reverse engineering, offline test |
| 1.20–1.26.40 | Unverified per build | Each exact executable fingerprint and in-game verification |

The current machine has no Minecraft package or running Minecraft process, so
there is no target executable available for the first bootstrap or feature
analysis.

## Delivery order

1. Add an internal-runtime solution boundary, signed payload validation, safe
   process-instance binding, and local IPC contract.
2. Obtain and fingerprint one authorised build, starting with a running GDK
   26.x installation.
3. Implement and test one harmless in-game runtime bootstrap path on that exact
   build.
4. Reverse-engineer and verify a single feature such as No Hurt Cam offline.
5. Add exact build records and modules one by one; only then expand to legacy
   version families.
6. Build the in-game UI after the bootstrap and module lifecycle are proven.

## UI direction

Vexel will retain its own graphite/dark-navy and cyan identity. The in-game UI
will be compact, keyboard accessible, minimal, and use a consistent 4/8-point
spacing scale. It will show module state in text as well as colour and will not
imitate Flarial artwork, controls, fonts, or layout one-for-one.
