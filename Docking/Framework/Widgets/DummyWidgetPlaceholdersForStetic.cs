// "Stetic" is the name of the GTK GUI editor contained in
// - MonoDevelop
// - Xamarin Studio
// It operates on its central input file, gui.stetic, which is an XML file and represents the GUI you edit.
// During the build process, that file gets read and converted into C# classes.
// The GTK generator which does that needs to know all widgets being mentioned in that XML file.
// Previously, we ran into a devil's circle here, because those widgets only were contained
// in the finished build result.
// To break this devil's circle, we have created this dummy placeholder library.
// This library is only intended to be given to the "Stetic" Toolbox, so it knows these classes.
// It can then happily generate its GUI code.
// In the final build result, not the dummy placeholder classes, but the real ones will run.
// http://jira.nts.neusoft.local/browse/TEMPO-16


using System;
using Gtk;


namespace Docking
{
   public class DockFrame : Gtk.HBox
   {
      public DockFrame() : base() {}
      public DockFrame(IntPtr raw) : base(raw) {}
      public DockFrame(bool homogeneous, int spacing) : base(homogeneous, spacing) {}
   }
}

namespace Docking.Widgets
{
   public class ButtonLocalized : Gtk.Button
   {
      public ButtonLocalized() : base() {}
      public ButtonLocalized(IntPtr raw) : base(raw) {}
      public ButtonLocalized(string stock_id) : base(stock_id) {}
      public ButtonLocalized(Widget widget) : base(widget) {}
   }

   public class CheckButtonLocalized : Gtk.CheckButton
   {
      public CheckButtonLocalized() : base() {}
      public CheckButtonLocalized(IntPtr raw) : base(raw) {}
      public CheckButtonLocalized(string label) : base(label) {}
   }

   public class EntryLocalized : Gtk.Entry
   {
      public EntryLocalized() : base() {}
      public EntryLocalized(int max) : base(max) {}
      public EntryLocalized(IntPtr raw) : base(raw) {}
      public EntryLocalized(string initialText) : base(initialText) {}
   }

   public class LabelLocalized : Gtk.Label
   {
      public LabelLocalized() : base() {}
      public LabelLocalized(IntPtr raw) : base(raw) {}
      public LabelLocalized(string str) : base(str) {}
   }

   public class RadioButtonLocalized : Gtk.RadioButton
   {
      public RadioButtonLocalized(IntPtr raw) : base(raw) {}
      public RadioButtonLocalized(RadioButton radio_group_member) : base(radio_group_member) {}
      public RadioButtonLocalized(string label) : base(label) {}
      public RadioButtonLocalized(RadioButton radio_group_member, string label) : base(radio_group_member, label) {}
   }

   public class Sheet : Gtk.TreeView
   {
      public Sheet() : base() {}
      public Sheet(IntPtr raw) : base(raw) {}
      public Sheet(TreeModel model) : base(model) {}
   }

   public class SpinButtonLocalized : Gtk.SpinButton
   {
      public SpinButtonLocalized(IntPtr raw) : base(raw) {}
      public SpinButtonLocalized(Adjustment adjustment, double climb_rate, uint digits) : base(adjustment, climb_rate, digits) {}
      public SpinButtonLocalized(double min, double max, double step) : base(min, max, step) {}
   }

   public class TextViewLocalized : Gtk.TextView
   {
      public TextViewLocalized() : base() {}
      public TextViewLocalized(IntPtr raw) : base(raw) {}
      public TextViewLocalized(TextBuffer buffer) : base(buffer) {}
   }

   // TODO SLohse: this class currently is IMHO inherited currently from the wrong parent class ("Component" which inherits from Gtk.Bin),
   // but instead should inherit from Gtk.Widget
   public class VirtualListView : Gtk.Bin
   {
      protected VirtualListView() : base() {}
      public VirtualListView(IntPtr raw) : base(raw) {}
   }
}

// TODO this should be relocated to somewhere else out of DockingFramework
namespace Florence.GtkSharp
{
   public class PlotWidget : Gtk.DrawingArea
   {
      public PlotWidget() : base() {}
      public PlotWidget(IntPtr raw) : base(raw) {}
   }
}
