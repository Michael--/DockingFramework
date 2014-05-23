using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Docking.Components;
using Docking.Tools;
using Gtk;


namespace Docking.Widgets
{
   [System.ComponentModel.ToolboxItem(true)]
   public class LabelLocalized : Gtk.Label, ILocalizableWidget
   {
      public LabelLocalized()           : base()    {}
      public LabelLocalized(IntPtr raw) : base(raw) {}
      public LabelLocalized(string s)   : base(s)   {}

      private string mLocalizationKey;
      public string LocalizationKey
      {
         set { mLocalizationKey = value; }
         get
         {
            if(mLocalizationKey==null || mLocalizationKey.Length<=0)
               mLocalizationKey = StringTools.StripSpecialCharacters(LabelProp);
            return mLocalizationKey;
         }
      }

      void ILocalizableWidget.Localize(string namespc)
      {
         LabelProp = LocalizationKey.Localized(namespc);
      }
   }


   [System.ComponentModel.ToolboxItem(true)]
   public class ButtonLocalized : Gtk.Button, ILocalizableWidget
   {
      public ButtonLocalized()                  : base()         {}
      public ButtonLocalized(IntPtr raw)        : base(raw)      {}
      public ButtonLocalized(string stock_id)   : base(stock_id) {}
      public ButtonLocalized(Gtk.Widget widget) : base(widget)   {}

      private string mLocalizationKey;
      public string LocalizationKey
      {
         set { mLocalizationKey = value; }
         get
         {
            if(mLocalizationKey==null || mLocalizationKey.Length<=0)
               mLocalizationKey = StringTools.StripSpecialCharacters(Label);
            return mLocalizationKey;
         }
      }

      void ILocalizableWidget.Localize(string namespc)
      {
         if(LocalizationKey!=null)
            Label = LocalizationKey.Localized(namespc);
      }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class EntryLocalized : Gtk.Entry, ILocalizableWidget
   {
      public EntryLocalized()                   : base()            {}
      public EntryLocalized(int max)            : base(max)         {}
      public EntryLocalized(IntPtr raw)         : base(raw)         {}
      public EntryLocalized(string initialText) : base(initialText) {}

      protected override void OnPopulatePopup(Menu menu)
      {
         base.OnPopulatePopup(menu);
         Localization.LocalizeMenu(menu);
         foreach(Widget w in menu.Children)
            if((w is SeparatorMenuItem) || !w.Sensitive)
               w.Visible = false;
      }

      void ILocalizableWidget.Localize(string namespc)
      {
         // NOP
      }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class ComboBoxEntryLocalized : Gtk.ComboBoxEntry, ILocalizableWidget
   {
      public ComboBoxEntryLocalized()                                  : base()                   { Constructor(); }
      public ComboBoxEntryLocalized(IntPtr raw)                        : base(raw)                { Constructor(); }
      public ComboBoxEntryLocalized(string[] entries)                  : base(entries)            { Constructor(); }
      public ComboBoxEntryLocalized(TreeModel model, int text_column)  : base(model, text_column) { Constructor(); }

      void Constructor()
      {
         this.Entry.PopulatePopup += new PopulatePopupHandler(OnPopulatePopup);
      }

      void OnPopulatePopup(object o, PopulatePopupArgs args)
      {
         Localization.LocalizeMenu(args.Menu);
         foreach(Widget w in args.Menu.Children)
            if((w is SeparatorMenuItem) || !w.Sensitive)
               w.Visible = false;
      }

      void ILocalizableWidget.Localize(string namespc)
      {
         // NOP
      }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class SpinButtonLocalized : Gtk.SpinButton, ILocalizableWidget
   {
      public SpinButtonLocalized(IntPtr raw)                                            : base(raw)                            {}
      public SpinButtonLocalized(Adjustment adjustment, double climb_rate, uint digits) : base(adjustment, climb_rate, digits) {}
      public SpinButtonLocalized(double min, double max, double step)                   : base(min, max, step)                 {}

      protected override void OnPopulatePopup(Menu menu)
      {
         base.OnPopulatePopup(menu);
         Localization.LocalizeMenu(menu);
         foreach(Widget w in menu.Children)
            if((w is SeparatorMenuItem) || !w.Sensitive)
               w.Visible = false;
      }

      void ILocalizableWidget.Localize(string namespc)
      {
         // NOP
      }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class TextViewLocalized : Gtk.TextView, ILocalizableWidget
   {
      public TextViewLocalized()                  : base()       {}
      public TextViewLocalized(IntPtr raw)        : base(raw)    {}
      public TextViewLocalized(TextBuffer buffer) : base(buffer) {}

      protected override void OnPopulatePopup(Menu menu)
      {
         base.OnPopulatePopup(menu);
         Localization.LocalizeMenu(menu);

         foreach(Widget w in menu.Children)
            if((w is SeparatorMenuItem) || !w.Sensitive)
               w.Visible = false;
      }

      void ILocalizableWidget.Localize(string namespc)
      {
         // NOP
      }
   }

   // [System.ComponentModel.ToolboxItem(true)] // ?? is this really necessary as a Toolbox item ??
   public class TreeViewColumnLocalized : Gtk.TreeViewColumn, ILocalizableWidget
   {
      public TreeViewColumnLocalized()                                                           : base()                   { Constructor(); }
      public TreeViewColumnLocalized(IntPtr raw)                                                 : base(raw)                { Constructor(); }
      public TreeViewColumnLocalized(string title, Gtk.CellRenderer cell, Array attrs)           : base(title, cell, attrs) { Constructor(); }
      public TreeViewColumnLocalized(string title, Gtk.CellRenderer cell, params object[] attrs) : base(title, cell, attrs) { Constructor(); }

      // By default, THIS class makes the columns resizable (different than parent class Gtk.TreeViewColumn).
      // If you explicitly want NON-resizable columns, you manually have to set .Resizable = false AFTER the constructor.
      void Constructor()
      {
         Resizable = true;
      }

      private string mLocalizationKey;
      public string LocalizationKey
      {
         set { mLocalizationKey = value; }
         get
         {
            if(mLocalizationKey == null || mLocalizationKey.Length<=0)
               mLocalizationKey = StringTools.StripSpecialCharacters(Title);
            return mLocalizationKey;
         }
      }

      void ILocalizableWidget.Localize(string namespc)
      {
         Title = LocalizationKey.Localized(namespc);
      }

      // workaround for the strange problem that property Width is READONLY
      public int Width_
      { 
         get { return Width;                             }
         set { (this as Gtk.TreeViewColumn).SetWidth(value); } // note that this does not necessarily PRECISELY set this width. it just tries to.
      }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class CheckButtonLocalized : Gtk.CheckButton, ILocalizableWidget
   {
      public CheckButtonLocalized()             : base()      {}
      public CheckButtonLocalized(IntPtr raw)   : base(raw)   {}
      public CheckButtonLocalized(string label) : base(label) {}

      private string mLocalizationKey;
      public string LocalizationKey
      {
         set { mLocalizationKey = value; }
         get
         {
            if(mLocalizationKey==null || mLocalizationKey.Length<=0)
               mLocalizationKey = StringTools.StripSpecialCharacters(Label);
            return mLocalizationKey;
         }
      }

      void ILocalizableWidget.Localize(string namespc)
      {
         Label = LocalizationKey.Localized(namespc);
      }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class RadioButtonLocalized : Gtk.RadioButton, ILocalizableWidget
   {      
      public RadioButtonLocalized(IntPtr raw)                                       : base(raw)                       {}
      public RadioButtonLocalized(Gtk.RadioButton radio_group_member)               : base(radio_group_member)        {}
      public RadioButtonLocalized(string label)                                     : base(label)                     {}
      public RadioButtonLocalized()                                                 : base("")                        {}
      public RadioButtonLocalized(Gtk.RadioButton radio_group_member, string label) : base(radio_group_member, label) {}

      private string mLocalizationKey;
      public string LocalizationKey
      {
         set { mLocalizationKey = value; }
         get
         {
            if(mLocalizationKey == null || mLocalizationKey.Length<=0)
               mLocalizationKey = StringTools.StripSpecialCharacters(Label);
            return mLocalizationKey;
         }
      }

      void ILocalizableWidget.Localize(string namespc)
      {
         Label = LocalizationKey.Localized(namespc);
      }
   }

   public class FileChooserDialogLocalized : Gtk.FileChooserDialog, ILocalizableWidget
   {
      public static string InitialFolderToShow = null;
      public static int    InitialW            = 0;
      public static int    InitialH            = 0;
      public static int    InitialX            = 0;
      public static int    InitialY            = 0;

      static FileChooserDialogLocalized()
      {
         string home = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
         if(Directory.Exists(home))
            InitialFolderToShow = home;
      } 

      protected FileChooserDialogLocalized()
      : base() 
      { Constructor(); }

      public FileChooserDialogLocalized(IntPtr raw)
      : base(raw)
      { Constructor(); }

      public FileChooserDialogLocalized(string title, Gtk.Window parent, Gtk.FileChooserAction action, params object[] button_data)
      : base(title, parent, action, button_data)
      { Constructor(); }

      public FileChooserDialogLocalized(string backend, string title, Gtk.Window parent, Gtk.FileChooserAction action, params object[] button_data)
      : base(backend, title, parent, action, button_data)
      { Constructor(); }

      private void Constructor()
      {
         (this as ILocalizableWidget).Localize(this.GetType().Namespace);
         if(!String.IsNullOrEmpty(InitialFolderToShow) && Directory.Exists(InitialFolderToShow))
            this.SetCurrentFolder(InitialFolderToShow);
         if(InitialW>0 && InitialH>0)
         {
            this.Resize(InitialW, InitialH);
            this.Move(InitialX, InitialY);
         }
      }

      void ILocalizableWidget.Localize(string namespc)
      {
          Localization.LocalizeControls(namespc, this);
      }

      new public int Run()
      {
         int result = base.Run();

         if(Directory.Exists(this.CurrentFolder))
            InitialFolderToShow = this.CurrentFolder;

         this.GetSize(out InitialW, out InitialH);
         this.GetPosition(out InitialX, out InitialY);

         return result;
      }
   }

   public class TaggedLocalizedMenuItem : MenuItem, ILocalizableWidget
   {
      public TaggedLocalizedMenuItem(IntPtr raw)
      : base(raw)
      {}

      public TaggedLocalizedMenuItem(String label)
      : base(label)
      {}

      public System.Object Tag { get; set; }

      void ILocalizableWidget.Localize(string namespc)
      {
         Label l = this.Child as Label;
         if(LocalizationKey == null)
            LocalizationKey = l.Text;
         l.Text = LocalizationKey.Localized(namespc);
      }

      public string LocalizationKey { get; set; }
   }

   public class TaggedImageMenuItem : ImageMenuItem
   {
      public TaggedImageMenuItem(IntPtr raw)
      : base(raw)
      {}

      public TaggedImageMenuItem(String label)
      : base(label)
      {}

      public System.Object Tag { get; set; }
   }

   public class TaggedLocalizedImageMenuItem : TaggedImageMenuItem, ILocalizableWidget
   {
      public TaggedLocalizedImageMenuItem(IntPtr raw)
      : base(raw)
      {}

      public TaggedLocalizedImageMenuItem(String label)
      : base(label)
      {}

      void ILocalizableWidget.Localize(string namespc)
      {
         Label l = this.Child as Label;
         if(LocalizationKey == null)
            LocalizationKey = l.Text;
         l.Text = LocalizationKey.Localized(namespc);
      }

      public string LocalizationKey { get; set; }
   }

   public class TaggedLocalizedCheckedMenuItem : CheckMenuItem, ILocalizableWidget
   {
      public TaggedLocalizedCheckedMenuItem(IntPtr raw)
         : base(raw)
      {
         IgnoreLocalization = false;
      }

      public TaggedLocalizedCheckedMenuItem(String name)
         : base(name)
      {
         IgnoreLocalization = false;
      }

      public System.Object Tag { get; set; }

      void ILocalizableWidget.Localize(string namespc)
      {
         if(!IgnoreLocalization)
         {
            Label l = this.Child as Label;
            if(LocalizationKey == null)
               LocalizationKey = l.Text;
            l.Text = LocalizationKey.Localized(namespc);
         }
      }

      public string LocalizationKey { get; set; }

      public bool IgnoreLocalization { get; set; }
   }
}
