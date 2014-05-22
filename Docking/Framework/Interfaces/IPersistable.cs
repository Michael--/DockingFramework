namespace Docking.Components
{
   public interface IPersistable
   {
      void SaveTo  (IPersistency persistency);
      void LoadFrom(IPersistency persistency);
   }
}
