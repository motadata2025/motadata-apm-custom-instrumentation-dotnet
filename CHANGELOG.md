# Changelog

All notable changes to Motadata.Apm.CustomInstrumentation will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-02-17

### Added
- Initial release of Motadata.Apm.CustomInstrumentation
- Support for setting scalar attributes on OpenTelemetry spans
  - Boolean (`bool`)
  - Integer (`int`, `long`)
  - Floating-point (`float`, `double`)
  - String (`string`)
- Support for setting array attributes on OpenTelemetry spans
  - Boolean arrays (`bool[]`)
  - Integer arrays (`int[]`, `long[]`)
  - Floating-point arrays (`float[]`, `double[]`)
  - String arrays (`string[]`)
- Automatic key validation and normalization
  - Keys converted to lowercase
  - Automatic `apm.` prefix added to all keys
  - Validation for allowed characters (alphanumeric and dots only)
- Automatic value validation
  - Null, empty, and whitespace checks for string values
  - NaN and Infinity checks for floating-point values
  - Null filtering for array elements
  - Invalid value filtering for floating-point arrays
- Standardized error handling mechanism
  - Uses standard `System.Exception` for all error conditions
  - Clear, descriptive error messages for debugging
- Multi-framework support
  - .NET Framework 4.6.2 (Windows only)
  - .NET Framework 4.7 (Windows only)
  - .NET Framework 4.8 (Windows only)
  - .NET 8.0 (Windows & Linux)
  - .NET 9.0 (Windows & Linux)
- Cross-platform compatibility
  - Windows: .NET Framework 4.6.2+ and .NET 8+
  - Linux: .NET 8+
- Comprehensive documentation and IntelliSense support
- Thread-safe implementation using Activity.Current

### Technical Details
- Uses `System.Diagnostics.DiagnosticSource` for OpenTelemetry integration
- C# 7.3 language features for broad compatibility
- Compiled with deterministic builds for reproducibility
- Includes symbol packages (.snupkg) for debugging support
- Source Link integration for source-level debugging

### Notes
- Production release
- Package ID: `Motadata.Apm.CustomInstrumentation`
