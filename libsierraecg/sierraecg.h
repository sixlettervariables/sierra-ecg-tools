/*  Copyright (c) 2012 Christopher A. Watford
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy of
 *  this software and associated documentation files (the "Software"), to deal in
 *  the Software without restriction, including without limitation the rights to
 *  use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
 *  of the Software, and to permit persons to whom the Software is furnished to do
 *  so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */

#pragma once
#ifndef _SIERRAECG_H_
#define _SIERRAECG_H_

typedef struct _lead {
	char*  name;
	short* samples;
	size_t count;
	size_t duration; /** msec */
} lead_t;

/** Philips Sierra ECG (DXL-style)
 *
 * Leads are stored in the following order:
 * 0. I
 * 1. II
 * 2. III
 * 3. aVR
 * 4. aVL
 * 5. aVF
 * 6-11. V1-V6
 */
typedef struct _ecg {
	char version[8];    /** i.e. 1.03 or 1.04 */
	lead_t leads[16]; /** Retrieved lead data */
	size_t valid;     /** Count of valid leads */
} ecg_t;

#define ECG_I   0
#define ECG_II  1
#define ECG_III 2
#define ECG_AVR 3
#define ECG_AVL 4
#define ECG_AVF 5
#define ECG_V1  6
#define ECG_V2  7
#define ECG_V3  8
#define ECG_V4  9
#define ECG_V5 10
#define ECG_V6 11

#define E_SUCCESS 0

#define E_FAIL (!E_SUCCESS)

/** Performs required library initialization.
 *
 */
int sierraecg_init();

/** Performs required library cleanup.
 *
 */
void sierraecg_cleanup();

/** Read the ECG recorded in a Sierra ECG XML file.
 *
 * @param path [in]     Path to the Sierra ECG XML file.
 * @param ecg  [in,out] ECG data retrieved from the XML file.
 * @return #E_SUCCESS on success.
 */
int sierraecg_read(const char *path, ecg_t *ecg);

/** Decompress a Sierra ECG XML file.
 *
 * @param path        [in] Path to a Sierra ECG XML file.
 * @param output_path [in] Path to save the resulting XML.
 * @return #E_SUCCESS if successful.
 */
int sierraecg_decompress(const char *path, const char *output_path);

/** Frees ECG memory allocated during library routines.
 *
 * @param ecg [in] Pointer to an #ecg_t read by the library.
 */
void sierraecg_free(ecg_t *ecg);

#endif _SIERRAECG_H_
