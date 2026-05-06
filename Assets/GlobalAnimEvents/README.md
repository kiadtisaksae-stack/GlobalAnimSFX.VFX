# Global Anim Events

Data-driven animation event system for Unity with:

- `AnimEventTrackAsset` as source-of-truth
- Fluent builder API (`AnimEventMessage.For(...).With...Build()`)
- Runtime dispatcher + handlers
- Global editor window for authoring + clip event management
- Migration utility from legacy `Anim_SpawnVfx` / `Anim_PlaySfx` clips

## Runtime entrypoint

Animation clips should call:

- `AnimEvent_Emit`

with `intParameter = markerId`.

The runtime bridge resolves marker data from the current clip's `AnimEventTrackAsset`.
