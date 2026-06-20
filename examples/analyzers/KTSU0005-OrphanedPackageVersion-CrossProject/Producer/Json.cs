namespace Producer;

using Newtonsoft.Json;

/// <summary>Uses Newtonsoft.Json so the reference is a genuine, used dependency.</summary>
public static class Json
{
	/// <summary>Serializes a value to JSON.</summary>
	/// <param name="value">The value to serialize.</param>
	/// <returns>The JSON representation.</returns>
	public static string Serialize(object value) => JsonConvert.SerializeObject(value);
}
