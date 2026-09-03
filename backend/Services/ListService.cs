using Microsoft.EntityFrameworkCore;
using Trellochocero.Api.Data;
using Trellochocero.Api.DTOs;
using Trellochocero.Api.Models;

namespace Trellochocero.Api.Services;

public class ListService : IListService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ListService> _logger;

    public ListService(AppDbContext dbContext, ILogger<ListService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<BoardListResponse>> CreateListAsync(Guid boardId, CreateListRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var board = await _dbContext.Boards
            .Include(b => b.Workspace)
                .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);

        if (board == null)
        {
            return Result<BoardListResponse>.Failure("Board not found.", 404);
        }

        var member = board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<BoardListResponse>.Failure("You do not have access to this board.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<BoardListResponse>.Failure("Observers cannot create lists.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result<BoardListResponse>.Failure(
                "List title is required.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field is required."] } });
        }

        if (request.Title.Trim().Length > 100)
        {
            return Result<BoardListResponse>.Failure(
                "List title must not exceed 100 characters.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field must not exceed 100 characters."] } });
        }

        var maxPosition = await _dbContext.BoardLists
            .Where(l => l.BoardId == boardId)
            .MaxAsync(l => (int?)l.Position, cancellationToken) ?? -1;

        var list = new BoardList
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Title = request.Title.Trim(),
            Position = maxPosition + 1,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.BoardLists.Add(list);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new BoardListResponse(
            list.Id,
            list.BoardId,
            list.Title,
            list.Position,
            list.IsArchived,
            new List<CardSummaryResponse>()
        );

        return Result<BoardListResponse>.Created(response);
    }

    public async Task<Result<BoardListResponse>> UpdateListAsync(Guid listId, UpdateListRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.BoardLists
            .Include(l => l.Board)
                .ThenInclude(b => b.Workspace)
                    .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (list == null)
        {
            return Result<BoardListResponse>.Failure("List not found.", 404);
        }

        var member = list.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<BoardListResponse>.Failure("You do not have access to this board.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<BoardListResponse>.Failure("Observers cannot update lists.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result<BoardListResponse>.Failure(
                "List title is required.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field is required."] } });
        }

        if (request.Title.Trim().Length > 100)
        {
            return Result<BoardListResponse>.Failure(
                "List title must not exceed 100 characters.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field must not exceed 100 characters."] } });
        }

        list.Title = request.Title.Trim();
        list.IsArchived = request.IsArchived;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new BoardListResponse(
            list.Id,
            list.BoardId,
            list.Title,
            list.Position,
            list.IsArchived,
            new List<CardSummaryResponse>()
        );

        return Result<BoardListResponse>.Success(response);
    }

    public async Task<Result<bool>> ReorderListsAsync(Guid boardId, ReorderListsRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var board = await _dbContext.Boards
            .Include(b => b.Workspace)
                .ThenInclude(w => w.Members)
            .Include(b => b.Lists)
            .FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);

        if (board == null)
        {
            return Result<bool>.Failure("Board not found.", 404);
        }

        var member = board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<bool>.Failure("You do not have access to this board.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<bool>.Failure("Observers cannot reorder lists.", 403);
        }

        if (request.ListIds == null || request.ListIds.Count == 0)
        {
            return Result<bool>.Failure("ListIds array is required.", 400);
        }

        var listMap = board.Lists.ToDictionary(l => l.Id);
        for (int i = 0; i < request.ListIds.Count; i++)
        {
            var listId = request.ListIds[i];
            if (listMap.TryGetValue(listId, out var boardList))
            {
                boardList.Position = i;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true, 204);
    }

    public async Task<Result<bool>> DeleteListAsync(Guid listId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.BoardLists
            .Include(l => l.Board)
                .ThenInclude(b => b.Workspace)
                    .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (list == null)
        {
            return Result<bool>.Failure("List not found.", 404);
        }

        var member = list.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<bool>.Failure("You do not have access to this board.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<bool>.Failure("Observers cannot delete lists.", 403);
        }

        _dbContext.BoardLists.Remove(list);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true, 204);
    }
}
