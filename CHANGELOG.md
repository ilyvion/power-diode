# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- Power diode intakes and outlets, including the wall-mounted ones, can no longer be rotated while placing them. Rotating them did nothing except turn their texture sideways or upside down.

## [0.3.0] - 2026-10-09

### Added

- Wall-mounted power diode intakes and outlets, built into an existing wall to save space in compact setups. They work like the regular ones, take the place of a power conduit running under the wall, and can be paired with either variant. Each variant is in the same build menu dropdown as its regular counterpart.
- Power diode intakes and outlets, including the wall-mounted ones, can now be uninstalled and reinstalled elsewhere. A reinstalled outlet keeps its settings, and like a newly built one, it can only be installed directly next to an unpaired intake.
- While placing or installing a power diode intake or outlet, a line shows which building it will pair with. The line also shows when a placed blueprint or frame is selected.
- An unpaired power diode intake or outlet next to an unpaired partner now has a gizmo to link the two, since they don't link up on their own once both are already built. When there's more than one unpaired partner next to it, the gizmo lets you pick which one.
- A power diode set to a mode that can't work because a network has no batteries now shows a warning overlay and explains the problem in its inspect text. For an Overflow outlet whose intake network has no batteries, this shows on the intake. For a Top-up outlet whose own network has no batteries, it shows on the outlet.

### Fixed

- Power diode outlets now power consumers on their network even when that network has no battery or other power source. Previously such consumers stayed off for good, including after a brief power shortage on the intake side. A consumer that loses power because the intake side ran low only switches back on once the intake side can cover it again, so it doesn't keep flicking on and off.
- Devices on a power diode intake's network that are waiting for power, such as newly built ones or ones that lost power, now get to switch back on. Previously the diode could take all of the spare power for the outlet side, leaving them off. Spare power that covers such a device is left for it. While any device is waiting, the diode also stops drawing from the intake network's batteries once they're down to 5 Wd, the charge they need before anything can switch on. If a waiting device needs more than the spare power and the batteries are below 5 Wd, the diode feeds nothing until the spare power has charged them up to 5 Wd.
- A power diode outlet's inspect text now says it is receiving power from its intake, rather than wrongly saying it is feeding the intake.
- A power diode intake or outlet placed next to both an already-paired diode building and an unpaired one now pairs with the unpaired one. Previously it could stay unpaired and log an error. An intake placed next to only an already-paired outlet now simply stays unpaired, without logging an error.
- Loading a save no longer changes which power diode intakes and outlets are paired. Previously an unpaired intake and outlet next to each other could pair up on load, and a building could come back paired with a different neighbour than before.
- The game no longer logs a warning about Power Diode's gizmo icons not being loaded on the main thread when it starts.
- Setting the same minimum and maximum for the wattage cap or the battery reserve in the mod settings no longer breaks the outlets' setting. The matching slider is now hidden, since there is nothing to choose.
- The mod settings window now shows your saved minimum and maximum values after restarting the game. Previously it showed the defaults, even though your saved values were still the ones in effect.
- A power diode outlet no longer keeps feeding its network while its intake is switched off, such as during a power shortage on the intake side or right after the intake is built. Previously the outlet side got that power for free, without the intake network supplying it.
- Power conduit blueprints connect visually to the conduits next to them again. Previously, with Power Diode installed, there was a gap between a built conduit and a blueprint placed next to it, on every map.
- Changing the operating mode with several power diode outlets selected now changes it on all of them. Previously only one of the selected outlets switched to the chosen mode.
- Power diodes now treat batteries stunned by an EMP as empty, as the rest of the game does. Previously their charge still counted toward the battery reserve and the Overflow and Top-up thresholds while they were stunned.
- The power wire between a linked power diode intake and outlet now updates right away when a connection elsewhere joins their two networks or splits them apart again. Previously it could keep showing the old state, connected or cut, until something next to the diode changed.

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

[Unreleased]: https://github.com/ilyvion/power-diode/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/ilyvion/power-diode/compare/v0.2.1..v0.3.0
[0.2.1]: https://github.com/ilyvion/power-diode/compare/v0.2.0..v0.2.1
[0.2.0]: https://github.com/ilyvion/power-diode/compare/v0.1.0..v0.2.0
[0.1.0]: https://github.com/ilyvion/power-diode/releases/tag/v0.1.0
