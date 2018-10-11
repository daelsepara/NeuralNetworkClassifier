using DeepLearnCS;
using Newtonsoft.Json;
using System.IO;

public class ModelJSON
{
	public double[,] Wji;
	public double[,] Wkj;
	public double[,] Normalization;
}

public static class Utility
{
	public static double[] Convert1D(ManagedArray A)
	{
		var model = new double[A.Length()];

		for (var i = 0; i < A.Length(); i++)
			model[i] = A[i];

		return model;
	}

	public static double[,] Convert2D(ManagedArray A)
	{
		var model = new double[A.y, A.x];

		for (var y = 0; y < A.y; y++)
			for (var x = 0; x < A.x; x++)
				model[y, x] = A[x, y];

		return model;
	}

	public static double[,,] Convert3D(ManagedArray A)
	{
		var model = new double[A.y, A.x, A.z];

		for (var z = 0; z < A.z; z++)
			for (var y = 0; y < A.y; y++)
				for (var x = 0; x < A.x; x++)
					model[y, x, z] = A[x, y, z];

		return model;
	}

	public static double[,,,] Convert4DIJ(ManagedArray A)
	{
		var model = new double[A.i, A.j, A.y, A.x];

		var temp = new ManagedArray(A.x, A.y);

		for (var i = 0; i < A.i; i++)
		{
			for (var j = 0; j < A.j; j++)
			{
				ManagedOps.Copy4DIJ2D(temp, A, i, j);

				for (var y = 0; y < A.y; y++)
					for (var x = 0; x < A.x; x++)
						model[i, j, y, x] = temp[x, y];
			}
		}

		ManagedOps.Free(temp);

		return model;
	}

	public static ManagedArray Set(double[] A, bool vert = false)
	{
		var ii = A.GetLength(0);

		var model = vert ? new ManagedArray(1, ii) : new ManagedArray(ii);

		for (var i = 0; i < ii; i++)
			model[i] = A[i];

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

	public static ManagedArray Set(double[,,] A)
	{
		var yy = A.GetLength(0);
		var xx = A.GetLength(1);
		var zz = A.GetLength(2);

		var model = new ManagedArray(xx, yy, zz);

		for (var z = 0; z < zz; z++)
			for (var y = 0; y < yy; y++)
				for (var x = 0; x < xx; x++)
					model[x, y, z] = A[y, x, z];

		return model;
	}

	public static ManagedArray Set(double[,,,] A)
	{
		var ii = A.GetLength(0);
		var jj = A.GetLength(1);
		var yy = A.GetLength(2);
		var xx = A.GetLength(3);

		var model = new ManagedArray(xx, yy, 1, ii, jj);

		var temp = new ManagedArray(xx, yy);

		for (var i = 0; i < ii; i++)
		{
			for (var j = 0; j < jj; j++)
			{
				for (var y = 0; y < yy; y++)
					for (var x = 0; x < xx; x++)
						temp[x, y] = A[i, j, y, x];

				ManagedOps.Copy2D4DIJ(model, temp, i, j);
			}
		}

		ManagedOps.Free(temp);

		return model;
	}

	public static ModelJSON Convert(ManagedNN network)
	{
		var model = new ModelJSON()
		{
			Wji = Convert2D(network.Wji),
			Wkj = Convert2D(network.Wkj)
		};

		return model;
	}

	public static ModelJSON Convert(ManagedNN network, ManagedArray normalization)
	{
		var model = new ModelJSON()
		{
			Wji = Convert2D(network.Wji),
			Wkj = Convert2D(network.Wkj),
			Normalization = Convert2D(normalization)
		};

		return model;
	}

	public static string Serialize(ManagedNN network)
	{
		var model = Convert(network);

		string output = JsonConvert.SerializeObject(model);

		return output;
	}

	public static string Serialize(ManagedNN network, ManagedArray normalization)
	{
		var model = Convert(network, normalization);

		string output = JsonConvert.SerializeObject(model);

		return output;
	}

	public static ManagedNN Deserialize(string json, ManagedArray normalization)
	{
		var model = JsonConvert.DeserializeObject<ModelJSON>(json);

		var network = new ManagedNN
		{
			Wji = Set(model.Wji),
			Wkj = Set(model.Wkj)
		};

		if (model.Normalization != null)
		{
			var temp = Set(model.Normalization);

			if (normalization == null)
			{
				normalization = new ManagedArray(temp);
			}
			else
			{
				normalization.Resize(temp);
			}

			ManagedOps.Copy2D(normalization, temp, 0, 0);

			ManagedOps.Free(temp);
		}

		return network;
	}

	public static ManagedNN Deserialize(string json)
	{
		var model = JsonConvert.DeserializeObject<ModelJSON>(json);

		var network = new ManagedNN
		{
			Wji = Set(model.Wji),
			Wkj = Set(model.Wkj)
		};

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
