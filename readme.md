# seq-exporter
This tool takes configurable queries and exports their results as prometheus compatible metrics.

## Queries
For the seq-exporter to provide metrics it is necessary to configure the queries that should be sent to the Seq server. You can do that by using the ASP.NET configuration mechanisms for the following configuration values. The `Queries` configuration value can take a list of queries that are handled individually and will be made available to the `/metrics` endpoint.

```json
  "Queries": [
    {
      "Query": "select count(*) as count from stream group by @Level", 
      "Signal": "signal-1,signal-2",
      "MetricName": "seqexporter_count_by_level", 
      "MetricUnit": "messages",
      "MetricDescription": "Shows the amount of incidents depending on their level.",
      "LabelName": "level"
    }
```

You can combine Signals and Queries the same way as in the Seq Events overview. Multiple Signals should be separated by `,`.

The `LabelName` can be used to set different labels for the same metric based on multiple result rows of the query.

## Metrics endpoint
The default metrics endpoint is available via `/metrics` and contains the results of the queries if any.

### Sample output
```text
# TYPE seqexporter_count_by_level gauge
# UNIT seqexporter_count_by_level messages
# HELP seqexporter_count_by_level Shows the amount of error messages per endpoint that require manual review.
seqexporter_count_by_level{level="Debug"} 3 1675784613469
seqexporter_count_by_level{level="Info"} 1 1675784613469

# EOF
```

## Limitations
- Authentication for the provided metrics endpoint is not implemented.