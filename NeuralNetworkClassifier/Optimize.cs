using System;

public class FuncOutput
{
	public double Error;
	public double[] Gradient;

	public FuncOutput(double error, double[] X)
	{
		Gradient = X;
		Error = error;
	}
}

public class Optimize
{
	// RHO and SIG are the constants in the Wolfe-Powell conditions
	double RHO = 0.01;
	double SIG = 0.5;

	// don't reevaluate within 0.1 of the limit of the current bracket
	double INT = 0.1;

	// extrapolate maximum 3 times the current bracket
	double EXT = 3.0;

	// max 20 function evaluations per line search
	int MAX = 20;

	// maximum allowed slope ratio
	double RATIO = 100.0;

	// reduction parameter
	int Red = 1;

	double[] s;
	double[] df1;

	public int MaxIterations;
	public int Iterations;
	public int Evaluations;

	int length;
	int M;
	int iteration;
	bool ls_failed;

	public double f1;

	double[] X0;
	double[] DF0;

	double d1;
	double z1;

	double Multiply(double[] a, double[] b)
	{
		if (a.Length == b.Length)
		{
			var dot = 0.0;

			for (var i = 0; i < a.Length; i++)
				dot += a[i] * b[i];

			return dot;
		}

		return 0.0;
	}

	public void Setup(Func<double[], FuncOutput> F, double[] X)
	{
		s = new double[X.Length];

		Evaluations = 0;
		Iterations = 0;

		length = MaxIterations;
		M = 0;
		iteration = 0; // zero the run length counter
		ls_failed = false; // no previous line search has failed

		// get function value and gradient
		var eval = F(X);
		f1 = eval.Error;
		df1 = eval.Gradient;

		Evaluations++;

		// count epochs?!
		if (length < 0)
			iteration++;

		// search direction is steepest
		for (int i = 0; i < df1.Length; i++)
			s[i] = -df1[i];

		// this is the slope
		d1 = 0.0;
		for (int j = 0; j < s.Length; j++)
			d1 += -s[j] * s[j];

		// initial step is red / (|s|+1)
		z1 = Red / (1 - d1);

		X0 = new double[X.Length];
		DF0 = new double[X.Length];
	}

	public bool Step(Func<double[], FuncOutput> F, double[] X)
	{
		// count iterations?!
		if (length > 0)
			iteration++;

		Iterations = iteration;

		// make a copy of current values
		for (int j = 0; j < X0.Length; j++)
			X0[j] = X[j];

		for (int j = 0; j < DF0.Length; j++)
			DF0[j] = df1[j];

		double F0 = f1;

		// begin line search
		for (int j = 0; j < X.Length; j++)
			X[j] += s[j] * z1;

		// evaluate cost - and gradient function with new params
		double f2 = F(X).Error;
		double[] df2 = F(X).Gradient;

		Evaluations++;

		// count epochs?!
		if (length < 0)
			iteration++;

		// initialize point 3 equal to point 1
		double d2 = 0;

		for (int i = 0; i < df2.Length; i++)
			d2 += df2[i] * s[i];

		double f3 = f1, d3 = d1, z3 = -z1;

		if (length > 0)
		{
			M = MAX;
		}
		else
		{
			M = Math.Min(MAX, -length - iteration);
		}

		// initialize quantities
		bool success = false;
		double limit = -1.0;

		FuncOutput eval;

		while (true)
		{
			while (((f2 > f1 + z1 * RHO * d1) || (d2 > -SIG * d1)) && (M > 0))
			{
				// tighten bracket
				limit = z1;

				double A = 0.0d;
				double B = 0.0d;
				double z2 = 0.0d;

				if (f2 > f1)
				{
					// quadratic fit 
					z2 = z3 - ((0.5 * d3 * z3 * z3) / (d3 * z3 + f2 - f3));
				}
				else
				{
					// cubic fit
					A = (6 * (f2 - f3)) / (z3 + (3 * (d2 + d3)));
					B = (3 * (f3 - f2) - (z3 * ((d3 + 2) * d2)));

					// numerical error possible - ok!
					z2 = Math.Sqrt(((B * B) - (A * d2 * z3)) - B) / A;
				}

				if (Double.IsNaN(z2) || Double.IsInfinity(z2) || Double.IsNegativeInfinity(z2))
				{
					// if we had a numerical problem then bisect
					z2 = z3 / 2.0;
				}

				// don't accept too close to limit
				z2 = Math.Max(Math.Min(z2, INT * z3), (1 - INT) * z3);

				// update the step
				z1 = z1 + z2;
				for (int j = 0; j < X.Length; j++)
					X[j] += s[j] * z2;

				eval = F(X);
				f2 = eval.Error;
				df2 = eval.Gradient;
				Evaluations++;

				M = M - 1;

				// count epochs?!
				if (length < 0)
					iteration++;

				d2 = 0.0;
				for (int i = 0; i < df2.Length; i++)
					d2 += df2[i] * s[i];

				// z3 is now relative to the location of z2
				z3 = z3 - z2;
			}

			if (f2 > (f1 + z1 * RHO * d1) || d2 > (-SIG * d1))
			{
				// this is a failure
				break;
			}
			else if (d2 > (SIG * d1))
			{
				// success
				success = true;

				break;
			}
			else if (M == 0)
			{
				// failure
				break;
			}

			// make cubic extrapolation
			var A1 = 6 * (f2 - f3) / z3 + 3 * (d2 + d3);
			var B1 = 3 * (f3 - f2) - z3 * (d3 + 2 * d2);

			// num.error possible - ok!
			var z21 = -d2 * z3 * z3 / (B1 + Math.Sqrt(B1 * B1 - A1 * d2 * z3 * z3));

			if (z21 < 0)
			{
				z21 = z21 * -1;
			}

			// num prob or wrong sign?
			if (double.IsNaN(z21) || double.IsInfinity(z21) || z21 < 0)
			{
				// if we have no upper limit
				if (limit < -0.5)
				{
					// then extrapolate the maximum amount
					z21 = z1 * (EXT - 1);
				}
				else
				{
					// otherwise bisect
					z21 = (limit - z1) / 2;
				}
			}
			else if (limit > -0.5 && (z21 + z1 > limit))
			{
				// extrapolation beyond limit?

				// set to extrapolation limit
				z21 = (limit - z1) / 2;
			}
			else if (limit < -0.5 && (z21 + z1 > z1 * EXT))
			{
				z21 = z1 * (EXT - 1.0);
			}
			else if (z21 < -z3 * INT)
			{
				// too close to limit?
				z21 = -z3 * INT;
			}
			else if ((limit > -0.5) && (z21 < (limit - z1) * (1.0 - INT)))
			{
				z21 = (limit - z1) * (1.0 - INT);
			}

			// set point 3 equal to point 2
			f3 = f2;
			d3 = d2;
			z3 = -z21;
			z1 = z1 + z21;

			// update current estimates
			for (int j = 0; j < X.Length; j++)
				X[j] += s[j] * z21;

			// evaluate functions
			eval = F(X);
			df2 = eval.Gradient;
			f2 = eval.Error;

			M = M - 1;

			// count epochs?!
			iteration = iteration + (length < 0 ? 1 : 0);

			d2 = 0;
			for (int i = 0; i < df2.Length; i++)
				d2 += df2[i] * s[i];

			// end of line search
		}

		// if line searched succeeded 
		if (success)
		{
			f1 = f2;

			// Polack-Ribiere direction
			var ptemp1 = Multiply(df2, df2) - Multiply(df1, df2);
			var ptemp2 = Multiply(df1, df1);
			var ptemp3 = ptemp1 / ptemp2;

			for (int j = 0; j < s.Length; j++)
				s[j] = s[j] * ptemp3 - df2[j];

			// swap derivatives
			var tmp = df1;
			df1 = df2;
			df2 = tmp;

			// get slope
			d2 = 0;
			for (int i = 0; i < df1.Length; i++)
				d2 += df1[i] * s[i];

			// new slope must be negative 
			if (d2 > 0)
			{
				// use steepest direction
				for (int i = 0; i < s.Length; i++)
					s[i] = -df1[i];

				d2 = 0;
				for (int i = 0; i < df1.Length; i++)
					d2 -= s[i] * s[i];
			}

			// slope ratio but max RATIO
			z1 = z1 * Math.Min(RATIO, (d1 / (d2 - 2.225074e-308)));
			d1 = d2;

			// this line search did not fail
			ls_failed = false;
		}
		else
		{
			// restore point from before failed line search
			f1 = F0;

			for (int j = 0; j < X.Length; j++)
				X[j] = X0[j];

			for (int j = 0; j < df1.Length; j++)
				df1[j] = DF0[j];

			// line search twice in a row
			if (ls_failed || iteration > Math.Abs(length))
			{
				// or we ran out of time, so we give up
				return true;
			}

			// swap derivatives
			var tmp = df1;
			df1 = df2;
			df2 = tmp;

			// try steepest
			for (int i = 0; i < df1.Length; i++)
				s[i] = -df1[i];

			d1 = 0;
			for (int i = 0; i < s.Length; i++)
				d1 -= s[i] * s[i];

			z1 = 1d / (1d - d1);

			// this line search failed
			ls_failed = true;
		}

		return !(iteration < Math.Abs(length));
	}
}
