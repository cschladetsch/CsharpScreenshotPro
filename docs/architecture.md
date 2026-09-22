---
layout: default
title: Architecture
---

# ScreenshotPro Architecture

## Solution layout

```mermaid
graph LR
    Sol[ScreenshotPro.sln]
    Sol --> UI[ScreenshotPro.UI<br/>WinUI3]
    Sol --> Core[ScreenshotPro.Core]
    Sol --> Tests[ScreenshotPro.Tests]
    UI --> Core
    Tests --> Core
    Core --> Services[Services<br/>capture / annotate / export]
```
