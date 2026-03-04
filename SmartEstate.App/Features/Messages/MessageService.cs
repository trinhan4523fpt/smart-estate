using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.App.Features.Messages.Dtos;
using SmartEstate.Domain.Entities;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;

namespace SmartEstate.App.Features.Messages;

public sealed class MessageService
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public MessageService(SmartEstateDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> StartConversationAsync(StartConversationRequest req, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<Guid>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings
            .Include(x => x.ResponsibleUser)
            .FirstOrDefaultAsync(x => x.Id == req.ListingId && !x.IsDeleted, ct);

        if (listing is null) return Result<Guid>.Fail(ErrorCodes.NotFound, "Listing not found.");

        if (listing.ResponsibleUserId == userId.Value)
             return Result<Guid>.Fail(ErrorCodes.Conflict, "You cannot chat with yourself.");

        var existing = await _db.Conversations
            .FirstOrDefaultAsync(x => x.ListingId == req.ListingId && x.BuyerUserId == userId.Value, ct);

        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(req.InitialMessage))
            {
                await SendMessageAsync(existing.Id, new SendMessageRequest(req.InitialMessage), ct);
            }
            return Result<Guid>.Ok(existing.Id);
        }

        var conv = Conversation.Create(req.ListingId, userId.Value, listing.ResponsibleUserId);
        
        if (!string.IsNullOrWhiteSpace(req.InitialMessage))
        {
            conv.UpdateLastMessage(req.InitialMessage, _clock.UtcNow);
        }

        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync(true, ct);

        if (!string.IsNullOrWhiteSpace(req.InitialMessage))
        {
             // We can't call SendMessageAsync directly easily because we need to insert the message after conversation is saved, or add it to collection.
             // But SendMessageAsync fetches conversation.
             // Let's just create message here.
             var msg = Message.Create(conv.Id, userId.Value, req.InitialMessage);
             _db.Messages.Add(msg);
             await _db.SaveChangesAsync(true, ct);
        }

        return Result<Guid>.Ok(conv.Id);
    }

    public async Task<Result<MessageDto>> SendMessageAsync(Guid conversationId, SendMessageRequest req, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<MessageDto>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var conv = await _db.Conversations
            .Include(x => x.Listing)
            .FirstOrDefaultAsync(x => x.Id == conversationId, ct);

        if (conv is null) return Result<MessageDto>.Fail(ErrorCodes.NotFound, "Conversation not found.");

        var isBuyer = conv.BuyerUserId == userId.Value;
        var isSeller = conv.Listing.ResponsibleUserId == userId.Value;

        if (!isBuyer && !isSeller)
            return Result<MessageDto>.Fail(ErrorCodes.Forbidden, "You are not a participant of this conversation.");

        var msg = Message.Create(conversationId, userId.Value, req.Content);

        _db.Messages.Add(msg);

        conv.UpdateLastMessage(msg.Content.Length > 50 ? msg.Content.Substring(0, 50) + "..." : msg.Content, msg.SentAt);

        await _db.SaveChangesAsync(true, ct);

        return Result<MessageDto>.Ok(new MessageDto(msg.Id, msg.SenderUserId, msg.Content, msg.SentAt, msg.IsRead));
    }

    public async Task<Result<List<ConversationDto>>> GetConversationsAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<List<ConversationDto>>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var convs = await _db.Conversations
            .AsNoTracking()
            .Include(x => x.Listing)
            .Include(x => x.Listing.Images)
            .Include(x => x.BuyerUser)
            .Include(x => x.Listing.ResponsibleUser)
            .Where(x => x.BuyerUserId == userId.Value || x.Listing.ResponsibleUserId == userId.Value)
            .OrderByDescending(x => x.LastMessageAt)
            .ToListAsync(ct);

        var dtos = convs.Select(x => {
            var isBuyer = x.BuyerUserId == userId.Value;
            var otherUser = isBuyer ? x.Listing.ResponsibleUser : x.BuyerUser;
            var hasRead = x.LastMessageAt is null
                || (isBuyer
                    ? (x.BuyerLastReadAt is not null && x.BuyerLastReadAt >= x.LastMessageAt)
                    : (x.ResponsibleLastReadAt is not null && x.ResponsibleLastReadAt >= x.LastMessageAt));
            
            return new ConversationDto(
                x.Id,
                x.ListingId,
                x.Listing.Title,
                x.Listing.Images?.FirstOrDefault()?.Url ?? "",
                otherUser.Id,
                otherUser.DisplayName,
                null,
                x.LastMessagePreview,
                x.LastMessageAt,
                hasRead
            );
        }).ToList();

        return Result<List<ConversationDto>>.Ok(dtos);
    }

    public async Task<Result<List<MessageDto>>> GetMessagesAsync(Guid conversationId, CancellationToken ct = default)
    {
         var userId = _currentUser.UserId;
        if (userId is null) return Result<List<MessageDto>>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var conv = await _db.Conversations
            .Include(x => x.Listing)
            .FirstOrDefaultAsync(x => x.Id == conversationId, ct);

        if (conv is null) return Result<List<MessageDto>>.Fail(ErrorCodes.NotFound, "Conversation not found.");

        var isBuyer = conv.BuyerUserId == userId.Value;
        var isSeller = conv.Listing.ResponsibleUserId == userId.Value;

        if (!isBuyer && !isSeller)
            return Result<List<MessageDto>>.Fail(ErrorCodes.Forbidden, "You are not a participant.");

        var msgs = await _db.Messages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.SentAt)
            .ToListAsync(ct);

        return Result<List<MessageDto>>.Ok(msgs.Select(x => new MessageDto(x.Id, x.SenderUserId, x.Content, x.SentAt, x.IsRead)).ToList());
    }

    public async Task<Result> MarkConversationReadAsync(Guid conversationId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var conv = await _db.Conversations
            .Include(x => x.Listing)
            .FirstOrDefaultAsync(x => x.Id == conversationId, ct);
        if (conv is null) return Result.Fail(ErrorCodes.NotFound, "Conversation not found.");

        conv.MarkRead(userId.Value);

        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }

    public async Task<Result<int>> GetUnreadCountAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<int>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var count = await _db.Conversations
            .AsNoTracking()
            .CountAsync(x => 
                (x.BuyerUserId == userId.Value && (x.BuyerLastReadAt == null || x.BuyerLastReadAt < x.LastMessageAt)) ||
                (x.ResponsibleUserId == userId.Value && (x.ResponsibleLastReadAt == null || x.ResponsibleLastReadAt < x.LastMessageAt)),
                ct);

        return Result<int>.Ok(count);
    }
}
