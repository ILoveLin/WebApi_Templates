using System.Buffers;
using System.Net.WebSockets;
using System.Text;

namespace WebApi_Templates.Models.XWebSocket;

public class WebSocketServerManager
{
    #region public variable

    public Dictionary<string, WebSocketClientContext> WebScoketClients = new();

    public Func<WebSocketClientContext, Task> OnConnectAsync = null;

    public Func<WebSocketClientContext, Task> OnDisConnectAsync = null;

    public Func<WebSocketClientContext, string, Task> OnMsgAsync = null;

    public Func<WebSocketClientContext, Exception, Task> OnErrorAsync = null;

    #endregion

    #region private method

    private async Task Handler(WebSocketClientContext webSocketClientContext, CancellationToken ctk)
    {
        var client = webSocketClientContext.client;
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        WebSocketReceiveResult receiveResult = null;
        StringBuilder messageBuilder = new StringBuilder();
        while (!client.CloseStatus.HasValue && client.State == WebSocketState.Open && !ctk.IsCancellationRequested)
        {
            try
            {
                receiveResult = await client.ReceiveAsync(buffer, ctk);
                if (receiveResult.MessageType != WebSocketMessageType.Close)
                {
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));
                    if (receiveResult.EndOfMessage)
                    {
                        OnMsgAsync?.Invoke(webSocketClientContext, messageBuilder.ToString());
                        messageBuilder.Clear();
                    }

                    Array.Clear(buffer, 0, receiveResult.Count);
                }
            }
            catch (Exception e)
            {
                OnErrorAsync?.Invoke(webSocketClientContext, e);
            }
        }

        ArrayPool<byte>.Shared.Return(buffer, true);
        if (client.State != WebSocketState.Aborted && client.State != WebSocketState.Closed)
        {
            await client.CloseAsync(receiveResult?.CloseStatus ?? WebSocketCloseStatus.NormalClosure, receiveResult.CloseStatusDescription, ctk);
        }
    }

    private async Task _SendAsync(WebSocketClientContext webSocketClientContext, string msg)
    {
        await webSocketClientContext?.client.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    #endregion

    #region public method

    public async Task SendAsync(string id, string msg)
    {
        if (WebScoketClients.TryGetValue(id, out var value))
        {
            await _SendAsync(value, msg);
        }
    }

    public async Task SendAllAsync(string msg)
    {
        var data = Encoding.UTF8.GetBytes(msg);
        foreach (var webScoketClient in WebScoketClients)
        {
            await webScoketClient.Value.client.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public async Task AcceptWebSocketAsync(HttpContext httpContext, CancellationToken ctk)
    {
        bool isConnect = false;
        try
        {
            var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
            if (webSocket.State == WebSocketState.Open)
            {
                var webSocketClientContext = new WebSocketClientContext { HttpContext = httpContext, client = webSocket };
                WebScoketClients.TryAdd(httpContext.TraceIdentifier, webSocketClientContext);
                isConnect = true; 
                OnConnectAsync?.Invoke(webSocketClientContext);
                await Handler(webSocketClientContext, ctk);
                OnDisConnectAsync?.Invoke(webSocketClientContext);
                WebScoketClients.Remove(httpContext.TraceIdentifier);
            }
        }
        catch (Exception e)
        {
            OnErrorAsync?.Invoke(isConnect ? WebScoketClients[httpContext.TraceIdentifier] : null, e);
            if (isConnect)
            {
                OnDisConnectAsync?.Invoke(WebScoketClients[httpContext.TraceIdentifier]);
            }
            WebScoketClients.Remove(httpContext.TraceIdentifier);
        }
    }

    #endregion
}