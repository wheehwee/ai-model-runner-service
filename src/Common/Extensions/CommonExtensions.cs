namespace Common.Extensions
{
    public static class CommonExtensions
    {
        public static TTEntity CreateDeepCopy<TTEntity>(this TTEntity obj) where TTEntity : class
        {
            if (obj == null)
            {
                return null;
            }

            var options = new System.Text.Json.JsonSerializerOptions()
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false,
                Converters = {
                    new System.Text.Json.Serialization.JsonStringEnumConverter()
                },
                IncludeFields = true
            };

            var serialized = System.Text.Json.JsonSerializer.Serialize(obj, options);

            var output = System.Text.Json.JsonSerializer.Deserialize<TTEntity>(serialized, options);

            return output;
        }
    }
}
