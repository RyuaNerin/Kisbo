using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Kisbo.Core
{
    [DataContract]
    internal class RGMeta
    {
        private static readonly XmlObjectSerializer Serializer = new DataContractJsonSerializer(typeof(RGMeta));
        public static RGMeta Parse(string json)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(json)))
                return (RGMeta)Serializer.ReadObject(stream);
        }

        [DataMember(Name = "ou")]
        public Uri ImageUrl { get; set; }

        [DataMember(Name = "ow")]
        public int Width { get; set; }

        [DataMember(Name = "oh")]
        public int Height { get; set; }
    }
}
