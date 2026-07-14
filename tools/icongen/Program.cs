using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// Genera el ícono de ForexWidget: insignia circular plana con la paleta de marca.
// Diseño (flat, sin degradados/sombras/bisel, legible hasta 44 px):
//   - insignia circular navy #1A1A2E que llena el canvas (esquinas transparentes),
//   - aro de reloj FINO en coral #E94560 con una abertura abajo,
//   - barra de sesión activa en menta #00FFAA descansando en esa abertura.
// Pocas formas + alto contraste = reconocible a tamaños muy chicos.
// Salida: AppIcon.ico (16/32/48/256) + PNGs para el manifest MSIX (44/50/150/256).

string outDir = args.Length > 0 ? args[0] : ".";
Directory.CreateDirectory(outDir);

var bg = ColorTranslator.FromHtml("#1A1A2E");       // Background (insignia navy)
var accent = ColorTranslator.FromHtml("#E94560");   // Accent (aro de reloj coral)
var overlap = ColorTranslator.FromHtml("#00FFAA");  // Overlap (barra de sesión menta)

Bitmap Draw(int s)
{
    var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.Clear(Color.Transparent);

    // Insignia circular: fondo navy que llena el canvas (esquinas transparentes)
    using (var b = new SolidBrush(bg))
        g.FillEllipse(b, 0, 0, s - 1, s - 1);

    // Aro de reloj FINO en Accent, con abertura de 90° abajo (para la barra).
    // GDI: 0° = 3 en punto, positivo = horario. Dibuja 135°→405°, deja hueco 45°→135°.
    float arcW = Math.Max(2f, s / 14f);
    float inset = s * 0.22f;                 // ≈22% de margen: sin recortes a tamaños chicos
    using (var p = new Pen(accent, arcW) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        g.DrawArc(p, inset, inset, s - 2 * inset, s - 2 * inset, 135, 270);

    // Barra de sesión activa en Overlap, descansando en la abertura inferior del aro
    float barH = Math.Max(2f, s * 0.09f);
    float barW = s * 0.44f;
    float barX = (s - barW) / 2;
    float barY = s * 0.63f;
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
