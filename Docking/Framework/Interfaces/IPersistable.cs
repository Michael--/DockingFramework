namespace Docking.Components
{
   public interface IPersistable
   {
      void LoadFrom(IPersistency persistency);
      void SaveTo  (IPersistency persistency);
   }
}
