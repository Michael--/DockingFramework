using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

namespace Docking.Tools
{
   public class MD5Helper
   {
      // returns null on error, or otherwise the MD5 hashsum of a given stream, e.g. 040c05e7e32e24f588dbda77e60053d7
      public static string ComputeMD5(Stream input, int chunksize = 16*1024*1024, CancellationToken token = new CancellationToken())
      {
         if(chunksize<1)
            return null;

         try     
         {
            long todo = input.Length;
            byte[] chunk = new byte[chunksize];

            MD5 md5 = MD5.Create();
            while(todo>0)
            {      
               if(token.IsCancellationRequested)
                  return null;

               chunksize = (int) Math.Min((long) chunk.Length, todo);
               int read = input.Read(chunk, 0, chunksize);
               if(read<=0)
                  return null;

               chunksize = read;
               md5.TransformBlock(chunk, 0, chunksize, chunk, 0);
               todo -= chunksize;
            }
            md5.TransformFinalBlock(new byte[0], 0, 0);
            byte[] hash = md5.Hash;

            StringBuilder builder = new StringBuilder();
            for(int i = 0; i<hash.Length; i++)
               builder.Append(hash[i].ToString("x2"));
            return builder.ToString();
         }
         catch   
         {
            return null;
         }
      }
   }
}
