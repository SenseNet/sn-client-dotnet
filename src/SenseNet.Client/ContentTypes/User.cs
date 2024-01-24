using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public enum Gender
{
    [JsonProperty("...")]
    NotDefined,
    Female,
    Male
}

public enum MaritalStatus
{
    [JsonProperty("...")]
    NotDefined,
    Single,
    Married
}

/// <summary>
/// Represents a user in the sensenet repository.
/// </summary>
public class User : Content
{
    public User(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger)
    {
    }

    public string LoginName { get; set; }
    public string JobTitle { get; set; }
    public bool Enabled { get; set; }
    public string Domain { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public Image ImageRef { get; set; }
    public Avatar Avatar { get; set; }
    public string SyncGuid { get; set; }
    public DateTime LastSync { get; set; }
    public User Manager { get; set; }
    public string Department { get; set; }
    public string Languages { get; set; }
    public string Phone { get; set; }
    public Gender? Gender { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }
    public DateTime? BirthDate { get; set; }
    public string Education { get; set; }
    public string TwitterAccount { get; set; }
    public string FacebookURL { get; set; }
    public string LinkedInURL { get; set; }
    public string ProfilePath { get; set; }
    public bool? MultiFactorEnabled { get; set; }
    public bool? MultiFactorRegistered { get; set; }
    DateTime? LastLoggedOut { get; set; }
    public string ExternalUserProviders { get; set; }
    IEnumerable<Workspace> FollowedWorkspaces { get; set; }

    [JsonIgnore] // Read only field
    public IEnumerable<Content> AllRoles { get; set; }
    [JsonIgnore] // Read only field
    public IEnumerable<Content> DirectRoles { get; set; }

    // Not implemented properties
    //     ImageData:Binary
    //     Language:Choice
    //     Captcha:Captcha
}

public class Avatar
{
    public string Url { get; set; }
}