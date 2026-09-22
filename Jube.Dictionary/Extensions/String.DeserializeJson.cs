// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Runtime.Serialization.Json;
using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string? json)
        {
            public T? DeserializeJson<T>()
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    return default;
                }

                try
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));

                    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    var result = serializer.ReadObject(stream);
                    return result is T typed ? typed : default;
                }
                catch
                {
                    return default;
                }
            }

            public T? DeserializeJson<T>(Encoding? encoding)
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    return default;
                }

                encoding ??= Encoding.UTF8;

                try
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));

                    using var stream = new MemoryStream(encoding.GetBytes(json));
                    var result = serializer.ReadObject(stream);
                    return result is T typed ? typed : default;
                }
                catch
                {
                    return default;
                }
            }
        }
    }
}
