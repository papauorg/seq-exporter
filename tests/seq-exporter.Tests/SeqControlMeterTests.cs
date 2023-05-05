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
            var queries = new[] { new QueryDefinition { MetricName = "metric_test" } };
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

            var result = SeqControlMeter.ToMeasurements(seqResults, new QueryDefinition { MetricName = "metric_test" });

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
                new object[] { "123456", "IdentityProvider", "Documents", 5 },
                new object[] { "5387191", "MainApplication", "Mails", 8 },
                new object[] { "9845651", "SupportingApplication", "Tasks", 12 },
                new object[] { "4545454", "Some\r\nNewline\r\nContext", "VeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryLongApplication", 5},
                new object[] { "", null!, "  t", 1}
            };

            var expectedList = new List<KeyValuePair<CompositeMetricKey, int>> {
                new KeyValuePair<CompositeMetricKey, int>(new CompositeMetricKey(new [] {
                    new KeyValuePair<string, object>("EventId", "123456"),
                    new KeyValuePair<string, object>("SourceContext", "IdentityProvider"),
                    new KeyValuePair<string, object>("Application", "Documents"),
                }), 5),
                new KeyValuePair<CompositeMetricKey, int>(new CompositeMetricKey(new [] {
                    new KeyValuePair<string, object>("EventId", "5387191"),
                    new KeyValuePair<string, object>("SourceContext", "MainApplication"),
                    new KeyValuePair<string, object>("Application", "Mails"),
                }), 8),
                new KeyValuePair<CompositeMetricKey, int>(new CompositeMetricKey(new [] {
                    new KeyValuePair<string, object>("EventId", "9845651"),
                    new KeyValuePair<string, object>("SourceContext", "SupportingApplication"),
                    new KeyValuePair<string, object>("Application", "Tasks"),
                }), 12),
                new KeyValuePair<CompositeMetricKey, int>(new CompositeMetricKey(new [] {
                    new KeyValuePair<string, object>("EventId", "4545454"),
                    new KeyValuePair<string, object>("SourceContext", "Some"),
                    new KeyValuePair<string, object>("Application", "VeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVeryVery"),
                }), 5),
                new KeyValuePair<CompositeMetricKey, int>(new CompositeMetricKey(new [] {
                    new KeyValuePair<string, object>("EventId", ""),
                    new KeyValuePair<string, object>("SourceContext", null!),
                    new KeyValuePair<string, object>("Application", "t"),
                }), 1),
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
                new object[] { "123456", 5, "IdentityProvider", "Documents" },
                new object[] { "5387191", 8, "MainApplication", "Mails" },
                new object[] { "9845651", 12,"SupportingApplications", "Other" },
            };

            Assert.Throws<FormatException>(() => SeqBackgroundService.ConvertValuePairs(result).ToList());
        }
    }
}