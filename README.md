# NeuralNetworkClassifier
The GTK-based no-programming-required simple neural network classifier software for Win/Unix/Linux/OSX platforms

**About Page**

NeuralNetworkClassifier uses a simple 3-layer artificial neural network architecture. It is not deep and may not qualify as Deep Learning but it can perform multi-category classification tasks on small to medium-sized data sets.

![About Page](/Screenshots/about.png)

**Data Page**

Training and Test sets from a csv/text file can be loaded provided you indicate the correct delimiter. Some network parameters are guessed but you can modify it. When loading training set data for classification, the last column in each line is assumed to be the classification category. 0 is not counted as a classification category but in scenarios involving binary classification (0 or 1) it is handled automatically. 

![Data Page](/Screenshots/data.png)

**Training Page**

This is where the actual artificial neural network training happens. Learning rate refers to the rate or speed at which the network 'learns' or reconfigure itself (by changing the interconnection strenghts between each layer). In each iteration of the training, the amount of difference between the network's current output and the expected output is measured, i.e. Error. Tolerance is related to the mimimum value of the Error to consider the network 'trained' enough to stop. Epochs refers to the maximum number of iterations to run the training.

You can freely start/stop/continue the training process. Training is performed during idle mode so you can freely move between pages. 

You do not need a test set to train the neural network. However, a test set was loaded, it will automatically classfiy them once training is completed. Classification threshold can be set (from 1-100). This sets the minimum score a test data point to be classified into a category. Default is 50, but for stricter classification, it should be set at a higher value, e.g. 90.

Once training is completed, you can enter new data points in the 'Test set' box and click on the 'Classify' button to classify them. If you change the number of hidden nodes, learning rate, or tolerance, you must retrain the network. 

![Training Page](/Screenshots/training.png)

**Network Page**

Finally, trained network parameters can be saved and loaded for use in future classification tasks or to provide a better starting point for training.

![Network Page](/Screenshots/network.png)
