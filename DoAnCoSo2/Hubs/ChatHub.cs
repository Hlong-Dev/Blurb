namespace DoAnCoSo2.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class ChatHub : Hub
{
    // Method for joining a specific room
    public async Task JoinRoom(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        await Clients.Group(roomName).SendAsync("ReceiveMessage", "System", $"{Context.User.Identity.Name} has joined the room.", "");
    }

    // Method for leaving a specific room
    public async Task LeaveRoom(string roomName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        await Clients.Group(roomName).SendAsync("ReceiveMessage", "System", $"{Context.User.Identity.Name} has left the room.", "");
    }

    // Send message to a specific room
    public async Task SendMessageToRoom(string roomName, string user, string message, string avatarUrl)
    {
        await Clients.Group(roomName).SendAsync("ReceiveMessage", user, message, avatarUrl);
    }
}
