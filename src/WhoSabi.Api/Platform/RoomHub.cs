using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WhoSabi.Infrastructure.Persistence;
using System.Collections.Concurrent;

namespace WhoSabi.Api.Platform;

[Authorize]
public sealed class RoomHub(WhoSabiDbContext db, RoundService rounds, RoomConnections connections) : Hub
{
    public async Task<object> SubscribeRoom(string roomId)
    {
        try
        {
            await ApiSupport.Member(db, roomId, Context.User!.UserId(), Context.ConnectionAborted);
            await Groups.AddToGroupAsync(Context.ConnectionId, Group(roomId));
            connections.Add(Context.User!.UserId(), roomId, Context.ConnectionId);
            // Close the race between membership validation and host removal.
            await ApiSupport.Member(db, roomId, Context.User!.UserId(), Context.ConnectionAborted);
            return await rounds.Snapshot(roomId, Context.ConnectionAborted);
        }
        catch (ApiFault e)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(roomId));
            connections.Remove(Context.User!.UserId(), roomId, Context.ConnectionId);
            throw new HubException(e.Code);
        }
    }
    public Task UnsubscribeRoom(string roomId)
    {
        connections.Remove(Context.User!.UserId(), roomId, Context.ConnectionId);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(roomId));
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    { connections.Disconnected(Context.ConnectionId); return base.OnDisconnectedAsync(exception); }
    public static string Group(string roomId) => "room:" + roomId;
}

public sealed class RoomConnections
{
    private readonly ConcurrentDictionary<(Guid User, string Room, string Connection), byte> entries = new();
    public void Add(Guid user, string room, string connection) => entries[(user, room, connection)] = 0;
    public void Remove(Guid user, string room, string connection) => entries.TryRemove((user, room, connection), out _);
    public void Disconnected(string connection)
    { foreach (var entry in entries.Keys.Where(x => x.Connection == connection)) entries.TryRemove(entry, out _); }
    public string[] For(Guid user, string room) => entries.Keys.Where(x => x.User == user && x.Room == room).Select(x => x.Connection).ToArray();
}
