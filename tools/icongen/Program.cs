using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// Genera el ícono de ForexWidget: insignia circular con la paleta de marca.
// Salida: AppIcon.ico (16/32/48/256) + PNGs para el manifest MSIX (44/50/150/256).

string outDir = args.Length > 0 ? args[0] : ".";
Directory.CreateDirectory(outDir);

var bg = ColorTranslator.FromHtml("#1A1A2E");       // Background
var primary = ColorTranslator.FromHtml("#0F3460");  // Primary (aro sutil)
var accent = ColorTranslator.FromHtml("#E94560");   // Accent (aro de reloj)
var overlap = ColorTranslator.FromHtml("#00FFAA");  // Overlap (barra de sesión)

Bitmap Draw(int s)
{
    var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.Clear(Color.Transparent);

    // Fondo circular
    using (var b = new SolidBrush(bg))
        g.FillEllipse(b, 0, 0, s - 1, s - 1);

    // Borde exterior sutil en Primary
    float borderW = Math.Max(1f, s / 28f);
    using (var p = new Pen(primary, borderW))
        g.DrawEllipse(p, borderW / 2, borderW / 2, s - 1 - borderW, s - 1 - borderW);

    // Aro de reloj: arco 3/4 en Accent, arranca a las 12
    float arcW = Math.Max(1.5f, s / 9f);
    float inset = s * 0.18f;
    using (var p = new Pen(accent, arcW) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        g.DrawArc(p, inset, inset, s - 2 * inset, s - 2 * inset, -90, 270);

    // Barra de sesión activa en Overlap, cruzando bajo el centro
    float barH = Math.Max(1.5f, s * 0.11f);
    float barW = s * 0.42f;
    float barX = (s - barW) / 2;
    float barY = s * 0.56f;
    using (var b = new SolidBrush(overlap))
    {
        if (s >= 32)
        {
            using var path = RoundedRect(barX, barY, barW, barH, barH / 2);
            g.FillPath(b, path);
        }
        else
        {
            g.FillRectangle(b, barX, barY, barW, barH);
        }
    }

    // Manecilla corta hacia la 1 en punto (solo legible en tamaños grandes)
    if (s >= 48)
    {
        float cx = s / 2f, cy = s / 2f;
        float handLen = s * 0.16f;
        double angle = -60 * Math.PI / 180; // "1 en punto"
        using var p = new Pen(accent, Math.Max(1.5f, s / 24f)) { EndCap = LineCap.Round };
        g.DrawLine(p, cx, cy,
            cx + (float)(handLen * Math.Cos(angle)),
            cy + (float)(handLen * Math.Sin(angle)));
    }

    return bmp;
}

static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
{
    var path = new GraphicsPath();
    path.AddArc(x, y, 2 * r, 2 * r, 180, 90);
    path.AddArc(x + w - 2 * r, y, 2 * r, 2 * r, 270, 90);
    path.AddArc(x + w - 2 * r, y + h - 2 * r, 2 * r, 2 * r, 0, 90);
    path.AddArc(x, y + h - 2 * r, 2 * r, 2 * r, 90, 90);
    path.CloseFigure();
    return path;
}

byte[] ToPng(Bitmap bmp)
{
    using var ms = new MemoryStream();
    bmp.Save(ms, ImageFormat.Png);
    return ms.ToArray();
}

// ── .ico multi-resolución (entradas PNG, soportadas desde Vista) ──────
int[] icoSizes = [16, 32, 48, 256];
var entries = new List<(int Size, byte[] Png)>();
foreach (int s in icoSizes)
{
    using var bmp = Draw(s);
    entries.Add((s, ToPng(bmp)));
}

string icoPath = Path.Combine(outDir, "AppIcon.ico");
using (var w = new BinaryWriter(File.Create(icoPath)))
{
    w.Write((ushort)0);              // reserved
    w.Write((ushort)1);              // type: icon
    w.Write((ushort)entries.Count);
    int offset = 6 + 16 * entries.Count;
    foreach (var (size, png) in entries)
    {
        w.Write((byte)(size >= 256 ? 0 : size)); // width (0 = 256)
        w.Write((byte)(size >= 256 ? 0 : size)); // height
        w.Write((byte)0);            // colors in palette
        w.Write((byte)0);            // reserved
        w.Write((ushort)1);          // planes
        w.Write((ushort)32);         // bpp
        w.Write(png.Length);
        w.Write(offset);
        offset += png.Length;
    }
    foreach (var (_, png) in entries)
        w.Write(png);
}
Console.WriteLine($"OK {icoPath} ({new FileInfo(icoPath).Length} bytes)");

// ── PNGs para el manifest MSIX ─────────────────────────────────────────
(string Name, int Size)[] msixAssets =
[
    ("Square44x44Logo.png", 44),
    ("StoreLogo.png", 50),
    ("Square150x150Logo.png", 150),
    ("LargeTile.png", 256),
];
foreach (var (name, size) in msixAssets)
{
    using var bmp = Draw(size);
    string p = Path.Combine(outDir, name);
    File.WriteAllBytes(p, ToPng(bmp));
    Console.WriteLine($"OK {p}");
}
