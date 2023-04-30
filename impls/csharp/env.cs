namespace Mal;

class Env
{
    internal class EnvironmentException : Exception
    {
        public EnvironmentException(string message) : base(message) { }
    }

    private Dictionary<MalSymbol, IMalType> _data = new();
    private Env _outer;

    public Env(Env outer) => _outer = outer;

    public void Set(MalSymbol key, IMalType value) => _data[key] = value;

    private Env Find(MalSymbol key)
    {
        if (_data.ContainsKey(key))
        {
            return this;
        }
        else if (_outer != null)
        {
            return _outer.Find(key);
        }
        
        return null;
    }

    public IMalType Get(MalSymbol key)
    {
        var env = Find(key);
        if (env != null)
        {
            return env._data[key];
        }

        throw new EnvironmentException($"'{key.Name}' not found");
    }
}