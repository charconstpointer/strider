using System.Collections.Generic;

namespace Strider.Messaging
{
    public class Tick
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public IEnumerable<byte> Payload { get; set; }
    }
}