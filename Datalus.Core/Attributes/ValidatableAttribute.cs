using System;

namespace Datalus.Core
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ValidatableAttribute : FormattableAttribute
    {
        public bool AllowBlank { get; set; } = true;
        public bool AllowNull { get; set; } = false;
    }
}
