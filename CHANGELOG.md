# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Mod settings to configure the minimum and maximum wattage cap and the minimum and maximum battery reserve (Wd) that outlets can be set to, instead of these limits being fixed.

### Fixed

- The wattage cap and battery reserve sliders now show their tooltips when hovered.
- Dragging one selected diode outlet's wattage cap or battery reserve slider no longer visually disturbs the same slider on other selected outlets.

## [0.1.0] - 2026-09-03

### Added

- The power diode: a pair of buildings, a power diode intake and a power diode outlet, that let power flow one-way between two otherwise-separate power networks. Build an intake on the network you want to draw from and an outlet directly next to it on the network you want to feed; the outlet has a wattage cap slider and only ever feeds as much as its network currently needs (including charging batteries), and never more than the intake's network has to spare.

[Unreleased]: https://github.com/ilyvion/power-diode/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/ilyvion/power-diode/releases/tag/v0.1.0
