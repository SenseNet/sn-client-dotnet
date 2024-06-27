using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Client.Security
{
    /// <summary>
    /// Static API for permission-related operations.
    /// </summary>
    public static class SecurityManager
    {
        internal static readonly string SETPERMISSIONS = "SetPermissions";

        #region Obsolete
        /// <summary>
        /// Sets the provided permissions on the provided content.
        /// </summary>
        /// <param name="contentId">Id of a content.</param>
        /// <param name="permissions">Permission settings to be sent to the server.</param>
        /// <param name="server">Target server.</param>
        [Obsolete("Use SetPermissionsAsync(int, SetPermissionRequest[], IRepository, CancellationToken) overload instead.", true)]
        public static Task SetPermissionsAsync(int contentId, SetPermissionRequest[] permissions, ServerContext server = null) => throw new NotSupportedException();

        /// <summary>
        /// Checks whether a user has the provided permissions on the provided content.
        /// </summary>
        /// <param name="contentId">Id of a content.</param>
        /// <param name="permissions">Permission names to check.</param>
        /// <param name="user">The user who's permissions need to be checked. If it is not provided, the server checks the current user.</param>
        /// <param name="server">Target server.</param>
        [Obsolete("Use HasPermissionAsync(int, string[], string?, IRepository, CancellationToken) overload instead.", true)]
        public static Task<bool> HasPermissionAsync(int contentId, string[] permissions, string? user = null, ServerContext? server = null) => throw new NotSupportedException();

        /// <summary>
        /// Breaks permissions on the provided content.
        /// </summary>
        /// <param name="contentId">Id of a content.</param>
        /// <param name="server">Target server.</param>
        [Obsolete("Use BreakInheritanceAsync(int, IRepository, CancellationToken) overload instead.", true)]
        public static Task BreakInheritanceAsync(int contentId, ServerContext? server = null) => throw new NotSupportedException();

        /// <summary>
        /// Removes permission break on the provided content.
        /// </summary>
        /// <param name="contentId">Id of a content.</param>
        /// <param name="server">Target server.</param>
        [Obsolete("Use UnbreakInheritanceAsync(int, IRepository, CancellationToken) overload instead.", true)]
        public static Task UnbreakInheritanceAsync(int contentId, ServerContext? server = null) => throw new NotSupportedException();

        #endregion

        /// <summary>
        /// Sets the provided permissions on the provided content.
        /// </summary>
        /// <param name="contentId">Id of a content.</param>
        /// <param name="permissions">Permission settings to be sent to the server.</param>
        /// <param name="repository">Target repository</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents an asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the <paramref name="permissions"/> parameter is null or empty.</exception>
        public static async Task SetPermissionsAsync(int contentId, SetPermissionRequest[] permissions, IRepository repository, CancellationToken cancel)
        {
            if (permissions == null || permissions.Length == 0)
                throw new InvalidOperationException("Please provide at least one permission entry.");


            await repository.InvokeActionAsync<string>(new OperationRequest
            {
                ContentId = contentId,
                OperationName = SETPERMISSIONS,
                PostData = new {r = permissions}
            }, cancel).ConfigureAwait(false);
        }

        /// <summary>
        /// Checks whether a user has the provided permissions on the provided content.
        /// </summary>
        /// <param name="contentId">Id of a content.</param>
        /// <param name="permissions">Permission names to check.</param>
        /// <param name="user">The user who's permissions need to be checked. If it is not provided (null), the server checks the current user.</param>
        /// <param name="repository">Target repository</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents an asynchronous operation and wraps a boolean value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the <paramref name="permissions"/> parameter is null or empty.</exception>
        public static async Task<bool> HasPermissionAsync(int contentId, string[] permissions, string? user, IRepository repository, CancellationToken cancel)
        {
            if (permissions == null || permissions.Length == 0)
                throw new InvalidOperationException("Please provide at least one permission entry.");

            var requestData = new ODataRequest(repository.Server)
            {
                ContentId = contentId,
                ActionName = "HasPermission",
                Permissions = permissions,
                User = user
            };

            var result = await repository.GetResponseStringAsync(requestData, HttpMethod.Get, cancel).ConfigureAwait(false);

            if (bool.TryParse(result, out var hasPermission))
                return hasPermission;
            return false;
        }

        /// <summary>
        /// Breaks permissions on the provided content.
        /// </summary>
        /// <param name="contentId">Id of a content.</param>
        /// <param name="repository">Target repository</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents an asynchronous operation.</returns>
        public static async Task BreakInheritanceAsync(int contentId, IRepository repository, CancellationToken cancel)
        {
            await repository.InvokeActionAsync<string>(new OperationRequest
            {
                ContentId = contentId,
                OperationName = SETPERMISSIONS,
                PostData = new { inheritance = "break" }
            }, cancel).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes permission break on the provided content.
        /// </summary>
        /// <param name="contentId">Id of a content.</param>
        /// <param name="repository">Target repository</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents an asynchronous operation.</returns>
        public static async Task UnbreakInheritanceAsync(int contentId, IRepository repository, CancellationToken cancel)
        {
            await repository.InvokeActionAsync<string>(new OperationRequest
            {
                ContentId = contentId,
                OperationName = SETPERMISSIONS,
                PostData = new { inheritance = "unbreak" }
            }, cancel).ConfigureAwait(false);
        }
    }
}
