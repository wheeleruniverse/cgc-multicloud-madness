
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
            List<string> response = Headers.Select(i => $"Header: {i}").ToList();
            response.Add($"Bytes : {Data.Length}");

            return string.Join("\n", response);
        }
    }
}
