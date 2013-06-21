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
      public CheckButtonLocalized() : base() { }
      public CheckButtonLocalized(string label) : base(label) { }

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
      public RadioButtonLocalized() : base("") { }
      public RadioButtonLocalized(string label) : base(label) { }
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
          int x = 3;
/*
          Localization.LocalizeControls(namespc, this);
		if (LocalizationKey == null  || LocalizationKey.Length<=0)
            LocalizationKey = StringTools.StripSpecialCharacters(Label);
         Label = LocalizationKey.Localized(namespc);
 */
      }
      public string LocalizationKey { get; set; }
   }
}
