using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class Group : Content
{
    public IEnumerable<Content>? Members { get; set; }
    public string? SyncGuid { get; set; }
    public DateTime? LastSync { get; set; }
    [JsonIgnore] // Read only field
    public IEnumerable<Content>? AllRoles { get; set; }
    [JsonIgnore] // Read only field
    public IEnumerable<Content>? DirectRoles { get; set; }

    /* ============================================================================= Constructors */

    public Group(IRestCaller restCaller, ILogger<Image> logger) : base(restCaller, logger) { }

    /* ============================================================================= Static API */

    /// <summary>
    /// Adds members to a group.
    /// </summary>
    /// <param name="groupId">Group id.</param>
    /// <param name="memberIds">Ids of members to add to the group.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use AddMembersAsync instance method", true)]
    public static async Task AddMembersAsync(int groupId, int[] memberIds, ServerContext server = null)
    {
        throw new NotSupportedException();
    }
    /// <summary>
    /// Removes members from a group.
    /// </summary>
    /// <param name="groupId">Group id.</param>
    /// <param name="memberIds">Ids of members to remove from the group.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use RemoveMembersAsync instance method", true)]
    public static async Task RemoveMembersAsync(int groupId, int[] memberIds, ServerContext server = null)
    {
        throw new NotSupportedException();
    }

    //============================================================================= Instance API

    /// <summary>
    /// Adds members to a group.
    /// </summary>
    /// <param name="memberIds">Ids of members to add to the group.</param>
    /// <param name="cancel">Optional cancellation token.</param>
    public async Task AddMembersAsync(int[] memberIds, CancellationToken cancel = default)
    {
        await Repository.InvokeActionAsync(new OperationRequest
        {
            ContentId = this.Id,
            OperationName = "AddMembers",
            PostData = new {contentIds = memberIds},
        }, cancel).ConfigureAwait(false);
    }
    /// <summary>
    /// Removes members from a group.
    /// </summary>
    /// <param name="memberIds">Ids of members to remove from the group.</param>
    /// <param name="cancel">Optional cancellation token.</param>
    public async Task RemoveMembersAsync(int[] memberIds, CancellationToken cancel = default)
    {
        await Repository.InvokeActionAsync(new OperationRequest
        {
            ContentId = this.Id,
            OperationName = "RemoveMembers",
            PostData = new { contentIds = memberIds },
        }, cancel).ConfigureAwait(false);
    }
}