using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Csv;
using System.Linq;

namespace DataExtractor
{
  public static class DataExtractor
  {
    private const string OutFile = @"path¥to¥File.csv";
    private const string GaiaPath = @"path¥to¥Gaia¥";
    private const string HipPath = @"path¥to¥Hip.csv";
    private const string CrossMatchPath = @"path¥to¥CrossMatch.csv";

    public static void Main()
    {
      using var outFStream = File.Open(OutFile, FileMode.OpenOrCreate);
      // Gaia ID : Hipparcos ID
      var crossMatch = new Dictionary<int, long>();
      var crossMatchText = File.ReadAllText(CrossMatchPath);
      foreach (ICsvLine line in CsvReader.ReadFromText(crossMatchText))
      {
        crossMatch[int.Parse(line[1])] = long.Parse(line[0]);
      }

      var gaiaFiles = GetGaiaFiles();
      foreach (var gaiaFile in gaiaFiles)
      {
        using var fStream = File.OpenRead(GaiaPath + gaiaFile);
        using var gStream = new GZipStream(fStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gStream);
        var isFirstLine = true;
        while (reader.EndOfStream == false)
        {
          // 1行目はタイトルなのでスキップする
          if (isFirstLine)
          {
            isFirstLine = false;
            continue;
          }

          var line = reader.ReadLine();
          if (line == null) continue;
          var strData = line.Split(',');
          outFStream.Write(
            StarData.FromGaiaProperty(
              double.Parse(strData[5]),
              double.Parse(strData[7]),
              float.Parse(strData[69]),
              float.Parse(strData[74]),
              float.Parse(strData[79])
            ).ToByte(), 0, 24);
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
          if (crossMatch.ContainsKey(id)) continue;
          // B-V?V-B?
          // キャスト
          outFStream.Write(
            new StarData(
              double.Parse(line[15..29]),
              double.Parse(line[29..43]),
              float.Parse(line[129..137]),
              -1 * float.Parse(line[152..159])
            ).ToByte(), 0, 24);
        }
      }
    }

    private static IEnumerable<string> GetGaiaFiles()
    {
      // 今回は、一緒にダウンロードした_MD5SUM.txtからファイル名を抜き出して使います
      var sr = new StreamReader(GaiaPath + "_MD5SUM.txt");
      var md5SumContent = sr.ReadToEnd();
      sr.Close();
      return md5SumContent.Split('\n').Select(s => s.Split(' ')[1]);
    }
  }
}