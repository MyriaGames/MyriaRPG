namespace Myria.Wpf.Model
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class LocalizedKeyAttribute : Attribute
    {
        public string Key { get; }
        public LocalizedKeyAttribute(string key) => Key = key;
    }

}
