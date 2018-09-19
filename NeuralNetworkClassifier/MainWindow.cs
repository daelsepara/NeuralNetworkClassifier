using DeepLearnCS;
using Gdk;
using GLib;
using Gtk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

public partial class MainWindow : Gtk.Window
{
    Dialog Confirm;

    FileChooserDialog TextSaver, TextLoader;
    String TrainingSetFileName, TestSetFileName;
    String WJIFileName, WKJFileName, NormalizationFileName;

    List<Delimiter> Delimiters = new List<Delimiter>();

    bool Paused = true;
    bool NetworkSetuped;

    Mutex Processing = new Mutex();

    int CurrentEpoch;
    bool TrainingDone;

    ManagedNN Network = new ManagedNN();
    NeuralNetworkOptions Options = new NeuralNetworkOptions();
    ManagedArray InputData = new ManagedArray();
    ManagedArray OutputData = new ManagedArray();
    ManagedArray TestData = new ManagedArray();
    ManagedArray NormalizationData = new ManagedArray();

    CultureInfo ci = new CultureInfo("en-us");

	enum Pages
	{
        DATA = 0,
        TRAINING = 1,
        NETWORK = 2,
        ABOUT = 3
	};

    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();

        InitializeUserInterface();
    }

    protected FileFilter AddFilter(string name, params string[] patterns)
    {
        var filter = new FileFilter() { Name = name };

        foreach (var pattern in patterns)
            filter.AddPattern(pattern);

        return filter;
    }

    protected void InitializeUserInterface()
    {
        Title = "Neural Network Classifier";

        Confirm = new Dialog(
            "Are you sure?",
            this,
            DialogFlags.Modal,
            "Yes", ResponseType.Accept,
            "No", ResponseType.Cancel
        )
        {
            Resizable = false,
            KeepAbove = true,
            TypeHint = WindowTypeHint.Dialog,
            WidthRequest = 250
        };

        Confirm.ActionArea.LayoutStyle = ButtonBoxStyle.Center;
        Confirm.WindowStateEvent += OnWindowStateEvent;

        TextSaver = new FileChooserDialog(
            "Save Text File",
            this,
            FileChooserAction.Save,
            "Cancel", ResponseType.Cancel,
            "Save", ResponseType.Accept
        );

        TextLoader = new FileChooserDialog(
            "Load Text File",
            this,
            FileChooserAction.Save,
            "Cancel", ResponseType.Cancel,
            "Load", ResponseType.Accept
        );

        TextLoader.AddFilter(AddFilter("Text files (csv/txt)", "*.txt", "*.csv"));

        TextSaver.AddFilter(AddFilter("txt", "*.txt"));
        TextSaver.AddFilter(AddFilter("csv", "*.csv"));

        Delimiters.Add(new Delimiter("Tab \\t", '\t'));
        Delimiters.Add(new Delimiter("Comma ,", ','));
        Delimiters.Add(new Delimiter("Space \\s", ' '));
        Delimiters.Add(new Delimiter("Vertical Pipe |", '|'));
        Delimiters.Add(new Delimiter("Colon :", ':'));
        Delimiters.Add(new Delimiter("Semi-Colon ;", ';'));
        Delimiters.Add(new Delimiter("Forward Slash /", '/'));
        Delimiters.Add(new Delimiter("Backward Slash \\", '\\'));

        UpdateDelimiterBox(DelimiterBox, Delimiters);

        ToggleControls(Paused);

        Idle.Add(new IdleHandler(OnIdle));
    }

    protected void ToggleControls(bool toggle)
    {
        TrainingSet.Sensitive = toggle;
        DataTrainingSet.Sensitive = toggle;
        OpenTrainingSet.Sensitive = toggle;
        ReloadTrainingSet.Sensitive = toggle;
        Examples.Sensitive = toggle;
        InputLayerNodes.Sensitive = toggle;
        Categories.Sensitive = toggle;

        TestSet.Sensitive = toggle;
        DataTestSet.Sensitive = toggle;
        OpenTestSet.Sensitive = toggle;
        ReloadTestSet.Sensitive = toggle;
        Samples.Sensitive = toggle;

        DelimiterBox.Sensitive = toggle;

        LearningRate.Sensitive = toggle;
        HiddenLayerNodes.Sensitive = toggle;
        Epochs.Sensitive = toggle;
        Tolerance.Sensitive = toggle;
        Classification.Sensitive = toggle;
        Threshold.Sensitive = toggle;

        StartButton.Sensitive = toggle;
        StopButton.Sensitive = !toggle;
        ResetButton.Sensitive = toggle;
		ClassifyButton.Sensitive = toggle;

        WJIWindow.Sensitive = toggle;
        WKJWindow.Sensitive = toggle;
		NormalizationWindow.Sensitive = toggle;
        WJIFile.Sensitive = toggle;
        WKJFile.Sensitive = toggle;
		NormalizationFile.Sensitive = toggle;

		OpenWJIButton.Sensitive = toggle;
        SaveWJIButton.Sensitive = toggle;
        OpenWKJButton.Sensitive = toggle;
        SaveWKJButton.Sensitive = toggle;
		OpenNormalization.Sensitive = toggle;
		SaveNormalization.Sensitive = toggle;

        LoadNetworkButton.Sensitive = toggle;
    }

    protected void Pause()
    {
        if (Paused)
            return;

        Paused = true;

        ToggleControls(Paused);
    }

    protected void Run()
    {
        if (!Paused)
            return;

        Paused = false;

        ToggleControls(Paused);
    }

    protected string GetBaseFileName(string fullpath)
    {
        return System.IO.Path.GetFileNameWithoutExtension(fullpath);
    }

    protected string GetDirectory(string fullpath)
    {
        return System.IO.Path.GetDirectoryName(fullpath);
    }

    protected void ReloadTextFile(string FileName, TextView view, bool isTraining = false, SpinButton counter = null)
    {
        try
        {
            var current = DelimiterBox.Active;
            var delimiter = current >= 0 && current < Delimiters.Count ? Delimiters[current].Character : '\t';

            List<int> categories = new List<int>();

            if (File.Exists(FileName) && view != null)
            {
                var text = "";

                using (TextReader reader = File.OpenText(FileName))
                {
                    string line;
                    var count = 0;

                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (!string.IsNullOrEmpty(line))
                        {
                            if (isTraining && counter != null)
                            {
                                var tokens = line.Split(delimiter);

                                if (tokens.Length > 1)
                                {
                                    var last = Convert.ToInt32(tokens[tokens.Length - 1]);

                                    if (!categories.Contains(last) && last > 0)
                                    {
                                        categories.Add(last);
                                    }
                                }
                            }

                            text += count > 0 ? "\n" + line : line;

                            count++;
                        }
                    }
                }

                if (isTraining && counter != null)
                {
                    counter.Value = Convert.ToInt32(categories.Count);
                }

                view.Buffer.Clear();

                view.Buffer.Text = text.Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: {0}", ex.Message);
        }
    }

    protected void LoadTextFile(ref string FileName, string title, TextView view, Entry entry, bool isTraining = false, SpinButton counter = null)
    {
        TextLoader.Title = title;

        // Add most recent directory
        if (!string.IsNullOrEmpty(TextLoader.Filename))
        {
            var directory = System.IO.Path.GetDirectoryName(TextLoader.Filename);

            if (Directory.Exists(directory))
            {
                TextLoader.SetCurrentFolder(directory);
            }
        }

        if (TextLoader.Run() == (int)ResponseType.Accept)
        {
            if (!string.IsNullOrEmpty(TextLoader.Filename))
            {
                FileName = TextLoader.Filename;

                ReloadTextFile(FileName, view, isTraining, counter);

                if (entry != null)
                {
                    entry.Text = FileName;
                }
            }
        }

        TextLoader.Hide();
    }

    protected void LoadNetworkData(ref string FileName, string title, TextView view, Entry entry, ref int x, ref int y)
    {
        TextLoader.Title = title;

        // Add most recent directory
        if (!string.IsNullOrEmpty(TextLoader.Filename))
        {
            var directory = System.IO.Path.GetDirectoryName(TextLoader.Filename);

            if (Directory.Exists(directory))
            {
                TextLoader.SetCurrentFolder(directory);
            }
        }

        if (TextLoader.Run() == (int)ResponseType.Accept)
        {
            if (!string.IsNullOrEmpty(TextLoader.Filename))
            {
				var current = DelimiterBox.Active;
                var delimiter = current >= 0 && current < Delimiters.Count ? Delimiters[current].Character : '\t';

                FileName = TextLoader.Filename;

				x = 0;
				y = 0;

				var text = "";
				string line;

				using (TextReader reader = File.OpenText(FileName))
				{
					while ((line = reader.ReadLine()) != null)
					{
						line = line.Trim();

						if (!string.IsNullOrEmpty(line))
						{
							y++;

							if (!string.IsNullOrEmpty(text))
								text += "\n";

							var tokens = line.Split(delimiter);

							x = x > tokens.Length ? x : tokens.Length;

							text += line;
						}
					}
				}

                if (entry != null)
                {
                    entry.Text = FileName;
                }

                if (view != null)
				{
					view.Buffer.Clear();
					view.Buffer.Text = text;
				}
            }
        }

        TextLoader.Hide();
    }

	protected void SaveTextFile(ref string FileName, string title, Entry entry, ManagedArray data)
	{
		TextSaver.Title = title;

		TextSaver.SelectFilename(FileName);

		string directory;

        // Add most recent directory
        if (!string.IsNullOrEmpty(TextSaver.Filename))
        {
			directory = System.IO.Path.GetDirectoryName(TextSaver.Filename);

            if (Directory.Exists(directory))
            {
				TextSaver.SetCurrentFolder(directory);
            }
        }

		if (TextSaver.Run() == (int)ResponseType.Accept)
		{
			if (!string.IsNullOrEmpty(TextSaver.Filename))
			{
				FileName = TextSaver.Filename;

				directory = GetDirectory(FileName);

				var ext = TextSaver.Filter.Name;

				FileName = String.Format("{0}.{1}", GetBaseFileName(FileName), ext);

				if (data != null)
				{
					var current = DelimiterBox.Active;
                    var delimiter = current >= 0 && current < Delimiters.Count ? Delimiters[current].Character : '\t';

					var fullpath = String.Format("{0}/{1}", directory, FileName);

					try
                    {
						ManagedFile.Save2D(fullpath, data, delimiter);

                        FileName = fullpath;

						entry.Text = FileName;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error saving {0}: {1}", FileName, ex.Message);
                    }
				}
			}
		}

		TextSaver.Hide();
	}

    protected void UpdateDelimiterBox(ComboBox combo, List<Delimiter> delimeters)
    {
        combo.Clear();

        var cell = new CellRendererText();
        combo.PackStart(cell, false);
        combo.AddAttribute(cell, "text", 0);
        var store = new ListStore(typeof(string));
        combo.Model = store;

        foreach (var delimeter in delimeters)
        {
            store.AppendValues(delimeter.Name);
        }

        combo.Active = delimeters.Count > 0 ? 0 : -1;
    }

    protected void ReparentTextView(Fixed parent, ScrolledWindow window, int x, int y)
    {
        var source = (Fixed)window.Parent;
        source.Remove(window);

        parent.Add(window);

        Fixed.FixedChild child = ((Fixed.FixedChild)(parent[window]));

        child.X = x;
        child.Y = y;
    }

    protected void ReparentLabel(Fixed parent, Label label, int x, int y)
    {
        label.Reparent(parent);

        parent.Move(label, x, y);
    }

    protected void UpdateClassifierInfo()
    {
        if (NetworkSetuped)
        {
            Iterations.Text = Network.Iterations.ToString(ci);
            ErrorCost.Text = Network.L2.ToString("0.#####e+00", ci);
        }
    }

    protected void UpdateProgressBar()
    {
        if (Epochs.Value > 0)
        {
            TrainingProgress.Fraction = Math.Round(CurrentEpoch / Epochs.Value, 2);

            TrainingProgress.Text = TrainingDone ? "Done" : String.Format("Training ({0}%)...", Convert.ToInt32(TrainingProgress.Fraction * 100));
        }
    }

    protected void UpdateParameters(TextView text, SpinButton counter, SpinButton counter2, bool isTraining = true)
    {
        var input = text.Buffer;

        var current = DelimiterBox.Active;

        var delimiter = current >= 0 && current < Delimiters.Count ? Delimiters[current].Character : '\t';

        if (input.LineCount > 0)
        {
            counter.Value = input.LineCount;

            bool first = false;

            using (StringReader reader = new StringReader(input.Text.Trim()))
            {
                string line = reader.ReadLine();

                if (!string.IsNullOrEmpty(line))
                {
                    if (!first)
                        first = true;

                    var tokens = line.Split(delimiter);

                    if (first)
                    {
                        if (isTraining && counter2 != null && tokens.Length > 0)
                        {
                            counter2.Value = tokens.Length - 1;
                        }
                    }
                }
            }
        }
    }

    protected void NormalizeData(ManagedArray input, ManagedArray normalization)
    {
        for (int y = 0; y < input.y; y++)
        {
            for (int x = 0; x < input.x; x++)
            {
                var min = normalization[x, 0];
                var max = normalization[x, 1];

                input[x, y] = (input[x, y] - min) / (max - min);
            }
        }
    }

    protected bool SetupInputData(string training)
    {
        var text = training.Trim();

        if (string.IsNullOrEmpty(text))
            return false;

        var TrainingBuffer = new TextBuffer(new TextTagTable())
        {
            Text = text
        };

        Examples.Value = Convert.ToInt32(TrainingBuffer.LineCount);

        var inpx = Convert.ToInt32(InputLayerNodes.Value);
        var inpy = Convert.ToInt32(Examples.Value);

        ManagedOps.Free(InputData, OutputData, NormalizationData);

        InputData = new ManagedArray(inpx, inpy);
        NormalizationData = new ManagedArray(inpx, 2);
        OutputData = new ManagedArray(1, inpy);

        int min = 0;
        int max = 1;

        for (int x = 0; x < inpx; x++)
        {
            NormalizationData[x, min] = double.MaxValue;
            NormalizationData[x, max] = double.MinValue;
        }

        var current = DelimiterBox.Active;
        var delimiter = current >= 0 && current < Delimiters.Count ? Delimiters[current].Character : '\t';
        var inputs = inpx;

        using (var reader = new StringReader(TrainingBuffer.Text))
        {
            for (int y = 0; y < inpy; y++)
            {
                var line = reader.ReadLine();

                if (!string.IsNullOrEmpty(line))
                {
                    var tokens = line.Split(delimiter);

                    if (inputs > 0 && tokens.Length > inputs)
                    {
                        OutputData[0, y] = Convert.ToDouble(tokens[inputs]);

                        for (int x = 0; x < inpx; x++)
                        {
                            var data = Convert.ToDouble(tokens[x]);

                            NormalizationData[x, min] = data < NormalizationData[x, min] ? data : NormalizationData[x, min];
                            NormalizationData[x, max] = data > NormalizationData[x, max] ? data : NormalizationData[x, max];

                            InputData[x, y] = data;
                        }
                    }
                }
            }
        }

        NormalizeData(InputData, NormalizationData);

		UpdateTextView(Normalization, NormalizationData);

        return true;
    }

    protected bool SetupTestData(string test)
    {
        var text = test.Trim();

        if (string.IsNullOrEmpty(text))
            return false;

        var TestBuffer = new TextBuffer(new TextTagTable())
        {
            Text = text
        };

        Samples.Value = Convert.ToInt32(TestBuffer.LineCount);

        var inpx = Convert.ToInt32(InputLayerNodes.Value);
        var tsty = Convert.ToInt32(Samples.Value);

        ManagedOps.Free(TestData);

        TestData = new ManagedArray(inpx, tsty);

        var current = DelimiterBox.Active;
        var delimiter = current >= 0 && current < Delimiters.Count ? Delimiters[current].Character : '\t';
        var inputs = inpx;

        using (var reader = new StringReader(TestBuffer.Text))
        {
            for (int y = 0; y < tsty; y++)
            {
                var line = reader.ReadLine();

                if (!string.IsNullOrEmpty(line))
                {
                    var tokens = line.Split(delimiter);

                    if (inputs > 0 && tokens.Length >= inpx)
                    {
                        for (int x = 0; x < inpx; x++)
                        {
                            TestData[x, y] = Convert.ToDouble(tokens[x]);
                        }
                    }
                }
            }
        }

        NormalizeData(TestData, NormalizationData);

        return true;
    }

    protected void SetupNetworkTraining()
    {
        NetworkSetuped = false;

        var training = DataTrainingSet.Buffer.Text.Trim();

        if (string.IsNullOrEmpty(training))
            return;

        NetworkSetuped = SetupInputData(training);

        // Reset Network
        Network.Free();

        Options.Alpha = Convert.ToDouble(LearningRate.Value) / 1.0e2;
        Options.Epochs = Convert.ToInt32(Epochs.Value);
        Options.Inputs = Convert.ToInt32(InputLayerNodes.Value);
        Options.Categories = Convert.ToInt32(Categories.Value);
        Options.Items = InputData.y;
        Options.Nodes = Convert.ToInt32(HiddenLayerNodes.Value);
        Options.Tolerance = Convert.ToDouble(Tolerance.Value) / 1.0e5;

		if (UseOptimizer.Active)
		{
			Network.SetupOptimizer(InputData, OutputData, Options);
		}
		else
		{
            Network.Setup(OutputData, Options);
        }

        DataTrainingSet.Buffer.Text = training;
    }

	protected bool SetupInputLayerWeights(string inputlayer)
    {
        var text = inputlayer.Trim();

        if (string.IsNullOrEmpty(text))
            return false;

        var InputLayerBuffer = new TextBuffer(new TextTagTable())
        {
            Text = text
        };

        var inpx = Convert.ToInt32(InputLayerNodes.Value) + 1;
        var inpy = Convert.ToInt32(HiddenLayerNodes.Value);

        if (inpx < 2 || inpy < 2 || inpy != InputLayerBuffer.LineCount)
        {
            return false;
        }

        ManagedOps.Free(Network.Wji);
        Network.Wji = new ManagedArray(inpx, inpy);

        var current = DelimiterBox.Active;
        var delimiter = current >= 0 && current < Delimiters.Count ? Delimiters[current].Character : '\t';

        using (var reader = new StringReader(InputLayerBuffer.Text))
        {
            for (int y = 0; y < inpy; y++)
            {
                var line = reader.ReadLine();

                if (line != null)
                {
                    var tokens = line.Split(delimiter);

                    for (int x = 0; x < inpx; x++)
                    {
                        if (x < tokens.Length)
                        {
                            Network.Wji[x, y] = Convert.ToDouble(tokens[x], ci);
                        }
                    }
                }
            }
        }

        return true;
    }

    protected bool SetupHiddenLayerWeights(string hiddenLayer)
    {
		var text = hiddenLayer.Trim();

        if (string.IsNullOrEmpty(text))
            return false;

		var HiddenLayerBuffer = new TextBuffer(new TextTagTable())
        {
            Text = text
        };

		var hidx = Convert.ToInt32(HiddenLayerNodes.Value) + 1;
		var hidy = Convert.ToInt32(Categories.Value);

        if (hidx < 2 || hidy < 1 || hidy != HiddenLayerBuffer.LineCount)
        {
            return false;
        }

		ManagedOps.Free(Network.Wkj);
		Network.Wkj = new ManagedArray(hidx, hidy);

        var current = DelimiterBox.Active;
        var delimiter = current >= 0 && current < Delimiters.Count ? Delimiters[current].Character : '\t';

        using (var reader = new StringReader(HiddenLayerBuffer.Text))
        {
			for (int y = 0; y < hidy; y++)
            {
                var line = reader.ReadLine();

                if (line != null)
                {
                    var tokens = line.Split(delimiter);

					for (int x = 0; x < hidx; x++)
                    {
                        if (x < tokens.Length)
                        {
							Network.Wkj[x, y] = Convert.ToDouble(tokens[x], ci);
                        }
                    }
                }
            }
        }

        return true;
    }

	protected bool SetupNormalization(string normalization)
    {
        var text = normalization.Trim();

        if (string.IsNullOrEmpty(text))
            return false;

		var NormalizationBuffer = new TextBuffer(new TextTagTable())
        {
            Text = text
        };

		var nrmx = Convert.ToInt32(InputLayerNodes.Value);
		var nrmy = 2;

        if (nrmx < 2 || nrmy < 2 || NormalizationBuffer.LineCount < nrmy)
        {
            return false;
        }

		ManagedOps.Free(NormalizationData);
		NormalizationData = new ManagedArray(nrmx, nrmy);

        var current = DelimiterBox.Active;
        var delimiter = current >= 0 && current < Delimiters.Count ? Delimiters[current].Character : '\t';

        using (var reader = new StringReader(NormalizationBuffer.Text))
        {
            for (int y = 0; y < nrmy; y++)
            {
                var line = reader.ReadLine();

                if (line != null)
                {
                    var tokens = line.Split(delimiter);

                    for (int x = 0; x < nrmx; x++)
                    {
                        if (x < tokens.Length)
                        {
							NormalizationData[x, y] = Convert.ToDouble(tokens[x], ci);
                        }
                    }
                }
            }
        }

        return true;
    }

	protected void SetupNetwork()
    {
		NetworkSetuped = false;

		var inputLayer = WJIView.Buffer.Text.Trim();
		var hiddenLayer = WKJView.Buffer.Text.Trim();
		var normalization = Normalization.Buffer.Text.Trim();

		if (string.IsNullOrEmpty(inputLayer) || string.IsNullOrEmpty(hiddenLayer) || string.IsNullOrEmpty(normalization))
			return;

        // Reset Network
        Network.Free();

        Options.Alpha = Convert.ToDouble(LearningRate.Value) / 1.0e2;
        Options.Epochs = Convert.ToInt32(Epochs.Value);
        Options.Inputs = Convert.ToInt32(InputLayerNodes.Value);
        Options.Categories = Convert.ToInt32(Categories.Value);
        Options.Items = InputData.y;
        Options.Nodes = Convert.ToInt32(HiddenLayerNodes.Value);
        Options.Tolerance = Convert.ToDouble(Tolerance.Value) / 1.0e5;

		NetworkSetuped = SetupInputLayerWeights(inputLayer) && SetupHiddenLayerWeights(hiddenLayer) && SetupNormalization(normalization);
    }

    protected void Classify()
	{
		var test = DataTestSet.Buffer.Text.Trim();

		if (string.IsNullOrEmpty(test))
			return;

		if (NetworkSetuped && SetupTestData(test))
        {
            var TestOptions = Options;

            TestOptions.Items = TestData.y;

            var classification = Network.Classify(TestData, TestOptions, Threshold.Value / 100);

            Classification.Buffer.Clear();

            string text = "";

            for (var i = 0; i < classification.x; i++)
            {
                text += Convert.ToString(classification[i], ci);

                if (i < classification.x - 1)
                    text += "\n";
            }

            Classification.Buffer.Text = text;

            classification.Free();
        }

		DataTestSet.Buffer.Text = test;
    }

	protected void UpdateTextView(TextView view, ManagedArray data)
	{
		if (data != null)
		{
			var current = DelimiterBox.Active;
            var delimiter = current >= 0 && current < Delimiters.Count ? Delimiters[current].Character : '\t';

			view.Buffer.Clear();

			var text = "";

			for (int y = 0; y < data.y; y++)
			{
                if (y > 0)
                    text += "\n";

                for (int x = 0; x < data.x; x++)
				{
					if (x > 0)
						text += delimiter;

					text += data[x, y].ToString(ci);
				}
			}

			view.Buffer.Text = text;
		}
	}

    protected void UpdateNetworkWeights()
	{
		if (NetworkSetuped)
		{
			UpdateTextView(WJIView, Network.Wji);
			UpdateTextView(WKJView, Network.Wkj);
		}
	}

    protected bool GetConfirmation()
    {
        var confirm = Confirm.Run() == (int)ResponseType.Accept;

        Confirm.Hide();

        return confirm;
    }

    protected void CleanShutdown()
    {
        // Clean-Up Routines Here
        Network.Free();

        ManagedOps.Free(InputData, OutputData, TestData, NormalizationData);
    }

    protected void Quit()
    {
        CleanShutdown();

        Application.Quit();
    }

    protected void OnWindowStateEvent(object sender, WindowStateEventArgs args)
    {
        var state = args.Event.NewWindowState;

        if (state == WindowState.Iconified)
        {
            Confirm.Hide();
        }

        args.RetVal = true;
    }

    void OnQuitButtonClicked(object sender, EventArgs args)
    {
        OnDeleteEvent(sender, new DeleteEventArgs());
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        if (GetConfirmation())
        {
            Quit();
        }

        a.RetVal = true;
    }

    bool OnIdle()
    {
        Processing.WaitOne();

        if (!Paused && NetworkSetuped)
        {
			var result = UseOptimizer.Active ? Network.StepOptimizer(InputData, Options) : Network.Step(InputData, Options);

			CurrentEpoch = Network.Iterations;

            if (result)
            {
				var Epoch = Convert.ToInt32(Epochs.Value);

				UpdateNetworkWeights();

				CurrentEpoch = Epoch;

                TrainingDone = true;

                Classify();

                Pause();
            }

            if (CurrentEpoch % 100 == 0)
            {
                UpdateClassifierInfo();
                UpdateProgressBar();
            }
        }

        Processing.ReleaseMutex();

        return true;
    }

    protected void OnOpenTrainingSetClicked(object sender, EventArgs e)
    {
        LoadTextFile(ref TrainingSetFileName, "Load Training Set", DataTrainingSet, TrainingSet, true, Categories);

        UpdateParameters(DataTrainingSet, Examples, InputLayerNodes, true);

        HiddenLayerNodes.Value = 2 * InputLayerNodes.Value;
    }

    protected void OnReloadTrainingSetClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(TrainingSetFileName))
            ReloadTextFile(TrainingSetFileName, DataTrainingSet, true, Categories);

        UpdateParameters(DataTrainingSet, Examples, InputLayerNodes, true);

        HiddenLayerNodes.Value = 2 * InputLayerNodes.Value;
    }

    protected void OnOpenTestSetClicked(object sender, EventArgs e)
    {
        LoadTextFile(ref TestSetFileName, "Load Test Set", DataTestSet, TestSet, false, null);

        UpdateParameters(DataTestSet, Samples, null, false);
    }

    protected void OnReloadTestSetClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(TestSetFileName))
            ReloadTextFile(TestSetFileName, DataTestSet);

        UpdateParameters(DataTestSet, Samples, null, false);
    }

	protected void OnMainNotebookSwitchPage(object sender, SwitchPageArgs args)
    {
        switch (args.PageNum)
        {
			case (int)Pages.DATA:

                ReparentTextView(LayoutPageData, WindowTestSet, 20, 290);
                ReparentLabel(LayoutPageData, LabelTestSet, 20, 230);

                break;

			case (int)Pages.TRAINING:

                ReparentTextView(LayoutPageTraining, WindowTestSet, 20, 260);
                ReparentLabel(LayoutPageTraining, LabelTestSet, 20, 230);

                break;

            default:

                ReparentTextView(LayoutPageData, WindowTestSet, 20, 290);
                ReparentLabel(LayoutPageData, LabelTestSet, 20, 230);

                break;
        }
    }

    protected void OnStartButtonClicked(object sender, EventArgs args)
    {
        if (!Paused)
            return;

        if (TrainingDone)
        {
            TrainingDone = false;

            NetworkSetuped = false;

            CurrentEpoch = 0;
        }

        if (!NetworkSetuped)
        {
			SetupNetworkTraining();

			Classification.Buffer.Clear();

			CurrentEpoch = Network.Iterations;
        }

        UpdateProgressBar();

        if (NetworkSetuped)
            Run();
    }

    protected void OnStopButtonClicked(object sender, EventArgs args)
    {
        if (Paused)
            return;

        UpdateProgressBar();
        UpdateClassifierInfo();

        Pause();
    }

    protected void OnResetButtonClicked(object sender, EventArgs args)
    {
        if (!Paused)
            return;

        CurrentEpoch = 0;

        UpdateProgressBar();

        Iterations.Text = "";
        ErrorCost.Text = "";

        NetworkSetuped = false;

        TrainingDone = false;

        TrainingProgress.Text = "";
    }

	protected void OnClassifyButtonClicked(object sender, EventArgs args)
	{
		if (!Paused)
			return;

		Classify();
	}

	protected void OnOpenWJIButtonClicked(object sender, EventArgs e)
	{
		if (!Paused)
			return;

		int x = 0;
		int y = 0;

		LoadNetworkData(ref WJIFileName, "Load Input Layer Weights", WJIView, WJIFile, ref x, ref y);

        if (x > 0)
			InputLayerNodes.Value = x - 1;

		if (y > 0)
			HiddenLayerNodes.Value = y;

        Console.WriteLine("Input Layer Weights: Loaded {0}x{1} values", x, y);
	}

	protected void OnOpenWKJButtonClicked(object sender, EventArgs e)
	{
		if (!Paused)
            return;

        int x = 0;
        int y = 0;

		LoadNetworkData(ref WKJFileName, "Load Hidden Layer Weights", WKJView, WKJFile, ref x, ref y);

		if (x > 0)
			HiddenLayerNodes.Value = x - 1;

        if (y > 0)
			Categories.Value = y;

        Console.WriteLine("Hidden Layer Weights: Loaded {0}x{1} values", x, y);
	}

	protected void OnOpenNormalizationClicked(object sender, EventArgs e)
	{
		if (!Paused)
            return;

        int x = 0;
        int y = 0;

		LoadNetworkData(ref NormalizationFileName, "Load Input Normalization Data", Normalization, NormalizationFile, ref x, ref y);

		if (x > 0)
			InputLayerNodes.Value = x;

		Console.WriteLine("Normalization: Loaded {0}x{1} values", x, y);
	}

	protected void OnSaveWJIButtonClicked(object sender, EventArgs e)
	{
		if (!Paused)
			return;

        if (NetworkSetuped)
		{
			SaveTextFile(ref WJIFileName, "Save Input Layer Weights", WJIFile, Network.Wji);
		}
	}

	protected void OnSaveWKJButtonClicked(object sender, EventArgs e)
	{
		if (!Paused)
            return;

        if (NetworkSetuped)
        {
			SaveTextFile(ref WKJFileName, "Save Hidden Layer Weights", WKJFile, Network.Wkj);
        }
	}

	protected void OnSaveNormalizationClicked(object sender, EventArgs e)
	{
		if (!Paused)
            return;

        if (NetworkSetuped)
        {
			SaveTextFile(ref NormalizationFileName, "Save Normalization Data", NormalizationFile, NormalizationData);
        }
	}

	protected void OnLoadNetworkButtonClicked(object sender, EventArgs e)
	{
		if (!Paused)
			return;

		SetupNetwork();
	}

	protected void OnAboutButtonClicked(object sender, EventArgs e)
	{
		MainNotebook.Page = (int)Pages.ABOUT;
	}
}
