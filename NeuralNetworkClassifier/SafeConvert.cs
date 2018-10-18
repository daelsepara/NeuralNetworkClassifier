using System;
using System.Globalization;

public static class SafeConvert
{
    static readonly CultureInfo ci = new CultureInfo("en-us");

    public static int ToInt32(string src)
    {
        int result;

        try
        {
            result = Convert.ToInt32(src, ci);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to convert {0} to int: {1}", src, ex.Message);

            result = 0;
        }

        return result;
    }

    public static double ToDouble(string src)
    {
        double result;

        try
        {
            result = Convert.ToDouble(src, ci);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to convert {0} to double: {1}", src, ex.Message);

            result = 0;
        }

        return result;
    }
}
