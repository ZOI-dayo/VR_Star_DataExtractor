using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace DataExtractor
{
  public static class DataExtractor
  {
    private const string OutFile = @"F:\out\Compiled";

    // http://cdn.gea.esac.esa.int/Gaia/gedr3/gaia_source/
    private const string GaiaPath = @"F:\Gaia EDR3\data\raw\cdn.gea.esac.esa.int\Gaia\gedr3\gaia_source\";

    // https://cdsarc.cds.unistra.fr/ftp/I/311/hip2.dat.gz
    private const string HipPath = @"F:\Hipparcos\hip2.dat.gz";

    // http://cdn.gea.esac.esa.int/Gaia/gedr3/cross_match/hipparcos2_best_neighbour/Hipparcos2BestNeighbour.csv.gz
    private const string CrossMatchPath = @"F:\Gaia EDR3\data\cross-match\Hipparcos2BestNeighbour.csv";

    public static void Main()
    {
      using var outFStream = File.Open(OutFile, FileMode.OpenOrCreate);
      outFStream.SetLength(0);
      // Gaia ID : Hipparcos ID
      var crossMatch = new Dictionary<long, int>();
      var crossMatchText = File.ReadAllText(CrossMatchPath);
      {
        var lineCount = 0;
        var fileLength = crossMatchText.Split('\n').Length;
        foreach (var line in crossMatchText.Split('\n').Select(s => s.Split(',')))
        {
          lineCount++;
          if (lineCount == 1) continue;
          if (lineCount == fileLength) break;
          crossMatch[long.Parse(line[0])] = int.Parse(line[1]);
        }
      }

      var loadedCrossMatch = new List<int>();

      var gaiaFiles = GetGaiaFiles();
      foreach (var gaiaFile in gaiaFiles)
      {
        if(gaiaFile.Length == 0) continue;
        using var fStream = File.OpenRead(GaiaPath + gaiaFile);
        using var gStream = new GZipStream(fStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gStream);
        var lineCount = 0;
        while (reader.EndOfStream == false)
        {
          lineCount++;
          // 1行目はタイトルなのでスキップする
          if (lineCount == 1) continue;

          if(lineCount % 10000 == 0) Console.WriteLine(lineCount);

          var line = reader.ReadLine();
          if (line == null) continue;
          var strData = line.Split(',');
          try
          {
            outFStream.Write(
              StarData.FromGaiaProperty(
                double.Parse(strData[5]),
                double.Parse(strData[7]),
                float.Parse(strData[69]),
                float.Parse(strData[74]),
                float.Parse(strData[79])
              ).ToByte(), 0, 24);
            if (crossMatch.ContainsKey(long.Parse(strData[2])))
              loadedCrossMatch.Add(crossMatch[long.Parse(strData[2])]);
          }
          catch (FormatException e)
          {
            // Console.WriteLine(e);
            // No Enough Data
          }
        }
      }

      // https://cdsarc.cds.unistra.fr/viz-bin/cat/I/311#/browse
      using (var fStream = File.OpenRead(HipPath))
      using (var gStream = new GZipStream(fStream, CompressionMode.Decompress))
      using (var reader = new StreamReader(gStream))
      {
        while (reader.EndOfStream == false)
        {
          var line = reader.ReadLine();
          if (line == null) continue;
          var id = int.Parse(line[..7]);
          if (loadedCrossMatch.Contains(id)) continue;

          try
          {
            // https://cdsarc.cds.unistra.fr/ftp/I/311/ReadMe
            outFStream.Write(
              new StarData(
                double.Parse(line[15..28]),
                double.Parse(line[29..42]),
                float.Parse(line[129..136]),
                float.Parse(line[152..158])
              ).ToByte(), 0, 24);
          }
          catch (FormatException e)
          {
            // Console.WriteLine(e);
            // No Enough Data
          }
        }
      }
    }

    private static IEnumerable<string> GetGaiaFiles()
    {
      // 今回は、一緒にダウンロードした_MD5SUM.txtからファイル名を抜き出して使います
      var sr = new StreamReader(GaiaPath + "_MD5SUM.txt");
      var md5SumContent = sr.ReadToEnd();
      sr.Close();
      return md5SumContent.Split('\n').Select(s =>
      {
        var keyPair = s.Split(' ');
        return keyPair.Length == 3 ? keyPair[2] : "";
      });
    }
  }
}