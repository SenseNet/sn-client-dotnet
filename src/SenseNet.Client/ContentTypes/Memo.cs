using Microsoft.Extensions.Logging;
using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class Memo : ListItem
{
    public DateTime Date { get; set; }
    //UNDONE: Implement MemoType property
    //public Choice MemoType { get; set; } 
    public Content SeeAlso { get; set; }

    public Memo(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}