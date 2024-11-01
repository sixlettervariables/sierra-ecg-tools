// <copyright file="XLinqExtensions.cs">
//  Copyright (c) 2024 Christopher A. Watford
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy of
//  this software and associated documentation files (the "Software"), to deal in
//  the Software without restriction, including without limitation the rights to
//  use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//  of the Software, and to permit persons to whom the Software is furnished to do
//  so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.
// </copyright>
// <author>Christopher A. Watford [christopher.watford@gmail.com]</author>

using System.Xml.Linq;

namespace SierraEcg;

internal static class XLinqExtensions
{
    public static XElement ElementOrThrow(this XElement element, XName name)
        => element.Element(name) ?? throw new InvalidSierraEcgFileException($"Missing required {name} child element on {element.Name}");

    public static XElement ElementOrThrow(this XElement element, XName name, XName child)
        => (element.Element(name) ?? throw new InvalidSierraEcgFileException($"Missing required {name} child element on {element.Name}"))
            .Element(child) ?? throw new InvalidSierraEcgFileException($"Missing required {child} child element on {name}");

    public static XAttribute AttributeOrThrow(this XElement element, XName name)
        => element.Attribute(name) ?? throw new InvalidSierraEcgFileException($"Missing required {name} attribute on {element.Name}");
}