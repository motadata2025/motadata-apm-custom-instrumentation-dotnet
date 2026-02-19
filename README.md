# Motadata APM Custom Instrumentation for .NET

[![.NET](https://img.shields.io/badge/.NET-8%2B-blue.svg)](https://dotnet.microsoft.com/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.6.2%2B-blue.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux-lightgrey.svg)](https://dotnet.microsoft.com/)
[![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Compatible-brightgreen.svg)](https://opentelemetry.io/)
[![License](https://img.shields.io/badge/License-Proprietary-red.svg)](LICENSE)

A lightweight, enterprise-grade utility designed to simplify custom instrumentation for .NET applications using OpenTelemetry. Built as a seamless extension of Motadata Auto Instrumentation, it ensures custom attributes are validated, secure, and consistently namespaced.

---

## Table of Contents

- [Overview](#overview)
  - [Requirements](#requirements)
  - [Installation](#installation)
  - [Quick Start](#quick-start)
  - [API at a Glance](#api-at-a-glance)
  - [Behavior & Validation](#behavior--validation)
  - [Best Practices](#best-practices)
  - [Support](#support)
  - [License](#license)

---

## Overview

Motadata APM Custom Instrumentation helps you attach business context to traces without risking invalid attributes or inconsistent naming. Keys are automatically namespaced and normalized, inputs are validated, and the API is easy to adopt across .NET services.

> **Prerequisite:** Instrument your app first with **[Motadata Auto Instrumentation](https://docs.motadata.com/motadata-aiops-docs/apm/apm-in-motadata/)** so `Activity.Current` span context is available.

---

## Requirements

- .NET targets:
  - .NET `8+` (Windows/Linux)
  - .NET Framework `4.6.2+` (Windows)
- Motadata APM agent (auto-instrumented app)
- OpenTelemetry span context available in runtime
- Dependency: `System.Diagnostics.DiagnosticSource` `8.0.0+` (installed via NuGet)

---

## Installation

```bash
dotnet add package Motadata.Apm.CustomInstrumentation
```

Or from Package Manager Console:

```powershell
Install-Package Motadata.Apm.CustomInstrumentation
```

Or add it directly in your project file (`.csproj`):

```xml
<ItemGroup>
  <PackageReference Include="Motadata.Apm.CustomInstrumentation" Version="1.0.0" />
</ItemGroup>
```

---

## Quick Start

```csharp
using Motadata.Apm.CustomInstrumentation;

CustomInstrumentation.Set("apm.user.id", 12345L);
CustomInstrumentation.Set("apm.user.name", "john.doe");
CustomInstrumentation.Set("apm.request.success", true);
CustomInstrumentation.SetStringList("apm.tags", new[] { "api", "production", "critical" });
```

Keys are automatically prefixed with `apm.` when missing, but prefer providing the prefix yourself for consistency.
Since the package throws runtime exceptions for invalid input or missing span context, wrap calls in `try/catch` in production code:

```csharp
using Motadata.Apm.CustomInstrumentation;

try
{
    CustomInstrumentation.Set("apm.order.id", orderId);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to set apm.order.id");
}
```

---

## API at a Glance

### Scalar

| Method | Parameter |
|--------|-----------|
| `Set(string key, bool value)` | `bool` |
| `Set(string key, double value)` | `double` (finite) |
| `Set(string key, float value)` | `float` (finite, converted to `double`) |
| `Set(string key, int value)` | `int` (converted to `long`) |
| `Set(string key, long value)` | `long` |
| `Set(string key, string value)` | `string` (not null/empty/whitespace) |

### Collections

| Method | Parameter |
|--------|-----------|
| `SetBooleanList(string key, bool[] values)` | `bool[]` |
| `SetDoubleList(string key, double[] values)` | `double[]` (filters `NaN`/`Infinity`) |
| `SetFloatList(string key, float[] values)` | `float[]` (filters `NaN`/`Infinity`, converts to `double[]`) |
| `SetIntegerList(string key, int[] values)` | `int[]` (converted to `long[]`) |
| `SetLongList(string key, long[] values)` | `long[]` |
| `SetStringList(string key, string[] values)` | `string[]` (filters null elements) |

---

## Behavior & Validation

- Keys auto-prefix to `apm.` when absent and are lowercased.
- Only alphanumeric characters and dots are allowed in keys.
- Keys must not be null/empty/whitespace.
- `double` and `float` scalar values must be finite (`NaN`/`Infinity` not allowed).
- `string` scalar values must be non-null and not whitespace-only.
- List methods require non-null, non-empty arrays.
- `SetStringList` removes null elements and requires at least one remaining value.
- `SetDoubleList` and `SetFloatList` remove invalid numeric values and require at least one remaining value.
- Throws `Exception` for invalid input or when no active span is present (`Activity.Current == null`).

Key rules: not null/empty, only alphanumeric and dots, lowercased, prefixed `apm.`.  
Value rules: type-safe input, finite numeric constraints, list must stay valid after filtering.

---

## Best Practices

- Use descriptive, hierarchical keys already prefixed with `apm.` (for example: `apm.order.id`).
- Use numeric types for metrics/IDs and booleans for flags instead of converting everything to strings.
- Use list setters for collections and let filtering happen inside the library.
- Keep key naming consistent across services for easier querying and dashboards.
- Treat instrumentation as non-blocking telemetry: log exceptions and continue business logic.

---

## Support

- Email: engg@motadata.com
- Issues: GitHub Issues on this repository

---

## License

**Copyright (c) 2026 Motadata. All rights reserved.**

Proprietary software; see [LICENSE](LICENSE) for full terms.
