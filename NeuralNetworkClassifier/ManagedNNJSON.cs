using DeepLearnCS;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

public class ManagedNNJSON
{
    public double[,] Wji;
    public double[,] Wkj;
    public List<double[]> Normalization = new List<double[]>();
}

public static class Utility
{
    public static double[,] Convert2D(ManagedArray A)
    {
        var model = new double[A.y, A.x];

        for (var y = 0; y < A.y; y++)
            for (var x = 0; x < A.x; x++)
                model[y, x] = A[x, y];

        return model;
    }

    public static ManagedArray Set(double[,] A)
    {
        var yy = A.GetLength(0);
        var xx = A.GetLength(1);

        var model = new ManagedArray(xx, yy);

        for (var y = 0; y < yy; y++)
            for (var x = 0; x < xx; x++)
                model[x, y] = A[y, x];

        return model;
    }

    public static ManagedNNJSON Convert(ManagedNN network)
    {
        var model = new ManagedNNJSON()
        {
            Wji = Convert2D(network.Wji),
            Wkj = Convert2D(network.Wkj)
        };

        model.Normalization.Add(network.Min);
        model.Normalization.Add(network.Max);

        return model;
    }

    public static string Serialize(ManagedNN network)
    {
        var model = Convert(network);

        string output = JsonConvert.SerializeObject(model);

        return output;
    }

    public static ManagedNN Deserialize(string json, ManagedArray normalization)
    {
        var network = Deserialize(json);

        if (network.Min.GetLength(0) > 0 && network.Max.GetLength(0) > 0)
        {
            if (normalization == null)
            {
                normalization = new ManagedArray(network.Min.GetLength(0), 2);
            }
            else
            {
                normalization.Resize(network.Min.GetLength(0), 2);
            }

            for (var x = 0; x < 2; x++)
            {
                normalization[x, 0] = network.Min[x];
                normalization[x, 1] = network.Max[x];
            }
        }

        return network;
    }

    public static ManagedNN Deserialize(string json)
    {
        var model = JsonConvert.DeserializeObject<ManagedNNJSON>(json);

        var network = new ManagedNN();

        if (model.Wji != null && model.Wkj != null)
        {
            network.Wji = Set(model.Wji);
            network.Wkj = Set(model.Wkj);
        }

        if (model.Normalization != null && model.Normalization.Count > 1)
        {
            network.Min = model.Normalization[0];
            network.Max = model.Normalization[1];
        }

        return network;
    }

    public static string LoadJson(string FileName)
    {
        var json = "";

        if (File.Exists(FileName))
        {
            using (TextReader reader = File.OpenText(FileName))
            {
                string line = "";

                do
                {
                    line = reader.ReadLine();

                    if (!string.IsNullOrEmpty(line))
                        json += line;
                }
                while (!string.IsNullOrEmpty(line));
            }
        }

        return json;
    }
}
