namespace SmartEstate.App.Common.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
