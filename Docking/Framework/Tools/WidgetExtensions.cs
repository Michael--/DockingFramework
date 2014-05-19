using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Docking.Tools
{
    public static class WidgetExtensions
    {
        public static void DumpWidgetsHierarchy(this Gtk.Widget w, string prefix = "")
        {
            if (prefix == "")
                prefix = "this";
            string s = prefix + " = " + w.Name;
            if(w is Gtk.Label)
                s += "(\""+(w as Gtk.Label).LabelProp+"\")";
            else if(w is Gtk.Button)
                s += "(\"" + (w as Gtk.Button).Label + "\")";
            Debug.Print(s);
            Gtk.Container c = (w as Gtk.Container);
            if (c != null)
            {
                int i = 0;
                foreach (Gtk.Widget w2 in c)
                {
                    w2.DumpWidgetsHierarchy(prefix+"["+i+"]");
                    i++;
                }
            }
        }

        // comfort function to easily access a specific child widget in a nested widget children tree
        public static Gtk.Widget GetChild(this Gtk.Widget w, int i1)
        {
            Gtk.Container c = w as Gtk.Container;
            return (c==null || i1>=c.Children.Count()) ? null : c.Children[i1];
        }

        // comfort function to easily access a specific child widget in a nested widget children tree
        public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2)
        {
            Gtk.Container c = w.GetChild(i1) as Gtk.Container;
            return c==null ? null : c.GetChild(i2);
        }

        // comfort function to easily access a specific c widget in a nested widget children tree
        public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3)
        {
            Gtk.Container c = w.GetChild(i1, i2) as Gtk.Container;
            return c == null ? null : c.GetChild(i3);
        }

        // comfort function to easily access a specific c widget in a nested widget children tree
        public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4)
        {
            Gtk.Container c = w.GetChild(i1, i2, i3) as Gtk.Container;
            return c == null ? null : c.GetChild(i4);
        }

        // comfort function to easily access a specific c widget in a nested widget children tree
        public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5)
        {
            Gtk.Container c = w.GetChild(i1, i2, i3, i4) as Gtk.Container;
            return c == null ? null : c.GetChild(i5);
        }

        // comfort function to easily access a specific c widget in a nested widget children tree
        public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5, int i6)
        {
            Gtk.Container c = w.GetChild(i1, i2, i3, i4, i5) as Gtk.Container;
            return c == null ? null : c.GetChild(i6);
        }

        // comfort function to easily access a specific c widget in a nested widget children tree
        public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5, int i6, int i7)
        {
            Gtk.Container c = w.GetChild(i1, i2, i3, i4, i5, i6) as Gtk.Container;
            return c == null ? null : c.GetChild(i7);
        }

        // comfort function to easily access a specific c widget in a nested widget children tree
        public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8)
        {
            Gtk.Container c = w.GetChild(i1, i2, i3, i4, i5, i6, i7) as Gtk.Container;
            return c == null ? null : c.GetChild(i8);
        }

        // comfort function to easily access a specific c widget in a nested widget children tree
        public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9)
        {
            Gtk.Container c = w.GetChild(i1, i2, i3, i4, i5, i6, i7, i8) as Gtk.Container;
            return c == null ? null : c.GetChild(i9);
        }

        // comfort function to easily access a specific c widget in a nested widget children tree
        public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10)
        {
            Gtk.Container c = w.GetChild(i1, i2, i3, i4, i5, i6, i7, i8, i9) as Gtk.Container;
            return c == null ? null : c.GetChild(i10);
        }

        // comfort function to easily access a specific c widget in a nested widget children tree
        public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11)
        {
            Gtk.Container c = w.GetChild(i1, i2, i3, i4, i5, i6, i7, i8, i9, i10) as Gtk.Container;
            return c == null ? null : c.GetChild(i11);
        }                 
    }
}
