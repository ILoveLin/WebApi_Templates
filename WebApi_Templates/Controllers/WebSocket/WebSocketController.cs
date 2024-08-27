using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApi_Templates.Models.Filters;

namespace WebApi_Templates.Controllers;


[ApiController]
[NoNeedLogin]
[ApiVersionNeutral]
//不启用websocket
[NonController]
public class WebSocketController : ControllerBase
{
    private readonly ILogger<WebSocketController> logger;

    public WebSocketController(ILogger<WebSocketController> _logger)
    {
        this.logger = _logger;
    }


    [Route("/ws")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            if (App.WebSocketServerManager.OnMsgAsync == null)
            {
                App.WebSocketServerManager.OnMsgAsync = async (a, b) =>
                {
                    Console.WriteLine(b);
                    await App.WebSocketServerManager.SendAsync(a.HttpContext.TraceIdentifier, b);
                };
            }
            await App.WebSocketServerManager.AcceptWebSocketAsync(HttpContext, CancellationToken.None);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}