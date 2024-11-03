export = LzwReader;
declare class LzwReader {
    /**
     * @param {Uint8Array | String} input
     * @param {{ bits?: number }} options
     */
    constructor(input: Uint8Array | string, options: {
        bits?: number;
    });
    /**
     * Decodes the input buffer.
     * @returns {Uint8Array} The decompressed buffer.
     */
    decode(): Uint8Array;
    #private;
}
