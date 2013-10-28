using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RealEstate.SmartProcessing
{
    [Export(typeof(RulesManager))]
    public class RulesManager
    {
        const string FILENAME = "SmartProcessing//ProcessingRules.xml";

        public List<Rule> Rules { get; set; }

        public void Load()
        {
            ConditionFactory factory = new ConditionFactory();

            XDocument doc = XDocument.Load(FILENAME);
            foreach (var rule in doc.Root.Elements("rule"))
            {
                try
                {
                    ImportSite site = (ImportSite)Enum.Parse(typeof(ImportSite), rule.Attribute("ImportSite").Value);

                    var verb = Verb.Skip;
                    if (rule.Element("verb") != null)
                    {

                    }
                    else if (rule.Attribute("verb") != null)
                    {
                        verb = (Verb)Enum.Parse(typeof(Verb), rule.Attribute("verb").Value);
                    }
                    else
                    {
                        Trace.TraceError("Invalid rule. Missing verb: " + rule.ToString());
                        continue;
                    }

                    var r = new Rule();
                    r.Verb = verb;
                    r.Site = site;

                    foreach (var condition in rule.Element("conditions").Elements())
                    {
                        Condition cond = factory.GetCondition(condition.Name.LocalName);
                        cond.Parse(condition);
                        r.Conditions.Add(cond);
                    }

                    Rules.Add(r);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Invalid rule. {1}: {0}", rule.ToString(), ex.Message);
                }
            }
        }

        public RulesManager()
        {
            Rules = new List<Rule>();
        }
    }

    public class Rule
    {
        public Verb Verb { get; set; }
        public List<Condition> Conditions { get; set; }
        public ImportSite Site { get; set; }
        public Rule()
        {
            Conditions = new List<Condition>();
        }

        public override string ToString()
        {
            return String.Format("Site: {0}, Verb: {1}, Conditions [{2}]", Site.ToString(), Verb.ToString(), String.Join(", ",Conditions.Select(c => c.ToString()).ToArray()));
        }

    }

    public enum Verb
    {
        Skip
    }

    public class ConditionFactory
    {
        public Condition GetCondition(string name)
        {
            switch (name)
            {
                case "equals": return new Equals();
                case "contains": return new Contains();
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public abstract class Condition
    {
        protected string Property { get; set; }
        protected string Value { get; set; }
        public abstract bool IsSatisfy(Advert advert);
        public void Parse(XElement element)
        {
            if (element.Attribute("property") != null)
                Property = element.Attribute("property").Value;
            if (element.Attribute("value") != null)
                Value = element.Attribute("value").Value;
        }
    }

    public class Equals : Condition
    {
        public override bool IsSatisfy(Advert advert)
        {
            var prop = typeof(Advert).GetProperty(Property);
            if (prop != null)
            {
                var value = prop.GetValue(advert, null);
                return value.ToString() == Value;
            }

            return false;
        }

        public override string ToString()
        {
            return String.Format("{0} = {1}", Property, Value);
        }
    }

    public class Contains : Condition
    {
        public override bool IsSatisfy(Advert advert)
        {
            var prop = typeof(Advert).GetProperty(Property);
            if (prop != null)
            {
                var value = prop.GetValue(advert, null);
                if (value is string)
                {
                    return (value as string).ToLower().Contains(Value.ToLower());
                }
            }

            return false;
        }

        public override string ToString()
        {
            return String.Format("{0}.Contains('{1}')", Property, Value);
        }
    }
}
