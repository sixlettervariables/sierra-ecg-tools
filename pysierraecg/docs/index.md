# `sierraecg` User Guide

!!! info

    For more information on how this was built and deployed, as well as other Python best
    practices, see [`python-blueprint`](https://github.com/johnthagen/python-blueprint).

## Installation

Install the `sierraecg` package using pip:

```bash
pip install sierraecg
```

## Quick Start

To use `sierraecg` within your project, import the `read_file` function and execute it like:

```python
from sierraecg import read_file

# (1)
f = read_file("path/to/file.xml")
assert f is not None
```

1. This assertion will be `True`

!!! tip

    Within PyCharm, use ++tab++ to auto-complete suggested imports while typing.

## Contributing
First, create and activate a Python virtual environment:

=== "Linux/macOS"

    ```bash
    python3 -m venv venv
    source venv/bin/activate
    ```

=== "Windows"

    ```powershell
    py -m venv venv
    venv\Scripts\activate
    ```

Next install the bootstrap requirements:

=== "Linux/macOS"

    ```bash
    pip install -r bootstrap-requirements.txt
    ```

=== "Windows"

    ```powershell
    pip install -r bootstrap-requirements.txt
    ```

Next create local requirements files:

=== "Linux/macOS"

    ```bash
    pip-compile --extra=dev --output-file=dev-requirements.txt pyproject.toml
    pip-compile --output-file=requirements.txt pyproject.toml
    ```

=== "Windows"

    ```powershell
    pip-compile --extra=dev --output-file=dev-requirements.txt pyproject.toml
    pip-compile --output-file=requirements.txt pyproject.toml
    ```
