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

        var conv = new Conversation
        {
            ListingId = req.ListingId,
            BuyerUserId = userId.Value,
            ResponsibleUserId = listing.ResponsibleUserId,
            LastMessageAt = _clock.UtcNow,
            LastMessagePreview = req.InitialMessage
        };

        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync(true, ct);

        if (!string.IsNullOrWhiteSpace(req.InitialMessage))
        {
             await SendMessageAsync(conv.Id, new SendMessageRequest(req.InitialMessage), ct);
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

        var msg = new Message
        {
            ConversationId = conversationId,
            SenderUserId = userId.Value,
            Content = req.Content,
            SentAt = _clock.UtcNow,
            IsRead = false
        };

        _db.Messages.Add(msg);

        conv.LastMessageAt = msg.SentAt;
        conv.LastMessagePreview = msg.Content.Length > 50 ? msg.Content.Substring(0, 50) + "..." : msg.Content;

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

        var convIds = convs.Select(x => x.Id).ToList();
        var readStates = await _db.ConversationReadStates
            .AsNoTracking()
            .Where(rs => convIds.Contains(rs.ConversationId) && rs.UserId == userId.Value)
            .ToDictionaryAsync(rs => rs.ConversationId, ct);

        var dtos = convs.Select(x => {
            var isBuyer = x.BuyerUserId == userId.Value;
            var otherUser = isBuyer ? x.Listing.ResponsibleUser : x.BuyerUser;
<<<<<<< Updated upstream
            var hasRead = x.LastMessageAt is null
                || (isBuyer
                    ? (x.BuyerLastReadAt is not null && x.BuyerLastReadAt >= x.LastMessageAt)
                    : (x.ResponsibleLastReadAt is not null && x.ResponsibleLastReadAt >= x.LastMessageAt));
=======
            var hasRead = readStates.TryGetValue(x.Id, out var rs)
                ? (x.LastMessageAt is null || (rs.LastReadAt is not null && rs.LastReadAt >= x.LastMessageAt))
                : false;
>>>>>>> Stashed changes
            
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

<<<<<<< Updated upstream
        var conv = await _db.Conversations
            .Include(x => x.Listing)
            .FirstOrDefaultAsync(x => x.Id == conversationId, ct);
        if (conv is null) return Result.Fail(ErrorCodes.NotFound, "Conversation not found.");

        var now = _clock.UtcNow;
        var isBuyer = conv.BuyerUserId == userId.Value;
        var isSeller = conv.Listing.ResponsibleUserId == userId.Value;
        if (!isBuyer && !isSeller) return Result.Fail(ErrorCodes.Forbidden, "Not participant.");
        if (isBuyer) conv.BuyerLastReadAt = now; else conv.ResponsibleLastReadAt = now;
=======
        var conv = await _db.Conversations.FirstOrDefaultAsync(x => x.Id == conversationId, ct);
        if (conv is null) return Result.Fail(ErrorCodes.NotFound, "Conversation not found.");

        var lastMsg = await _db.Messages
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.SentAt)
            .FirstOrDefaultAsync(ct);

        var now = _clock.UtcNow;
        var rs = await _db.ConversationReadStates
            .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.UserId == userId.Value, ct);

        if (rs is null)
        {
            rs = new ConversationReadState
            {
                ConversationId = conversationId,
                UserId = userId.Value,
                LastReadMessageId = lastMsg?.Id,
                LastReadAt = now
            };
            _db.ConversationReadStates.Add(rs);
        }
        else
        {
            rs.LastReadMessageId = lastMsg?.Id;
            rs.LastReadAt = now;
        }
>>>>>>> Stashed changes

        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }
}
