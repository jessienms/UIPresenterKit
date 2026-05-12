using System;

namespace UILib
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class WindowAttribute : Attribute
    {
        public string Key { get; }
        public WindowAttribute(string _key) => Key = _key;
    }
}
