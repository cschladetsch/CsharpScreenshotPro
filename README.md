# ScreenshotPro - Advanced Screenshot Annotation Tool

ScreenshotPro is a professional screenshot annotation application built with .NET 8, and WinUI3. It provides advanced annotation capabilities with a focus on productivity and user experience.

## Features

### Core Functionality
- **Multi-Mode Screenshot Capture**
  - Full screen capture
  - Active window capture
  - Region selection with drag interface
  - Multi-monitor support
  - Delayed capture (3-10 seconds)
  - Scrolling capture (for web pages and documents)

### Advanced Annotation Engine
- **Drawing Tools**
  - Arrows with customizable styles
  - Shapes (rectangles, circles, lines)
  - Freehand drawing with pressure simulation
  - Text annotations with font customization
  - Callout bubbles with leader lines
  - Numbered bullets for step-by-step guides

- **Effects & Enhancements**
  - Highlight tool with adjustable transparency
  - Blur/pixelate for sensitive information
  - Layer system for complex annotations
  - Undo/redo with comprehensive history

### Professional Features
- **Templates System**
  - Built-in templates for bug reports, tutorials, feedback
  - Custom template creation and sharing
  - Batch application of consistent styling

- **Export Options**
  - Standard formats: PNG, JPG, GIF, PDF
  - Professional formats: PowerPoint slides, Word documents
  - Web-optimized outputs for social media

- **File Management**
  - Smart naming with customizable templates
  - Automatic organization by date/application
  - Version control with backup systems
  - Search functionality across screenshots

### User Interface
- **Modern Design**
  - Dark/light theme support
  - Responsive layout for different screen sizes
  - Floating or docked tool palettes
  - Context-sensitive properties panel

- **Productivity Features**
  - Customizable global hotkeys
  - Grid and ruler guides
  - Snap-to-grid functionality
  - Real-time annotation preview

## Technical Architecture

### Project Structure
```
ScreenshotPro.sln
├── ScreenshotPro.Core/          # Shared business logic
│   ├── Models/                  # Data models and entities
│   ├── Services/                # Core service implementations
│   ├── Interfaces/              # Service contracts
│   └── Utilities/               # Helper classes
├── Services/			 # Catch system keys and other required services
├── ScreenshotPro.UI/            # 
│   ├── Components/              # Reusable UI components
│   ├── Pages/                   # Application pages
└── ScreenshotPro.Tests/         # Unit and integration tests
```

### Key Technologies
- **.NET 8**: Latest .NET framework for performance and features
- **WinUI3**: Cross-platform application framework
- **SkiaSharp**: High-performance 2D graphics rendering
- **System.Drawing**: Image processing and manipulation
- **Newtonsoft.Json**: Settings and data serialization

### Core Services

#### ICaptureService
Handles all screenshot capture operations with support for:
- Multiple capture modes (region, window, full screen)
- Multi-monitor environments
- High DPI displays
- Hardware acceleration

#### IAnnotationService
Manages annotation creation and rendering:
- Vector-based annotations for scalability
- Real-time rendering with Canvas API
- Command pattern for undo/redo functionality
- Layer management for complex compositions

#### IFileService
Comprehensive file management:
- Multiple export formats with quality options
- Backup and version control systems
- Search and organization features
- Cloud sync preparation (future enhancement)

#### ISettingsService
User preferences and configuration:
- JSON-based settings storage
- Migration support for version updates
- Validation and error recovery
- Import/export functionality

## Getting Started

### Prerequisites

#### Required Software
- **.NET 8 SDK** (Required - see installation instructions below)
- **Visual Studio 2022** (17.8 or later) with .NET workload, or **Visual Studio Code** with C# extension
- **Windows 10/11** (primary target platform, version 1809 or later)

#### Installing .NET 8 SDK

**Option 1: Direct Download (Recommended)**
1. Go to https://dotnet.microsoft.com/download/dotnet/8.0
2. Download the SDK (not just Runtime) for your operating system
3. Run the installer

*Windows:*
```powershell
# Using winget
winget install Microsoft.DotNet.SDK.8

# Using Chocolatey
choco install dotnet-8.0-sdk
```

**Verify Installation:**
```bash
dotnet --version  # Should show 8.0.x
```

### Quick Start with Run Script

The easiest way to build and run ScreenshotPro is using the included run script:

### Manual Build Process

If you prefer to build manually or the run script doesn't work:

```bash
# Clone the repository
git clone https://github.com/cschladetsch/ScreenshotPro.git
cd ScreenshotPro

# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run --project ScreenshotPro.UI
```

### Building in Visual Studio
1. Open `ScreenshotPro.sln` in Visual Studio 2022
2. Ensure WinUI3 workload is installed
3. Set `ScreenshotPro.UI` as the startup project
4. Restore NuGet packages (right-click solution → Restore NuGet Packages)
5. Build the solution (`Ctrl+Shift+B`)
6. Run the application (`F5`)

### Troubleshooting Installation

## Usage

### Basic Screenshot Workflow
1. **Capture**: Use `Ctrl+Shift+S` to start region capture
2. **Annotate**: Select tools from the palette and add annotations
3. **Export**: Save or export in your preferred format

### Advanced Features
- **Templates**: Apply pre-built annotation templates for consistent styling
- **Layers**: Use multiple layers for complex annotations
- **Hotkeys**: Customize keyboard shortcuts for faster workflow
- **Batch Export**: Process multiple screenshots with consistent formatting

### Keyboard Shortcuts
- `Ctrl+Shift+S`: Capture region
- `Ctrl+Shift+F`: Capture full screen
- `Ctrl+Shift+W`: Capture active window
- `Ctrl+S`: Quick save
- `Ctrl+Z`: Undo
- `Ctrl+Y`: Redo
- `Delete`: Remove selected annotations

## Configuration

Settings are stored in `%AppData%/ScreenshotPro/settings.json` and include:

### Capture Settings
```json
{
  "Capture": {
    "DefaultMode": "SelectedRegion",
    "IncludeCursor": false,
    "DefaultDelay": 0,
    "AutoCopyToClipboard": true,
    "PlayCaptureSound": true
  }
}
```

### File Settings
```json
{
  "Files": {
    "DefaultSaveLocation": "Pictures/ScreenshotPro",
    "FileNameTemplate": "Screenshot_{timestamp:yyyy-MM-dd_HH-mm-ss}",
    "CreateDateFolders": true,
    "BackupOriginal": true,
    "MaxBackupVersions": 5
  }
}
```

## Performance Considerations

### Optimization Features
- **Hardware acceleration** for graphics rendering
- **Memory pooling** for frequent allocations
- **Lazy loading** of screenshots and thumbnails
- **Progressive rendering** for complex annotations
- **Background processing** using async/await patterns

### System Requirements
- **Minimum**: Windows 10 version 1809, 4GB RAM, DirectX 11 compatible graphics
- **Recommended**: Windows 11, 8GB RAM, dedicated graphics card
- **Storage**: 100MB application, additional space for screenshots

## Testing

The project includes comprehensive test coverage:

# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter Category=Integration
```

### Test Categories
- **Unit Tests**: Core business logic and services
- **Integration Tests**: File operations and capture functionality
- **UI Tests**: Component behavior and user interactions

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines
- Follow C# coding standards and conventions
- Write comprehensive unit tests for new features
- Update documentation for API changes
- Ensure cross-platform compatibility considerations

## Roadmap

### Phase 1 (v1.0) - MVP ✅
- [x] Basic screenshot capture
- [x] Core annotation tools
- [x] File operations
- [x] Settings management

### Phase 2 (v1.1) - Enhanced Features
- [ ] Template system implementation
- [ ] Advanced export options
- [ ] Cloud sync integration
- [ ] Performance optimizations

### Phase 3 (v2.0) - Professional Edition
- [ ] OCR text extraction
- [ ] Batch processing tools
- [ ] Plugin system
- [ ] Advanced analytics

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Documentation**: [docs/](docs/)
- **Issues**: GitHub Issues tracker
- **Community**: Discord server (coming soon)
- **Email**: support@screenshotpro.com

