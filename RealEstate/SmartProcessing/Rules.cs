using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Linq;
using RealEstate.Utils;
using System.IO;
using System.Xml.Serialization;

namespace RealEstate.SmartProcessing
{
    [Export(typeof(RulesManager))]
    public class RulesManager
    {
        const string FILENAME = "SmartProcessing//ProcessingRules.xml";

        public ObservableCollection<Rule> Rules { get; set; }

        public RulesManager()
        {
            Rules = new ObservableCollection<Rule>();
        }

        public void Load()
        {
            if (File.Exists(FILENAME))
            {
                XmlSerializer reader = new XmlSerializer(typeof(List<Rule>));
                StreamReader file = new System.IO.StreamReader(FILENAME);
                var list = (List<Rule>)reader.Deserialize(file);
                foreach (var item in list)
                {
                    Rules.Add(item);
                }
            }
        }

        public void Save()
        {
            if (!File.Exists(FILENAME))
            {
                var str = File.CreateText(FILENAME);
                str.Close();
            }

            var writer = new XmlSerializer(typeof(List<Rule>));
            StreamWriter file = new System.IO.StreamWriter(FILENAME);
            writer.Serialize(file, Rules.ToList());
            file.Close();
        }

        public void Remove(Rule rule)
        {
            Rules.Remove(rule);
            Save();
        }

        public void AddBlackListedWord(string word)
        {
            var ruleFull = new Rule()
            {
                Site = ImportSite.All,
                Verb = Verb.Cut,
                Conditions = new List<Condition>()
                {
                    new Contains(){IgnoreCase = true, Property = "MessageFull", Value = word}
                },
                VerbValue = word,
                VerbValue2 = "MessageFull"

            };

            var ruleName = new Rule()
            {
                Site = ImportSite.All,
                Verb = Verb.Cut,
                Conditions = new List<Condition>()
                {
                    new Contains(){IgnoreCase = true, Property = "Name", Value = word}
                },
                VerbValue = word,
                VerbValue2 = "Name"
            };

            Rules.Add(ruleFull);
            Rules.Add(ruleName);

            Save();
        }
    }

    public class Rule
    {
        public Verb Verb { get; set; }
        public string VerbValue { get; set; }
        public string VerbValue2 { get; set; }
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
                return String.Format("Сайт: {0}, Дествие: {1}, Условия [{2}]", Site.GetDisplayName(), Verb.GetDisplayName(), String.Join(", ", Conditions.Select(c => c.ToString()).ToArray()));
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

    [XmlInclude(typeof(Equals))]
    [XmlInclude(typeof(Contains))]
    public abstract class Condition
    {
        public string Property { get; set; }
        public string Value { get; set; }
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
                if(value != null )
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
                if (value != null && value is string)
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
