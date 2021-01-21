
using System.Collections.Generic;
using System.Linq;

namespace Wheeler.PictureAnalyzer
{
    class InMemoryObject
    {
        public IEnumerable<string> Headers { get; set; }
        public byte[] Data { get; set; }

        public override string ToString()
        {
            return string.Join("\n", Headers.Select(i => $"Header: {i}").ToList());
        }
    }
}
