using System.Security.Claims;
using WhoSabi.Application.Rooms;
using WhoSabi.Contracts.Rooms;

namespace WhoSabi.Api.Rooms;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/rooms/live", GetLiveRoomsAsync)
            .RequireAuthorization()
            .WithTags("Rooms")
            .WithName("GetLiveRooms");

        app.MapGet("/api/rooms/{roomId}", GetRoomDetailAsync)
            .RequireAuthorization()
            .WithTags("Rooms")
            .WithName("GetRoomDetail");

        app.MapPost("/api/rooms/{roomId}/join", JoinRoomAsync)
            .RequireAuthorization()
            .WithTags("Rooms")
            .WithName("JoinRoom");

        app.MapPost("/api/rooms/{roomId}/answers", SubmitRoomAnswerAsync)
            .RequireAuthorization()
            .WithTags("Rooms")
            .WithName("SubmitRoomAnswer");

        return app;
    }

    private static async Task<IResult> GetLiveRoomsAsync(
        IGetLiveRoomsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.HandleAsync(cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetRoomDetailAsync(
        string roomId,
        ClaimsPrincipal principal,
        IGetRoomDetailUseCase useCase,
        CancellationToken cancellationToken)
    {
        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var response = await useCase.HandleAsync(
                userId,
                roomId,
                cancellationToken);

            return Results.Ok(response);
        }
        catch (RoomDetailException exception) when (exception.Code == "ROOM_NOT_FOUND")
        {
            return Results.NotFound(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
        catch (RoomDetailException exception)
        {
            return Results.BadRequest(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
    }

    private static async Task<IResult> JoinRoomAsync(
        string roomId,
        JoinRoomRequest request,
        ClaimsPrincipal principal,
        IJoinRoomUseCase useCase,
        CancellationToken cancellationToken)
    {
        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var response = await useCase.HandleAsync(
                userId,
                roomId,
                request,
                cancellationToken);

            return Results.Ok(response);
        }
        catch (JoinRoomException exception) when (
            exception.Code is "ROOM_NOT_FOUND" or "USER_NOT_FOUND" or "WALLET_NOT_FOUND")
        {
            return Results.NotFound(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
        catch (JoinRoomException exception)
        {
            return Results.BadRequest(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
    }

    private static async Task<IResult> SubmitRoomAnswerAsync(
        string roomId,
        SubmitRoomAnswerRequest request,
        ClaimsPrincipal principal,
        ISubmitRoomAnswerUseCase useCase,
        CancellationToken cancellationToken)
    {
        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var response = await useCase.HandleAsync(
                userId,
                roomId,
                request,
                cancellationToken);

            return Results.Ok(response);
        }
        catch (SubmitRoomAnswerException exception) when (
            exception.Code is "ROOM_NOT_FOUND" or "QUESTION_NOT_FOUND" or "WALLET_NOT_FOUND")
        {
            return Results.NotFound(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
        catch (SubmitRoomAnswerException exception)
        {
            return Results.BadRequest(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
    }
}
