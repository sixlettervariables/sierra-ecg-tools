sierra-ecg-tools
================

Tools to work with the Philips Sierra ECG XML format.

## Introduction
This project provides reference implementations of readers for the Philips Sierra ECG XML format (v. 1.03, 1.04, and 1.04.01), specifically focusing on the [XLI compression format used](https://github.com/sixlettervariables/sierra-ecg-tools/wiki).

## API
Currently there are sample libraries for:

- C/C++
- C#
- Java

## Examples
There are currently integration examples for:

- C/C++
- C#
- Java
- Matlab/Octave

## Building
There are no required compilers for these projects, however, currently only the following has been tested:

- C/C++: Visual Studio 2010, Visual Studio 2012, clang (LLVM 5.1)
- C# (.Net 2.0+): Visual Studio 2010, Visual Studio 2012, 
- Java: JDK 1.6+ with JAXB support

## Contributing
Sample XML files from various Philips devices would help ensure robustness of the underlying API. To contribute source code please review `CONTRIBUTING.md`.

## Contact
For any questions regarding the library you can contact Christopher at christopher.watford@gmail.com.
