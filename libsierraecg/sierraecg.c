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
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <assert.h>

#include <libxml/tree.h>
#include <libxml/parser.h>
#include <libxml/xpath.h>
#include <libxml/xpathInternals.h>

#include "b64.h"
#include "lzw.h"
#include "sierraecg.h"

#define FAILED(e) E_SUCCESS != (e)

/** Decodes an XLI compressed chunk of data
 *
 * @param chunk   [in]  XLI compressed data
 * @param samples [out] Decoded samples
 * @param count   [in]  Number of samples allocated
 */
static size_t xli_decodeChunk(void *chunk, short *samples, size_t count)
{
    unsigned char *input;
    unsigned char *deltas;  /** temporary space for decompression */
    size_t size;            /** chunk size */
    short lastValue;
    lzwctx_ptr context;

    assert(NULL != chunk);
    assert(NULL != samples);

    input = (unsigned char*)chunk;

    size = *(int*)(input);
    lastValue = *(short*)(input + 6);

    context = lzw_init(input + 8, size);
    deltas = (unsigned char*)malloc(count * sizeof(short));

    lzw_expand(context, deltas);
    lzw_destroy(context);

    {
        size_t j;

        /* unroll the deltas */
        for (j = 0; j < count; ++j) {
            samples[j] = (deltas[j] << 8) | deltas[count + j];
        }


        {
            /* decode the deltas */
            short x = samples[0];
            short y = samples[1];
            for (j = 2; j < count; ++j) {
                int z = (y + y) - x - lastValue;
                lastValue = samples[j] - 64;
                samples[j] = z;
                x = y;
                y = z;
            }
        }

        free(deltas);
    }

    return size + 8;
}

/** Performs required library initialization.
 *
 */
int sierraecg_init()
{
    xmlInitParser();
    LIBXML_TEST_VERSION;

    return E_SUCCESS;
}

/** Performs required library cleanup.
 *
 */
void sierraecg_cleanup()
{
    xmlCleanupParser();
    xmlMemoryDump();
}

/** Frees memory allocated during library routines.
 *
 * @param ecg [in] Pointer to an #ecg_t read by the library.
 */
void sierraecg_free(ecg_t *ecg)
{
    if (ecg) {
        size_t lead;
        for (lead = 0; lead < ecg->valid; ++lead) {
            free(ecg->leads[lead].samples);
        }
    }
}

/** Decoding context */
typedef struct _ctx {
    xmlDocPtr doc;                  /** XML Document */
    xmlXPathContextPtr xpathCtx;    /** XPath Context */
    ecg_t *ecg;                     /** User's ECG */
    int isBase64;                   /** TRUE if the 12-Lead data is Base64 */
    int isXliCompressed;            /** TRUE if the 12-Lead data uses XLI
                                     *  compression.
                                     */
} ctx_t;

/** Namespace prefix used internall */
#define NS "s"

/** Philips Sierra ECG XML namespace */
#define NS_XMLNS "http://www3.medical.philips.com"

/** XPath query for the <parsedwaveforms> element */
const xmlChar xp_parsedWaveforms[]
    = "/" NS ":restingecgdata/" NS ":waveforms/" NS ":parsedwaveforms"; 

/** XPath query for the <documentversion> element */
const xmlChar xp_version[]
    = "/" NS ":restingecgdata/" NS ":documentinfo/" NS ":documentversion";

/** Checks that the Sierra ECG file is a supported version.
 *
 * @param ctx [in] Decoding context.
 * @return #E_SUCCESS on success (version 1.03 and 1.04).
 */
static int internalCheckVersion(ctx_t *ctx)
{
    xmlXPathObjectPtr xpathObj;
    xmlNodeSetPtr nodes;

    assert(NULL != ctx);

    xpathObj = xmlXPathEvalExpression(xp_version, ctx->xpathCtx);
    if (xpathObj == NULL) {
        fprintf(stderr, "Error: unable to evaluate XPath expression \"%s\"\n", xp_version);
        return E_FAIL;
    }

    nodes = xpathObj->nodesetval;
    if (!nodes || nodes->nodeNr != 1) {
        fprintf(stderr, "Error: invalid number of <documentversion> elements (found %d)\n", 
            xpathObj->nodesetval->nodeNr);
        xmlXPathFreeObject(xpathObj);
        return E_FAIL;
    }
    else {
        xmlNodePtr node = nodes->nodeTab[0];
        xmlChar *version = xmlNodeGetContent(node);

        if (xmlStrEqual(version, "1.03")) {
            strcpy(ctx->ecg->version, "1.03");
        }
        else if (xmlStrEqual(version, "1.04")) {
            strcpy(ctx->ecg->version, "1.04");
        }
        else if (xmlStrEqual(version, "1.04.01")) {
            strcpy(ctx->ecg->version, "1.04");
        }
        else {
            xmlXPathFreeObject(xpathObj);
            return E_FAIL;
        }
    }

    return E_SUCCESS;
}

/** Opens a Sierra ECG file and initializes the decoding context.
 *
 * @param path [in]     Path to the Sierra ECG XML file.
 * @param ctx  [in,out] Decoding context.
 * @param ecg  [in,out] ECG to be populated from the XML file.
 * @return #E_SUCCESS on success
 */
static int internalOpen(const char *path, ctx_t *ctx, ecg_t *ecg)
{
    assert(NULL != path);
    assert(NULL != ctx);
    assert(NULL != ecg);

    memset(ctx, 0, sizeof(ctx_t));

    ctx->ecg = ecg;
    if (!ctx->ecg) {
        fprintf(stderr, "Internal Error: invalid ECG structure\n");
        return E_FAIL;
    }

    ctx->doc = xmlParseFile(path);
    if (ctx->doc == NULL) {
        fprintf(stderr, "Error: unable to parse file \"%s\"\n", path);
        return E_FAIL;
    }

    ctx->xpathCtx = xmlXPathNewContext(ctx->doc);
    if(ctx->xpathCtx == NULL) {
        fprintf(stderr,"Error: unable to create new XPath context\n");
        return E_FAIL;
    }

    if(xmlXPathRegisterNs(ctx->xpathCtx, NS, NS_XMLNS) != 0) {
        fprintf(stderr,"Error: unable to register NS with prefix=\"%s\" and href=\"%s\"\n", NS, NS_XMLNS);
        return E_FAIL;  
    }

    if (FAILED(internalCheckVersion(ctx))) {
        fprintf(stderr, "Error: unsupported Sierra ECG XML version.\n");
        return E_FAIL;
    }

    return E_SUCCESS;
}

/** Cleans up a decoding context */
static void internalClose(ctx_t *ctx)
{
    if (NULL != ctx) {
        if (NULL != ctx->xpathCtx) {
            xmlXPathFreeContext(ctx->xpathCtx);
        }

        if (NULL != ctx->doc) {
            xmlFreeDoc(ctx->doc); 
        }
    }
}

/** Finds the <parsedwaveforms> element and updates the context. */
static xmlXPathObjectPtr findParsedWaveforms(ctx_t *ctx) 
{
    xmlXPathObjectPtr xpathObj;
    xmlNodeSetPtr nodes;
    xmlNodePtr node;

    assert(NULL != ctx);

    /* Evaluate xpath expression */
    xpathObj = xmlXPathEvalExpression(xp_parsedWaveforms, ctx->xpathCtx);
    if (xpathObj == NULL) {
        fprintf(stderr,"Error: unable to evaluate xpath expression \"%s\"\n", xp_parsedWaveforms);
        return NULL;
    }

    nodes = xpathObj->nodesetval;
    if (!nodes || nodes->nodeNr != 1) {
        fprintf(stderr, "Error: invalid number of <parsedwaveforms> elements (found %d)\n", nodes ? nodes->nodeNr : 0);
        xmlXPathFreeObject(xpathObj);
        return NULL;
    }

    node = nodes->nodeTab[0];
    if (node) {
        xmlChar* dataEncoding;
        xmlChar* compressMethod;
        xmlChar* compressFlag;

        dataEncoding = xmlGetProp(node, "dataencoding");
        ctx->isBase64 = (dataEncoding && xmlStrEqual(dataEncoding, "Base64"));

        if (0 == strcmp(ctx->ecg->version, "1.04")) {
            compressMethod = xmlGetProp(node, "compression");
            ctx->isXliCompressed = compressMethod && xmlStrEqual(compressMethod, "XLI");
        }
        else {
            // 1.03
            compressFlag = xmlGetProp(node, "compressflag");
            if (ctx->isXliCompressed = (compressFlag && xmlStrEqual(compressFlag, "True"))) {
                compressMethod = xmlGetProp(node, "compressmethod");
               ctx->isXliCompressed = compressMethod && xmlStrEqual(compressMethod, "XLI");
            }
        }
    }

    return xpathObj;
}

/** Currently supported number of valid leads */
#define SIERRAECG_VALID 12

/** Currently supported number of samples per lead */
#define SIERRAECG_SAMPLES 5500

/** Currently supported duration (msec) per lead */
#define SIERRAECG_DURATION 11000

/** Names of the leads for STD-12 */
static char* leadNames[] = {
    "I",
    "II",
    "III",
    "aVR",
    "aVL",
    "aVF",
    "V1",
    "V2",
    "V3",
    "V4",
    "V5",
    "V6"
};

/** Initializes an #ecg_t structure
 *
 * @param [in,out] ecg   ECG to initialize.
 * @param [in]     valid Number of valid leads recorded.
 */
static int internalInitLeads(ecg_t *ecg, size_t valid)
{
    size_t lead;

    assert (NULL != ecg);

    ecg->valid = valid;
    memset(ecg->leads, 0, sizeof(ecg->leads));
    for (lead = 0; lead < valid; ++lead) {
        ecg->leads[lead].name     = leadNames[lead];
        ecg->leads[lead].count    = SIERRAECG_SAMPLES;
        ecg->leads[lead].duration = SIERRAECG_DURATION;
        ecg->leads[lead].samples  = (short*)calloc(SIERRAECG_SAMPLES, sizeof(short));
    }

    return E_SUCCESS;
}

/** Reads the <parsedwaveforms> element and retrieves the decoded ECG */
static int readParsedWaveforms(ctx_t *ctx, xmlXPathObjectPtr xpathObj)
{
    xmlNodePtr node;
    xmlChar* value;

    assert(NULL != ctx);
    assert(NULL != xpathObj);

    node = xpathObj->nodesetval->nodeTab[0];
    if (!node) return E_FAIL;

    value = xmlNodeGetContent(node);
    if (ctx->isBase64) {
        char *decoded;
        size_t decodedLength;

        decoded = b64_decode((const char*)value, xmlStrlen(value), &decodedLength);
        if (ctx->isXliCompressed) {
            size_t lead = 0, j;
            size_t offset = 0;
            ecg_t *ecg = ctx->ecg;
            lead_t *leadI, *leadII, *leadIII;

            internalInitLeads(ecg, SIERRAECG_VALID);
            leadI = &ecg->leads[ECG_I];
            leadII = &ecg->leads[ECG_II];
            leadIII = &ecg->leads[ECG_III];

            for (lead = 0; lead < ecg->valid && offset < decodedLength; ++lead) {
                lead_t *cur = &ecg->leads[lead];

                offset += xli_decodeChunk(decoded + offset, cur->samples, cur->count);
                
                /* recalculate certain leads */
                switch(lead) {
                case ECG_III:
                    for (j = 0; j < cur->count; ++j) {
                        cur->samples[j] = leadII->samples[j] - leadI->samples[j] - cur->samples[j];
                    }
                    break;

                case ECG_AVR:
                    for (j = 0; j < cur->count; ++j) {
                        cur->samples[j] = -((leadI->samples[j] + leadII->samples[j]) / 2) - cur->samples[j];
                    }
                    break;

                case ECG_AVL:
                    for (j = 0; j < cur->count; ++j) {
                        cur->samples[j] = ((leadI->samples[j] - leadIII->samples[j]) / 2) - cur->samples[j];
                    }
                    break;

                case ECG_AVF:
                    for (j = 0; j < cur->count; ++j) {
                        cur->samples[j] = ((leadII->samples[j] + leadIII->samples[j]) / 2) - cur->samples[j];
                    }
                    break;
                }
            }
        }
        else {
            /* TODO: need to tokenize value to read the data */
            fprintf(stderr, "Error: non-XLI encoded data is currently unsupported.\n");
            free(decoded);
            return E_FAIL;
        }

        free(decoded);
    }
    else {
        /* TODO: need to tokenize value to read the data */
        fprintf(stderr, "Error: non-XLI encoded data is currently unsupported.\n");
        return E_FAIL;
    }

    return E_SUCCESS;
}

/** Updates the <parsedwaveforms> element with the decoded ECG */
static int updateParsedWaveforms(ctx_t *ctx, xmlXPathObjectPtr xpathObj)
{
    xmlNodeSetPtr nodes;
    xmlNodePtr node;
    xmlBufferPtr output;
    size_t lead, sample;
    lead_t *cur;

    assert(NULL != ctx);
    assert(NULL != xpathObj);

    nodes = xpathObj->nodesetval;
    node = nodes->nodeTab[0];
    nodes->nodeTab[0] = NULL;

    xmlSetProp(node, "dataencoding", "Plain");
    if (0 == strcmp(ctx->ecg->version, "1.04")) {
        xmlSetProp(node, "compression", NULL);
    }
    else {
        xmlSetProp(node, "compressflag", "False");
    }

    output = xmlBufferCreate();
    for (lead = 0; lead < ctx->ecg->valid; ++lead) {
        char s[512] = {'\0'};
        cur = &ctx->ecg->leads[lead];

        assert(NULL != cur);

        for (sample = 0; sample < cur->count; sample += 25) {
            short *samples = cur->samples + sample;
            if (sample + 25 <= cur->count) {
                sprintf(s, "%d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d\n", 
                    samples[0], samples[1], samples[2], samples[3], samples[4], samples[5], samples[6], samples[7],
                    samples[8], samples[9], samples[10], samples[11], samples[12], samples[13], samples[14], samples[15],
                    samples[16], samples[17], samples[18], samples[19], samples[20], samples[21], samples[22], samples[23],
                    samples[24]);
                xmlBufferWriteChar(output, s);
            }
            else {
                while (sample < cur->count) {
                    sprintf(s, "%d ", cur->samples[sample++]);
                    xmlBufferWriteChar(output, s);
                }

                xmlBufferWriteChar(output, "\n");
                break;
            }
        }
    }

    xmlNodeSetContent(node, xmlBufferContent(output));

    xmlBufferFree(output);
    return E_SUCCESS;
}

/** Decompress a Sierra ECG XML file.
 *
 * @param path        [in] Path to a Sierra ECG XML file.
 * @param output_path [in] Path to save the resulting XML.
 * @return #E_SUCCESS if successful.
 */
int sierraecg_decompress(const char *path, const char *output_path)
{
    ctx_t ctx;
    ecg_t tempEcg;
    FILE *output;
    int e = E_FAIL;

    memset(&tempEcg, 0, sizeof(tempEcg));

    if (FAILED(internalOpen(path, &ctx, &tempEcg))) {
        fprintf(stderr, "Error: could not open input file \"%s\"\n", path);
    }
    else {
        xmlXPathObjectPtr xpathObj = findParsedWaveforms(&ctx);
        if (NULL != xpathObj) {
            if (!FAILED(readParsedWaveforms(&ctx, xpathObj))
             && !FAILED(updateParsedWaveforms(&ctx, xpathObj))) {
                e = E_SUCCESS;
            }

            /* Cleanup of XPath data */
            xmlXPathFreeObject(xpathObj);
        }
    }

    if (!FAILED(e)) {
        output = fopen(output_path, "wb");
        if (NULL == output) {
            fprintf(stderr, "Error: could not open output file \"%s\"\n", output_path);
        }
        else {
            /* dump the resulting document */
#if WIN32
            {
                xmlChar *buffer;
                int size;

                xmlDocDumpMemory(ctx.doc, &buffer, &size);

                fwrite(buffer, size, 1, output);

                xmlFree(buffer);
            }
#else 
            xmlDocDump(output, ctx.doc);
#endif

            fclose(output);
        }
    }
    
    sierraecg_free(&tempEcg);
    internalClose(&ctx);
    return e;
}

/** Read the ECG recorded in a Sierra ECG XML file.
 *
 * @param path [in]     Path to the Sierra ECG XML file.
 * @param ecg  [in,out] ECG data retrieved from the XML file.
 * @return #E_SUCCESS on success.
 */
int sierraecg_read(const char *path, ecg_t *ecg)
{
    ctx_t ctx;
    int e = E_FAIL;

    if (FAILED(internalOpen(path, &ctx, ecg))) {
        fprintf(stderr, "Error: could not open input file \"%s\"\n", path);
    }
    else {
        xmlXPathObjectPtr xpathObj = findParsedWaveforms(&ctx);
        if (NULL != xpathObj) {
            if (!FAILED(readParsedWaveforms(&ctx, xpathObj))) {
                e = E_SUCCESS;
            }

            /* Cleanup of XPath data */
            xmlXPathFreeObject(xpathObj);
        }
    }

    internalClose(&ctx);

    return e;
}
