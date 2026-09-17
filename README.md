# DuckovBetterActionCancel

Harmony mod for **Escape from Duckov**. Any action input interrupts the action that is currently casting, so the vanilla **Stop Action** key is no longer needed.

- Applies to the player character only; AI is untouched.
- Covers fire, reload, dodge, interact, skill, weapon swap and item use.
- Cancellation goes through the game's own `StopAction()` path, so loot search, item return and reload state behave like vanilla.
- The progress bar and its cancel hint are unchanged.

Build with `dotnet build src/DuckovBetterActionCancel.csproj -c Debug` (point `DuckovPath` in the csproj at your game folder first), then copy the build output into `<game>\Duckov_Data\Mods\DuckovBetterActionCancel\`.
