using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
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

        public ObservableCollection<Rule> Rules { get; set; }

        public void Load()
        {
            ConditionFactory factory = new ConditionFactory();

            XDocument doc = XDocument.Load(FILENAME);
            foreach (var rule in doc.Root.Elements("rule"))
            {
                try
                {
                    ImportSite site = (ImportSite)Enum.Parse(typeof(ImportSite), rule.Attribute("ImportSite").Value);

                    var verb = Verb.None;
                    string verbValue = null;

                    if (rule.Element("verb") != null)
                    {
                        verb = (Verb)Enum.Parse(typeof(Verb), rule.Element("verb").Attribute("name").Value);
                        verbValue = rule.Element("verb").Attribute("value").Value;
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
                    r.VerbValue = verbValue;

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
            Rules = new ObservableCollection<Rule>();
        }
    }

    public class Rule
    {
        public Verb Verb { get; set; }
        public string VerbValue { get; set; }
        public List<Condition> Conditions { get; set; }
        public ImportSite Site { get; set; }
        public Rule()
        {
            Conditions = new List<Condition>();
        }

        public override string ToString()
        {
            return String.Format("Site: {0}, Verb: {1}, Conditions [{2}]", Site.ToString(), Verb.ToString(), String.Join(", ", Conditions.Select(c => c.ToString()).ToArray()));
        }

        public string ToDisplayString
        {
            get
            {
                return String.Format("Сайт: {0}, Дествие: {1}, Условия [{2}]", Site.ToString(), Verb.ToString(), String.Join(", ", Conditions.Select(c => c.ToString()).ToArray()));
            }
        }

    }

    public enum Verb
    {
        [Display(Name="Нет действия")]
        None,
        [Display(Name = "Пропустить объявление")]
        Skip,
        [Display(Name = "Вырезать текст")]
        Cut,
        [Display(Name = "Удалить текст после")]
        RemoveAfter,
        [Display(Name = "Удалить весь текст")]
        RemoveAll
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
        public virtual void Parse(XElement element)
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
        public bool IgnoreCase = true;
        public override bool IsSatisfy(Advert advert)
        {
            var prop = typeof(Advert).GetProperty(Property);
            if (prop != null)
            {
                var value = prop.GetValue(advert, null);
                if (value is string)
                {
                    if (IgnoreCase)
                        return (value as string).ToLower().Contains(Value.ToLower());
                    else
                        return (value as string).Contains(Value);
                }
            }

            return false;
        }

        public override void Parse(XElement element)
        {
            base.Parse(element);
            if(element.Attribute("ignorecase") != null)
            {
                this.IgnoreCase = bool.Parse(element.Attribute("ignorecase").Value);
            }
        }

        public override string ToString()
        {
            return String.Format("{0}.Contains('{1}')", Property, Value);
        }
    }
}
