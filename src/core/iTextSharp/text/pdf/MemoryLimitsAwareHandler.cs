﻿/*
    This file is part of the iText (R) project.
    Copyright (c) 1998-2019 iText Group NV
    Authors: iText Software.

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License version 3
    as published by the Free Software Foundation with the addition of the
    following permission added to Section 15 as permitted in Section 7(a):
    FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
    ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
    OF THIRD PARTY RIGHTS
    
    This program is distributed in the hope that it will be useful, but
    WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
    or FITNESS FOR A PARTICULAR PURPOSE.
    See the GNU Affero General Public License for more details.
    You should have received a copy of the GNU Affero General Public License
    along with this program; if not, see http://www.gnu.org/licenses or write to
    the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
    Boston, MA, 02110-1301 USA, or download the license from the following URL:
    http://itextpdf.com/terms-of-use/
    
    The interactive user interfaces in modified source and object code versions
    of this program must display Appropriate Legal Notices, as required under
    Section 5 of the GNU Affero General Public License.
    
    In accordance with Section 7(b) of the GNU Affero General Public License,
    a covered work must retain the producer line in every PDF that is created
    or manipulated using iText.
    
    You can be released from the requirements of the license by purchasing
    a commercial license. Buying such a license is mandatory as soon as you
    develop commercial activities involving the iText software without
    disclosing the source code of your own applications.
    These activities include: offering paid services to customers as an ASP,
    serving PDFs on the fly in a web application, shipping iText with a closed
    source product.
    
    For more information, please contact iText Software Corp. at this
    address: sales@itextpdf.com
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace iTextSharp.text.pdf
{
    /// <summary>
    /// A
    /// <see cref="MemoryLimitsAwareHandler"/>
    /// handles memory allocation and prevents decompressed pdf streams from occupation of more space than allowed.
    /// </summary>
    public class MemoryLimitsAwareHandler
    {
        private static readonly int SINGLE_SCALE_COEFFICIENT = 100;
        private static readonly int SUM_SCALE_COEFFICIENT = 500;

        private static readonly int SINGLE_DECOMPRESSED_PDF_STREAM_MIN_SIZE = int.MaxValue / 100;
        private static readonly long SUM_OF_DECOMPRESSED_PDF_STREAMW_MIN_SIZE = int.MaxValue / 20;

        private int MaxSizeOfSingleDecompressedPdfStream;
        private long MaxSizeOfDecompressedPdfStreamsSum;

        private long AllMemoryUsedForDecompression = 0;
        private long MemoryUsedForCurrentPdfStreamDecompression = 0;

        internal bool ConsiderCurrentPdfStream = false;

        /// <summary>
        /// Creates a
        /// <see cref="MemoryLimitsAwareHandler"/>
        /// which will be used to handle decompression of pdf streams.
        /// The max allowed memory limits will be generated by default.
        /// </summary>
        public MemoryLimitsAwareHandler()
        {
            MaxSizeOfSingleDecompressedPdfStream = SINGLE_DECOMPRESSED_PDF_STREAM_MIN_SIZE;
            MaxSizeOfDecompressedPdfStreamsSum = SUM_OF_DECOMPRESSED_PDF_STREAMW_MIN_SIZE;
        }

        /// <summary>
        /// Creates a
        /// <see cref="MemoryLimitsAwareHandler"/>
        /// which will be used to handle decompression of pdf streams.
        /// The max allowed memory limits will be generated by default, based on the size of the document.
        /// </summary>
        /// <param name="documentSize">the size of the document, which is going to be handled by iText.</param>
        public MemoryLimitsAwareHandler(long documentSize)
        {
            MaxSizeOfSingleDecompressedPdfStream = (int)CalculateDefaultParameter(documentSize, SINGLE_SCALE_COEFFICIENT, SINGLE_DECOMPRESSED_PDF_STREAM_MIN_SIZE);
            MaxSizeOfDecompressedPdfStreamsSum = CalculateDefaultParameter(documentSize, SUM_SCALE_COEFFICIENT, SUM_OF_DECOMPRESSED_PDF_STREAMW_MIN_SIZE);
        }

        /// <summary>Gets the maximum allowed size which can be occupied by a single decompressed pdf stream.</summary>
        /// <returns>the maximum allowed size which can be occupied by a single decompressed pdf stream.</returns>
        public int GetMaxSizeOfSingleDecompressedPdfStream()
        {
            return MaxSizeOfSingleDecompressedPdfStream;
        }

        /// <summary>Sets the maximum allowed size which can be occupied by a single decompressed pdf stream.</summary>
        /// <remarks>
        /// Sets the maximum allowed size which can be occupied by a single decompressed pdf stream.
        /// This value correlates with maximum heap size. This value should not exceed limit of the heap size.
        /// iText will throw an exception if during decompression a pdf stream with two or more filters of identical type
        /// occupies more memory than allowed.
        /// </remarks>
        /// <param name="maxSizeOfSingleDecompressedPdfStream">the maximum allowed size which can be occupied by a single decompressed pdf stream.
        ///     </param>
        /// <returns>
        /// this
        /// <see cref="MemoryLimitsAwareHandler"/>
        /// instance.
        /// </returns>
        public MemoryLimitsAwareHandler SetMaxSizeOfSingleDecompressedPdfStream(int maxSizeOfSingleDecompressedPdfStream)
        {
            this.MaxSizeOfSingleDecompressedPdfStream = maxSizeOfSingleDecompressedPdfStream;
            return this;
        }

        /// <summary>Gets the maximum allowed size which can be occupied by all decompressed pdf streams.</summary>
        /// <returns>the maximum allowed size value which streams may occupy</returns>
        public long GetMaxSizeOfDecompressedPdfStreamsSum()
        {
            return MaxSizeOfDecompressedPdfStreamsSum;
        }

        /// <summary>Sets the maximum allowed size which can be occupied by all decompressed pdf streams.</summary>
        /// <remarks>
        /// Sets the maximum allowed size which can be occupied by all decompressed pdf streams.
        /// This value can be limited by the maximum expected PDF file size when it's completely decompressed.
        /// Setting this value correlates with the maximum processing time spent on document reading
        /// iText will throw an exception if during decompression pdf streams with two or more filters of identical type
        /// occupy more memory than allowed.
        /// </remarks>
        /// <param name="maxSizeOfDecompressedPdfStreamsSum">he maximum allowed size which can be occupied by all decompressed pdf streams.
        ///     </param>
        /// <returns>
        /// this
        /// <see cref="MemoryLimitsAwareHandler"/>
        /// instance.
        /// </returns>
        public MemoryLimitsAwareHandler SetMaxSizeOfDecompressedPdfStreamsSum(long maxSizeOfDecompressedPdfStreamsSum)
        {
            this.MaxSizeOfDecompressedPdfStreamsSum = maxSizeOfDecompressedPdfStreamsSum;
            return this;
        }

        /// <summary>Considers the number of bytes which are occupied by the decompressed pdf stream.</summary>
        /// <remarks>
        /// Considers the number of bytes which are occupied by the decompressed pdf stream.
        /// If memory limits have not been faced, throws an exception.
        /// </remarks>
        /// <param name="numOfOccupiedBytes">the number of bytes which are occupied by the decompressed pdf stream.</param>
        /// <returns>
        /// this
        /// <see cref="MemoryLimitsAwareHandler"/>
        /// instance.
        /// </returns>
        /// <seealso>
        /// 
        /// <see cref="MemoryLimitsAwareException"/>
        /// </seealso>
        internal MemoryLimitsAwareHandler ConsiderBytesOccupiedByDecompressedPdfStream(long numOfOccupiedBytes)
        {
            if (ConsiderCurrentPdfStream)
            {
                if (MemoryUsedForCurrentPdfStreamDecompression < numOfOccupiedBytes)
                {
                    MemoryUsedForCurrentPdfStreamDecompression = numOfOccupiedBytes;
                    if (MemoryUsedForCurrentPdfStreamDecompression > MaxSizeOfSingleDecompressedPdfStream)
                    {
                        throw new MemoryLimitsAwareException(MemoryLimitsAwareException.DuringDecompressionSingleStreamOccupiedMoreMemoryThanAllowed);
                    }
                }
            }
            return this;
        }

        /// <summary>Begins handling of current pdf stream decompression.</summary>
        /// <returns>
        /// this
        /// <see cref="MemoryLimitsAwareHandler"/>
        /// instance.
        /// </returns>
        internal MemoryLimitsAwareHandler BeginDecompressedPdfStreamProcessing()
        {
            EnsureCurrentStreamIsReset();
            ConsiderCurrentPdfStream = true;
            return this;
        }

        /// <summary>Ends handling of current pdf stream decompression.</summary>
        /// <remarks>
        /// Ends handling of current pdf stream decompression.
        /// If memory limits have not been faced, throws an exception.
        /// </remarks>
        /// <returns>
        /// this
        /// <see cref="MemoryLimitsAwareHandler"/>
        /// instance.
        /// </returns>
        /// <seealso>
        /// 
        /// <see cref="MemoryLimitsAwareException"/>
        /// </seealso>
        internal MemoryLimitsAwareHandler EndDecompressedPdfStreamProcessing()
        {
            AllMemoryUsedForDecompression += MemoryUsedForCurrentPdfStreamDecompression;
            if (AllMemoryUsedForDecompression > MaxSizeOfDecompressedPdfStreamsSum)
            {
                throw new MemoryLimitsAwareException(MemoryLimitsAwareException.DuringDecompressionMultipleStreamsInSumOccupiedMoreMemoryThanAllowed);
            }
            EnsureCurrentStreamIsReset();
            ConsiderCurrentPdfStream = false;
            return this;
        }

        internal long GetAllMemoryUsedForDecompression()
        {
            return AllMemoryUsedForDecompression;
        }

        private static long CalculateDefaultParameter(long documentSize, int scale, long min)
        {
            long result = documentSize * scale;
            if (result < min)
            {
                result = min;
            }
            if (result > min * scale)
            {
                result = min * scale;
            }
            return result;
        }

        private void EnsureCurrentStreamIsReset()
        {
            MemoryUsedForCurrentPdfStreamDecompression = 0;
        }

    }
}