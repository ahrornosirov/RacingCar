# Garage implementation

Approved scope: add ten cars one at a time, each with a usable cockpit, third-person view and individual handling. Retain the existing concept car as an additional vehicle.

## Current milestone

- Garage Home button; browse, confirm and cancel selection; cockpit and exterior previews.
- Saved selection by stable vehicle ID. Changing a vehicle is restricted to the Ready phase.
- Per-vehicle ScriptableObject: prefab, mass, torque/speed curve, front/rear/all-wheel drive, forward/reverse speed limits, brakes/bias, grip, suspension, steering response, center of mass, downforce and camera offsets.
- Camera, dashboard, engine audio and race references rebind together when changing cars.
- Per-car records. Existing concept-car record keys remain compatible.
- Concept Roadster, Toyota Corolla E180, Ford Mustang ROUSH 2019 and Porsche 911 Turbo S registered. The downloaded cars have fitted cockpits, separately animated wheels/steering, adapted URP materials and individual handling. Porsche uses 1K textured materials and AWD.

The Garage initially previews vehicles on the existing circuit. A separate garage building, paint customization and AI opponents are later work.

## Measured handling

On a flat test surface (game tuning, not manufacturer data):

| Vehicle | 0–100 km/h | Speed after 35s | Braking from that speed | Reverse |
|---|---:|---:|---:|---:|
| Concept Roadster | 12.42s | 136.9 km/h | 5.56s / 99.6m | 25.6 km/h |
| Toyota Corolla | 21.82s | 114.3 km/h | 4.68s / 71.8m | 22.2 km/h |
| Ford Mustang ROUSH | 11.38s | 223.2 km/h | 8.00s / 231.9m | 28.2 km/h |
| Porsche 911 Turbo S | 4.74s | 299.8 km/h | 8.04s / 296.9m | 30.6 km/h |

The Garage displays the configured speed limit, not a measured maximum speed. Torque, drag and gearing may prevent a car from reaching its limit.

## Integration order

1. Toyota Corolla E180 — integrated; Armored Wave, CC BY 4.0.
2. Ford Mustang ROUSH 2019 — integrated; _the.render.box_, CC BY 4.0.
3. Porsche 911 Turbo S — integrated; n.brizitskaya, CC BY 4.0.
4. BMW 3 Series E90.
5. Toyota GR Supra.
6. Mercedes-Benz C-Class S204.
7. Volkswagen I.D.R.
8. Lamborghini Centenario.
9. Lamborghini Urus.
10. Lamborghini Sesto Elemento.

Model availability and source attribution must be checked when each file is downloaded. A replacement for an unsuitable model will be reviewed before inclusion.

## Verification

`RacingGaragePlayChecks.Run` verifies UI actions, confirm/cancel, persistence, reference rebinding, per-car records, race-time switching lock, front-axle torque and reverse limiting. The same checks now exercise every shipped car through selection, drive-axle torque, camera switching and race entry. Its temporary second profile reuses the concept mesh only inside the test; it is not a shipped additional car.

`RacingVehicleChecks.Run` measures acceleration, straight-line speed, stopping distance and reverse speed on an isolated flat surface using Unity physics. Measurements are gameplay behavior, not real vehicle specifications. Existing menu/race regression checks remain applicable.

Final on-device testing on Quest 3/3S is required for comfort, controller navigation, seat scale, visual quality and sustained frame rate. An updated development APK is built after the vehicle checks; device validation is still pending.
