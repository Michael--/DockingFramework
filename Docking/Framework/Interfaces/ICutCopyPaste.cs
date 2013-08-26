namespace Docking.Components
{
   // a class implementing this interface supports cutting its current selection into the clipboard
   public interface ICut
   {
      void Cut();
   }

   // a class implementing this interface supports copying its current selection to the clipboard
   public interface ICopy
   {
      void Copy();
   }

   // a class implementing this interface supports pasting the current clipboard contents into its current selection
   public interface IPaste
   {
      void Paste();
   }
}

   