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
                if (payload is JsonElement jsonElement)
                {
                    JsonElement elementToDeserialize = jsonElement;

                    if (jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        if (jsonElement.GetArrayLength() == 0)
                        {
                            logger.LogWarning("Payload array is empty.");
                            return false;
                        }

                        elementToDeserialize = jsonElement[0];
                    }

                    if (elementToDeserialize.ValueKind == JsonValueKind.Object)
                    {
                        deserializedObject = JsonSerializer.Deserialize<T>(elementToDeserialize.GetRawText(), options);
                    }
                    else if (elementToDeserialize.ValueKind == JsonValueKind.String)
                    {
                        string? innerJson = elementToDeserialize.GetString();

                        if (string.IsNullOrWhiteSpace(innerJson))
                        {
                            logger.LogWarning("Payload string is empty.");
                            return false;
                        }

                        deserializedObject = JsonSerializer.Deserialize<T>(innerJson, options);
                    }
                    else
                    {
                        logger.LogWarning("Unsupported ValueKind: {ValueKind}", elementToDeserialize.ValueKind);
                        return false;
                    }

                    if (deserializedObject == null)
                    {
                        logger.LogWarning("Deserialization returned null.");
                        return false;
                    }

                    return true;
                }

                logger.LogWarning("Payload is not a JsonElement. Type: {PayloadType}", payload.GetType().FullName);
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
