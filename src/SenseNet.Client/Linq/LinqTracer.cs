using System.Text;

namespace SenseNet.Client.Linq
{
    public interface ILinqTracer
    {
        string Trace { get; }
        void AddTrace(string text);
    }

    public class LinqTracer : ILinqTracer
    {
        private readonly StringBuilder _sb = new StringBuilder();
        public string Trace => _sb.ToString();
        public void AddTrace(string text) => _sb.AppendLine(text);
    }
}
