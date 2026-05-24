using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.Stores.UserSettings
{
    public enum FactoryIconSource
    {
        Machine,

        [Description("First Output")]
        FirstOutput
    }

    public static class EnumExtensions 
    {
        public static string GetDescription(this Enum value) {
            FieldInfo? field = value.GetType().GetField(value.ToString());
            DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        public static Dictionary<string, T> GetValuesWithDescriptions<T>() where T: struct, Enum {
            return Enum.GetValues<T>().ToDictionary(value => value.GetDescription(), value => value);
        }
    }
}
