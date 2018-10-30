using Gdk;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class Common
{
    static Random random = new Random(Guid.NewGuid().GetHashCode());

    public static uint[] ColourValues = {
        0xFF0000, 0x00FF00, 0x0000FF, 0xFFFF00, 0xFF00FF, 0x00FFFF, 0x000000,
        0x800000, 0x008000, 0x000080, 0x808000, 0x800080, 0x008080, 0x808080,
        0xC00000, 0x00C000, 0x0000C0, 0xC0C000, 0xC000C0, 0x00C0C0, 0xC0C0C0,
        0x400000, 0x004000, 0x000040, 0x404000, 0x400040, 0x004040, 0x404040,
        0x200000, 0x002000, 0x000020, 0x202000, 0x200020, 0x002020, 0x202020,
        0x600000, 0x006000, 0x000060, 0x606000, 0x600060, 0x006060, 0x606060,
        0xA00000, 0x00A000, 0x0000A0, 0xA0A000, 0xA000A0, 0x00A0A0, 0xA0A0A0,
        0xE00000, 0x00E000, 0x0000E0, 0xE0E000, 0xE000E0, 0x00E0E0, 0xE0E0E0,
    };

    public static uint[] CE2000 = {
        0x00FF00, 0x0000FF, 0xFF0000, 0x01FFFE, 0xFFA6FE, 0xFFDB66, 0x006401,
        0x010067, 0x95003A, 0x007DB5, 0xFF00F6, 0xFFEEE8, 0x774D00, 0x90FB92,
        0x0076FF, 0xD5FF00, 0xFF937E, 0x6A826C, 0xFF029D, 0xFE8900, 0x7A4782,
        0x7E2DD2, 0x85A900, 0xFF0056, 0xA42400, 0x00AE7E, 0x683D3B, 0xBDC6FF,
        0x263400, 0xBDD393, 0x00B917, 0x9E008E, 0x001544, 0xC28C9F, 0xFF74A3,
        0x01D0FF, 0x004754, 0xE56FFE, 0x788231, 0x0E4CA1, 0x91D0CB, 0xBE9970,
        0x968AE8, 0xBB8800, 0x43002C, 0xDEFF74, 0x00FFC6, 0xFFE502, 0x620E00,
        0x008F9C, 0x98FF52, 0x7544B1, 0xB500FF, 0x00FF78, 0xFF6E41, 0x005F39,
        0x6B6882, 0x5FAD4E, 0xA75740, 0xA5FFD2, 0xFFB167, 0x009BFF, 0xE85EBE
    };

    public static Color[] RandomColors(int count)
    {
        Color[] colors = new Color[count];

        HashSet<Color> hs = new HashSet<Color>();

        for (int i = 0; i < count; i++)
        {
            Color color;

            while (!hs.Add(color = new Color((byte)random.Next(70, 200), (byte)random.Next(100, 225), (byte)random.Next(100, 230)))) { }

            colors[i] = color;
        }

        return colors;
    }

    public static Color[] Palette()
    {
        var palette = new Color[ColourValues.Length];

        for (var i = 0; i < ColourValues.Length; i++)
            palette[i] = I2C(ColourValues[i]);

        return palette;
    }

    public static Color[] Palette2()
    {
        var palette = new Color[CE2000.Length];

        for (var i = 0; i < CE2000.Length; i++)
            palette[i] = I2C(CE2000[i]);

        return palette;
    }

    public static uint C2I(Color color)
    {
        return (uint)((color.Red << 16) | (color.Green << 8) | color.Blue);
    }

    public static Color I2C(uint color)
    {
        byte r = (byte)(color >> 16);
        byte g = (byte)(color >> 8);
        byte b = (byte)(color >> 0);

        return new Color(r, g, b);
    }

    public static Pixbuf Pixbuf(int width, int height, Color c)
    {
        var pixbuf = new Pixbuf(Colorspace.Rgb, false, 8, width, height);

        pixbuf.Fill(C2I(c));

        return pixbuf;
    }

    // Fisher–Yates shuffle algorithm
    public static void Shuffle<T>(this T[] list)
    {
        int n = list.Length;

        for (int i = list.Length - 1; i > 1; i--)
        {
            int rnd = random.Next(i + 1);

            T value = list[rnd];

            list[rnd] = list[i];

            list[i] = value;
        }
    }

    public static Pixbuf Pixbuf(int width, int height)
    {
        var pixbuf = Pixbuf(width, height, new Color(0, 0, 0));

        return pixbuf;
    }

    public static void Point(Pixbuf pixbuf, int xp, int yp, Color c)
    {
        if (pixbuf == null)
            return;

        var yr = pixbuf.Height - yp;

        if (xp >= 0 && xp < pixbuf.Width && yr >= 0 && yr < pixbuf.Height)
        {
            var ptr = pixbuf.Pixels + yr * pixbuf.Rowstride + xp * pixbuf.NChannels;

            Marshal.WriteByte(ptr, 0, (byte)c.Red);
            Marshal.WriteByte(ptr, 1, (byte)c.Green);
            Marshal.WriteByte(ptr, 2, (byte)c.Blue);
        }
    }

    public static void Circle(Pixbuf pixbuf, int xc, int yc, int x, int y, Color color, bool filled = false)
    {
        if (filled)
        {
            for (var i = xc - x; i <= xc + x; i++)
                Point(pixbuf, i, yc + y, color);

            for (var i = xc - x; i <= xc + x; i++)
                Point(pixbuf, i, yc - y, color);

            for (var i = xc - y; i <= xc + y; i++)
                Point(pixbuf, i, yc + x, color);

            for (var i = xc - y; i <= xc + y; i++)
                Point(pixbuf, i, yc - x, color);
        }
        else
        {
            Point(pixbuf, xc - x, yc + y, color);
            Point(pixbuf, xc + x, yc + y, color);
            Point(pixbuf, xc - x, yc - y, color);
            Point(pixbuf, xc + x, yc - y, color);
            Point(pixbuf, xc - y, yc + x, color);
            Point(pixbuf, xc + y, yc + x, color);
            Point(pixbuf, xc - y, yc - x, color);
            Point(pixbuf, xc + y, yc - x, color);
        }
    }

    public static void Circle(Pixbuf pixbuf, int xc, int yc, int r, Color c, bool filled = false)
    {
        int x = 0, y = r;
        int d = 3 - 2 * r;

        while (y >= x)
        {
            // for each pixel we will 
            // draw all eight pixels 
            Circle(pixbuf, xc, yc, x, y, c, filled);

            x++;

            // check for decision parameter 
            // and correspondingly  
            // update d, x, y 
            if (d > 0)
            {
                y--;

                d = d + 4 * (x - y) + 10;
            }
            else
                d = d + 4 * x + 6;

            Circle(pixbuf, xc, yc, x, y, c, filled);
        }
    }

    public static void Line(Pixbuf pixbuf, int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;

        int err = (dx > dy ? dx : -dy) / 2, e2;

        while (true)
        {
            Point(pixbuf, x0, y0, color);

            if (x0 == x1 && y0 == y1)
                break;

            e2 = err;

            if (e2 > -dx)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dy)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public static void Free(params IDisposable[] trash)
    {
        foreach (var item in trash)
        {
            if (item != null)
            {
                item.Dispose();
            }
        }
    }
}
