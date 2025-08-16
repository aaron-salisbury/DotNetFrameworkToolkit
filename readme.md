<p align="left">
  <img src="https://raw.githubusercontent.com/aaron-salisbury/DotNetFrameworkToolkit/refs/heads/master/content/logo.png" width="175" alt=".Net Framework Toolkit Logo">
</p>

# .Net Framework Toolkit
Common C# app development and shim code for legacy .Net Framework 2.0 projects.

## Purpose
I occasionally find myself in restricted development scenarios where I target legacy Windows platforms limited to .Net Framework. So I created this library to aid in maintenance and rapid development under those constraints.

## Versioning
This project uses [Semantic Versioning](https://semver.org/).

- **MAJOR** version: Incompatible API changes
- **MINOR** version: Backward-compatible functionality
- **PATCH** version: Backward-compatible bug fixes

## Build Requirements
- The project targets .Net Framework 2.0 but is configured to use the latest language features as of the LTS version of the [.NET SDK](https://dotnet.microsoft.com/en-us/download).
- The Build project uses [Cake](https://cakebuild.net/) (C# Make) as the build orchestrator and can be launched from your IDE or via script.

	- On OSX/Linux run:
	```bash
	./build.sh
	```
	- If you get a "Permission denied" error, you may need to make the script executable first:
	```bash
	chmod +x build.sh
	```

	- On Windows PowerShell run:
	```powershell
	./build.ps1
	```
