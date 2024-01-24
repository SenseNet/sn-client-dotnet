using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class Group : Content
{
    public IEnumerable<Content> Members { get; set; }
    public string SyncGuid { get; set; }
    public DateTime LastSync { get; set; }
    //UNDONE: Implement AllRoles property
    //AllRoles AllRoles
    //UNDONE: Implement DirectRoles property
    //DirectRoles DirectRoles

    /* ============================================================================= Constructors */

    public Group(IRestCaller restCaller, ILogger<Image> logger) : base(restCaller, logger) { }

    /* ============================================================================= Static API */

    /// <summary>
    /// Adds members to a group.
    /// </summary>
    /// <param name="groupId">Group id.</param>
    /// <param name="memberIds">Ids of members to add to the group.</param>
    /// <param name="server">Target server.</param>
    public static async Task AddMembersAsync(int groupId, int[] memberIds, ServerContext server = null)
    {
        await RESTCaller.GetResponseStringAsync(groupId, "AddMembers", HttpMethod.Post, JsonHelper.GetJsonPostModel(new
                {
                    contentIds = memberIds
                }),
                server)
            .ConfigureAwait(false);
    }
    /// <summary>
    /// Removes members from a group.
    /// </summary>
    /// <param name="groupId">Group id.</param>
    /// <param name="memberIds">Ids of members to remove from the group.</param>
    /// <param name="server">Target server.</param>
    public static async Task RemoveMembersAsync(int groupId, int[] memberIds, ServerContext server = null)
    {
        await RESTCaller.GetResponseStringAsync(groupId, "RemoveMembers", HttpMethod.Post, JsonHelper.GetJsonPostModel(new
                {
                    contentIds = memberIds
                }),
                server)
            .ConfigureAwait(false);
    }

    //============================================================================= Instance API

    /// <summary>
    /// Adds members to a group.
    /// </summary>
    /// <param name="memberIds">Ids of members to add to the group.</param>
    public async Task AddMembersAsync(int[] memberIds)
    {
        await AddMembersAsync(this.Id, memberIds, this.Server).ConfigureAwait(false);
    }
    /// <summary>
    /// Removes members from a group.
    /// </summary>
    /// <param name="memberIds">Ids of members to remove from the group.</param>
    public async Task RemoveMembersAsync(int[] memberIds)
    {
        await RemoveMembersAsync(this.Id, memberIds, this.Server).ConfigureAwait(false);
    }
}