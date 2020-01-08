# custom-exporter-cs

This is a Custom prometheus exporter, which extracts custom metrics, 
custom metrics are configured in json format. Currently we support API calls and execution of scripts, with the limitation that only double values are accepted.

## requirements
- .NET core 3.1

## metric_definition.json
A metric definition is a json file of the form:
```json
[
  {
    "service_name": "grafana",
    "url": "http://grafana",
    "request_headers": [],
    "metrics": [
      {
        "metric_name": "database_health",
        "api_endpoint": "/api/health",
        "desired_response_field": "database",
        "string_value_mapping": {
          "ok": 1.0,
          "not ok": 0.0
        }
      },
      {
        "metric_name": "custom_output",
        "program": "python",
        "argument": "custom_script.py"
      }
    ]
  }
]
```
We define a json array which contains different definitions: 

```json
{
    "service_name": "grafana",
    "url": "http://grafana",
    "metrics": [
      {
        "metric_name": "database_health",
        "api_endpoint": "/api/health",
        "desired_response_field": "database",
        "string_value_mapping": {
          "ok": 1.0,
          "not ok": 0.0
        }
      },
      {
        "metric_name": "custom_output",
        "program": "python",
        "argument": "custom_script.py"
      },
      {
      "metric_name": "custom_bash_command",
      "program": "/bin/bash",
      "argument": "-c \"echo 1\""
      },
      {
      "metric_name": "custom_script2",
      "program": "/bin/bash",
      "argument": "test.sh"
      }
    ]
  }
```
- `service_name` defines the name of our service
- `url`defines the host of the service
- `metrics` is a list of metrics that belog to a service, 
a metric can be an API call: 
    ```json
    {
            "metric_name": "database_health",
            "api_endpoint": "/api/health",
            "desired_response_field": "database",
            "string_value_mapping": {
              "ok": 1.0,
              "not ok": 0.0
            }
    }
    ```
    - `metric_name` is the name of the metric
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

    Another example for a metric is the execution of a script:
    ```json
     {
        "metric_name": "custom_output",
        "program": "python",
        "argument": "custom_script.py"
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
