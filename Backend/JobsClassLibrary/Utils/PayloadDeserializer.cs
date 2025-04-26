using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JobsClassLibrary.Utils
{
    public static class PayloadDeserializer
    {
        private static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static bool TryParsePayloadDynamic<T>(object payload, ILogger logger, out T? deserializedObject) where T : class
        {
            deserializedObject = null;

            try
            {
                if (payload is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array && jsonElement.GetArrayLength() > 0)
                {
                    var firstElement = jsonElement[0];

                    if (firstElement.ValueKind == JsonValueKind.String)
                    {
                        var jsonString = firstElement.GetString();

                        if (string.IsNullOrWhiteSpace(jsonString))
                        {
                            logger.LogWarning("Payload contains an empty string.");
                            return false;
                        }
                        if (jsonString == "[]")
                        {
                            deserializedObject = Activator.CreateInstance<T>();
                            return true;
                        }

                        deserializedObject = JsonSerializer.Deserialize<T>(jsonString, options);
                    }
                    else
                    {
                        deserializedObject = JsonSerializer.Deserialize<T>(firstElement.GetRawText(), options);
                    }

                    if (deserializedObject == null)
                    {
                        logger.LogWarning("Deserialization returned null.");
                        return false;
                    }

                    return true;
                }

                logger.LogWarning("Invalid payload format.");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse payload: {Payload}", payload);
                return false;
            }
        }

    }
}
