using Microsoft.EntityFrameworkCore;
using Trellochocero.Api.Data;
using Trellochocero.Api.DTOs;
using Trellochocero.Api.Models;

namespace Trellochocero.Api.Services;

public class BoardService : IBoardService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<BoardService> _logger;

    public BoardService(AppDbContext dbContext, ILogger<BoardService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<List<BoardSummaryResponse>>> GetWorkspaceBoardsAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var isMember = await _dbContext.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId, cancellationToken);

        if (!isMember)
        {
            var workspaceExists = await _dbContext.Workspaces.AnyAsync(w => w.Id == workspaceId, cancellationToken);
            return workspaceExists
                ? Result<List<BoardSummaryResponse>>.Failure("You do not have access to this workspace.", 403)
                : Result<List<BoardSummaryResponse>>.Failure("Workspace not found.", 404);
        }

        var boards = await _dbContext.Boards
            .AsNoTracking()
            .Where(b => b.WorkspaceId == workspaceId)
            .OrderBy(b => b.Position)
            .ThenByDescending(b => b.CreatedAt)
            .Select(b => new BoardSummaryResponse(
                b.Id,
                b.WorkspaceId,
                b.Title,
                b.BackgroundColor,
                b.BackgroundImageUrl,
                b.IsClosed,
                b.Position,
                b.Lists.Count,
                b.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<List<BoardSummaryResponse>>.Success(boards);
    }

    public async Task<Result<BoardDetailResponse>> GetBoardByIdAsync(Guid boardId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var board = await _dbContext.Boards
            .AsNoTracking()
            .Include(b => b.Workspace)
                .ThenInclude(w => w.Members)
            .Include(b => b.Lists)
                .ThenInclude(l => l.Cards)
                    .ThenInclude(c => c.Comments)
            .Include(b => b.Lists)
                .ThenInclude(l => l.Cards)
                    .ThenInclude(c => c.Checklists)
                        .ThenInclude(ch => ch.Items)
            .FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);

        if (board == null)
        {
            return Result<BoardDetailResponse>.Failure("Board not found.", 404);
        }

        var currentMember = board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (currentMember == null)
        {
            return Result<BoardDetailResponse>.Failure("You do not have access to this board.", 403);
        }

        var listResponses = board.Lists
            .OrderBy(l => l.Position)
            .Select(l => new BoardListResponse(
                l.Id,
                l.BoardId,
                l.Title,
                l.Position,
                l.IsArchived,
                l.Cards
                    .OrderBy(c => c.Position)
                    .Select(c => new CardSummaryResponse(
                        c.Id,
                        c.ListId,
                        c.Title,
                        c.Description,
                        c.Position,
                        c.DueDate,
                        c.IsComplete,
                        c.CoverColor,
                        c.CoverImageUrl,
                        c.Comments.Count,
                        c.Checklists.Sum(ch => ch.Items.Count),
                        c.Checklists.Sum(ch => ch.Items.Count(i => i.IsChecked))
                    ))
                    .ToList()
            ))
            .ToList();

        var response = new BoardDetailResponse(
            board.Id,
            board.WorkspaceId,
            board.Workspace.Name,
            board.Title,
            board.BackgroundColor,
            board.BackgroundImageUrl,
            board.IsClosed,
            currentMember.Role.ToString(),
            listResponses
        );

        return Result<BoardDetailResponse>.Success(response);
    }

    public async Task<Result<BoardSummaryResponse>> CreateBoardAsync(Guid workspaceId, CreateBoardRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var workspace = await _dbContext.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace == null)
        {
            return Result<BoardSummaryResponse>.Failure("Workspace not found.", 404);
        }

        var member = workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<BoardSummaryResponse>.Failure("You are not a member of this workspace.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<BoardSummaryResponse>.Failure("Observers cannot create boards in this workspace.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result<BoardSummaryResponse>.Failure(
                "Board title is required.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field is required."] } });
        }

        if (request.Title.Trim().Length > 100)
        {
            return Result<BoardSummaryResponse>.Failure(
                "Board title must not exceed 100 characters.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field must not exceed 100 characters."] } });
        }

        var maxPosition = await _dbContext.Boards
            .Where(b => b.WorkspaceId == workspaceId)
            .MaxAsync(b => (int?)b.Position, cancellationToken) ?? -1;

        var board = new Board
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Title = request.Title.Trim(),
            BackgroundColor = !string.IsNullOrWhiteSpace(request.BackgroundColor) ? request.BackgroundColor.Trim() : "#0079bf",
            BackgroundImageUrl = request.BackgroundImageUrl?.Trim(),
            IsClosed = false,
            Position = maxPosition + 1,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Boards.Add(board);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new BoardSummaryResponse(
            board.Id,
            board.WorkspaceId,
            board.Title,
            board.BackgroundColor,
            board.BackgroundImageUrl,
            board.IsClosed,
            board.Position,
            0,
            board.CreatedAt
        );

        return Result<BoardSummaryResponse>.Created(response);
    }

    public async Task<Result<BoardDetailResponse>> UpdateBoardAsync(Guid boardId, UpdateBoardRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var board = await _dbContext.Boards
            .Include(b => b.Workspace)
                .ThenInclude(w => w.Members)
            .Include(b => b.Lists)
            .FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);

        if (board == null)
        {
            return Result<BoardDetailResponse>.Failure("Board not found.", 404);
        }

        var member = board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<BoardDetailResponse>.Failure("You do not have access to this board.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<BoardDetailResponse>.Failure("Observers cannot update boards in this workspace.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result<BoardDetailResponse>.Failure(
                "Board title is required.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field is required."] } });
        }

        if (request.Title.Trim().Length > 100)
        {
            return Result<BoardDetailResponse>.Failure(
                "Board title must not exceed 100 characters.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field must not exceed 100 characters."] } });
        }

        board.Title = request.Title.Trim();
        board.BackgroundColor = !string.IsNullOrWhiteSpace(request.BackgroundColor) ? request.BackgroundColor.Trim() : board.BackgroundColor;
        board.BackgroundImageUrl = request.BackgroundImageUrl?.Trim();
        board.IsClosed = request.IsClosed;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var listResponses = board.Lists
            .OrderBy(l => l.Position)
            .Select(l => new BoardListResponse(
                l.Id,
                l.BoardId,
                l.Title,
                l.Position,
                l.IsArchived,
                new List<CardSummaryResponse>()
            ))
            .ToList();

        var response = new BoardDetailResponse(
            board.Id,
            board.WorkspaceId,
            board.Workspace.Name,
            board.Title,
            board.BackgroundColor,
            board.BackgroundImageUrl,
            board.IsClosed,
            member.Role.ToString(),
            listResponses
        );

        return Result<BoardDetailResponse>.Success(response);
    }

    public async Task<Result<bool>> DeleteBoardAsync(Guid boardId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var board = await _dbContext.Boards
            .Include(b => b.Workspace)
                .ThenInclude(w => w.Members)
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

        if (member.Role != WorkspaceRole.Owner)
        {
            return Result<bool>.Failure("Only workspace owners can delete boards.", 403);
        }

        _dbContext.Boards.Remove(board);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true, 204);
    }
}
