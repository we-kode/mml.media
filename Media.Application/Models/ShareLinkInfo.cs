using System;

namespace Media.Application.Models;

/// <summary>
/// Describes an active share link without its secret token.
/// </summary>
/// <param name="Id">Id of the link.</param>
/// <param name="CreatedAt">Date when the link was created.</param>
/// <param name="ExpiresAt">Date when the link expires.</param>
/// <param name="SingleUse">True, if the link can be used only once.</param>
/// <param name="RecordCount">Number of the records of the link.</param>
public record ShareLinkInfo(Guid Id, DateTime CreatedAt, DateTime ExpiresAt, bool SingleUse, int RecordCount);
