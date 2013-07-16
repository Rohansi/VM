using System;
using System.Xml.Linq;

namespace VM
{
    static class Util
    {
        public static string ElementValue(XElement root, string element, string defaultValue = "")
        {
            var elem = root.Element(element);
            return elem == null ? defaultValue : elem.Value;
        }

        public static T EnumParse<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }
}
