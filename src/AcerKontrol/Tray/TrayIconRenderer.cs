using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace AcerKontrol.Tray;

public static class TrayIconRenderer
{
    /// <summary>Yüzde rakamını şeffaf zemine çizer. Dönen simgeyi çağıran dispose eder.</summary>
    public static Icon Render(TrayIconStyle style, int size = 32)
    {
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            // "100" sığana kadar yazı tipini küçült.
            var fontSize = size * 0.75f;
            Font font;
            while (true)
            {
                font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                if (g.MeasureString(style.Text, font).Width <= size + 4 || fontSize <= 6) break;
                font.Dispose();
                fontSize -= 1;
            }

            using (font)
            using (var brush = new SolidBrush(style.Foreground))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(style.Text, font, brush, new RectangleF(-2, 0, size + 4, size), format);
            }
        }

        var handle = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
