sierra-ecg-tools
================

Tools to work with the Philips Sierra ECG XML format.

## Introduction
This project provides reference implementations of readers for the Philips Sierra ECG XML format (v. 1.03, 1.04, and 1.04.01), specifically focusing on the [XLI compression format used](https://github.com/sixlettervariables/sierra-ecg-tools/wiki).

## API
Currently there are sample libraries for:

- Python
- C#
- C/C++
- NodeJS
- Java

## Examples
There are currently integration examples for:

- Python
- C#
- C/C++
- Java
- Matlab/Octave

## Building
There are no required compilers for these projects, however, currently only the following has been tested:

- Python: 3.9+
- C# (.NET Framework 4.8, .NET 8.0): Visual Studio 2019+, Visual Studio Code
- Node.js: node v6.0.0+
- C/C++: Visual Studio 2010, Visual Studio 2012, clang (LLVM 5.1)
- Java: JDK 1.6+ with JAXB support
- Octave: 3.6.0+

## Contributing
Sample XML files from various Philips devices would help ensure robustness of the underlying API. To contribute source code please review `CONTRIBUTING.md`.

## Publications citing Sierra ECG Tools
1. Khumrin P, Chumpoo P. Implementation of integrated heterogeneous electronic electrocardiography data into Maharaj Nakorn Chiang Mai Hospital Information System. Health Inform J 2014;1(2). ([PubMed](http://www.ncbi.nlm.nih.gov/pubmed/24771629))

## Contact
For any questions regarding the library you can contact Christopher at christopher.watford@gmail.com.
