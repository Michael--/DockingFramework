
namespace Docking.Components
{
   internal static class PlatformFonts
   {
      // default font for all languages except Arabic and Hebrew
      // @see DEFAULT_FONT_ARAB
      // @see DEFAULT_FONT_HEBR
      public static string DEFAULT_FONT
      {
         get
         {
            string osid = Docking.Tools.Platform.OSIDString.ToLowerInvariant();
            if (Docking.Tools.Platform.IsWindows)
            {
               // "Microsoft YaHei" is a sans-serif CJK font, installed by default on Windows 7 or higher.
               // It also is the default font for the user interface on Chinese Win7.
               // The font file contains all 20,902 original CJK Unified Ideographs code points specified in Unicode,
               // plus approximately 80 code points defined by the Standardization Administration of China.
               // It supports GBK character set, with localized glyphs.
               // https://en.wikipedia.org/wiki/Microsoft_YaHei
               // https://www.microsoft.com/typography/Fonts/family.aspx?FID=350
               // Therefore it is the best candidate to use here.
               // Fortunately, this font also properly supports European characters like ���� etc., so we can use the same for CHN and non-CHN :)
               // (We may not rely on "Office XYZ installed" here etc. -
               // If the Win7 standard fonts are not enough, TG will have to deliver its own, which we currently try to avoid.)
               // https://en.wikipedia.org/wiki/List_of_typefaces_included_with_Microsoft_Windows
               // https://en.wikipedia.org/wiki/List_of_CJK_fonts
               return "Microsoft YaHei";
            }
            else if (osid.Contains("ubuntu"))
            {
               // "Noto Mono" is a font coming by default with Ubuntu 18.04 LTS
               // https://packages.ubuntu.com/bionic/fonts-noto-mono
               return "Noto Mono";
            }
            else
            {
               // Sadly, not all Linuxes come with Chinese fonts preinstalled.
               // "Noto" is a free font by Google aiming to provide ALL Unicode characters.
               // http://en.wikipedia.org/wiki/Noto_Sans
               // http://en.wikipedia.org/wiki/Noto_Sans_CJK
               // https://www.google.com/get/noto/help/cjk
               // To get it, install the package containing it, for example on ArchLinux, that is
               //    sudo pacman -S noto-fonts-cjk
               return "Noto Sans CJK SC";
            }
         }
      }

      // default font for Arabic
      // @see DEFAULT_FONT
      // @see DEFAULT_FONT_HEBR
      public static string DEFAULT_FONT_ARAB
      {
         get
         {
            string osid = Docking.Tools.Platform.OSIDString.ToLowerInvariant();
            if (Docking.Tools.Platform.IsWindows)
            {
               // https://en.wikipedia.org/wiki/List_of_typefaces_included_with_Microsoft_Windows
               return "Arial";
            }
            else
            {
               return DEFAULT_FONT;
            }
         }
      }

      // default font for Hebrew
      // @see DEFAULT_FONT
      // @see DEFAULT_FONT_ARAB
      public static string DEFAULT_FONT_HEBR
      {
         get
         {
            string osid = Docking.Tools.Platform.OSIDString.ToLowerInvariant();
            if (Docking.Tools.Platform.IsWindows)
            {
               // https://en.wikipedia.org/wiki/List_of_typefaces_included_with_Microsoft_Windows
               return "Arial";
            }
            else
            {
               return DEFAULT_FONT;
            }
         }
      }
   }
}
