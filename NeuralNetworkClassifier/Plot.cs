using DeepLearnCS;
using Gdk;
using OxyPlot;
using System;
using System.Collections.Generic;

public static class Plot
{
    static double deltax;
    static double deltay;
    static double minx, maxx;
    static double miny, maxy;

    static int Rows(ManagedArray x)
    {
        return x.y;
    }

    static int Cols(ManagedArray x)
    {
        return x.x;
    }

    public static void Points(Pixbuf pixbuf, ManagedArray x, ManagedIntList c, Color[] colors, int f1 = 0, int f2 = 0)
    {
        f1 = f1 >= 0 && f1 < Cols(x) ? f1 : 0;
        f2 = f2 >= 0 && f2 < Cols(x) ? f2 : 0;

        if (pixbuf != null)
        {
            for (var i = 0; i < Rows(x); i++)
            {
                if (Math.Abs(deltax) > 0 && Math.Abs(deltay) > 0)
                {
                    var xp = (int)((x[f1, i] - minx) / deltax);
                    var yp = (int)((x[f2, i] - miny) / deltay);

                    Common.Circle(pixbuf, xp, yp, 2, colors[c[i] % colors.Length], true);
                }
            }
        }
    }

    public static void Points(Pixbuf pixbuf, ManagedNN network, NeuralNetworkOptions opts, double threshold, ManagedArray x, int width, int height, int f1 = 0, int f2 = 0)
    {
        var m = Rows(x);

        minx = Double.MaxValue;
        maxx = Double.MinValue;

        miny = Double.MaxValue;
        maxy = Double.MinValue;

        f1 = f1 >= 0 && f1 < Cols(x) ? f1 : 0;
        f2 = f2 >= 0 && f2 < Cols(x) ? f2 : 0;

        for (var j = 0; j < m; j++)
        {
            minx = Math.Min(x[f1, j], minx);
            maxx = Math.Max(x[f1, j], maxx);

            miny = Math.Min(x[f2, j], miny);
            maxy = Math.Max(x[f2, j], maxy);
        }

        deltax = (maxx - minx) / width;
        deltay = (maxy - miny) / height;

        minx = minx - 8 * deltax;
        maxx = maxx + 8 * deltax;
        miny = miny - 8 * deltay;
        maxy = maxy + 8 * deltay;

        deltax = (maxx - minx) / width;
        deltay = (maxy - miny) / height;

        var colors = Common.Palette2();

        colors.Shuffle();

        var PlotOptions = opts;

        PlotOptions.Items = Rows(x);

        var classification = network.Classify(x, PlotOptions, threshold);

        Points(pixbuf, x, classification, colors, f1, f2);

        // Plot bounding box
        var cw = pixbuf.Width - 1;
        var ch = pixbuf.Height;
        var border = new Color(128, 128, 128);

        Common.Line(pixbuf, 0, 1, cw, 1, border);
        Common.Line(pixbuf, cw, 1, cw, ch, border);
        Common.Line(pixbuf, 0, ch, cw, ch, border);
        Common.Line(pixbuf, 0, 1, 0, ch, border);

        ManagedOps.Free(classification);
    }

    public static Pixbuf Points(ManagedNN network, NeuralNetworkOptions opts, double threshold, ManagedArray x, int width, int height, int f1 = 0, int f2 = 0)
    {
        var pixbuf = Common.Pixbuf(width, height, new Color(255, 255, 255));

        Points(pixbuf, network, opts, threshold, x, width, height, f1, f2);

        return pixbuf;
    }

    static Pixbuf ContourGraph;
    static List<Color> Colors = new List<Color>();

    public static void InitializeContour(int zlevels, int width, int height)
    {
        if (ContourGraph != null)
            Common.Free(ContourGraph);

        ContourGraph = Common.Pixbuf(width, height, new Color(255, 255, 255));

        Colors.Clear();

        var c = Common.Palette2();

        c.Shuffle();

        for (var i = 0; i < zlevels; i++)
            Colors.Add(c[i]);
    }

    public static void ContourLine(double x1, double y1, double x2, double y2, double z)
    {
        if (ContourGraph != null)
        {
            if (Math.Abs(deltax) > 0 && Math.Abs(deltay) > 0)
            {
                var xs = (int)((x1 - minx) / deltax);
                var ys = (int)((y1 - miny) / deltay);
                var xe = (int)((x2 - minx) / deltax);
                var ye = (int)((y2 - miny) / deltay);

                var c = (int)(z * 10);

                if (c >= 0 && c < Colors.Count)
                    Common.Line(ContourGraph, xs, ys, xe, ye, Colors[c]);
            }
        }
    }

    public static Pixbuf Contour(ManagedNN network, NeuralNetworkOptions opts, double threshold, ManagedArray x, int width, int height, int f1 = 0, int f2 = 0)
    {
        InitializeContour(11, width, height);

        var m = Rows(x);

        var xplot = new double[width];
        var yplot = new double[height];
        var data = new double[height, width];

        minx = Double.MaxValue;
        maxx = Double.MinValue;

        miny = Double.MaxValue;
        maxy = Double.MinValue;

        f1 = f1 >= 0 && f1 < Cols(x) ? f1 : 0;
        f2 = f2 >= 0 && f2 < Cols(x) ? f2 : 0;

        for (var j = 0; j < m; j++)
        {
            minx = Math.Min(x[f1, j], minx);
            maxx = Math.Max(x[f1, j], maxx);

            miny = Math.Min(x[f2, j], miny);
            maxy = Math.Max(x[f2, j], maxy);
        }

        deltax = (maxx - minx) / width;
        deltay = (maxy - miny) / height;

        minx = minx - 8 * deltax;
        maxx = maxx + 8 * deltax;
        miny = miny - 8 * deltay;
        maxy = maxy + 8 * deltay;

        deltax = (maxx - minx) / width;
        deltay = (maxy - miny) / height;

        // For predict
        for (var i = 0; i < width; i++)
        {
            xplot[i] = minx + i * deltax;
        }

        for (var i = 0; i < height; i++)
        {
            yplot[i] = miny + i * deltay;
        }

        var xx = new ManagedArray(2, height);

        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                xx[f1, j] = xplot[i];
                xx[f2, j] = yplot[j];
            }

            var p = network.Predict(xx, opts);

            for (var j = 0; j < height; j++)
            {
                data[i, j] = p[j];
            }

            ManagedOps.Free(p);
        }

        var z = new double[] { 0.6, 0.8, 1 };

        Conrec.Contour(data, xplot, yplot, z, ContourLine);

        Points(ContourGraph, network, opts, threshold, x, width, height, f1, f2);

        ManagedOps.Free(xx);

        var border = new Color(128, 128, 128);

        // Plot bounding box
        var cw = ContourGraph.Width - 1;
        var ch = ContourGraph.Height;

        Common.Line(ContourGraph, 0, 1, cw, 1, border);
        Common.Line(ContourGraph, cw, 1, cw, ch, border);
        Common.Line(ContourGraph, 0, ch, cw, ch, border);
        Common.Line(ContourGraph, 0, 1, 0, ch, border);

        return ContourGraph;
    }

    public static void Free()
    {
        Common.Free(ContourGraph);
    }
}
