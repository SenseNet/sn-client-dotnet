namespace SenseNet.Client
{
    internal static class Constants
    {
        public static class Repository
        {
            public static readonly string RootPath = "/Root";
            public static readonly int RootId = 2;
        }

        public static class User
        {
            public static readonly int AdminId = 1;
            public static readonly string AdminPath = "/Root/IMS/BuiltIn/Portal/Admin";
            public static readonly int VisitorId = 6;
            public static readonly string VisitorPath = "/Root/IMS/BuiltIn/Portal/Visitor";
        }

        public static class Group
        {
            public static readonly string AdministratorsPath = "/Root/IMS/BuiltIn/Portal/Administrators";
        }

        public static class HttpClientName
        {
            public static readonly string Trusted = "Trusted";
            public static readonly string Untrusted = "Untrusted";
        }
    }
}
