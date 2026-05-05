namespace MESManager.Web.Security;

/// <summary>
/// Delegating handler che trasferisce il cookie di autenticazione dalla richiesta
/// Blazor Server alle chiamate HttpClient interne (localhost).
/// 
/// In Blazor Server, il circuito SignalR non trasporta automaticamente i cookie
/// quando il codice server-side usa HttpClient per chiamare i propri controller API.
/// Senza questo handler, tutte le chiamate server-side riceverebbero 401 anche se
/// l'utente è autenticato nel browser.
/// 
/// Sicurezza: i cookie vengono inoltrati SOLO a richieste verso localhost/127.0.0.1.
/// </summary>
public class CookieForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Inoltra i cookie SOLO per chiamate interne a localhost
        var host = request.RequestUri?.Host;
        if (host == "localhost" || host == "127.0.0.1")
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var cookieHeader = httpContext.Request.Headers["Cookie"].ToString();
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    // TryAddWithoutValidation evita eccezioni se l'header è già presente
                    request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
                }
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
