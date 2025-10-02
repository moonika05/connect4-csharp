namespace MenuSystem
{
    public class MenuItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public Func<string>? MethodToRun { get; set; }

        public MenuItem(string key, string value, Func<string>? methodToRun = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            MethodToRun = methodToRun;
        }

        public override string ToString()
        {
            return $"{Key}) {Value}";
        }
    }
}