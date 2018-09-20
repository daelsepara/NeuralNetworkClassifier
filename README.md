# Neural Network Classifier
The GTK-based no-programming-required simple neural network classifier software for Win/Unix/Linux/OSX platforms

**About Page**

![About Page](/Screenshots/about.png)

NeuralNetworkClassifier uses a simple 3-layer artificial neural network architecture. It is not deep and may not qualify as Deep Learning but it can perform multi-category classification tasks on small to medium-sized data sets.

**Data Page**

![Data Page](/Screenshots/data.png)

Training and Test sets from a csv/text file can be loaded provided you indicate the correct delimiter. Some network parameters are guessed but you can modify it. When loading training set data for classification, the last column in each line is assumed to be the classification category. 0 is not counted as a classification category but in scenarios involving binary classification (0 or 1) it is handled automatically. 

**Training Page**

![Training Page](/Screenshots/training.png)

This is where the actual artificial neural network training happens. Learning rate refers to the rate or speed at which the network 'learns' or reconfigures itself by changing the interconnection strenghts between the nodes in each layer. In each iteration of the training, the difference between the network's current output and the expected output is measured (Error/Cost function). Tolerance is related to the mimimum value of the Error to consider the network 'trained' enough to stop. Epochs refers to the maximum number of iterations to run the training.

You can freely start/stop/continue the training process. Training is performed during idle mode so you can freely move between pages. 

You do not need a test set to train the neural network. However, if a test set was loaded, it will automatically proceed to the classification step once training is completed. Classification threshold is the minimum a test data point needs to score in order to be classified into a category. The default classification threshold is 50 but it can be set to values 1-100. For stricter classification a threshold of 90 (or higher) is recommended.

Once training is completed, you can enter new data points in the 'Test set' box and click on the 'Classify' button to classify them. If you change the number of hidden nodes, learning rate, or tolerance, you must retrain the network.

You can use the included the powerful Fmincg optimizer (C.E. Rasmussen, 1999, 2000 & 2001) to speed up the training process and obtain better classification performance. This is available via a check box near the bottom right corner of the Training page.

**Network Page**

![Network Page](/Screenshots/network.png)

Finally, trained network parameters can be saved and loaded for use in future classification tasks or to provide a better starting point for training.
