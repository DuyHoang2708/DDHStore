using System.Text.Json;

namespace DDHSTORE.Helpers
{
    /// <summary>
    /// Extension methods để lưu/đọc object từ Session dưới dạng JSON
    /// </summary>
    public static class CartHelper
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}
