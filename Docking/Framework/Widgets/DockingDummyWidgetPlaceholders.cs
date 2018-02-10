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


using System;
using Gtk;


namespace Docking
{
   [System.ComponentModel.ToolboxItem(true)]
   public class DockFrame : Gtk.HBox
   {
      public DockFrame() : base() {}
      public DockFrame(IntPtr raw) : base(raw) {}
      public DockFrame(bool homogeneous, int spacing) : base(homogeneous, spacing) {}
   }
}

namespace Docking.Widgets
{
   [System.ComponentModel.ToolboxItem(true)]
   public class ButtonLocalized : Gtk.Button
   {
      public ButtonLocalized() : base() {}
      public ButtonLocalized(IntPtr raw) : base(raw) {}
      public ButtonLocalized(string stock_id) : base(stock_id) {}
      public ButtonLocalized(Widget widget) : base(widget) {}
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class CheckButtonLocalized : Gtk.CheckButton
   {
      public CheckButtonLocalized() : base() {}
      public CheckButtonLocalized(IntPtr raw) : base(raw) {}
      public CheckButtonLocalized(string label) : base(label) {}
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class EntryLocalized : Gtk.Entry
   {
      public EntryLocalized() : base() {}
      public EntryLocalized(int max) : base(max) {}
      public EntryLocalized(IntPtr raw) : base(raw) {}
      public EntryLocalized(string initialText) : base(initialText) {}
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class LabelLocalized : Gtk.Label
   {
      public LabelLocalized() : base() {}
      public LabelLocalized(IntPtr raw) : base(raw) {}
      public LabelLocalized(string str) : base(str) {}
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class RadioButtonLocalized : Gtk.RadioButton
   {
      public RadioButtonLocalized(IntPtr raw) : base(raw) {}
      public RadioButtonLocalized(RadioButton radio_group_member) : base(radio_group_member) {}
      public RadioButtonLocalized(string label = "") : base(label) {}
      public RadioButtonLocalized(RadioButton radio_group_member, string label) : base(radio_group_member, label) {}
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class Sheet : Gtk.TreeView
   {
      public Sheet() : base() {}
      public Sheet(IntPtr raw) : base(raw) {}
      public Sheet(TreeModel model) : base(model) {}
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class SpinButtonLocalized : Gtk.SpinButton
   {
      public SpinButtonLocalized(IntPtr raw) : base(raw) {}
      public SpinButtonLocalized(Adjustment adjustment, double climb_rate, uint digits) : base(adjustment, climb_rate, digits) {}
      public SpinButtonLocalized(double min, double max, double step) : base(min, max, step) {}

      // [Obsolete] // for old GtkSharp versions
      public SpinButtonLocalized() : this(0, 100, 1) {}
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class TextViewLocalized : Gtk.TextView
   {
      public TextViewLocalized() : base() {}
      public TextViewLocalized(IntPtr raw) : base(raw) {}
      public TextViewLocalized(TextBuffer buffer) : base(buffer) {}
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class ComboBoxEntryLocalized : Gtk.ComboBoxEntry
   {
      public ComboBoxEntryLocalized()                                  : base()                   {}
      public ComboBoxEntryLocalized(IntPtr raw)                        : base(raw)                {}
      public ComboBoxEntryLocalized(string[] entries)                  : base(entries)            {}
      public ComboBoxEntryLocalized(TreeModel model, int text_column)  : base(model, text_column) {}
   }

   // TODO SLohse: this class currently is IMHO inherited currently from the wrong parent class ("Component" which inherits from Gtk.Bin),
   // but instead should inherit from Gtk.Widget
   [System.ComponentModel.ToolboxItem(true)]
   public class VirtualListView : Gtk.Bin
   {
      protected VirtualListView() : base() {}
      public VirtualListView(IntPtr raw) : base(raw) {}
   }

 [System.ComponentModel.ToolboxItem (true)]
 public partial class Find : Gtk.Bin
   {
      protected Find() : base() {}
      public Find(IntPtr raw) : base(raw) {}
   }
}

// TODO adjust namespace?
namespace MonoDevelop.Components.PropertyGrid
{
   [System.ComponentModel.ToolboxItem(true)]
   public class PropertyGrid : Gtk.VBox
   {
      public PropertyGrid() : base() {}
      public PropertyGrid(IntPtr raw) : base(raw)  {}
      public PropertyGrid(bool homogeneous, int spacing) : base(homogeneous, spacing) {}
   }
}

namespace MonoDevelop.Components
{
   [System.ComponentModel.ToolboxItem(true)]
   public class ConsoleView : Gtk.ScrolledWindow
   {
      public ConsoleView() : base() {}
      public ConsoleView(IntPtr raw) : base(raw) {}
      public ConsoleView(Adjustment hadjustment, Adjustment vadjustment) : base(hadjustment, vadjustment) {}
   }
}

// TODO this should be relocated to somewhere else out of DockingFramework
namespace Florence.GtkSharp
{
   [System.ComponentModel.ToolboxItem(true)]
   public class PlotWidget : Gtk.DrawingArea
   {
      public PlotWidget() : base() {}
      public PlotWidget(IntPtr raw) : base(raw) {}
   }
}

// TODO relocate that custom control to Docking Framework
namespace TempoGiusto.Guidance
{
   [System.ComponentModel.ToolboxItem(true)]
   public class FieldInfoViewer : Gtk.TreeView
   {
      public FieldInfoViewer(TreeModel model) : base(model) {}
//    public FieldInfoViewer(NodeStore store) : base(store) {}
      public FieldInfoViewer()                : base()      {}
      public FieldInfoViewer(IntPtr raw)      : base(raw)   {}
   }
}