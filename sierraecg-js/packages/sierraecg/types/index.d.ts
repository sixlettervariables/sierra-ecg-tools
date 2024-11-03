/**
 * Read a Philips SierraECG file from disk.
 * @param {string} filename
 * @param {(reason: any, ecg: Ecg) => void} cb
 * @param {*} options
 */
export function readFile(filename: string, cb: (reason: any, ecg: Ecg) => void, options: any): void;
/**
 * Read a Philips SierraECG file from disk.
 * @param {string} filename
 * @param {*} options
 * @returns {Promise<Ecg>}
 */
export function readFileAsync(filename: string, options: any): Promise<Ecg>;
/**
 * Read a Philips SierraECG file from an XML string.
 * @param {string} value
 * @param {(reason: any, ecg: Ecg) => void} cb
 */
export function readString(value: string, cb: (reason: any, ecg: Ecg) => void): void;
/**
 * Read a Philips SierraECG file from an XML string.
 * @param {string} value
 * @returns {Promise<Ecg>}
 */
export function readStringAsync(value: string): Promise<Ecg>;
