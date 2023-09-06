using System;

namespace Datalus.Core
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FormattableAttribute : Attribute
    {
        public enum FormatCase
        {
            Any,
            Upper,
            Lower
        }

        public bool AutoTrim { get; set; } = true;
        public FormatCase Case { get; set; } = FormatCase.Any;
    }
}
