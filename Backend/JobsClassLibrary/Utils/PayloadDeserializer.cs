using System.Text.Json;
using Microsoft.Extensions.Logging;

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
                    // Handle empty List Objcets
                    if (jsonElement[0].ValueKind == JsonValueKind.String && jsonElement[0].GetString() == "[]")
                    {
                        deserializedObject = Activator.CreateInstance<T>();
                        return true;
                    }

                    JsonElement payloadJsonObj = jsonElement[0];
                    deserializedObject = JsonSerializer.Deserialize<T>(payloadJsonObj.GetRawText(), options);

                    if (deserializedObject == null)
                    {
                        logger.LogWarning("Failed to deserialize the payload into the target type.");
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
