using System;

namespace DataExtractor
{
  public class StarData
  {
    private readonly double _ra; // 赤経
    private readonly double _dec; // 赤緯
    private readonly float _vMag; // 等級
    private readonly float _bvColor; // 色指数

    public StarData(double ra, double dec, float vMag, float bvColor)
    {
      _ra = ra;
      _dec = dec;
      _vMag = vMag;
      _bvColor = bvColor;
    }

    public static StarData FromGaiaProperty(double ra, double dec, float phot_g_mean_mag, float phot_bp_mean_mag, float phot_rp_mean_mag)
    {
      var G_HP = 0.0149 * Math.Pow(phot_bp_mean_mag - phot_rp_mean_mag, 3)
                 - 0.12 * Math.Pow(phot_bp_mean_mag - phot_rp_mean_mag, 2)
                 - 0.2344 * (phot_bp_mean_mag - phot_rp_mean_mag)
                 - 0.01968;
      var bvColor = (float)(-0.5 <= G_HP
        ? -2.4 * G_HP - 0.075
        : -2.1 * Math.Pow(G_HP, 2) - 4.2 * G_HP - 0.45);
      return new StarData(ra, dec, phot_g_mean_mag, bvColor);
    }

    public byte[] ToByte()
    {
      var data = new byte[24];
      Buffer.BlockCopy(BitConverter.GetBytes(_ra), 0, data, 0, 8);
      Buffer.BlockCopy(BitConverter.GetBytes(_dec), 0, data, 8, 8);
      Buffer.BlockCopy(BitConverter.GetBytes(_vMag), 0, data, 16, 4);
      Buffer.BlockCopy(BitConverter.GetBytes(_bvColor), 0, data, 20, 4);
      return data;
    }
  }
}