using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SenseNet.Client.Security
{
    /// <summary>
    /// Static API for permission-related operations.
    /// </summary>
    public static class SecurityManager
    {
        internal static readonly string SETPERMISSIONS = "SetPermissions";

        /// <summary>
        /// Sets the provided permissions on the provided content.
        /// </summary>
        /// <param name="contentId">Id of a content.</param>
        /// <param name="permissions">Permission settings to be sent to the server.</param>
        /// <param name="server">Target server.</param>
        public static async Task SetPermissionsAsync(int contentId, SetPermissionRequest[] permissions, ServerContext server = null)
        {
            if (permissions == null || permissions.Length == 0)
                throw new InvalidOperationException("Please provide at least one permission entry.");

            await RESTCaller.GetResponseStringAsync(contentId, SETPERMISSIONS, HttpMethod.Post, JsonHelper.Serialize(new
            {
                r = permissions
            }),
            server)
            .ConfigureAwait(false);
        }

        /// <summary>
        /// Checks whether a user has the provided permissions on the provided content.
        /// </summary>
        /// <param name="contentId">Id of a content.</param>
        /// <param name="permissions">Permission names to check.</param>
        /// <param name="user">The user who's permissions need to be checked. If it is not provided, the server checks the current user.</param>
        /// <param name="server">Target server.</param>
        public static async Task<bool> HasPermissionAsync(int contentId, string[] permissions, string user = null, ServerContext server = null)
        {
            if (permissions == null || permissions.Length == 0)
                throw new InvalidOperationException("Please provide at least one permission entry.");

            var requestData = new ODataRequest(server)
            {
                ContentId = contentId,
                ActionName = "HasPermission"
            };

            foreach (var permission in permissions)
                requestData.Parameters.Add(new KeyValuePair<string, string>("permissions", permission));

            if (!string.IsNullOrEmpty(user))
                requestData.Parameters.Add(new KeyValuePair<string, string>("user", user));

            var result = await RESTCaller.GetResponseStringAsync(requestData.GetUri(), server).ConfigureAwait(false);

            bool hasPermission;
            if (bool.TryParse(result, out hasPermission))
                return hasPermission;

            return false;
        }

        /// <summary>
        /// Breaks permissions on the provided content.
        /// </summary>
        /// <param name="contentId">Id of a content.</param>
        /// <param name="server">Target server.</param>
        public static async Task BreakInheritanceAsync(int contentId, ServerContext server = null)
        {
            await RESTCaller.GetResponseStringAsync(contentId, SETPERMISSIONS, HttpMethod.Post, JsonHelper.Serialize(new
            {
                inheritance = "break"
            }),
            server)
            .ConfigureAwait(false);
        }

        /// <summary>
        /// Removes permission break on the provided content.
        /// </summary>
        /// <param name="contentId">Id of a content.</param>
        /// <param name="server">Target server.</param>
        public static async Task UnbreakInheritanceAsync(int contentId, ServerContext server = null)
        {
            await RESTCaller.GetResponseStringAsync(contentId, SETPERMISSIONS, HttpMethod.Post, JsonHelper.Serialize(new
            {
                inheritance = "unbreak"
            }),
            server)
            .ConfigureAwait(false);
        }
    }
}
