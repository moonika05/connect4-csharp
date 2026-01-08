using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Helpers
{
    public static class SessionExtensions
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            Converters = { new MultiDimensionalArrayConverter() }
        };

        public static void SetObject<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value, _options));
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value, _options);
        }
    }
    
    // Copy the MultiDimensionalArrayConverter from JsonRepository
    public class MultiDimensionalArrayConverter : JsonConverter<int[,]>
    {
        public override int[,]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jaggedArray = JsonSerializer.Deserialize<int[][]>(ref reader, options);
            if (jaggedArray == null) return null;
            
            int rows = jaggedArray.Length;
            int cols = jaggedArray[0].Length;
            var result = new int[rows, cols];
            
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[i, j] = jaggedArray[i][j];
            
            return result;
        }
        
        public override void Write(Utf8JsonWriter writer, int[,] value, JsonSerializerOptions options)
        {
            int rows = value.GetLength(0);
            int cols = value.GetLength(1);
            var jaggedArray = new int[rows][];
            
            for (int i = 0; i < rows; i++)
            {
                jaggedArray[i] = new int[cols];
                for (int j = 0; j < cols; j++)
                    jaggedArray[i][j] = value[i, j];
            }
            
            JsonSerializer.Serialize(writer, jaggedArray, options);
        }
    }
}
