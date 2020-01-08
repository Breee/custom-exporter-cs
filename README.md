# custom-exporter-cs

This is a Custom prometheus exporter, which extracts custom metrics.
Custom metrics are configured in json format. 
Currently we support API calls and execution of scripts.
Currently only Prometheus Gauge measurements are supported. Summary, Histogram and Counter not.

## requirements
- .NET core 3.1

## metric_definition.json
A metric definition is a json file of the form:
```json
[
  {
    "service_name": "grafana",
    "url": "http://grafana",
    "metrics": [
      {
        "metric_name": "database_health",
        "execution_type": "api_call",
        "api_endpoint": "/api/health",
        "desired_response_field": "database",
        "string_value_mapping": {
          "ok": 1.0,
          "not ok": 0.0
        },
        "labels": {
          "instance": "grafana",
          "custom_label": "some_label"
        }
      },
      {
        "metric_name": "custom_output",
        "execution_type": "script",
        "program": "python",
        "argument": "custom_script.py"
      },
      {
        "metric_name": "custom_bash_command",
        "execution_type": "script",
        "program": "/bin/bash",
        "argument": "-c \"echo 1\""
      },
      {
        "metric_name": "custom_script2",
        "execution_type": "script",
        "program": "/bin/bash",
        "argument": "test.sh"
      }
    ]
  }
]
```
It contains a set of json objects, where we define metrics for each service.

## Services

We define a service as follows: 
```json
{
    "service_name": "grafana",
    "url": "http://grafana",
    "auth_credentials": {...},
    "metrics": [...]
  }
```
- `service_name` defines the name of our service
- `url` defines the host of the service
- `auth_credentials` is a json dict which contains `token` or `username` and `password`.
- `metrics` is a list of metrics that belong to a service.

## Auth credentials:
- Username + Password, passed as Basic auth in the Authorization Header. (as Base64 encoded bytearray username:password)
```json
"auth_credentials": {
    "username" : "user",
    "password" : "thePassword1337"
}
```
- Token, passed as Bearer token in the Authorization Header.
```json
"auth_credentials": {
    "token": "xxxxxxxxx"
}
```

## Metrics
A `metric` can be an API call: 
```json
    {
            "metric_name": "database_health",
            "execution_type": "api_call",
            "api_endpoint": "/api/health",
            "desired_response_field": "database",
            "string_value_mapping": {
              "ok": 1.0,
              "not ok": 0.0
            },
            "labels": {
              "instance": "grafana",
              "custom_label": "some_label"
            }
    }
```
- `metric_name` is the name of the metric
- `execution_type` specifies how the metric is scraped, you can use either `api_call` or `script`
- `api_endpoint` is the API endpoint on which we perfom a GET request,
      the whole adress consists of `url` and `api_endpoint` combined, e.g: `http://grafana/api/health`
- `desired_response_field` is the field in the response body we want to extract, for example, the response could be:
    ```json
    {
     "commit": "092e514",
     "database": "ok",
     "version": "6.4.4"
    }
    ```
    in our example we want the `database` field.
- `string_value_mapping` is a mapping of strings to doubles, 
      this is useful if a `desired_response_field` contains a string like the example above, `"ok"`.

- `labels` is a mapping of strings to strings, where keys are label_names and values are label_values:
    ```json
    "labels": {
                  "instance": "grafana",
                  "custom_label": "some_label"
              }
    ```

Another example for a metric is the execution of a python or bash script/command:
```json
{
  "metric_name": "custom_output",
  "execution_type": "script",
  "program": "python",
  "argument": "custom_script.py"
},
{
  "metric_name": "custom_bash_command",
  "execution_type": "script",
  "program": "/bin/bash",
  "argument": "-c \"echo 1\""
},
{
  "metric_name": "custom_script2",
  "execution_type": "script",
  "program": "/bin/bash",
  "argument": "test.sh"
}
```
- `program` is the main application we execute
- `argument` is the script or command we execute.


## Docker
You can use the following docker-compose definition:
```yml
  customexp:
    build:
      context: ./custom-exporter-cs
    volumes:
      - ./custom-exporter-cs/metric_definition.json:/app/metric_definition.json
```
If you need specific tools, you have to alter the `Dockerfile`

## Publishing and execution of the application:
```
dotnet publish -c .\custom_exporter.csproj -o out
```
Execute the app: 
```
dotnet ./out/custom_exporter.dll
```

A HTTP server will be started, and the metrics exposed at `localhost:8888/metrics`

Custom metrics are named like this: `<service_name>_<metric_name>`, e.g. the `database_health` metric we defined in the `grafana` service, is exported as `grafana_database_health`.