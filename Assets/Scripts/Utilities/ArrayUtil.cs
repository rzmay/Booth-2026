using System;

public static class ArrayUtil
{
  // Source - https://stackoverflow.com/a/53584079
  // Posted by user10736013
  // Retrieved 2026-02-28, License - CC BY-SA 4.0

  public static float[] Resample(float[] source, int n)
  {
    //n destination length
    int m = source.Length; //source length
    float[] destination = new float[n];
    destination[0] = source[0];
    destination[n - 1] = source[m - 1];

    for (int i = 1; i < n - 1; i++)
    {
      float jd = (float)i * (float)(m - 1) / (float)(n - 1);
      int j = (int)jd;
      destination[i] = source[j] + (source[j + 1] - source[j]) * (jd - (float)j);
    }
    return destination;
  }

}
