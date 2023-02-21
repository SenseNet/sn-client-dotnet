﻿using System.Threading.Tasks;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    public interface IRepository
    {
        public ServerContext Server { get; set; }

        public Task<Content> LoadContentAsync(int id, CancellationToken cancel);
        public Task<Content> LoadContentAsync(string path, CancellationToken cancel);
        public Task<Content> LoadContentAsync(ODataRequest requestData, CancellationToken cancel);

        public Task<T> LoadContentAsync<T>(int id, CancellationToken cancel) where T : Content;
        public Task<T> LoadContentAsync<T>(string path, CancellationToken cancel) where T : Content;
        public Task<T> LoadContentAsync<T>(ODataRequest requestData, CancellationToken cancel) where T : Content;
    }
}
