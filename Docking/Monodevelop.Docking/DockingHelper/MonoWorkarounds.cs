#if __MonoCS__ // this is a define only set by mono, but not .NET

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

		public static void SetSource(this Context context, Gradient gradient)
        {
           throw new Exception("implement me");
        }

        public static Surface GetTarget(this Context context)
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