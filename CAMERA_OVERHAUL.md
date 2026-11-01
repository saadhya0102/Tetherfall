# Tetherfall camera overhaul foundation

`Assets/Scripts/TetherfallOrbitCameraMath.cs` is the pure, allocation-free foundation for one third-person orbit pipeline. It owns only values and math. Input polling, physics queries, target lookup, clocks, and transform writes remain integration responsibilities.

`Assets/Editor/TetherfallOrbitCameraMathVerification.cs` verifies the foundation in edit mode without creating scene objects or issuing physics calls.

## Pipeline contract

Run the eventual camera adapter in this order:

1. Resolve one `TetherfallOrbitCameraProfile` for the current traversal mode.
2. Build sensitivity and inversion values in `TetherfallOrbitLookSettings`.
3. Evaluate cursor-relock suppression before consuming look.
4. Integrate mouse delta with `IntegrateMouseLook`. Do not multiply mouse delta by `deltaTime`.
5. Integrate stick input with `IntegrateStickLook`. Its degrees-per-second tuning is multiplied by `deltaTime` inside the helper.
6. Update focus engagement from raw look magnitude, then apply yaw-only focus assist.
7. Compute the target pivot with `GetTargetPivot` and damp it independently with `UpdateFollowPivot`.
8. Perform the existing non-alloc obstruction cast outside this foundation. Pass the desired boom-axis distance and resolved clear distance to `UpdateCollisionBoom`.
9. Build the unshaken pose with `BuildPose`.
10. Add presentation shake to the final position with `ApplyAdditiveShake`, then write the camera transform.

The clean orbit geometry is:

```csharp
var targetPivot = targetPosition + Vector3.up * profile.PivotHeight;
var orbitOffset = Quaternion.Euler(state.Pitch, state.Yaw, 0f) *
                  new Vector3(profile.ShoulderOffset, 0f, -boomDistance);
var cameraPosition = targetPivot + orbitOffset;
```

Do not put camera or pivot height into the rotated offset. Pitch may rotate the shoulder/boom vector, but it must not alter its length or move the target pivot.

`state.PlanarForward` and `state.PlanarRight` are derived directly from the newly integrated yaw. They do not wait for damped camera rotation, shake, or a later transform update.

## Traversal profiles

`GetProfile` provides complete profiles for:

- `Default`
- `Mounted`
- `Climb`
- `Swim`
- `Glide`
- `Mantle`

Each profile carries pivot height, boom distance, shoulder offset, default/minimum/maximum pitch, follow sharpness, collision compression/release tuning, minimum collision distance, skin, deadband, and focus-assist hysteresis.

## Collision and teleports

The math layer intentionally performs no physics calls. The camera adapter should retain its reusable `RaycastHit[]` and `Physics.SphereCastNonAlloc` filtering, then convert the closest valid hit into a clear boom-axis distance. When no obstruction exists, pass the desired distance as both the desired and obstructed values.

Collision entry and release use different thresholds. Compression is faster than release, and small obstruction changes inside the deadband do not switch collision state. The minimum distance and skin live in the active traversal profile.

After any discontinuous player move, call:

```csharp
state = TetherfallOrbitCameraMath.ResetAfterTeleport(
    state,
    TetherfallOrbitCameraMath.GetTargetPivot(playerPosition, profile),
    profile.BoomDistance,
    profile);
```

This snaps follow and boom history while retaining the player's yaw and pitch. It also clears stale focus and relock state.

## Focus, hitstop, relock, and shake

Focus assist has separate engage and break look thresholds. It changes yaw deterministically with a degrees-per-second limit. It never blends the camera pivot toward an enemy; the pivot remains player-owned.

`ResolveHitstopPolicy` separates look intent from follow hold. During combat hitstop, valid look can still integrate while the follow pivot is held. A broader pause or blocked input context can disable look independently.

On cursor acquisition, call `RegisterCursorRelock`, then evaluate `ShouldSuppressLook` with both `CursorLockAcquiredThisFrame` and unscaled time. The current-frame flag rejects the acquisition spike; the short window also covers delayed legacy-axis spikes.

Shake is final-position-only. `ApplyAdditiveShake` receives the unshaken position and rotation and returns a presentation position. It has no state parameter and therefore cannot change yaw, pitch, focus, pivot, or the movement/attack basis.

## Far clip

`DeriveRecommendedFarClipFromRegionPacks` uses the complete horizontal diagonal of `TetherfallRegionPacks` world bounds, a bounded minimum, and a margin. `DeriveRecommendedFarClip` also accepts explicit bounds so a future annex registry can supply expanded extents without changing camera math.

## Later integration hooks

The following work is intentionally deferred until the owning runtime files are available:

- Replace `ThirdPersonCamera.UpdateCameraPose` with the shared pipeline above.
- Expose the camera state or aim basis and use `PlanarForward`/`PlanarRight` in `PlayerAvatar` movement and attack aim instead of the visually damped or shaken camera transform.
- Set `useRawAxis = true` for the default `Mouse X` and `Mouse Y` bindings. Keep stick bindings distinguishable so mouse deltas and stick rates take different integration paths.
- Skip look on `TetherfallGame.CursorLockAcquiredThisFrame`, register the relock, and honor the short unscaled-time suppression window.
- Add persisted mouse/stick sensitivity and horizontal/vertical inversion settings, then construct `TetherfallOrbitLookSettings` from them.
- Set the gameplay camera `farClipPlane` from `DeriveRecommendedFarClipFromRegionPacks`, or from explicit expanded bounds when annexes become dynamic.
- Chain `TetherfallOrbitCameraMathVerification.VerifyForFullGame` from `TetherfallFullGameVerification`; this task deliberately does not edit the existing full-game verifier.

Run the focused verifier later through **Tetherfall > Verify Orbit Camera Math**, or in batch mode with:

```text
-executeMethod Tetherfall.Editor.TetherfallOrbitCameraMathVerification.VerifyFromCommandLine
```
