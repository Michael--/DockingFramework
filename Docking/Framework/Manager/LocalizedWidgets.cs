using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Docking.Components;
using Docking.Tools;

namespace Docking.Components
{
   [System.ComponentModel.ToolboxItem(true)]
   public class LabelLocalized : Gtk.Label, ILocalized
   {
      void ILocalized.Localize(string namespc)
      {
         if (LocalizationKey == null || LocalizationKey.Length<=0)
            LocalizationKey = UseMarkup ? StringTools.StripGTKMarkupTags(LabelProp) : LabelProp;
         LabelProp = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class ButtonLocalized : Gtk.Button, ILocalized
   {
      void ILocalized.Localize(string namespc)
      {
			if (LocalizationKey == null || LocalizationKey.Length<=0)
            LocalizationKey = Label;
         Label = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }

   // [System.ComponentModel.ToolboxItem(true)] // ?? is this really necessary as a Toolbox item ??
   public class TreeViewColumnLocalized : Gtk.TreeViewColumn, ILocalized
   {
      void ILocalized.Localize(string namespc)
      {
			if (LocalizationKey == null  || LocalizationKey.Length<=0)
            LocalizationKey = Title;
         Title = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class CheckButtonLocalized : Gtk.CheckButton, ILocalized
   {
      public CheckButtonLocalized() : base() { }
      public CheckButtonLocalized(string label) : base(label) { }

      void ILocalized.Localize(string namespc)
      {
			if (LocalizationKey == null  || LocalizationKey.Length<=0)
            LocalizationKey = Label;
         Label = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class RadioButtonLocalized : Gtk.RadioButton, ILocalized
   {
      public RadioButtonLocalized() : base("") { }
      public RadioButtonLocalized(string label) : base(label) { }
      void ILocalized.Localize(string namespc)
      {
			if (LocalizationKey == null  || LocalizationKey.Length<=0)
            LocalizationKey = Label;
         Label = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }
}
