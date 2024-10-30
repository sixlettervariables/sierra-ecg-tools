# sierra-ecg-tools

Sierra ECG Tools for Python.

# Requirements

Python 3.9+.

# Dependencies

Dependencies are defined in:

- [`pyproject.toml`](./pyproject.toml)

# Example
```python
from sierraecg import read_file

f = read_file('path/to/file.xml')
for lead in f.leads:
    print(f"{lead.label}: dur={lead.duration} f={lead.sampling_freq} {lead.samples[0:8]}...")
```
