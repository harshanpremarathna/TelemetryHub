using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ObservableController : ControllerBase
{
    private readonly Counter<int> _loginCounter;
    private readonly Counter<int> _taskCompletionCounter;

    private readonly ActivitySource _dbActivitySource;
    private readonly ActivitySource _apiActivitySource;
    private readonly HttpClient _httpClient;

    ILogger<ObservableController> logger;

    public ObservableController(
        Meter Meter,
        ActivitySource dbActivitySource,
        ActivitySource apiActivitySource,
        IHttpClientFactory httpClientFactory,
        ILogger<ObservableController> logger)
    {
        _loginCounter = Meter.CreateCounter<int>("user.logins.count");
        _taskCompletionCounter = Meter.CreateCounter<int>("tasks.completed.count");

        this.logger = logger;


        _dbActivitySource = dbActivitySource;
        _apiActivitySource = apiActivitySource;
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpPost("login")]
    public IActionResult Login(UserLoginDto userLoginDto)
    {
        logger.LogInformation("Hit the login {@request}", userLoginDto);

        // Increment the login counter
        _loginCounter.Add(1);

        return Ok("User logged in successfully");
    }

    [HttpGet("process-multi-activity")]
    public async Task<IActionResult> ProcessMultiActivity()
    {
        // Simulate task processing
        using (var activity = _dbActivitySource.StartActivity("DatabaseOperation", ActivityKind.Internal))
        {
            // Simulate some database operation
            await Task.Delay(1000);
            activity?.SetTag("db.system", "SQL");
            activity?.SetTag("db.operation", "SELECT");
        }

        using (var activity = _apiActivitySource.StartActivity("ApiCall", ActivityKind.Client))
        {
            // Simulate an external API call
            var response = await _httpClient.GetAsync("https://jsonplaceholder.typicode.com/posts/1");
            var content = await response.Content.ReadAsStringAsync();
            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetTag("http.url", "https://jsonplaceholder.typicode.com/posts/1");
        }

        // Increment the task completion counter
        _taskCompletionCounter.Add(1);

        return Ok("Multi task processed successfully");
    }

    [HttpGet("process-single-activity")]
    public async Task<IActionResult> ProcessSingleActivity()
    {
        // Start a trace activity
        using var activity = _apiActivitySource.StartActivity("ApiCallV2", ActivityKind.Client);

        // Tag the activity with custom metadata
        activity?.SetTag("customTag", "example");

        var businessResult = await SomeBusinessLogic();

        var httpResponse = await MakeHttpCallAsync();

        // Add more information to the trace
        activity?.SetTag("BusinessLogicResult", businessResult);
        activity?.SetTag("HttpResponseCode", httpResponse.StatusCode.ToString());

        return Ok("Single task processed successfully");
    }

    private async Task<string> SomeBusinessLogic()
    {
        using var activity = _apiActivitySource.StartActivity("BusinessLogic", ActivityKind.Internal);
        
        await Task.Delay(100);

        return "BusinessResult";
    }

    private async Task<HttpResponseMessage> MakeHttpCallAsync()
    {
        using var activity = _apiActivitySource.StartActivity("ExternalHttpCall", ActivityKind.Client);

        var response = await _httpClient.GetAsync("https://example.com");

        activity?.SetTag("HttpCallResponseTime", response.Headers.Date?.ToString());

        return response;
    }

}

public class UserLoginDto
{
    public string UserName { get; set; }
    public string Password { get; set; }
}