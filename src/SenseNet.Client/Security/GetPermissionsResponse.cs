using Newtonsoft.Json;

namespace SenseNet.Client.Security;

public class GetPermissionsResponse
{
    public class Entry
    {
        [JsonProperty("identity")]
        public Identity Identity { get; set; }
        [JsonProperty("inherits")]
        public bool Inherits { get; set; }
        [JsonProperty("permissions")]
        public PermissionRecord Permissions { get; set; }
    }

    public class Identity
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
        [JsonProperty("domain")]
        public string Domain { get; set; }
        [JsonProperty("kind")]
        public string Kind { get; set; }
        [JsonProperty("avatar")]
        public string Avatar { get; set; }
    }

    [JsonProperty("id")]
    public int Id { get; set; }
    [JsonProperty("path")]
    public string Path { get; set; }
    [JsonProperty("inherits")]
    public bool Inherits { get; set; }
    [JsonProperty("entries")]
    public Entry[] Entries { get; set; }
}

public class PermissionRecord
{
    public class PermissionValue
    {
        [JsonProperty("value")] public string Value { get; set; }
        [JsonProperty("from")] public string From { get; set; }
        [JsonProperty("identity")] public string IdentityPath { get; set; }
    }

    public PermissionValue See { get; set; }
    public PermissionValue Preview { get; set; }
    public PermissionValue PreviewWithoutWatermark { get; set; }
    public PermissionValue PreviewWithoutRedaction { get; set; }
    public PermissionValue Open { get; set; }
    public PermissionValue OpenMinor { get; set; }
    public PermissionValue Save { get; set; }
    public PermissionValue Publish { get; set; }
    public PermissionValue ForceCheckin { get; set; }
    public PermissionValue AddNew { get; set; }
    public PermissionValue Approve { get; set; }
    public PermissionValue Delete { get; set; }
    public PermissionValue RecallOldVersion { get; set; }
    public PermissionValue DeleteOldVersion { get; set; }
    public PermissionValue SeePermissions { get; set; }
    public PermissionValue SetPermissions { get; set; }
    public PermissionValue RunApplication { get; set; }
    public PermissionValue ManageListsAndWorkspaces { get; set; }
    public PermissionValue TakeOwnership { get; set; }
    public PermissionValue Unused13 { get; set; }
    public PermissionValue Unused12 { get; set; }
    public PermissionValue Unused11 { get; set; }
    public PermissionValue Unused10 { get; set; }
    public PermissionValue Unused09 { get; set; }
    public PermissionValue Unused08 { get; set; }
    public PermissionValue Unused07 { get; set; }
    public PermissionValue Unused06 { get; set; }
    public PermissionValue Unused05 { get; set; }
    public PermissionValue Unused04 { get; set; }
    public PermissionValue Unused03 { get; set; }
    public PermissionValue Unused02 { get; set; }
    public PermissionValue Unused01 { get; set; }
    public PermissionValue Custom01 { get; set; }
    public PermissionValue Custom02 { get; set; }
    public PermissionValue Custom03 { get; set; }
    public PermissionValue Custom04 { get; set; }
    public PermissionValue Custom05 { get; set; }
    public PermissionValue Custom06 { get; set; }
    public PermissionValue Custom07 { get; set; }
    public PermissionValue Custom08 { get; set; }
    public PermissionValue Custom09 { get; set; }
    public PermissionValue Custom10 { get; set; }
    public PermissionValue Custom11 { get; set; }
    public PermissionValue Custom12 { get; set; }
    public PermissionValue Custom13 { get; set; }
    public PermissionValue Custom14 { get; set; }
    public PermissionValue Custom15 { get; set; }
    public PermissionValue Custom16 { get; set; }
    public PermissionValue Custom17 { get; set; }
    public PermissionValue Custom18 { get; set; }
    public PermissionValue Custom19 { get; set; }
    public PermissionValue Custom20 { get; set; }
    public PermissionValue Custom21 { get; set; }
    public PermissionValue Custom22 { get; set; }
    public PermissionValue Custom23 { get; set; }
    public PermissionValue Custom24 { get; set; }
    public PermissionValue Custom25 { get; set; }
    public PermissionValue Custom26 { get; set; }
    public PermissionValue Custom27 { get; set; }
    public PermissionValue Custom28 { get; set; }
    public PermissionValue Custom29 { get; set; }
    public PermissionValue Custom30 { get; set; }
    public PermissionValue Custom31 { get; set; }
    public PermissionValue Custom32 { get; set; }
}
