using System.Collections.Concurrent;

using FluentAssertions;

using SeqExporter.Metrics;

namespace SeqExporter.Tests;

public class SeqControlMeterTests
{

    public class Constructor : SeqControlMeterTests
    {
        [Test]
        public void Converts_The_Nested_Result_Dictionaries_To_Meters()
        {
            var seqResults = new SeqObservableMetrics();
            seqResults.MetricResults["metric_test"] = new Dictionary<string, int>{
                {"Debug", 5},
                {"Info", 3315},
                {"Error", 6}
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
            var seqResults = new ConcurrentDictionary<string, Dictionary<string, int>>();
            seqResults["metric_test"] = new Dictionary<string, int>
            {
                {"Debug", 5},
                {"Info", 3315},
                {"Error", 6}
            };

            var result = SeqControlMeter.ToMeasurements(seqResults, "metric_test", "level");

            result.Should().HaveCount(3);
        }
    }
}