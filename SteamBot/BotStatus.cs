using System;
using System.IO;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Xml;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using SteamTrade;

namespace SteamBot
{
    public class BotStatus
    {
        //THIS THIS STILL DOESN'T FUCKING WORK...FUCK

        private string fileName = "BotStatus.xml";
        private HashSet<string> BotList = new HashSet<string>();

        private string Indexer(XElement element, string x, out string s)
        {
            XDocument doc = XDocument.Load(fileName);
            //StaticConversions.TrimOffStart(element.ToString(), x);
            if (element.Elements(x).Any())
            {
                foreach (var name in element.Elements(x))
                {
                    s = name.ToString();
                    return s;
                }
            }
            return s = String.Empty;
        }

        public TF2Value ReadRefs()
        {
            XDocument doc = XDocument.Load(fileName);
            XElement name1, name2, name3;
            ReturnNextXElement(1, out name1);
            ReturnNextXElement(2, out name2);
            ReturnNextXElement(3, out name3);
            var refelement1 = name1.Element("refs");
            var refelement2 = name2.Element("refs");
            var refelement3 = name3.Element("refs");
            string refs = "refs";
            string a, b, c = String.Empty;
            Indexer(refelement1, refs, out a);
            Indexer(refelement2, refs, out b);
            Indexer(refelement3, refs, out c);
            double x, y, z, d;
            x = Convert.ToDouble(a);
            y = Convert.ToDouble(b);
            z = Convert.ToDouble(c);
            d = (x+y+z);
            return TF2Value.FromRef(d);
        }

        private XElement ReturnNextXElement(int i, out XElement n)
        {
            string s = String.Empty;
            GetName(i, out s);
            n = new XElement(s);
            return n;
        }

        public int ReadTrades()
        {
            XDocument doc = XDocument.Load(fileName);
            XElement name1, name2, name3;
            ReturnNextXElement(1, out name1);
            ReturnNextXElement(2, out name2);
            ReturnNextXElement(3, out name3);
            var tradeelement1 = name1.Element("trades");
            var tradeelement2 = name2.Element("trades");
            var tradeelement3 = name3.Element("trades");
            string trades = "trades";
            string a, b, c = String.Empty;
            Indexer(tradeelement1, trades, out a);
            Indexer(tradeelement2, trades, out b);
            Indexer(tradeelement3, trades, out c);
            int x, y, z;
            Int32.TryParse(a, out x);
            Int32.TryParse(b, out y);
            Int32.TryParse(c, out z);
            return x + y + z;
        }

        public void AddData(Bot bot, double refs, int trades)
        {
            StoreName(bot);
            if (File.Exists(fileName))
            {
                XDocument doc = XDocument.Load(fileName);
                XElement service = new XElement(bot.DisplayName);
                service.ReplaceNodes("trades", trades.ToString());
                doc.Save(fileName);
            }
            else if (File.Exists(fileName))
            {
                XDocument doc = XDocument.Load(fileName);
                XElement service = new XElement(bot.DisplayName);
                service.ReplaceNodes("refs", refs.ToString());
                doc.Save(fileName);
            }
            else
            {
                CreateFile();
                if (File.Exists(fileName))
                {
                    XDocument doc = XDocument.Load(fileName);
                    XElement service = new XElement(bot.DisplayName);
                    service.Add(new XElement("trades", trades.ToString()));
                    doc.Save(fileName);
                }
                if (File.Exists(fileName))
                {
                    XDocument doc = XDocument.Load(fileName);
                    XElement service = new XElement(bot.DisplayName);
                    service.Add(new XElement("refs", refs.ToString()));
                    doc.Save(fileName);
                }
            }
        }

        private void StoreName(Bot bot)
        {
            if (!BotList.Contains(bot.DisplayName))
            {
                BotList.Add(bot.DisplayName);
            }
        }

        private string GetName(int i, out string s)
        {
            s = BotList.ElementAt(i);
            return s;
        }

        private void CreateFile()
        {
            XDocument doc = new XDocument();
            doc.Save(fileName);
        }
    }
}
