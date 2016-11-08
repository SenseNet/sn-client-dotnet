using Newtonsoft.Json;

namespace SenseNet.Client.Security
{
    /// <summary>
    /// Represents the possible values of permissions.
    /// </summary>
    public enum PermissionValue
    { 
        /// <summary>
        /// Not defined.
        /// </summary>
        Undefined = 0,
        /// <summary>
        /// Allow this permission.
        /// </summary>
        Allow = 1,
        /// <summary>
        /// Deny this permission.
        /// </summary>
        Deny = 2
    }

    /// <summary>
    /// Represents the data that is sent to the server during a permission request. Fill only
    /// the permission values that you want to set or change.
    /// </summary>
    public class SetPermissionRequest
    {
        // Sample serialized request:
        // {r:[{identity:"/Root/IMS/BuiltIn/Portal/Visitor", OpenMinor:"allow", Save:"deny"},{Identity:"/Root/IMS/BuiltIn/Portal/Creators", OpenMinor:"A", Save:"1"}]}

        /// <summary>
        /// Id or Path of a user, group or organizational unit.
        /// </summary>
        [JsonProperty("identity")]
        public string Identity;
        /// <summary>
        /// Whether this permission is local only or inheritable.
        /// </summary>
        [JsonProperty("localOnly")]
        public bool? LocalOnly;

        /// <summary>
        /// See permission.
        /// </summary>
        public PermissionValue? See;
        /// <summary>
        /// Preview permission.
        /// </summary>
        public PermissionValue? Preview;
        /// <summary>
        /// PreviewWithoutWatermark permission.
        /// </summary>
        public PermissionValue? PreviewWithoutWatermark;
        /// <summary>
        /// PreviewWithoutRedaction permission.
        /// </summary>
        public PermissionValue? PreviewWithoutRedaction;
        /// <summary>
        /// Open permission.
        /// </summary>
        public PermissionValue? Open;
        /// <summary>
        /// OpenMinor permission.
        /// </summary>
        public PermissionValue? OpenMinor;
        /// <summary>
        /// Save permission.
        /// </summary>
        public PermissionValue? Save;
        /// <summary>
        /// Publish permission.
        /// </summary>
        public PermissionValue? Publish;
        /// <summary>
        /// ForceCheckin permission.
        /// </summary>
        public PermissionValue? ForceCheckin;
        /// <summary>
        /// AddNew permission.
        /// </summary>
        public PermissionValue? AddNew;
        /// <summary>
        /// Approve permission.
        /// </summary>
        public PermissionValue? Approve;
        /// <summary>
        /// Delete permission.
        /// </summary>
        public PermissionValue? Delete;
        /// <summary>
        /// RecallOldVersion permission.
        /// </summary>
        public PermissionValue? RecallOldVersion;
        /// <summary>
        /// DeleteOldVersion permission.
        /// </summary>
        public PermissionValue? DeleteOldVersion;
        /// <summary>
        /// SeePermissions permission.
        /// </summary>
        public PermissionValue? SeePermissions;
        /// <summary>
        /// SetPermissions permission.
        /// </summary>
        public PermissionValue? SetPermissions;
        /// <summary>
        /// RunApplication permission.
        /// </summary>
        public PermissionValue? RunApplication;
        /// <summary>
        /// ManageListsAndWorkspaces permission.
        /// </summary>
        public PermissionValue? ManageListsAndWorkspaces;

        /// <summary>
        /// Custom01 permission.
        /// </summary>
        public PermissionValue? Custom01;
        /// <summary>
        /// Custom02 permission.
        /// </summary>
        public PermissionValue? Custom02;
        /// <summary>
        /// Custom03 permission.
        /// </summary>
        public PermissionValue? Custom03;
        /// <summary>
        /// Custom04 permission.
        /// </summary>
        public PermissionValue? Custom04;
        /// <summary>
        /// Custom05 permission.
        /// </summary>
        public PermissionValue? Custom05;
        /// <summary>
        /// Custom06 permission.
        /// </summary>
        public PermissionValue? Custom06;
        /// <summary>
        /// Custom07 permission.
        /// </summary>
        public PermissionValue? Custom07;
        /// <summary>
        /// Custom08 permission.
        /// </summary>
        public PermissionValue? Custom08;
        /// <summary>
        /// Custom09 permission.
        /// </summary>
        public PermissionValue? Custom09;
        /// <summary>
        /// Custom10 permission.
        /// </summary>
        public PermissionValue? Custom10;
        /// <summary>
        /// Custom11 permission.
        /// </summary>
        public PermissionValue? Custom11;
        /// <summary>
        /// Custom12 permission.
        /// </summary>
        public PermissionValue? Custom12;
        /// <summary>
        /// Custom13 permission.
        /// </summary>
        public PermissionValue? Custom13;
        /// <summary>
        /// Custom14 permission.
        /// </summary>
        public PermissionValue? Custom14;
        /// <summary>
        /// Custom15 permission.
        /// </summary>
        public PermissionValue? Custom15;
        /// <summary>
        /// Custom16 permission.
        /// </summary>
        public PermissionValue? Custom16;
        /// <summary>
        /// Custom17 permission.
        /// </summary>
        public PermissionValue? Custom17;
        /// <summary>
        /// Custom18 permission.
        /// </summary>
        public PermissionValue? Custom18;
        /// <summary>
        /// Custom19 permission.
        /// </summary>
        public PermissionValue? Custom19;
        /// <summary>
        ///  Custom20permission.
        /// </summary>
        public PermissionValue? Custom20;
        /// <summary>
        /// Custom21 permission.
        /// </summary>
        public PermissionValue? Custom21;
        /// <summary>
        /// Custom22 permission.
        /// </summary>
        public PermissionValue? Custom22;
        /// <summary>
        /// Custom23 permission.
        /// </summary>
        public PermissionValue? Custom23;
        /// <summary>
        /// Custom24 permission.
        /// </summary>
        public PermissionValue? Custom24;
        /// <summary>
        /// Custom25 permission.
        /// </summary>
        public PermissionValue? Custom25;
        /// <summary>
        /// Custom26 permission.
        /// </summary>
        public PermissionValue? Custom26;
        /// <summary>
        /// Custom27 permission.
        /// </summary>
        public PermissionValue? Custom27;
        /// <summary>
        /// Custom28 permission.
        /// </summary>
        public PermissionValue? Custom28;
        /// <summary>
        /// Custom29 permission.
        /// </summary>
        public PermissionValue? Custom29;
        /// <summary>
        /// Custom30 permission.
        /// </summary>
        public PermissionValue? Custom30;
        /// <summary>
        /// Custom31 permission.
        /// </summary>
        public PermissionValue? Custom31;
        /// <summary>
        /// Custom32 permission.
        /// </summary>
        public PermissionValue? Custom32;

        /// <summary>
        /// Creates a copy of this permission request object.
        /// </summary>
        /// <returns></returns>
        public SetPermissionRequest Copy()
        {
            return new SetPermissionRequest
            {
                Identity = this.Identity,
                LocalOnly = this.LocalOnly,
                See = this.See,    
                Preview = this.Preview,
                PreviewWithoutWatermark = this.PreviewWithoutWatermark,
                PreviewWithoutRedaction = this.PreviewWithoutRedaction,
                Open = this.Open,
                OpenMinor = this.OpenMinor,
                Save = this.Save,
                Publish = this.Publish,
                ForceCheckin = this.ForceCheckin,
                AddNew = this.AddNew,
                Approve = this.Approve,
                Delete = this.Delete,
                RecallOldVersion = this.RecallOldVersion,
                DeleteOldVersion = this.DeleteOldVersion,
                SeePermissions = this.SeePermissions,
                SetPermissions = this.SetPermissions,
                RunApplication = this.RunApplication,
                ManageListsAndWorkspaces = this.ManageListsAndWorkspaces,
                
                Custom01 = this.Custom01,
                Custom02 = this.Custom02,
                Custom03 = this.Custom03,
                Custom04 = this.Custom04,
                Custom05 = this.Custom05,
                Custom06 = this.Custom06,
                Custom07 = this.Custom07,
                Custom08 = this.Custom08,
                Custom09 = this.Custom09,
                Custom10 = this.Custom10,
                Custom11 = this.Custom11,
                Custom12 = this.Custom12,
                Custom13 = this.Custom13,
                Custom14 = this.Custom14,
                Custom15 = this.Custom15,
                Custom16 = this.Custom16,
                Custom17 = this.Custom17,
                Custom18 = this.Custom18,
                Custom19 = this.Custom19,
                Custom20 = this.Custom20,
                Custom21 = this.Custom21,
                Custom22 = this.Custom22,
                Custom23 = this.Custom23,
                Custom24 = this.Custom24,
                Custom25 = this.Custom25,
                Custom26 = this.Custom26,
                Custom27 = this.Custom27,
                Custom28 = this.Custom28,
                Custom29 = this.Custom29,
                Custom30 = this.Custom30,
                Custom31 = this.Custom31,
                Custom32 = this.Custom32
            };
        }
    }
}
