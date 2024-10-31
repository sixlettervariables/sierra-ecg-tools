#!/usr/bin/env python3

from pathlib import Path

import setuptools

project_dir = Path(__file__).parent

setuptools.setup(
    name="sierraecg",
    version="0.4.0",
    description="Sierra ECG Tools for Python",
    # Use UTF-8 encoding for README even on Windows by using the encoding argument.
    long_description=project_dir.joinpath("README.md").read_text(encoding="utf-8"),
    long_description_content_type="text/markdown",
    keywords=["python"],
    author="",
    url="https://github.com/sixlettervariables/sierra-ecg-tools",
    packages=setuptools.find_packages("src"),
    package_dir={"": "src"},
    # pip 9.0+ will inspect this field when installing to help users install a
    # compatible version of the library for their Python version.
    python_requires=">=3.9",
    # There are some peculiarities on how to include package data for source
    # distributions using setuptools. You also need to add entries for package
    # data to MANIFEST.in.
    # See https://stackoverflow.com/questions/7522250/
    include_package_data=True,
    # This file is required to inform mypy that inline type hints are used.
    #   See: https://mypy.readthedocs.io/en/stable/installed_packages.html
    package_data={"sierraecg": ["py.typed"]},
    # This is a trick to avoid duplicating dependencies between both setup.py and
    # requirements.txt.
    # requirements.txt must be included in MANIFEST.in for this to work.
    # It does not work for all types of dependencies (e.g. VCS dependencies).
    # For VCS dependencies, use pip >= 19 and the PEP 508 syntax.
    #   Example: requests @ git+https://github.com/requests/requests.git@branch_or_tag
    #   See: https://github.com/pypa/pip/issues/6162
    install_requires=["defusedxml", "numpy"],
    zip_safe=False,
    license="MIT",
    classifiers=[
        "Development Status :: 5 - Production/Stable",
        "Intended Audience :: Developers",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
        "Programming Language :: Python",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3 :: Only",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
        "Programming Language :: Python :: 3.13",
        "Typing :: Typed",
    ],
)
