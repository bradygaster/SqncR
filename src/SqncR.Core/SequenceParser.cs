using SqncR.Core.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SqncR.Core;

public class SequenceParser
{
    private readonly IDeserializer _deserializer;

    public SequenceParser()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public Sequence Parse(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Sequence file not found: {filePath}");

        var yaml = File.ReadAllText(filePath);
        return ParseYaml(yaml);
    }

    public Sequence ParseYaml(string yaml)
    {
        var sequence = _deserializer.Deserialize<Sequence>(yaml);
        return sequence ?? new Sequence();
    }
}
