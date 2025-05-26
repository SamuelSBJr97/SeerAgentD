# SeerAgentD

SeerAgentD is a .NET-based process management system that allows you to monitor and control multiple applications from a single interface. It provides both a console-based interactive mode and a service mode for automated process management.

## Features

- Process monitoring and management
- Interactive console mode for manual control
- Service mode for automated process management
- Configurable process settings via JSON
- Logging support (console and file-based)

## Project Structure

```
src/
├── SeerAgentD.Console/         # Main process management application
├── SeerAgendD.Console.Example/ # Example application for testing
└── SeerAgentD.ProcessManager.sln
```

## Configuration

The application uses `apps-config.json` to define the processes to be managed. Example configuration:

```json
{
  "Apps": [
    {
      "Name": "App1",
      "ExecutablePath": "path/to/executable",
      "Arguments": "--mode=service",
      "WorkingDirectory": "path/to/working/directory"
    }
  ]
}
```

## Usage

### Console Mode
```bash
SeerAgentD.Console.exe --console
```

### Service Mode
```bash
SeerAgentD.Console.exe
```

## Requirements

- .NET 8.0 or higher
- Windows operating system

## Building

1. Open the solution in Visual Studio 2022
2. Build the solution in Release mode
3. The executables will be available in their respective `bin/Release/net8.0` directories

## License

See the [LICENSE.txt](LICENSE.txt) file for details.