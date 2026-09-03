# Power Diode

A RimWorld mod that adds a one-way power valve: a pair of buildings that let
power flow from one power network to another without merging them.

Build a **power diode intake** on the network you want to draw from, and a
**power diode outlet** directly adjacent to it, wired into whatever network
you want to feed - the outlet is just a normal power producer as far as that
network is concerned. The two buildings link automatically, and power only
ever flows from the intake's side to the outlet's side, never back the other
way. The outlet:

- has a wattage cap slider, and never feeds more than that cap,
- never feeds more than its network currently needs, including charging
  batteries,
- never feeds more than the intake's network currently has to spare, so it
  can't drag the source network into a brownout,
- has a battery reserve slider, letting you keep a configured amount of
  stored energy (in watt-days) untouched in batteries on the intake's
  network, so the diode won't drain a backup battery bank dry to feed
  another network.

Both buildings behave safely when unpaired or when their partner is
destroyed, dropping to 0 W with no errors. If some other wiring elsewhere on
the map ends up joining the intake's and outlet's networks into one, the
diode detects the shared grid and disables itself, with an on-map indicator.

## Requirements

- [Harmony](https://steamcommunity.com/workshop/filedetails/?id=2009463077)
- [ilyvion's Laboratory](https://github.com/ilyvion/ilyvion-laboratory/releases/latest)

## License

Licensed under either of

- Apache License, Version 2.0, ([LICENSE.Apache-2.0](LICENSE.Apache-2.0) or http://www.apache.org/licenses/LICENSE-2.0)
- MIT license ([LICENSE.MIT](LICENSE.MIT) or http://opensource.org/licenses/MIT)

at your option.

`SPDX-License-Identifier: Apache-2.0 OR MIT`

### Contribution

Unless you explicitly state otherwise, any contribution intentionally submitted
for inclusion in the work by you, as defined in the Apache-2.0 license, shall be
dual licensed as above, without any additional terms or conditions.
