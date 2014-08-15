/**************************************************************************
**
** Copyright (c) 1989 Mark R. Nelson
**
** LZW data compression/expansion demonstration program.
**
** April 13, 1989
**
** Minor mods made 7/19/2006 to conform with ANSI-C - prototypes, casting, 
** and argument agreement.
** http://marknelson.us/1989/10/01/lzw-data-compression/
**
**************************************************************************/
/* Portions Copyright (c) 2012 Christopher A. Watford
 * Modified 1/6/2012 to remove static data and allow reentry
 * as required by the sierra-ecg-tools project.
 * http://code.google.com/p/sierra-ecg-tools/
 */
#pragma once
#ifndef _LZW_H_
#define _LZW_H_

/** Opaque context for 10-bit LZW decompression */
typedef struct lzwctx *lzwctx_ptr;

/** Initializes a decompression context */
lzwctx_ptr lzw_init(const void *input, size_t input_length);

/** Destroys a decompression context */
void lzw_destroy(lzwctx_ptr context);

/** Expands the user's input into the output memory
 * nb. this is currently unbounded and may overflow!
 */
void lzw_expand(lzwctx_ptr context, void* output);

#endif _LZW_H_
