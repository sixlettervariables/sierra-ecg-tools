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
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "lzw.h"

#define BITS 10                   /* Setting the number of bits to 12, 13*/
#define HASHING_SHIFT (BITS-8)    /* or 14 affects several constants.    */
#define MAX_VALUE (1 << BITS) - 1 /* Note that MS-DOS machines need to   */
#define MAX_CODE MAX_VALUE - 1    /* compile their code in large model if*/
                                  /* 14 bits are selected.               */
#if BITS == 14
  #define TABLE_SIZE 18041        /* The string table size needs to be a */
#endif                            /* prime number that is somewhat larger*/
#if BITS == 13                    /* than 2**BITS.                       */
  #define TABLE_SIZE 9029
#endif
#if BITS <= 12
  #define TABLE_SIZE 5021
#endif

/*
 * Actual context definition
 */
typedef struct lzwctx {
    unsigned char decode_stack[4000]; /** Stack for decoding code words */
    unsigned int *prefix_code;        /** Prefix chain                  */
    unsigned char *append_character;  /** Character-to-code map         */
    const unsigned char *input;       /** User's input                  */
    size_t input_length;              /** Length of the user's input    */
    size_t pos;                       /** Position in the input buffer  */
    int input_bit_count;              /** Bits current read             */
    unsigned int input_bit_buffer;    /** Code word accumulator         */
} lzwctx_t;

/*
 * Forward declarations
 */
static unsigned int input_code(lzwctx_ptr context);
static unsigned char* decode_string(lzwctx_ptr context, size_t stackOffset, unsigned int code);

lzwctx_ptr lzw_init(const void *input, size_t input_length)
{
    lzwctx_t *context = (lzwctx_t*)malloc(sizeof(lzwctx_t));
    context->input = (unsigned char*)input;
    context->input_length = input_length;
    context->pos = 0;
    context->prefix_code = (unsigned int *)calloc(TABLE_SIZE, sizeof(unsigned int));
    context->append_character = (unsigned char *)calloc(TABLE_SIZE, sizeof(unsigned char));
    context->input_bit_buffer = 0L;
    context->input_bit_count = 0;

    return context;
}

void lzw_destroy(lzwctx_ptr context)
{
    if (context) {
        free(context->prefix_code);
        free(context->append_character);
    }

    free(context);
}

/*
**  This is the expansion routine.  It takes an LZW formatted data, and expands
**  it to an output location. The code here should be a fairly close match to
**  the algorithm in the accompanying article.
*/

void lzw_expand(lzwctx_ptr context, void *output)
{
unsigned int next_code;
unsigned int new_code;
unsigned int old_code;
unsigned int character;
unsigned char *string;
unsigned char *userOutput = (unsigned char*)output;

  next_code = 256;                /* This is the next available code */

  old_code = input_code(context); /* Read in the first code, initialize the */
  character = old_code;           /* character variable, and send the first */
  *userOutput = (unsigned char)old_code; /* code to the output file         */
  userOutput++;

/*
**  This is the main expansion loop.  It reads in characters from the LZW file
**  until it sees the special code used to inidicate the end of the data.
*/
  while ((new_code = input_code(context)) != (MAX_VALUE)) {
/*
** This code checks for the special STRING+CHARACTER+STRING+CHARACTER+STRING
** case which generates an undefined code.  It handles it by decoding
** the last code, and adding a single character to the end of the decode string.
*/
    if (new_code >= next_code) {
      context->decode_stack[0] = character;
      string = decode_string(context, 1, old_code);
    }
    else {
/*
** Otherwise we do a straight decode of the new code.
*/
      string = decode_string(context, 0, new_code);
    }

/*
** Now we output the decoded string in reverse order.
*/
    character = *string;
    while (string >= context->decode_stack) {
        *userOutput = *string--;
        userOutput++;
    }

/*
** Finally, if possible, add a new code to the string table.
*/
    if (next_code <= MAX_CODE) {
      context->prefix_code[next_code] = old_code;
      context->append_character[next_code] = character;
      next_code++;
    }

    old_code = new_code;
  }
}

/*
** This routine simply decodes a string from the string table, storing
** it in a buffer.  The buffer can then be output in reverse order by
** the expansion program.
*/

static unsigned char* decode_string(lzwctx_ptr context, size_t stackOffset, unsigned int code)
{
int i;
unsigned char *buffer;

  i = 0;
  buffer = &context->decode_stack[stackOffset];
  while (code > 255) {
    context->decode_stack[stackOffset++] = context->append_character[code];
    code = context->prefix_code[code];
    if (i++ >= MAX_CODE) {
      fprintf(stderr, "error: expansion of code exceeded table size.\n");
      abort();
    }
  }

  context->decode_stack[stackOffset] = code;

  return &context->decode_stack[stackOffset];;
}

static unsigned int input_code(lzwctx_ptr context)
{
unsigned int return_value;

  while (context->input_bit_count <= 24) {
    context->input_bit_buffer |= 
        (unsigned long) context->input[context->pos++] << (24 - context->input_bit_count);
    context->input_bit_count += 8;
    if (context->pos >= context->input_length) {
            break;
    }
  }

  return_value = context->input_bit_buffer >> (32-BITS);
  context->input_bit_buffer <<= BITS;
  context->input_bit_count -= BITS;

  return return_value & 0x0000FFFF;
}
