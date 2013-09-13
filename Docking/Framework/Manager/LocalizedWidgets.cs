using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Docking.Components;
using Docking.Tools;

namespace Docking.Components
{
   [System.ComponentModel.ToolboxItem(true)]
   public class LabelLocalized : Gtk.Label, ILocalizableWidget
   {
      public LabelLocalized()           : base()    {}
      public LabelLocalized(IntPtr raw) : base(raw) {} // http://jira.nts.neusoft.local/browse/NENA-790
      public LabelLocalized(string s)   : base(s)   {}      

      void ILocalizableWidget.Localize(string namespc)
      {
          if (LocalizationKey == null || LocalizationKey.Length <= 0)
              LocalizationKey = StringTools.StripSpecialCharacters(UseMarkup ? StringTools.StripGTKMarkupTags(LabelProp) : LabelProp);
         LabelProp = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class ButtonLocalized : Gtk.Button, ILocalizableWidget
   {
      public ButtonLocalized()                  : base()         {}
      public ButtonLocalized(IntPtr raw)        : base(raw)      {} // http://jira.nts.neusoft.local/browse/NENA-790
      public ButtonLocalized(string stock_id)   : base(stock_id) {}
      public ButtonLocalized(Gtk.Widget widget) : base(widget)   {}

      void ILocalizableWidget.Localize(string namespc)
      {
		if (LocalizationKey == null || LocalizationKey.Length<=0)
            LocalizationKey = StringTools.StripSpecialCharacters(Label);
         Label = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }

   // [System.ComponentModel.ToolboxItem(true)] // ?? is this really necessary as a Toolbox item ??
   public class TreeViewColumnLocalized : Gtk.TreeViewColumn, ILocalizableWidget
   {
      public TreeViewColumnLocalized()                                                           : base()                   {}
      public TreeViewColumnLocalized(IntPtr raw)                                                 : base(raw)                {} // http://jira.nts.neusoft.local/browse/NENA-790
      public TreeViewColumnLocalized(string title, Gtk.CellRenderer cell, Array attrs)           : base(title, cell, attrs) {}
      public TreeViewColumnLocalized(string title, Gtk.CellRenderer cell, params object[] attrs) : base(title, cell, attrs) {}

      void ILocalizableWidget.Localize(string namespc)
      {
		if (LocalizationKey == null  || LocalizationKey.Length<=0)
            LocalizationKey = StringTools.StripSpecialCharacters(Title);
         Title = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class CheckButtonLocalized : Gtk.CheckButton, ILocalizableWidget
   {
      public CheckButtonLocalized()             : base()      {}
      public CheckButtonLocalized(IntPtr raw)   : base(raw)   {} // http://jira.nts.neusoft.local/browse/NENA-790
      public CheckButtonLocalized(string label) : base(label) {}

      void ILocalizableWidget.Localize(string namespc)
      {
		if (LocalizationKey == null  || LocalizationKey.Length<=0)
            LocalizationKey = StringTools.StripSpecialCharacters(Label);
         Label = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class RadioButtonLocalized : Gtk.RadioButton, ILocalizableWidget
   {      
      public RadioButtonLocalized(IntPtr raw)                                       : base(raw)                       {} // http://jira.nts.neusoft.local/browse/NENA-790
      public RadioButtonLocalized(Gtk.RadioButton radio_group_member)               : base(radio_group_member)        {}
      public RadioButtonLocalized(string label)                                     : base(label)                     {}
      public RadioButtonLocalized()                                                 : base("")                        {}
      public RadioButtonLocalized(Gtk.RadioButton radio_group_member, string label) : base(radio_group_member, label) {}

      void ILocalizableWidget.Localize(string namespc)
      {
			if (LocalizationKey == null  || LocalizationKey.Length<=0)
            LocalizationKey = StringTools.StripSpecialCharacters(Label);
         Label = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }

   public class FileChooserDialogLocalized : Gtk.FileChooserDialog, ILocalizableWidget
   {
      protected FileChooserDialogLocalized()
      : base() 
      { (this as ILocalizableWidget).Localize(this.GetType().Namespace); }

       public FileChooserDialogLocalized(IntPtr raw) // http://jira.nts.neusoft.local/browse/NENA-790
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
