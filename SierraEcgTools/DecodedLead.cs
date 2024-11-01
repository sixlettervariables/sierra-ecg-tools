// <copyright file="DecodedLead.cs">
//  Copyright (c) 2011 Christopher A. Watford
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy of
//  this software and associated documentation files (the "Software"), to deal in
//  the Software without restriction, including without limitation the rights to
//  use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//  of the Software, and to permit persons to whom the Software is furnished to do
//  so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.
// </copyright>
// <author>Christopher A. Watford [christopher.watford@gmail.com]</author>

namespace SierraEcg
{
    /// <summary>
    /// Represents the decoded and decompressed data for a lead.
    /// </summary>
    public sealed class DecodedLead : IEnumerable<int>
    {
        #region Statics

        private static readonly HashSet<string> s_supportedLeadSets = new(StringComparer.OrdinalIgnoreCase)
            {
                "STD-12",
                "10-WIRE"
            };

        #endregion Statics

        #region Fields

        private readonly int[] _data;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets the lead name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the length of the lead data.
        /// </summary>
        public int Length => _data.Length;

        /// <summary>
        /// Gets the sample at the given index.
        /// </summary>
        /// <param name="index">Index of the sample.</param>
        /// <returns>Value of the lead at the sample.</returns>
        public int this[int index]
        {
            get { return _data[index]; }
        }

        #endregion Properties

        #region ctors

        /// <summary>
        /// Initializes a new instance of the <see cref="DecodedLead"/> class
        /// with the given index and data.
        /// </summary>
        /// <param name="index">Index of the lead in the lead set.</param>
        /// <param name="data">Samples payload.</param>
        internal DecodedLead(int index, int[] data)
        {
            Name = LeadNameFromIndex(index);
            _data = data;
        }

        #endregion ctors

        /// <summary>
        /// Performs the necessary math to fill out leads III, aVR, aVL, and aVF.
        /// </summary>
        /// <remarks>
        /// Currently only supports the STD-12 lead set.
        /// </remarks>
        /// <param name="leadSet">Lead set used during acquisition.</param>
        /// <param name="leadSamples">Samples for each of the acquired leads.</param>
        /// <returns>An enumeration of <see cref="DecodedLead"/> objects containing the completed lead data.</returns>
        public static IEnumerable<DecodedLead> ReinterpretLeads(string leadSet, IEnumerable<int[]> leadSamples)
        {
            if (leadSet is null)
            {
                throw new ArgumentNullException(nameof(leadSet));
            }
            else if (leadSamples is null)
            {
                throw new ArgumentNullException(nameof(leadSamples));
            }

            if (!s_supportedLeadSets.Contains(leadSet))
            {
                throw new ArgumentException("Unsupported lead set: " + leadSet, nameof(leadSet));
            }
            else if (leadSamples.Count() < 12)
            {
                throw new ArgumentException("Not enough lead samples", nameof(leadSamples));
            }

            DecodedLead[] leads = leadSamples.Select((ll, ix) => new DecodedLead(ix + 1, ll)).ToArray();
            DecodedLead leadI = leads[0];
            DecodedLead leadII = leads[1];
            DecodedLead leadIII = leads[2];

            // Reconstitute leads III, aVR, aVL, and aVF

            // lead III
            for (int ii = 0; ii < leadIII.Length; ++ii)
            {
                leadIII._data[ii] = leadII[ii] - leadI[ii] - leadIII[ii];
            }

            // lead aVR
            for (int ii = 0; ii < leads[3].Length; ++ii)
            {
                leads[3]._data[ii] = -leads[3][ii] - ((leadI[ii] + leadII[ii]) / 2);
            }

            // lead aVL
            for (int ii = 0; ii < leads[4].Length; ++ii)
            {
                leads[4]._data[ii] = ((leadI[ii] - leadIII[ii]) / 2) - leads[4][ii];
            }

            // lead aVF
            for (int ii = 0; ii < leads[5].Length; ++ii)
            {
                leads[5]._data[ii] = ((leadII[ii] + leadIII[ii]) / 2) - leads[5][ii];
            }

            return leads;
        }

        /// <summary>
        /// Retrieves the name of a Lead based on its index in a lead set.
        /// </summary>
        /// <param name="index">Index of a lead in the lead set.</param>
        /// <returns>A <see cref="string"/> representation of the lead name.</returns>
        internal static string LeadNameFromIndex(int index)
        {
            return index switch
            {
                1 => "Lead I",
                2 => "Lead II",
                3 => "Lead III",
                4 => "Lead aVR",
                5 => "Lead aVL",
                6 => "Lead aVF",
                7 or 8 or 9 or 10 or 11 or 12 => "Lead V" + (index - 6),
                _ => "Channel " + index,
            };
        }

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the ECG samples.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the ECG samples.</returns>
        public IEnumerator<int> GetEnumerator()
        {
            foreach (int value in _data)
            {
                yield return value;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the ECG samples.
        /// </summary>
        /// <returns>An <see cref="System.Collections.IEnumerator"/> that can be used to iterate through the ECG samples.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IEnumerable<T> Members
    }
}
