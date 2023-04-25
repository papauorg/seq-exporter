namespace SeqExporter;

public class CompositeMetricKey : List<KeyValuePair<string, object>>
{
    public CompositeMetricKey() { }

    public CompositeMetricKey(IEnumerable<KeyValuePair<string, object>> collection) : base(collection) { }

    public static CompositeMetricKey FromKeyValue(string key, object value)
    {
        return new CompositeMetricKey { new(key, value) };
    }
}