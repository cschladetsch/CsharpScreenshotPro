# Repository Guidelines

## Project Structure & Module Organization
ScreenshotPro.sln orchestrates four projects. ScreenshotPro.Core/ holds business logic split into Models/, Services/, Interfaces/, and Utilities/. Services/ packages low-level input hooks consumed across the solution. ScreenshotPro.UI/ is the WinUI 3 shell; XAML views live under Views/ and static assets under Assets/. Tests live in ScreenshotPro.Tests/, while esources/ stores product mockups and UX references. Runtime settings are persisted to %AppData%/ScreenshotPro/settings.json.

## Build, Test, and Development Commands
Run dotnet restore once per machine or after dependency changes. dotnet build ScreenshotPro.sln produces binaries for all targets. Use dotnet run --project ScreenshotPro.UI to launch the WinUI app in debug mode. Execute dotnet test to run the xUnit suite; append --collect:"XPlat Code Coverage" when auditing coverage. Visual Studio users can build via Ctrl+Shift+B with ScreenshotPro.UI as the startup project.

## Coding Style & Naming Conventions
We follow .NET defaults: four-space indentation, nullable reference types, and implicit usings. Use PascalCase for public types and methods, camelCase for locals, and suffix async methods with Async. Interfaces retain the I prefix defined in Interfaces/. Prefer expression-bodied members for simple accessors. Before committing, run dotnet format (install with dotnet tool install --global dotnet-format if needed) to align analyzers across projects.

## Testing Guidelines
Tests target xUnit (Fact attributes) inside ScreenshotPro.Tests. Mirror the namespace of the code under test and name files <Type>Tests.cs. Keep assertions focused; use data-driven [Theory] tests for combinatorial cases. Integration scenarios should be tagged with Category=Integration so dotnet test --filter Category=Integration remains meaningful. Aim to expand coverage around core models, capture services, and WinUI bindings.

## Commit & Pull Request Guidelines
Commits should use a short imperative line, mirroring history such as Add mockups. Group related changes and include key implementation notes in the body when needed (<72 characters per line). Open PRs with a summary, test evidence (dotnet test output), and any UI screenshots relevant to ScreenshotPro.UI. Link GitHub issues or roadmap items and note configuration impacts (e.g., settings schema changes).
