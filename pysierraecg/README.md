# sierra-ecg-tools

Sierra ECG Tools for Python.

# Requirements

Python 3.9+.

## Runtime Dependencies

- defusedxml
- numpy

# Example
```python
from sierraecg import read_file

# 12-Lead Data
f = read_file('path/to/file.xml')
for lead in f.leads:
    print(f"{lead.label}: dur={lead.duration} f={lead.sampling_freq} {lead.samples[0:8]}...")

# 12-Lead Data + Representative Beats
f = read_file('path/to/file.xml', include_repbeats=True)
for repbeat in f.repbeats:
    print(f"{repbeat.label}: dur={repbeat.duration} f={repbeat.sampling_freq} {repbeat.samples[0:8]}...")
```
