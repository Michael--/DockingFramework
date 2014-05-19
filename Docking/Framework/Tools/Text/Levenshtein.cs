using System;
using System.Collections.Generic;


namespace Docking.Tools
{
    public interface ITextMetric
    {
        // Compute the distance between strings s1 and s2.
        // One possible such distance could e.g. be a Levenshtein distance.
        byte distance(string s1, string s2);
    }

    // author: SLohse
    public class Levenshtein : ITextMetric
    {
        int mMaxLen; // the maximum string prefix length which is looked at for comparison

        byte[,] d;  // The Levenshtein matrix.
                    // We do not store the full matrix n x m,
                    // but only the "last two rows" of it. This saves a lot of RAM.
        
        byte mCostSubst;    // Levenshtein penalty for changing a character, usually ==1
        byte mCostInsOrDel; // Levenshtein penalty for adding or deleting a character, usually ==1
        
        // the same as Math.Min, but taking 4 arguments
        static private uint MyMin(uint x1, uint x2, uint x3, uint x4)
        {
            return Math.Min(Math.Min(x1, x2), Math.Min(x3, x4));
        }
        
        public Levenshtein(int maxlen)
        {
            if(maxlen<1)
                maxlen = 1;

            mMaxLen = maxlen;
            d = new byte[2, mMaxLen+1];
            
            mCostSubst    = 1; // standard value
            mCostInsOrDel = 1; // standard value
        }
        
        // Computes the Levenshtein distance between 2 strings.
        // The larger the distance, the more different the strings are.
        // The maximum possible value being returned will be 255.
        // Currently, this implementation is a straightforward standard implementation
        // of this algorithm, not using sophisticated tricks like
        // * assigning just a small, constant penalty for length differences instead of accumulating a large one
        // * equivalence classes like ü==ue ä==ae ö==oe ß==ss etc.
        public byte distance(string s1, string s2)
        {
            int i; // running index for ROWS
            int j; // running index for COLUMNS
            
            int n = s1.Length;
            if(n>mMaxLen)
               n = mMaxLen;
            
            int m = s2.Length;
            if(m>mMaxLen)
               m = mMaxLen;
            
            // initialize d as follows (with s2=="interest" as example")
            //           i   n   t   e   r   e   s   t
            //   +------------------------------------
            //   |   0   1   2   3   4   5   6   7   8
            //   |
            byte previous = d[0,0] = 0;
            for(j = 1; j<=m; j++)
            {
                previous = d[0, j] = (byte) Math.Min(((uint) previous) + mCostInsOrDel, (uint) 255);
            }
            previous = 0; // important! will be used even in the first loop iteration below!
            
            int curRow = 1;
            int prevRow = 0;
            for(i = 1; i<=n; i++)
            {
                previous = d[curRow, 0] = (byte) Math.Min(((uint) previous) + mCostInsOrDel, (uint) 255);
                for(j = 1; j<=m; j++)
                {
                    byte comparisonCosts = s1[i-1]==s2[j-1] ? (byte) 0 : mCostSubst;
                    d[curRow, j] = (byte) MyMin(((uint) d[prevRow, j-1]) + comparisonCosts, // substitution
                                                ((uint) d[prevRow, j  ]) + mCostInsOrDel,   // insertion
                                                ((uint) d[curRow,  j-1]) + mCostInsOrDel,   // deletion
                                                ((uint) 255)
                                               );
                }
                prevRow = curRow;
                curRow = curRow==0 ? 1 : 0; // remember that we only store 2 rows of the matrix
            }
            return d[prevRow, m];
        }


        public static string[] Search(String[] token, String txt, out Byte distance)
        {
            if (txt.Length > 0)
            {
                Byte bestKey = 255;
                SortedDictionary<Byte, List<string>> matches = new SortedDictionary<byte, List<string>>();
                Docking.Tools.ITextMetric tm = new Docking.Tools.Levenshtein(50);
                foreach(string s in token)
                {
                    Byte d = tm.distance(txt, s);
                    if (d <= bestKey) // ignore worse matches as previously found
                    {
                        bestKey = d;
                        List<string> values;
                        if (!matches.TryGetValue(d, out values))
                        {
                            values = new List<string>();
                            matches.Add(d, values);
                        }
                        if (!values.Contains(s))
                            values.Add(s);
                    }
                }

                if (matches.Count > 0)
                {
                    List<string> result = matches[bestKey];
                    distance = bestKey;
                    return result.ToArray();
                }
            }
            distance = 0;
            return null;
        }

    }
}
