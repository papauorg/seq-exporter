using System.Collections.Concurrent;
using FluentAssertions;
using NUnit.Framework;
using Seq.Api.Model.Data;

namespace SeqExporter.Tests;


public class SeqControlMeterTests 
{

    public class Constructor : SeqControlMeterTests
    {
        [Test]
        public void Converts_The_Nested_Result_Dictionaries_To_Meters()
        {
            var seqResults = new SeqObservableMetrics();
            seqResults.MetricResults["metric_test"] = new Dictionary<CompositeMetricKey, int>{
                {CompositeMetricKey.FromKeyValue("Level", "Debug"), 5},
                {CompositeMetricKey.FromKeyValue("Level", "Info"), 3315},
                {CompositeMetricKey.FromKeyValue("Level", "Error"), 6},
            };
            var queries = new [] { new QueryDefinition { MetricName = "metric_test", LabelName = "level"} };
            Action create = () => new SeqControlMeter(seqResults, queries);

            create.Should().NotThrow();
        }
    }

    public class ToMeasurementsMethod : SeqControlMeterTests
    {
        [Test]
        public void Returns_Measurements_With_Labels_For_Multiple_Rows_In_The_Seq_Results()
        {
            var seqResults = new ConcurrentDictionary<string, Dictionary<CompositeMetricKey, int>>();
            seqResults["metric_test"] = new Dictionary<CompositeMetricKey, int>
            {
                {CompositeMetricKey.FromKeyValue("Level", "Debug"), 5},
                {CompositeMetricKey.FromKeyValue("Level", "Info"), 3315},
                {CompositeMetricKey.FromKeyValue("Level", "Error"), 6},
            };

            var result = SeqControlMeter.ToMeasurements(seqResults, "metric_test", "level");

            result.Should().HaveCount(3);
        }
    }

    public class ConvertValuePairs : SeqControlMeterTests
    {
        [Test]
        public void Should_Convert_QueryResultPart_To_KeyValuePairList()
        {
            var result = new QueryResultPart();
            result.Columns = new[] { "EventId", "SourceContext", "Application", "Count" };
            result.Rows = new object[][] {
                new object[] { "123456", "IdentityProvider", "DMS.NET", 5 },
                new object[] { "5387191", "HoloGraph", "SMS.NET", 8 },
                new object[] { "9845651", "DocumentService", "LAS.NET", 12 },
            };

            var expectedList = new List<KeyValuePair<CompositeMetricKey, int>> {
                new KeyValuePair<CompositeMetricKey, int>(new CompositeMetricKey(new [] {
                    new KeyValuePair<string, object>("EventId", "123456"),
                    new KeyValuePair<string, object>("SourceContext", "IdentityProvider"),
                    new KeyValuePair<string, object>("Application", "DMS.NET"),
                }), 5),
                new KeyValuePair<CompositeMetricKey, int>(new CompositeMetricKey(new [] {
                    new KeyValuePair<string, object>("EventId", "5387191"),
                    new KeyValuePair<string, object>("SourceContext", "HoloGraph"),
                    new KeyValuePair<string, object>("Application", "SMS.NET"),
                }), 8),
                new KeyValuePair<CompositeMetricKey, int>(new CompositeMetricKey(new [] {
                    new KeyValuePair<string, object>("EventId", "9845651"),
                    new KeyValuePair<string, object>("SourceContext", "DocumentService"),
                    new KeyValuePair<string, object>("Application", "LAS.NET"),
                }), 12),
            };

            var actualList = SeqBackgroundService.ConvertValuePairs(result).ToList();

            actualList.Should().BeEquivalentTo(expectedList);
        }

        [Test]
        public void Throws_Exception_If_Count_Is_Not_Last_Column_In_The_Seq_Results()
        {
            var result = new QueryResultPart();
            result.Columns = new[] { "EventId", "Count", "SourceContext", "Application" };
            result.Rows = new object[][] {
                new object[] { "123456", 5, "IdentityProvider", "DMS.NET" },
                new object[] { "5387191", 8, "HoloGraph", "SMS.NET" },
                new object[] { "9845651", 12,"DocumentService", "LAS.NET" },
            };

            Assert.Throws<FormatException>(() => SeqBackgroundService.ConvertValuePairs(result).ToList());
        }
    }
}