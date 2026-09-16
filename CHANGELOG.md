# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.1] - 2026-09-16

### Changed

- Updated README.md, SteamDescription.txt, and About.xml to document the operating modes and mod settings added in 0.2.0.

## [0.2.0] - 2026-09-16

### Added

- Mod settings to configure the minimum and maximum wattage cap and the minimum and maximum battery reserve (Wd) that outlets can be set to, instead of these limits being fixed.
- Two new outlet operating modes, alongside the existing one-way valve behavior: Overflow, which only feeds power once the intake network's batteries are charged above a set percentage, and Top-up, which only feeds power while the outlet network's batteries are below a set percentage. Switch modes from a new gizmo on the outlet.
- A mod setting to set the one-way valve battery reserve as a percentage of the intake network's total battery capacity instead of an absolute amount of watt-days.

### Fixed

- The wattage cap and battery reserve sliders now show their tooltips when hovered.
- Dragging one selected diode outlet's wattage cap or battery reserve slider no longer visually disturbs the same slider on other selected outlets.
- Changing the wattage cap or battery reserve (Wd) limits in mod settings can no longer silently change an outlet's already-configured wattage cap or battery reserve if it happened to have the default value set.

## [0.1.0] - 2026-09-03

### Added

- The power diode: a pair of buildings, a power diode intake and a power diode outlet, that let power flow one-way between two otherwise-separate power networks. Build an intake on the network you want to draw from and an outlet directly next to it on the network you want to feed; the outlet has a wattage cap slider and only ever feeds as much as its network currently needs (including charging batteries), and never more than the intake's network has to spare.

[Unreleased]: https://github.com/ilyvion/power-diode/compare/v0.2.1...HEAD
[0.2.1]: https://github.com/ilyvion/power-diode/compare/v0.2.0..v0.2.1
[0.2.0]: https://github.com/ilyvion/power-diode/compare/v0.1.0..v0.2.0
[0.1.0]: https://github.com/ilyvion/power-diode/releases/tag/v0.1.0
