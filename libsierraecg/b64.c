/*********************************************************************\

MODULE NAME:    b64.c

AUTHOR:         Bob Trower 08/04/01

PROJECT:        Crypt Data Packaging

COPYRIGHT:      Copyright (c) Trantor Standard Systems Inc., 2001

NOTE:           This source code may be used as you wish, subject to
                the MIT license.  See the LICENCE section below.

LICENCE:        Copyright (c) 2001 Bob Trower, Trantor Standard Systems Inc.

                Permission is hereby granted, free of charge, to any person
                obtaining a copy of this software and associated
                documentation files (the "Software"), to deal in the
                Software without restriction, including without limitation
                the rights to use, copy, modify, merge, publish, distribute,
                sublicense, and/or sell copies of the Software, and to
                permit persons to whom the Software is furnished to do so,
                subject to the following conditions:

                The above copyright notice and this permission notice shall
                be included in all copies or substantial portions of the
                Software.

                THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
                KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
                WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
                PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
                OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR
                OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
                OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
                SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

VERSION HISTORY:
                Bob Trower 08/04/01 -- Create Version 0.00.00B
				Christopher Watford 01/06/2012 -- Adapted for sierra-ecg-tools

\********************************************************************/
#include <math.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>

static const char cd64[]="|$$$}rstuvwxyz{$$$$$$$>?@ABCDEFGHIJKLMNOPQRSTUVW$$$$$$XYZ[\\]^_`abcdefghijklmnopq";

char* b64_decode(const char *input, size_t input_length, size_t *output_length)
{
    unsigned char in[4], out[3], v;
    int i, len;
	size_t pos = 0, size = 0;
	char *output, *output_ptr;

	for (i = 0; i < input_length; ++i) {
		size += (input[i] >= 43 && input[i] <= 122) ? 1 : 0;
	}

	size = size / 4 * 3;
	while (input[--i] == '=') size--;

	output = output_ptr = (char*)malloc(size);

    while( pos < input_length ) {
        for( len = 0, i = 0; i < 4 && pos < input_length; i++ ) {
            v = 0;
            while( pos < input_length && v == 0 ) {
                v = (unsigned char) input[pos++];
                v = (unsigned char) ((v < 43 || v > 122) ? 0 : cd64[ v - 43 ]);
                if( v ) {
                    v = (unsigned char) ((v == '$') ? 0 : v - 61);
                }
            }
            if( pos < input_length ) {
				len++;
                if( v ) {
                    in[ i ] = (unsigned char) (v - 1);
                }
            }
            else {
                in[i] = 0;
            }
        }
        if( len ) {
            out[ 0 ] = (unsigned char) (in[0] << 2 | in[1] >> 4);
			out[ 1 ] = (unsigned char) (in[1] << 4 | in[2] >> 2);
			out[ 2 ] = (unsigned char) (((in[2] << 6) & 0xc0) | in[3]);
            for( i = 0; i < len - 1; i++, output_ptr++ ) {
				*output_ptr = out[i];
            }
        }
    }

	*output_length = size;

	return output;
}
