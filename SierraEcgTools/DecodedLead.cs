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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SierraEcg
{
    /// <summary>
    /// Represents the decoded and decompressed data for a lead.
    /// </summary>
    public sealed class DecodedLead : IEnumerable<int>
    {
        #region Statics

        private static HashSet<string> supportedLeadSets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "STD-12",
                "10-WIRE"
            };

        #endregion Statics

        #region Fields

        private int[] data;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets the lead name.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the length of the lead data.
        /// </summary>
        public int Length
        {
            get { return this.data.Length; }
        }

        /// <summary>
        /// Gets the sample at the given index.
        /// </summary>
        /// <param name="index">Index of the sample.</param>
        /// <returns>Value of the lead at the sample.</returns>
        public int this[int index]
        {
            get { return this.data[index]; }
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
            this.Name = LeadNameFromIndex(index);
            this.data = data;
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
            if (!supportedLeadSets.Contains(leadSet))
            {
                throw new ArgumentException("Unsupported lead set: " + leadSet, "leadSet");
            }
            else if (leadSamples.Count() < 12)
            {
                throw new ArgumentException("Not enough lead samples", "leadSamples");
            }

            var leads = leadSamples.Select((ll, ix) => new DecodedLead(ix + 1, ll)).ToArray();
            var leadI = leads[0];
            var leadII = leads[1];
            var leadIII = leads[2];

            // Reconstitute leads III, aVR, aVL, and aVF

            // lead III
            for (int ii = 0; ii < leadIII.Length; ++ii)
            {
                leadIII.data[ii] = leadII[ii] - leadI[ii] - leadIII[ii];
            }

            // lead aVR
            for (int ii = 0; ii < leads[3].Length; ++ii)
            {
                leads[3].data[ii] = -leads[3][ii] - ((leadI[ii] + leadII[ii]) / 2);
            }

            // lead aVL
            for (int ii = 0; ii < leads[4].Length; ++ii)
            {
                leads[4].data[ii] = ((leadI[ii] - leadIII[ii]) / 2) - leads[4][ii];
            }

            // lead aVF
            for (int ii = 0; ii < leads[5].Length; ++ii)
            {
                leads[5].data[ii] = ((leadII[ii] + leadIII[ii]) / 2) - leads[5][ii];
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
            switch (index)
            {
                case 1:
                    return "Lead I";
                case 2:
                    return "Lead II";
                case 3:
                    return "Lead III";
                case 4:
                    return "Lead aVR";
                case 5:
                    return "Lead aVL";
                case 6:
                    return "Lead aVF";
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                    return "Lead V" + (index - 6);
                default:
                    return "Channel " + index;
            }
        }

        #region Object Members

        /// <summary>
        /// Returns a string that represents the current <see cref="DecodedLead"/>.
        /// </summary>
        /// <returns>A string that represents the current <see cref="DecodedLead"/>.</returns>
        public override string ToString()
        {
            return String.Format("{0} ({1} samples)", this.Name, this.Length);
        }

        #endregion Object Members

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the ECG samples.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the ECG samples.</returns>
        public IEnumerator<int> GetEnumerator()
        {
            foreach (var value in this.data)
                yield return value;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the ECG samples.
        /// </summary>
        /// <returns>An <see cref="System.Collections.IEnumerator"/> that can be used to iterate through the ECG samples.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion IEnumerable<T> Members
    }
}
