using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RealEstate.Parsing.Parsers
{
    public class HandsParser : ParserBase
    {

        protected override Advert ParseAdvertHtml(HtmlAgilityPack.HtmlNode advertNode)
        {
            throw new NotImplementedException();
        }

        protected override List<HtmlAgilityPack.HtmlNode> GetAdvertsNode(HtmlAgilityPack.HtmlNode pageNode)
        {
            throw new NotImplementedException();
        }

        public override List<Advert> ParsePage(string url)
        {
            throw new NotImplementedException();
        }
    }
}
