# WarcraftBattle.Unity

This is a minimal Unity project skeleton for the 3D rewrite.

Open this folder in Unity 2022.3 LTS or update `ProjectSettings/ProjectVersion.txt` to match your editor.

Notes:
- A bootstrap GameObject is created at runtime via `Assets/Scripts/Runtime/EntryPoint.cs`.
- A camera and directional light are created automatically if the scene has none.
- Put your XML config in `Assets/StreamingAssets` if you want to load it at runtime.
- Create a new 3D scene on first open and save it under `Assets/Scenes`.
