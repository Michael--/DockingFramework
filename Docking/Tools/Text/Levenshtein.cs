using System;


namespace Docking.Tools
{
    public interface ITextMetric
    {
        // Compute the distance between strings s1 and s2.
        // One possible such distance could e.g. be a Levenshtein distance.
        byte distance(string s1, string s2);
    }

    public class Levenshtein : ITextMetric
    {
        int MAXLEN;
        byte[,] d;  // The Levenshtein matrix.
        // We do not store the full matrix n x m,
        // but only the "last two rows" of it. This saves a lot of RAM.
        
        byte COST_SUBST;      // Levenshtein penalty for changing a character
        byte COST_INS_OR_DEL; // Levenshtein penalty for adding or deleting a character
        
        // the same as Math.Min, but taking 4 arguments
        static private uint MyMin(uint x1, uint x2, uint x3, uint x4)
        {
            return Math.Min(Math.Min(x1, x2), Math.Min(x3, x4));
        }
        
        public Levenshtein(int maxlen)
        {
            if(maxlen<1)
                maxlen = 1;
            
            /* We currently do not set an upper limit. Theoretically any length is possible up to your possible RAM.
         if(maxlen>10000)
            maxlen = 10000;
         */
            
            MAXLEN = maxlen;
            d = new byte[2, MAXLEN+1];
            
            COST_SUBST      = 1; // standard values
            COST_INS_OR_DEL = 1; // standard values
        }
        
        // Computes the Levenshtein distance between 2 strings.
        // The larger the distance, the more different the strings are.
        // The maximum possible value being returned will be 255.
        // The implementation of this function has been taken from
        // //IS_Depot/navigation/Development/Default/navigation/NavLibs/TextMetrics/private/CLevenshtein.cpp#11
        // Currently, this implementation is a straightforward standard implementation
        // of this algorithm without any sophisticated tricks like
        //   - giving just a constant, small penalty for different length instead of accumulating a penalty
        //     for each character
        //   - equivalences like ü==ue ä==ae ö==oe ss==ß etc.
        public byte distance(string s1, string s2)
        {
            int i; // running index for ROWS
            int j; // running index for COLUMNS
            
            int n = s1.Length;
            if(n > MAXLEN)
                n = MAXLEN;
            
            int m = s2.Length;
            if(m > MAXLEN)
                m = MAXLEN;
            
            // initialize d as follows:
            //           i   n   t   e   r   e   s   t
            //   +------------------------------------
            //   |   0   1   2   3   4   5   6   7   8
            // i |
            byte previous = d[0,0] = 0;
            for(j = 1; j <= m; j++)
            {
                previous = d[0, j] = (byte) Math.Min(((uint) previous) + COST_INS_OR_DEL, (uint) 255);
            }
            previous = 0; // important! see below
            
            // Now do the Levenshtein run over the matrix:
            int cur_row = 1;
            int prev_row = 0;
            for(i = 1; i <= n; i++)
            {
                previous = d[cur_row, 0] = (byte) Math.Min(((uint) previous) + COST_INS_OR_DEL, (uint) 255);
                for(j = 1; j <= m; j++)
                {
                    byte comparison_costs = s1[i - 1] == s2[j - 1] ? (byte) 0 : COST_SUBST;
                    d[cur_row, j] = (byte) MyMin(((uint) d[prev_row, j - 1]) + comparison_costs,  // substitution
                                                 ((uint) d[prev_row, j    ]) + COST_INS_OR_DEL,   // insertion
                                                 ((uint) d[cur_row, j - 1 ]) + COST_INS_OR_DEL,   // deletion
                                                 ((uint) 255)
                                                 );
                }
                prev_row = cur_row;
                cur_row = cur_row == 0 ? 1 : 0;
            }
            return d[prev_row, m];
        }
    }
}
