export = XliReader;
declare class XliReader {
    /**
     * @param {Uint8Array} input
     */
    constructor(input: Uint8Array);
    extractLeads(): number[][];
    #private;
}
