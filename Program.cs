using HtmlAgilityPack;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;

namespace ConsoleKelebek
{
    class Program
    {
        static void Main(string[] args)
        {
            //Tüm resimlerin çekilmesi işlemi nasılsa
            string filePath = Directory.GetCurrentDirectory() + "\\" + "kelebekdeta.txt";
            var enumLines = File.ReadLines(filePath, Encoding.UTF8);
            int lineCount = enumLines.Count();

            string writeFile = Directory.GetCurrentDirectory() + "\\" + "kelebekfull.txt";
            StreamWriter writer = new StreamWriter(writeFile);
            StreamReader sr = new StreamReader(filePath, Encoding.UTF8);

            string line = "";
            int sayac = 1;
            int turKodu = -1;
            int turSira = 0;
            while ((line = sr.ReadLine()) != null)
            {
                try
                {
                    string[] temp1 = line.Split(";");
                    if (turKodu != Convert.ToInt32(temp1[0]))
                    {
                        turKodu = Convert.ToInt32(temp1[0]);
                        turSira = 1;
                    }

                    string turAdi = temp1[1];
                    string turAdet = temp1[2];
                    string url = temp1[3];
                    string img = temp1[4];

                    //Klasör var mı?
                    string klasorAdi = replaceDirectory(turAdi);
                    string directoryPath = Directory.GetCurrentDirectory() + "\\" + klasorAdi;
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    //Dosya var mı? Varsa istekte bulunma
                    img = img.Replace("../resim/kus/ozel/", "");
                    string picUrl = "https://www.trakel.org/resim/kus/orji/" + img;
                    System.Drawing.Image image = DownloadImageFromUrl(picUrl.Trim());
                    string fileName = System.IO.Path.Combine(directoryPath, img);
                    ClearCurrentConsoleLine();
                    Console.Write("\r{0}/{1} resim işleniyor", sayac, lineCount);
                    if (File.Exists(fileName))
                    {
                        sayac++;
                        turSira++;

                        ClearCurrentConsoleLine();
                        Console.Write("\r{0}/{1} resim mevcut", sayac, lineCount);
                        continue;
                    }

                    //Detay yükleniyor
                    HtmlDocument doc = getHTML(url);
                    var turNode = doc.DocumentNode.SelectNodes("//div[@class='kisimBaslikYesil']");
                    if (turNode == null)
                    {
                        string tempAbsolutePath = fileName.Replace(Directory.GetCurrentDirectory() + "\\", "");
                        image.Save(fileName);
                        writer.Write(turKodu + ";" + temp1[1] + ";" + turSira + ";" + turAdet + ";null;null;null;null;null;null;Hata;" + tempAbsolutePath + System.Environment.NewLine);
                        continue;
                    }


                    string turNodeDetail = turNode[0].InnerText;
                    turNodeDetail = turNodeDetail.Replace("&nbsp;", "");
                    turNodeDetail = turNodeDetail.Replace("&raquo; ", ";");

                    string[] temp2 = turNodeDetail.Split(";");
                    string turAdiLatin = (temp2[1].Trim().Length == 0 ? "null" : temp2[1].Trim());
                    string turAdiEn = (temp2[2].Trim().Length == 0 ? "null" : temp2[2].Trim());


                    //Resme ait diğer bilgiler çekiliyor
                    var fotoInfoNode = doc.DocumentNode.SelectNodes("//div[@class='fotoInfo']//i");
                    string fotoTur = (fotoInfoNode[2].InnerText.Trim().Length == 0 ? "null" : fotoInfoNode[2].InnerText.Trim());
                    string tarih = (fotoInfoNode[3].InnerText.Length == 0 ? "null" : fotoInfoNode[3].InnerText.Trim());
                    string cekimTarih = (fotoInfoNode[4].InnerText.Length == 0 ? "null" : fotoInfoNode[4].InnerText.Trim());
                    string cekimAlan = (fotoInfoNode[5].InnerText.Length == 0 ? "null" : fotoInfoNode[5].InnerText.Trim());

                    //Resim kaydediliyor
                    string fileAbsolutePath = fileName.Replace(Directory.GetCurrentDirectory() + "\\", "");
                    writer.Write(turKodu + ";" + temp1[1] + ";" + turSira + ";" + turAdet + ";" + turAdiLatin + ";" + turAdiEn + ";" + fotoTur + ";" + tarih + ";" + cekimTarih + ";" + cekimAlan + ";" + "Başarılı" + ";" + fileAbsolutePath + System.Environment.NewLine);
                    image.Save(fileName);
                }
                catch
                {

                }

                ClearCurrentConsoleLine();
                Console.Write("\r{0}/{1} resim tamamlandı", sayac, lineCount);
                sayac++;
                turSira++;
            }

            writer.Close();
            sr.Close();


        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        public static System.Drawing.Image DownloadImageFromUrl(string imageUrl)
        {
            System.Drawing.Image image = null;

            try
            {
                System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(imageUrl);
                webRequest.AllowWriteStreamBuffering = true;
                webRequest.Timeout = 30000;
                System.Net.WebResponse webResponse = webRequest.GetResponse();
                System.IO.Stream stream = webResponse.GetResponseStream();
                image = System.Drawing.Image.FromStream(stream);
                webResponse.Close();
            }
            catch (Exception ex)
            {
                return null;
            }

            return image;
        }

        public static string replaceDirectory(string str)
        {
            string[] oldChars = { "ç", "ğ", "ı", "ö", "ş", "ü", "Ç", "Ğ", "İ", "Ö", "Ş", "Ü", "-", "`" , "´" };
            string[] newChars = { "c", "g", "i", "o", "s", "u", "C", "G", "I", "O", "S", "U", "", "", "" };
            string temp = str.Trim();
            for(int i = 0; i < oldChars.Length; i ++ )
            {
                temp = temp.Replace(oldChars[i], newChars[i]);
            }

            return temp.Replace(" ", "");
        }

        public static HtmlDocument getHTML(string url)
        {
            var web = new HtmlWeb();
            web.PreRequest = delegate (HttpWebRequest webReq)
            {
                webReq.Timeout = 10000; // number of milliseconds
                return true;
            };

            var doc = web.Load(url);
            return doc;
        }
    }
}
