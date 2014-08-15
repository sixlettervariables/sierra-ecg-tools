# Sierra ECG XML Reader MATLAB/Octave Example
This script is a standalone MATLAB/[Octave](http://www.gnu.org/software/octave/) script which implements the basic functionality required to read a Philips Sierra ECG XML file. Included in this distribution are the two dependencies (`base64decode` and `lzw2norm`) of the file. If you are using GNU Octave you do not need `base64decode.m` as it is included in your distribution.

## Usage
Simply load `sierra_ecg.m` in your MATLAB or Octave environment. You will be prompted to select a Sierra ECG XML file. Once the processing has been completed you will be prompted to save a CSV file containing the results. Lastly, you will be provided a quick plot of each of the leads as an example.

Once the script has completed, a single matrix variable (`leads`) containing the 12-Lead data will be available:

```
octave:1> source sierra_ecg.m
octave:2> class(leads)
ans = double
octave:3> size(leads)
ans =

   5500     12
   
octave:4> leads(:, 1)
ans = 

  -15
  -15
  -14
  -14
  -14
...
```
## Additional Notes
This script is not intended to be definitive or even robust, merely an example implementation of the Sierra ECG XML format in a MATLAB/Octave compatible script. It has the following known limitations:

* The script does not support the `.tgz` form transmitted by many Philips devices. Users can find the 12-Lead ECG XML files under the `{serial}.MRX\12LEADS\` folder in the archive.
* The script does not perform any error checking.
* The script does not calculate how many leads are actually present, and will only read 12.
* The script hand parses the XML and may not recognize non-XLI compressed data.
