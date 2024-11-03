export class Ecg {
    /**
     * Creates an Ecg instance from an XML document.
     * @param {string} text Philips SierraECG XML document (stringy format)
     * @returns {Promise<Ecg>} an Ecg instance from the XML document given in xdoc.
     */
    static fromXmlAsync(text: string): Promise<Ecg>;
    /**
     * @param {string} type
     * @param {Lead[]} leads
     * @param {string} version
     * @param {any} originalXml
     */
    constructor(type: string, leads: Lead[], version: string, originalXml: any);
    type: string;
    leads: Lead[];
    version: string;
    originalXml: any;
}
export class Lead {
    /**
     * @param {string} name
     * @param {number[]} data
     * @param {boolean} enabled
     */
    constructor(name: string, data: number[], enabled: boolean);
    name: string;
    data: number[];
    enabled: boolean;
}
