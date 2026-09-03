using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Trellochocero.Api.Data;
using Trellochocero.Api.DTOs;
using Trellochocero.Api.Models;

namespace Trellochocero.Api.Services;

public class CardService : ICardService
{
    private readonly AppDbContext _dbContext;
    private readonly IBoardRealtimeNotifier _realtimeNotifier;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CardService> _logger;

    public CardService(
        AppDbContext dbContext,
        IBoardRealtimeNotifier realtimeNotifier,
        IWebHostEnvironment environment,
        ILogger<CardService> logger)
    {
        _dbContext = dbContext;
        _realtimeNotifier = realtimeNotifier;
        _environment = environment;
        _logger = logger;
    }

    #region Card Core

    public async Task<Result<CardDetailResponse>> CreateCardAsync(Guid listId, CreateCardRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.BoardLists
            .Include(l => l.Board)
                .ThenInclude(b => b.Workspace)
                    .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (list == null)
        {
            return Result<CardDetailResponse>.Failure("List not found.", 404);
        }

        var member = list.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<CardDetailResponse>.Failure("You do not have access to this board.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<CardDetailResponse>.Failure("Observers cannot create cards.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result<CardDetailResponse>.Failure(
                "Card title is required.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field is required."] } });
        }

        if (request.Title.Trim().Length > 200)
        {
            return Result<CardDetailResponse>.Failure(
                "Card title must not exceed 200 characters.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field must not exceed 200 characters."] } });
        }

        var maxPosition = await _dbContext.Cards
            .Where(c => c.ListId == listId)
            .MaxAsync(c => (int?)c.Position, cancellationToken) ?? -1;

        var card = new Card
        {
            Id = Guid.NewGuid(),
            ListId = listId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            CoverColor = request.CoverColor?.Trim(),
            Position = maxPosition + 1,
            IsComplete = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new CardDetailResponse(
            card.Id,
            card.ListId,
            list.BoardId,
            card.Title,
            card.Description,
            card.Position,
            card.DueDate,
            card.IsComplete,
            card.CoverColor,
            card.CoverImageUrl,
            card.CreatedAt,
            new List<CardMemberResponse>(),
            new List<LabelResponse>(),
            new List<ChecklistResponse>(),
            new List<CommentResponse>(),
            new List<AttachmentResponse>()
        );

        await _realtimeNotifier.NotifyCardCreatedAsync(list.BoardId, response, cancellationToken);

        return Result<CardDetailResponse>.Created(response);
    }

    public async Task<Result<CardDetailResponse>> GetCardByIdAsync(Guid cardId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var card = await _dbContext.Cards
            .AsNoTracking()
            .Include(c => c.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Workspace)
                        .ThenInclude(w => w.Members)
            .Include(c => c.Members)
                .ThenInclude(cm => cm.User)
            .Include(c => c.Labels)
                .ThenInclude(cl => cl.Label)
            .Include(c => c.Checklists)
                .ThenInclude(ch => ch.Items)
            .Include(c => c.Comments)
                .ThenInclude(cc => cc.Author)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null)
        {
            return Result<CardDetailResponse>.Failure("Card not found.", 404);
        }

        var member = card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<CardDetailResponse>.Failure("You do not have access to this card.", 403);
        }

        return Result<CardDetailResponse>.Success(MapToDetailResponse(card));
    }

    public async Task<Result<CardDetailResponse>> UpdateCardAsync(Guid cardId, UpdateCardRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var card = await _dbContext.Cards
            .Include(c => c.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Workspace)
                        .ThenInclude(w => w.Members)
            .Include(c => c.Members)
                .ThenInclude(cm => cm.User)
            .Include(c => c.Labels)
                .ThenInclude(cl => cl.Label)
            .Include(c => c.Checklists)
                .ThenInclude(ch => ch.Items)
            .Include(c => c.Comments)
                .ThenInclude(cc => cc.Author)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null)
        {
            return Result<CardDetailResponse>.Failure("Card not found.", 404);
        }

        var member = card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<CardDetailResponse>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<CardDetailResponse>.Failure("Observers cannot edit cards.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result<CardDetailResponse>.Failure(
                "Card title is required.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field is required."] } });
        }

        if (request.Title.Trim().Length > 200)
        {
            return Result<CardDetailResponse>.Failure(
                "Card title must not exceed 200 characters.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field must not exceed 200 characters."] } });
        }

        card.Title = request.Title.Trim();
        card.Description = request.Description?.Trim();
        card.DueDate = request.DueDate;
        card.IsComplete = request.IsComplete;
        card.CoverColor = request.CoverColor?.Trim();
        card.CoverImageUrl = request.CoverImageUrl?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = MapToDetailResponse(card);
        var boardId = card.List?.BoardId ?? Guid.Empty;
        if (boardId != Guid.Empty)
        {
            await _realtimeNotifier.NotifyCardUpdatedAsync(boardId, response, cancellationToken);
        }

        return Result<CardDetailResponse>.Success(response);
    }

    public async Task<Result<bool>> MoveCardAsync(Guid cardId, MoveCardRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var card = await _dbContext.Cards
            .Include(c => c.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Workspace)
                        .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null)
        {
            return Result<bool>.Failure("Card not found.", 404);
        }

        var member = card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<bool>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<bool>.Failure("Observers cannot move cards.", 403);
        }

        var targetList = await _dbContext.BoardLists
            .FirstOrDefaultAsync(l => l.Id == request.TargetListId, cancellationToken);

        if (targetList == null)
        {
            return Result<bool>.Failure("Target list not found.", 404);
        }

        var boardId = card.List.BoardId;
        var sourceListId = card.ListId;
        var oldPosition = card.Position;
        var newPosition = Math.Max(0, request.NewPosition);

        if (sourceListId == request.TargetListId)
        {
            // Moving within same list
            var sameListCards = await _dbContext.Cards
                .Where(c => c.ListId == sourceListId && c.Id != cardId)
                .OrderBy(c => c.Position)
                .ToListAsync(cancellationToken);

            sameListCards.Insert(Math.Min(newPosition, sameListCards.Count), card);

            for (int i = 0; i < sameListCards.Count; i++)
            {
                sameListCards[i].Position = i;
            }
        }
        else
        {
            // Moving to a different list
            var sourceListCards = await _dbContext.Cards
                .Where(c => c.ListId == sourceListId && c.Id != cardId)
                .OrderBy(c => c.Position)
                .ToListAsync(cancellationToken);

            for (int i = 0; i < sourceListCards.Count; i++)
            {
                sourceListCards[i].Position = i;
            }

            var targetListCards = await _dbContext.Cards
                .Where(c => c.ListId == request.TargetListId && c.Id != cardId)
                .OrderBy(c => c.Position)
                .ToListAsync(cancellationToken);

            card.List = targetList;
            card.ListId = request.TargetListId;
            targetListCards.Insert(Math.Min(newPosition, targetListCards.Count), card);

            for (int i = 0; i < targetListCards.Count; i++)
            {
                targetListCards[i].Position = i;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _realtimeNotifier.NotifyCardMovedAsync(boardId, card.Id, sourceListId, targetList.Id, request.NewPosition, cancellationToken);

        return Result<bool>.Success(true, 204);
    }

    public async Task<Result<bool>> DeleteCardAsync(Guid cardId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var card = await _dbContext.Cards
            .Include(c => c.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Workspace)
                        .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null)
        {
            return Result<bool>.Failure("Card not found.", 404);
        }

        var member = card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<bool>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<bool>.Failure("Observers cannot delete cards.", 403);
        }

        var boardId = card.List.BoardId;
        var listId = card.ListId;
        var deletedCardId = card.Id;

        _dbContext.Cards.Remove(card);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _realtimeNotifier.NotifyCardDeletedAsync(boardId, deletedCardId, listId, cancellationToken);

        return Result<bool>.Success(true, 204);
    }

    #endregion

    #region Labels

    public async Task<Result<List<LabelResponse>>> GetBoardLabelsAsync(Guid boardId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var board = await _dbContext.Boards
            .Include(b => b.Workspace)
                .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);

        if (board == null)
        {
            return Result<List<LabelResponse>>.Failure("Board not found.", 404);
        }

        var member = board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<List<LabelResponse>>.Failure("You do not have access to this board.", 403);
        }

        var labels = await _dbContext.Labels
            .AsNoTracking()
            .Where(l => l.BoardId == boardId)
            .OrderBy(l => l.Name)
            .Select(l => new LabelResponse(l.Id, l.Name, l.Color))
            .ToListAsync(cancellationToken);

        return Result<List<LabelResponse>>.Success(labels);
    }

    public async Task<Result<LabelResponse>> CreateBoardLabelAsync(Guid boardId, CreateLabelRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var board = await _dbContext.Boards
            .Include(b => b.Workspace)
                .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);

        if (board == null)
        {
            return Result<LabelResponse>.Failure("Board not found.", 404);
        }

        var member = board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<LabelResponse>.Failure("You do not have access to this board.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<LabelResponse>.Failure("Observers cannot create labels.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Color))
        {
            return Result<LabelResponse>.Failure(
                "Label name and color are required.",
                400,
                new Dictionary<string, string[]> { { "Name", ["Name and Color are required."] } });
        }

        var label = new Label
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Name = request.Name.Trim(),
            Color = request.Color.Trim()
        };

        _dbContext.Labels.Add(label);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<LabelResponse>.Created(new LabelResponse(label.Id, label.Name, label.Color));
    }

    public async Task<Result<bool>> AddLabelToCardAsync(Guid cardId, Guid labelId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var card = await _dbContext.Cards
            .Include(c => c.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Workspace)
                        .ThenInclude(w => w.Members)
            .Include(c => c.Labels)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null)
        {
            return Result<bool>.Failure("Card not found.", 404);
        }

        var member = card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<bool>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<bool>.Failure("Observers cannot add labels.", 403);
        }

        var label = await _dbContext.Labels.FirstOrDefaultAsync(l => l.Id == labelId, cancellationToken);
        if (label == null)
        {
            return Result<bool>.Failure("Label not found.", 404);
        }

        if (!card.Labels.Any(cl => cl.LabelId == labelId))
        {
            card.Labels.Add(new CardLabel { CardId = cardId, LabelId = labelId });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true, 204);
    }

    public async Task<Result<bool>> RemoveLabelFromCardAsync(Guid cardId, Guid labelId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var card = await _dbContext.Cards
            .Include(c => c.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Workspace)
                        .ThenInclude(w => w.Members)
            .Include(c => c.Labels)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null)
        {
            return Result<bool>.Failure("Card not found.", 404);
        }

        var member = card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<bool>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<bool>.Failure("Observers cannot remove labels.", 403);
        }

        var cardLabel = card.Labels.FirstOrDefault(cl => cl.LabelId == labelId);
        if (cardLabel != null)
        {
            card.Labels.Remove(cardLabel);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true, 204);
    }

    #endregion

    #region Checklists & Items

    public async Task<Result<ChecklistResponse>> CreateChecklistAsync(Guid cardId, CreateChecklistRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var card = await _dbContext.Cards
            .Include(c => c.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Workspace)
                        .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null)
        {
            return Result<ChecklistResponse>.Failure("Card not found.", 404);
        }

        var member = card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<ChecklistResponse>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<ChecklistResponse>.Failure("Observers cannot create checklists.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result<ChecklistResponse>.Failure(
                "Checklist title is required.",
                400,
                new Dictionary<string, string[]> { { "Title", ["The Title field is required."] } });
        }

        var maxPosition = await _dbContext.Checklists
            .Where(ch => ch.CardId == cardId)
            .MaxAsync(ch => (int?)ch.Position, cancellationToken) ?? -1;

        var checklist = new Checklist
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            Title = request.Title.Trim(),
            Position = maxPosition + 1
        };

        _dbContext.Checklists.Add(checklist);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ChecklistResponse>.Created(new ChecklistResponse(
            checklist.Id,
            checklist.Title,
            checklist.Position,
            new List<ChecklistItemResponse>()
        ));
    }

    public async Task<Result<bool>> DeleteChecklistAsync(Guid checklistId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var checklist = await _dbContext.Checklists
            .Include(ch => ch.Card)
                .ThenInclude(c => c.List)
                    .ThenInclude(l => l.Board)
                        .ThenInclude(b => b.Workspace)
                            .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(ch => ch.Id == checklistId, cancellationToken);

        if (checklist == null)
        {
            return Result<bool>.Failure("Checklist not found.", 404);
        }

        var member = checklist.Card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<bool>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<bool>.Failure("Observers cannot delete checklists.", 403);
        }

        _dbContext.Checklists.Remove(checklist);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, 204);
    }

    public async Task<Result<ChecklistItemResponse>> CreateChecklistItemAsync(Guid checklistId, CreateChecklistItemRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var checklist = await _dbContext.Checklists
            .Include(ch => ch.Card)
                .ThenInclude(c => c.List)
                    .ThenInclude(l => l.Board)
                        .ThenInclude(b => b.Workspace)
                            .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(ch => ch.Id == checklistId, cancellationToken);

        if (checklist == null)
        {
            return Result<ChecklistItemResponse>.Failure("Checklist not found.", 404);
        }

        var member = checklist.Card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<ChecklistItemResponse>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<ChecklistItemResponse>.Failure("Observers cannot add checklist items.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Result<ChecklistItemResponse>.Failure(
                "Item text is required.",
                400,
                new Dictionary<string, string[]> { { "Text", ["The Text field is required."] } });
        }

        var maxPosition = await _dbContext.ChecklistItems
            .Where(i => i.ChecklistId == checklistId)
            .MaxAsync(i => (int?)i.Position, cancellationToken) ?? -1;

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistId = checklistId,
            Text = request.Text.Trim(),
            IsChecked = false,
            Position = maxPosition + 1
        };

        _dbContext.ChecklistItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ChecklistItemResponse>.Created(new ChecklistItemResponse(item.Id, item.Text, item.IsChecked, item.Position));
    }

    public async Task<Result<ChecklistItemResponse>> UpdateChecklistItemAsync(Guid itemId, UpdateChecklistItemRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.ChecklistItems
            .Include(i => i.Checklist)
                .ThenInclude(ch => ch.Card)
                    .ThenInclude(c => c.List)
                        .ThenInclude(l => l.Board)
                            .ThenInclude(b => b.Workspace)
                                .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

        if (item == null)
        {
            return Result<ChecklistItemResponse>.Failure("Checklist item not found.", 404);
        }

        var member = item.Checklist.Card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<ChecklistItemResponse>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<ChecklistItemResponse>.Failure("Observers cannot update checklist items.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Result<ChecklistItemResponse>.Failure(
                "Item text is required.",
                400,
                new Dictionary<string, string[]> { { "Text", ["The Text field is required."] } });
        }

        item.Text = request.Text.Trim();
        item.IsChecked = request.IsChecked;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ChecklistItemResponse>.Success(new ChecklistItemResponse(item.Id, item.Text, item.IsChecked, item.Position));
    }

    public async Task<Result<bool>> DeleteChecklistItemAsync(Guid itemId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.ChecklistItems
            .Include(i => i.Checklist)
                .ThenInclude(ch => ch.Card)
                    .ThenInclude(c => c.List)
                        .ThenInclude(l => l.Board)
                            .ThenInclude(b => b.Workspace)
                                .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

        if (item == null)
        {
            return Result<bool>.Failure("Checklist item not found.", 404);
        }

        var member = item.Checklist.Card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<bool>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<bool>.Failure("Observers cannot delete checklist items.", 403);
        }

        _dbContext.ChecklistItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, 204);
    }

    #endregion

    #region Comments

    public async Task<Result<CommentResponse>> AddCommentAsync(Guid cardId, CreateCommentRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var card = await _dbContext.Cards
            .Include(c => c.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Workspace)
                        .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null)
        {
            return Result<CommentResponse>.Failure("Card not found.", 404);
        }

        var member = card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<CommentResponse>.Failure("You do not have access to this card.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Result<CommentResponse>.Failure(
                "Comment text is required.",
                400,
                new Dictionary<string, string[]> { { "Text", ["The Text field is required."] } });
        }

        var comment = new CardComment
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            AuthorId = currentUserId,
            Text = request.Text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CardComments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var author = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        return Result<CommentResponse>.Created(new CommentResponse(
            comment.Id,
            comment.AuthorId,
            author?.FullName ?? "Unknown",
            author?.AvatarUrl,
            comment.Text,
            comment.CreatedAt,
            comment.UpdatedAt
        ));
    }

    public async Task<Result<CommentResponse>> UpdateCommentAsync(Guid commentId, UpdateCommentRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var comment = await _dbContext.CardComments
            .Include(cc => cc.Author)
            .FirstOrDefaultAsync(cc => cc.Id == commentId, cancellationToken);

        if (comment == null)
        {
            return Result<CommentResponse>.Failure("Comment not found.", 404);
        }

        if (comment.AuthorId != currentUserId)
        {
            return Result<CommentResponse>.Failure("You can only edit your own comments.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Result<CommentResponse>.Failure(
                "Comment text is required.",
                400,
                new Dictionary<string, string[]> { { "Text", ["The Text field is required."] } });
        }

        comment.Text = request.Text.Trim();
        comment.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<CommentResponse>.Success(new CommentResponse(
            comment.Id,
            comment.AuthorId,
            comment.Author.FullName,
            comment.Author.AvatarUrl,
            comment.Text,
            comment.CreatedAt,
            comment.UpdatedAt
        ));
    }

    public async Task<Result<bool>> DeleteCommentAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var comment = await _dbContext.CardComments
            .Include(cc => cc.Card)
                .ThenInclude(c => c.List)
                    .ThenInclude(l => l.Board)
                        .ThenInclude(b => b.Workspace)
                            .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(cc => cc.Id == commentId, cancellationToken);

        if (comment == null)
        {
            return Result<bool>.Failure("Comment not found.", 404);
        }

        var isAuthor = comment.AuthorId == currentUserId;
        var isOwner = comment.Card.List.Board.Workspace.Members.Any(m => m.UserId == currentUserId && m.Role == WorkspaceRole.Owner);

        if (!isAuthor && !isOwner)
        {
            return Result<bool>.Failure("You do not have permission to delete this comment.", 403);
        }

        _dbContext.CardComments.Remove(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, 204);
    }

    #endregion

    #region Attachments

    public async Task<Result<AttachmentResponse>> UploadAttachmentAsync(Guid cardId, IFormFile file, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return Result<AttachmentResponse>.Failure("No file uploaded.", 400);
        }

        var card = await _dbContext.Cards
            .Include(c => c.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Workspace)
                        .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null)
        {
            return Result<AttachmentResponse>.Failure("Card not found.", 404);
        }

        var member = card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<AttachmentResponse>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<AttachmentResponse>.Failure("Observers cannot upload attachments.", 403);
        }

        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "cards", cardId.ToString());
        if (!Directory.Exists(uploadsRoot))
        {
            Directory.CreateDirectory(uploadsRoot);
        }

        var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsRoot, safeFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var fileUrl = $"/uploads/cards/{cardId}/{safeFileName}";

        var attachment = new CardAttachment
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            UploadedById = currentUserId,
            FileName = file.FileName,
            FileUrl = fileUrl,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CardAttachments.Add(attachment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<AttachmentResponse>.Created(new AttachmentResponse(
            attachment.Id,
            attachment.FileName,
            attachment.FileUrl,
            attachment.ContentType,
            attachment.FileSizeBytes,
            attachment.CreatedAt
        ));
    }

    public async Task<Result<(Stream Stream, string ContentType, string FileName)>> GetAttachmentFileAsync(Guid attachmentId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var attachment = await _dbContext.CardAttachments
            .Include(ca => ca.Card)
                .ThenInclude(c => c.List)
                    .ThenInclude(l => l.Board)
                        .ThenInclude(b => b.Workspace)
                            .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(ca => ca.Id == attachmentId, cancellationToken);

        if (attachment == null)
        {
            return Result<(Stream Stream, string ContentType, string FileName)>.Failure("Attachment not found.", 404);
        }

        var member = attachment.Card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<(Stream Stream, string ContentType, string FileName)>.Failure("You do not have access to this attachment.", 403);
        }

        var relativePath = attachment.FileUrl.TrimStart('/');
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            return Result<(Stream Stream, string ContentType, string FileName)>.Failure("File not found on server.", 404);
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = !string.IsNullOrWhiteSpace(attachment.ContentType) ? attachment.ContentType : "application/octet-stream";

        return Result<(Stream Stream, string ContentType, string FileName)>.Success((stream, contentType, attachment.FileName));
    }

    public async Task<Result<bool>> DeleteAttachmentAsync(Guid attachmentId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var attachment = await _dbContext.CardAttachments
            .Include(ca => ca.Card)
                .ThenInclude(c => c.List)
                    .ThenInclude(l => l.Board)
                        .ThenInclude(b => b.Workspace)
                            .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(ca => ca.Id == attachmentId, cancellationToken);

        if (attachment == null)
        {
            return Result<bool>.Failure("Attachment not found.", 404);
        }

        var isUploader = attachment.UploadedById == currentUserId;
        var isOwner = attachment.Card.List.Board.Workspace.Members.Any(m => m.UserId == currentUserId && m.Role == WorkspaceRole.Owner);

        if (!isUploader && !isOwner)
        {
            return Result<bool>.Failure("You do not have permission to delete this attachment.", 403);
        }

        // Try deleting local file
        try
        {
            var relativePath = attachment.FileUrl.TrimStart('/');
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file on disk for attachment {AttachmentId}", attachmentId);
        }

        _dbContext.CardAttachments.Remove(attachment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, 204);
    }

    #endregion

    #region Card Members

    public async Task<Result<bool>> AddMemberToCardAsync(Guid cardId, Guid userId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var card = await _dbContext.Cards
            .Include(c => c.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Workspace)
                        .ThenInclude(w => w.Members)
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null)
        {
            return Result<bool>.Failure("Card not found.", 404);
        }

        var member = card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<bool>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<bool>.Failure("Observers cannot assign members.", 403);
        }

        var targetWorkspaceMember = card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == userId);
        if (targetWorkspaceMember == null)
        {
            return Result<bool>.Failure("Target user is not a member of this workspace.", 400);
        }

        if (!card.Members.Any(cm => cm.UserId == userId))
        {
            card.Members.Add(new CardMember { CardId = cardId, UserId = userId });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true, 204);
    }

    public async Task<Result<bool>> RemoveMemberFromCardAsync(Guid cardId, Guid userId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var card = await _dbContext.Cards
            .Include(c => c.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Workspace)
                        .ThenInclude(w => w.Members)
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null)
        {
            return Result<bool>.Failure("Card not found.", 404);
        }

        var member = card.List.Board.Workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member == null)
        {
            return Result<bool>.Failure("You do not have access to this card.", 403);
        }

        if (member.Role == WorkspaceRole.Observer)
        {
            return Result<bool>.Failure("Observers cannot remove members.", 403);
        }

        var cardMember = card.Members.FirstOrDefault(cm => cm.UserId == userId);
        if (cardMember != null)
        {
            card.Members.Remove(cardMember);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true, 204);
    }

    #endregion

    private static CardDetailResponse MapToDetailResponse(Card card)
    {
        return new CardDetailResponse(
            card.Id,
            card.ListId,
            card.List?.BoardId ?? Guid.Empty,
            card.Title,
            card.Description,
            card.Position,
            card.DueDate,
            card.IsComplete,
            card.CoverColor,
            card.CoverImageUrl,
            card.CreatedAt,
            card.Members.Select(cm => new CardMemberResponse(
                cm.UserId,
                cm.User?.FullName ?? "Unknown",
                cm.User?.Email ?? string.Empty,
                cm.User?.AvatarUrl
            )).ToList(),
            card.Labels.Select(cl => new LabelResponse(
                cl.Label.Id,
                cl.Label.Name,
                cl.Label.Color
            )).ToList(),
            card.Checklists.OrderBy(ch => ch.Position).Select(ch => new ChecklistResponse(
                ch.Id,
                ch.Title,
                ch.Position,
                ch.Items.OrderBy(i => i.Position).Select(i => new ChecklistItemResponse(
                    i.Id,
                    i.Text,
                    i.IsChecked,
                    i.Position
                )).ToList()
            )).ToList(),
            card.Comments.OrderBy(c => c.CreatedAt).Select(c => new CommentResponse(
                c.Id,
                c.AuthorId,
                c.Author?.FullName ?? "Unknown",
                c.Author?.AvatarUrl,
                c.Text,
                c.CreatedAt,
                c.UpdatedAt
            )).ToList(),
            card.Attachments.OrderByDescending(a => a.CreatedAt).Select(a => new AttachmentResponse(
                a.Id,
                a.FileName,
                a.FileUrl,
                a.ContentType,
                a.FileSizeBytes,
                a.CreatedAt
            )).ToList()
        );
    }
}
