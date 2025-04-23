namespace DoAnCoSo2.Repositories;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

public class CustomUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        // Trích xuất userId từ JWT token
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}



