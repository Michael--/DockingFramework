#if __MonoCS__ // http://stackoverflow.com/questions/329043/how-can-i-conditionally-compile-my-c-sharp-for-mono-vs-microsoft-net

using System;
using Cairo;

namespace Docking
{
	public static class MonoWorkarounds
	{
		public static void SetSource(this Context context, Surface surface)
        {
           throw new Exception("implement me");
        }

        public static Surface GetTarget(this Context context)
        {
           throw new Exception("implement me");
        }

		public static void SetSource(this Context context, Pattern pattern)
        {
           throw new Exception("implement me");           
        }

        public static void Dispose(this Context context)
        {
           throw new Exception("implement me");           
        }
	}
}

#endif