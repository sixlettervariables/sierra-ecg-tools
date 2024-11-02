# sierraecg.js
Sierra ECG tools for NodeJS.

## Example

```javascript
const sierraEcg = require('sierraecg');

const ecg = await sierraEcg.readFileAsync('129DYPRG.XML');
console.log('Found %d leads', ecg.leads.length);
ecg.leads
  .filter(function (lead) {
    return lead.enabled;
  }).forEach(function (lead) {
    console.log(
      '    %s: %s ... (%d samples)',
      lead.name,
      lead.data.slice(0, 10).join(' '),
      lead.data.length);
  });
```

## Getting Started
Install via `npm`:

```bash
$ npm install sierraecg
```

Include the module in your project:

```javascript
const sierraEcg = require('sierraecg');
```

That's it!

## API

### sierraEcg.readFileAsync(filename [, options])
Reads a Sierra ECG XML file from the location `filename`.

#### filename {String}
**required** Path to the Sierra ECG XML file.

#### options {Object}
*optional* Options object to pass to `fs.readFile`

#### Returns
Returns a Promise of an `Ecg` object.

## Linting
Linting is provided via `eslint`:

```bash
$ npm run lint
```

## Tests
Tests are written using `tape`:

```bash
$ npm test
```

## Coverage
Code coverage is provided by `istanbul`:

```bash
$ npm run cover
```

## Contributors
- Christopher A. Watford <christopher.watford@gmail.com>

## License
The MIT License (MIT)

Copyright (c) 2014-2024 Christopher A. Watford

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
