using System.Text.Json;
using System.Text.Json.Serialization;

namespace Promissio.Domain.ValueObjects.Converters;

/// <summary>
/// JSON converter for the Money value object.
/// Serializes Money as an object with "amount" and "currency" properties.
/// </summary>
public sealed class MoneyConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        Decimal amount = root.GetProperty("amount").GetDecimal();
        string currency = root.GetProperty("currency").GetString() ?? throw new JsonException("Currency is required.");

        return new Money(amount, currency);
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.Currency);
        writer.WriteEndObject();
    }
}
