using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Docking.Components;
using Docking.Tools;

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
      protected FileChooserDialogLocalized()
      : base() 
      { (this as ILocalizableWidget).Localize(this.GetType().Namespace); }

       public FileChooserDialogLocalized(IntPtr raw)
       : base(raw)
       { (this as ILocalizableWidget).Localize(this.GetType().Namespace); }

       public FileChooserDialogLocalized(string title, Gtk.Window parent, Gtk.FileChooserAction action, params object[] button_data)
       : base(title, parent, action, button_data)
       { (this as ILocalizableWidget).Localize(this.GetType().Namespace); }

       public FileChooserDialogLocalized(string backend, string title, Gtk.Window parent, Gtk.FileChooserAction action, params object[] button_data)
       : base(backend, title, parent, action, button_data)
       { (this as ILocalizableWidget).Localize(this.GetType().Namespace); }

      void ILocalizableWidget.Localize(string namespc)
      {
          Localization.LocalizeControls(namespc, this);
      }
   }
}
