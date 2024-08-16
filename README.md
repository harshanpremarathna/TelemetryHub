
# OpenTelemetry Integration in .NET 7 Web API

## Introduction

This project demonstrates how to integrate OpenTelemetry in a .NET 7 Web API for observability and telemetry. The provided code includes controllers and setup to track custom metrics, activities, and logs using OpenTelemetry. The goal is to capture relevant telemetry data that can be used for monitoring and troubleshooting in production environments.

## Installation and Setup

Follow these steps to set up the environment and run the project:

1. **Clone the Repository:**

   Clone the repository that contains this project:

   ```bash
   git clone <repository-url>
   cd <repository-directory>
   ```

2. **Install NuGet Packages:**

   Install the required NuGet packages for OpenTelemetry and Serilog:

   ```bash
   dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore
   dotnet add package Serilog.AspNetCore
   dotnet add package Serilog.Sinks.Console
   dotnet add package Serilog.Sinks.OpenTelemetry
   dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
   ```

3. **Use Secret Manager for Secure Configuration:**

   Avoid committing sensitive data like connection strings in `appsettings.json`. Instead, use `secrets.json` for local development:

   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "AzureMonitor:ConnectionString" "your-connection-string"
   ```

   Ensure the secrets are loaded during development in `Program.cs`:

   ```csharp
   if (builder.Environment.IsDevelopment())
   {
       builder.Configuration.AddUserSecrets<Program>();
   }
   ```

4. **Run the API Project:**

   Navigate to the Api directory and execute the following command to run the project:

   ```bash
   dotnet run
   ```

   The API will be accessible at `http://localhost:<port>/`.

## Implementation

### ObservableController.cs

The `ObservableController.cs` file contains the following key implementations:

- **Metrics:** 
  - `Counter<int> _loginCounter`: Tracks the number of user logins.
  - `Counter<int> _taskCompletionCounter`: Tracks the number of tasks completed.

- **Activities:**
  - `_dbActivitySource`: Captures database-related operations as activities.
  - `_apiActivitySource`: Captures API-related operations as activities.

- **Logging:**
  - `ILogger<ObservableController>` is used for structured logging of user requests and operations.

### Program.cs

The `Program.cs` file sets up the necessary services and dependencies:

- **OpenTelemetry Setup:**
  - Meter and ActivitySource services are added to track custom metrics and activities.
  - Logging is configured using Serilog with OpenTelemetry integration.

- **HTTP Client Setup:**
  - `IHttpClientFactory` is registered to handle outbound HTTP requests with tracing.

### OpenTelemetry Integration

The project integrates OpenTelemetry to collect and export telemetry data. Here's what it covers:

- **Custom Metrics:** 
  - Tracks metrics like login counts and task completions.
  
- **Distributed Tracing:**
  - Captures activities across different services, including database operations and external API calls.

- **Logging:**
  - Structured logging is implemented to provide insights into API operations.

## Testing and Verification

### Testing Locally with Aspire Dashboard

To view telemetry data locally, run the Aspire Dashboard using Docker:

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS='true' -d --name aspire-dashboard mcr.microsoft.com/dotnet/nightly/aspire-dashboard:latest
```

### Using Azure Monitor

If deployed in production, telemetry data will be exported to Azure Monitor for detailed analysis and monitoring.

## Conclusion

This project provides a robust starting point for integrating OpenTelemetry in .NET 7 Web API projects. With proper telemetry in place, you can gain valuable insights into your application's performance and behavior.
