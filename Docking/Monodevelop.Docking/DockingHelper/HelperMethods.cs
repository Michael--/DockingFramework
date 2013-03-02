using System;

namespace Docking.Helper
{
    public static class HelperMethods
    {
        public static void Line (this Cairo.Context cr, double x1, double y1, double x2, double y2)
        {
            cr.MoveTo (x1, y1);
            cr.LineTo (x2, y2);
        }
    }
}


namespace Docking.Helper
{
#if false
    public static class GtkUtil
    {
        public static Cairo.Color ToCairoColor (this Gdk.Color color)
        {
            return new Cairo.Color ((double)color.Red / ushort.MaxValue,
                                    (double)color.Green / ushort.MaxValue,
                                    (double)color.Blue / ushort.MaxValue);
        }
    }
#endif
}
