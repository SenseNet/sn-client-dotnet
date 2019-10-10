using System.Net.Http;
using System.Threading.Tasks;

namespace SenseNet.Client
{
    /// <summary>
    /// Specialized client content for handling group-related operations.
    /// </summary>
    public class Group : Content
    {
        //============================================================================= Constructors

        /// <summary>
        /// Initializes an instance of a Group.
        /// </summary>
        /// <param name="server"></param>
        protected Group(ServerContext server) : base(server) { }

        //============================================================================= Static API

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
}
