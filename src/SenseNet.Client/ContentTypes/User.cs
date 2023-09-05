using System;
using Microsoft.Extensions.Logging;
// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace

namespace SenseNet.Client
{
    public class Avatar
    {
        public string Url { get; set; }
    }

    /// <summary>
    /// Represents a user in the sensenet repository.
    /// </summary>
    public class User : Content
    {
        public string LoginName { get; set; }

        public string DisplayName { get; set; }

        public string JobTitle { get; set; }

        public bool Enabled { get; set; }

        public string Domain { get; set; }

        public string Email { get; set; }

        public string FullName { get; set; }

        public Image ImageRef { get; set; }
        public Avatar Avatar { get; set; }
        
        public DateTime LastSync { get; set; }

        public User Manager { get; set; }
        
        public string Department { get; set; }

        public string Languages { get; set; }

        public string Phone { get; set; }

        public DateTime BirthDate { get; set; }

        public string TwitterAccount { get; set; }

        public string FacebookURL { get; set; }

        public string LinkedInURL { get; set; }

        public string ProfilePath { get; set; }

        public bool MultiFactorEnabled { get; set; }

        public User(IRestCaller restCaller, ILogger<User> logger) : base(restCaller, logger)
        {
        }
    }
}
