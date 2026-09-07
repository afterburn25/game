# Validation

The development branch is validated through GitHub Actions before merge to `main`.

Current validation gates:

1. `dotnet restore Game.csproj`
2. `dotnet build Game.csproj --configuration Release`
3. download the pinned official Godot 4.7.2 .NET Linux editor asset
4. verify the Godot archive SHA-256
5. run a headless Godot editor/project-load smoke test
6. run the main scene headlessly for a short runtime smoke test

A green C# compile is necessary but not sufficient: scene loading and project startup are checked separately because runtime integration errors can survive ordinary compilation.
