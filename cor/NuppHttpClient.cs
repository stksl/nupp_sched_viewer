using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;

namespace NuppSchedViewer.Core;
/// <summary>
/// Http client extension for NUPP-related requests
/// </summary>
public sealed class NuppHttpClient : HttpClient
{
    public new Uri BaseAddress { get; private set; } = new Uri("https://portal.nupp.edu.ua/");

    private readonly IConfiguration config;
    private IBrowsingContext browsingContext;
    public NuppHttpClient(IConfiguration _config)
    {
        base.BaseAddress = BaseAddress;

        config = _config;
        browsingContext = BrowsingContext.New(config);
    }

    public async Task<string> GetCsrfLoginTokenAsync()
    {
        var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, "/login"));

        string content = await response.Content.ReadAsStringAsync();

        return content.Substring(Regex.Match(await response.Content.ReadAsStringAsync(), "<input type=\"hidden\" name=\".+\"><").Index + 50, 88);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="csrfToken"></param>
    /// <returns>Identity identifier token or null in case of invalid password</returns>
    public async Task<string?> LoginAsync(string csrfToken, NuppUserInfo userInfo)
    {
        MultipartFormDataContent formData = new MultipartFormDataContent();
        formData.Add(new StringContent(csrfToken), "_csrf-frontend");
        formData.Add(new StringContent(userInfo.Login), "LoginForm[username]");
        formData.Add(new StringContent(userInfo.Password), "LoginForm[password]");
        formData.Add(new StringContent("0"), "LoginForm[rememberMe]");
        formData.Add(new StringContent(""), "login-button");

        HttpRequestMessage postMessage = new HttpRequestMessage(HttpMethod.Post, "/login")
        {
            Content = formData
        };

        var response = await SendAsync(postMessage);

        if (response.StatusCode != System.Net.HttpStatusCode.Redirect)
            return null;

        const string identityCookieName = "_identity-frontend";
        string identityCookie = response.Headers.GetValues("Set-Cookie").First(c => c.StartsWith(identityCookieName));

        identityCookie = identityCookie.Substring(identityCookieName.Length, identityCookie.IndexOf(';') + 1);

        return identityCookie;
    }
    /// <summary>
    /// Returns a schedule for a specified date
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public async Task<IList<string>> GetScheduleAsync(DateTime date, string identityToken) 
    {
        HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, "/self/time-table");

        req.Headers.Add("Cookie", "_identity-frontend=" + identityToken);

        var response = await SendAsync(req);

        string htmlContent = await response.Content.ReadAsStringAsync();

        IDocument document = await browsingContext.OpenAsync(r => r.Content(htmlContent));
        
        List<string> classes = new List<string>();

        foreach(IElement element in document.QuerySelectorAll($"div[title^='{date.ToString("dd.MM.yyyy")}']")) 
        {
            string titleName = element.GetAttribute("title")!;
            classes.Add(titleName.Substring(titleName.Length - 6, 6) + "\n" + element.TextContent);
        }
        return classes;
    }

}