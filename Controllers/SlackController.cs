using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using SlackIntegration.Models;

namespace SlackIntegration.Controllers;

public class SlackController : Controller
{
   private readonly IConfiguration _config;
    public SlackController( IConfiguration config)
    {
        _config = config;
    }

    public IActionResult Login()
    {
        var clientId = _config["Slack:ClientId"];
        var redirectUri = Url.Action("Callback", "Slack", null, Request.Scheme);
        var slackUrl = $"https://slack.com/oauth/v2/authorize?client_id={clientId}&scope=chat:write&user_scope=chat:write&redirect_uri={redirectUri}";
        return Redirect(slackUrl);
    }

    public async Task<IActionResult> Callback(string code)
    {
        var clientId = _config["Slack:ClientId"];
        var clientSecret = _config["Slack:ClientSecret"];
        var redirectUri = Url.Action("Callback", "Slack", null, Request.Scheme);

        using var client = new HttpClient();
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
        });

        var response = await client.PostAsync("https://slack.com/api/oauth.v2.access", content);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var accessToken = json.RootElement.GetProperty("authed_user").GetProperty("access_token").GetString();
        HttpContext.Session.SetString("SlackAccessToken", accessToken);

        return RedirectToAction("Send");
    }

    public IActionResult Send() => View();

    [HttpPost]
    public async Task<IActionResult> Send(string channel, string message)
    {
        var token = HttpContext.Session.GetString("SlackAccessToken");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login");

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { channel, text = message };
        var json = JsonSerializer.Serialize(payload);

        var res = await client.PostAsync("https://slack.com/api/chat.postMessage",
            new StringContent(json, Encoding.UTF8, "application/json"));

        var result = await res.Content.ReadAsStringAsync();
        ViewBag.Response = result;
        return View();
    }

}
